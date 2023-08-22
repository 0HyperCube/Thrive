// ------------------------------------ //
#include "PhysicalWorld.hpp"

#include <cstring>
#include <fstream>

#include "boost/circular_buffer.hpp"

// TODO: switch to a custom thread pool
#include "Jolt/Core/JobSystemThreadPool.h"
#include "Jolt/Core/StreamWrapper.h"
#include "Jolt/Physics/Body/BodyCreationSettings.h"
#include "Jolt/Physics/Collision/CastResult.h"
#include "Jolt/Physics/Collision/RayCast.h"
#include "Jolt/Physics/Constraints/SixDOFConstraint.h"
#include "Jolt/Physics/PhysicsScene.h"
#include "Jolt/Physics/PhysicsSettings.h"
#include "Jolt/Physics/PhysicsSystem.h"

// #include "core/TaskSystem.hpp"

#include "core/Time.hpp"

#include "BodyActivationListener.hpp"
#include "BodyControlState.hpp"
#include "ContactListener.hpp"
#include "PhysicsBody.hpp"
#include "StepListener.hpp"
#include "TrackedConstraint.hpp"

#ifdef JPH_DEBUG_RENDERER
#include "DebugDrawForwarder.hpp"
#endif

JPH_SUPPRESS_WARNINGS

// Enables slower turning in ApplyBodyControl when close to the target rotation
// #define USE_SLOW_TURN_NEAR_TARGET

// ------------------------------------ //
namespace Thrive::Physics
{

class PhysicalWorld::Pimpl
{
public:
    Pimpl() : durationBuffer(30)
    {
#ifdef JPH_DEBUG_RENDERER
        // Convex shapes
        // This is very expensive in terms of debug rendering data amount
        bodyDrawSettings.mDrawGetSupportFunction = false;

        // Wireframe is preferred when
        bodyDrawSettings.mDrawShapeWireframe = true;

        bodyDrawSettings.mDrawCenterOfMassTransform = true;

        // TODO: some of the extra settings should be enableable
#endif
    }

    float AddAndCalculateAverageTime(float duration)
    {
        durationBuffer.push_back(duration);

        const auto size = durationBuffer.size();

        if (size < 1)
        {
            LOG_ERROR("Duration circular buffer empty");
            return -1;
        }

        float durations = 0;

        for (const auto value : durationBuffer)
        {
            durations += value;
        }

        return durations / static_cast<float>(size);
    }

public:
    BroadPhaseLayerInterface broadPhaseLayer;
    ObjectToBroadPhaseLayerFilter objectToBroadPhaseLayer;
    ObjectLayerPairFilter objectToObjectPair;

    JPH::PhysicsSettings physicsSettings;

    boost::circular_buffer<float> durationBuffer;

    std::vector<Ref<PhysicsBody>> bodiesWithPerStepControl;

    JPH::Vec3 gravity = JPH::Vec3(0, -9.81f, 0);

#ifdef JPH_DEBUG_RENDERER
    JPH::BodyManager::DrawSettings bodyDrawSettings;

    JPH::Vec3Arg debugDrawCameraLocation = {};
#endif
};

PhysicalWorld::PhysicalWorld() : pimpl(std::make_unique<Pimpl>())
{
#ifdef USE_OBJECT_POOLS
    tempAllocator = std::make_unique<JPH::TempAllocatorImpl>(32 * 1024 * 1024);
#else
    tempAllocator = std::make_unique<JPH::TempAllocatorMalloc>();
#endif

    // Create job system
    // TODO: configurable threads (should be about 1-8), or well if we share thread with other systems then maybe up
    // to like any cores not used by the C# background tasks
    int physicsThreads = 2;
    jobSystem =
        std::make_unique<JPH::JobSystemThreadPool>(JPH::cMaxPhysicsJobs, JPH::cMaxPhysicsBarriers, physicsThreads);

    InitPhysicsWorld();
}

PhysicalWorld::~PhysicalWorld()
{
    if (bodyCount != 0)
    {
        LOG_ERROR(
            "PhysicalWorld destroyed while not all bodies were removed, existing bodies: " + std::to_string(bodyCount));
    }
}

// ------------------------------------ //
void PhysicalWorld::InitPhysicsWorld()
{
    physicsSystem = std::make_unique<JPH::PhysicsSystem>();
    physicsSystem->Init(maxBodies, maxBodyMutexes, maxBodyPairs, maxContactConstraints, pimpl->broadPhaseLayer,
        pimpl->objectToBroadPhaseLayer, pimpl->objectToObjectPair);
    physicsSystem->SetPhysicsSettings(pimpl->physicsSettings);

    physicsSystem->SetGravity(pimpl->gravity);

    // Contact listening
    contactListener = std::make_unique<ContactListener>();

    // contactListener->SetNextListener(something);
    physicsSystem->SetContactListener(contactListener.get());

    // Activation listening
    activationListener = std::make_unique<BodyActivationListener>();
    physicsSystem->SetBodyActivationListener(activationListener.get());

    stepListener = std::make_unique<StepListener>(*this);
    physicsSystem->AddStepListener(stepListener.get());
}

// ------------------------------------ //
bool PhysicalWorld::Process(float delta)
{
    // TODO: update thread count if changed (we won't need this when we have the custom job system done)

    elapsedSinceUpdate += delta;

    const auto singlePhysicsFrame = 1 / physicsFrameRate;

    bool simulatedPhysics = false;
    float simulatedTime = 0;

    // TODO: limit max steps per frame to avoid massive potential for lag spikes
    // TODO: alternatively to this it is possible to use a bigger timestep at once but then collision steps and
    // integration steps should be incremented
    while (elapsedSinceUpdate > singlePhysicsFrame)
    {
        elapsedSinceUpdate -= singlePhysicsFrame;
        simulatedTime += singlePhysicsFrame;
        StepPhysics(*jobSystem, singlePhysicsFrame);
        simulatedPhysics = true;
    }

    if (!simulatedPhysics)
        return false;

    // TODO: Trigger stuff from the collision detection (but maybe some stuff needs to trigger for each step?)

    DrawPhysics(simulatedTime);

    return true;
}

// ------------------------------------ //
Ref<PhysicsBody> PhysicalWorld::CreateMovingBody(const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position,
    JPH::Quat rotation /* = JPH::Quat::sIdentity()*/, bool addToWorld /*= true*/)
{
    if (shape == nullptr)
    {
        LOG_ERROR("No shape given to body create");
        return nullptr;
    }

    // TODO: multithreaded body adding?
    return OnBodyCreated(CreateBody(*shape, JPH::EMotionType::Dynamic, Layers::MOVING, position, rotation), addToWorld);
}

Ref<PhysicsBody> PhysicalWorld::CreateMovingBodyWithAxisLock(const JPH::RefConst<JPH::Shape>& shape,
    JPH::RVec3Arg position, JPH::Quat rotation, JPH::Vec3 lockedAxes, bool lockRotation, bool addToWorld /*= true*/)
{
    if (shape == nullptr)
    {
        LOG_ERROR("No shape given to body create");
        return nullptr;
    }

    JPH::EAllowedDOFs degreesOfFreedom = JPH::EAllowedDOFs::All;

    if (lockedAxes.GetX() != 0)
        degreesOfFreedom &= ~JPH::EAllowedDOFs::TranslationX;

    if (lockedAxes.GetY() != 0)
        degreesOfFreedom &= ~JPH::EAllowedDOFs::TranslationY;

    if (lockedAxes.GetZ() != 0)
        degreesOfFreedom &= ~JPH::EAllowedDOFs::TranslationZ;

    if (lockRotation)
    {
        if (lockedAxes.GetX() != 0)
        {
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationY;
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationZ;
        }

        if (lockedAxes.GetY() != 0)
        {
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationX;
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationZ;
        }

        if (lockedAxes.GetZ() != 0)
        {
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationX;
            degreesOfFreedom &= ~JPH::EAllowedDOFs::RotationY;
        }
    }

    // TODO: multithreaded body adding?
    return OnBodyCreated(
        CreateBody(*shape, JPH::EMotionType::Dynamic, Layers::MOVING, position, rotation, degreesOfFreedom),
        addToWorld);
}

Ref<PhysicsBody> PhysicalWorld::CreateStaticBody(const JPH::RefConst<JPH::Shape>& shape, JPH::RVec3Arg position,
    JPH::Quat rotation /* = JPH::Quat::sIdentity()*/, bool addToWorld /*= true*/)
{
    if (shape == nullptr)
    {
        LOG_ERROR("No shape given to static body create");
        return nullptr;
    }

    // TODO: multithreaded body adding?
    auto body = CreateBody(*shape, JPH::EMotionType::Static, Layers::NON_MOVING, position, rotation);

    if (body == nullptr)
        return nullptr;

    if (addToWorld)
    {
        physicsSystem->GetBodyInterface().AddBody(body->GetId(), JPH::EActivation::DontActivate);
        OnPostBodyAdded(*body);
    }

    return body;
}

void PhysicalWorld::AddBody(PhysicsBody& body, bool activate)
{
    if (body.IsInWorld())
    {
        LOG_ERROR("Physics body is already in some world, not adding it to this world");
        return;
    }

    // Create constraints if not done yet
    for (auto& constraint : body.GetConstraints())
    {
        if (!constraint->IsCreatedInWorld())
        {
            physicsSystem->AddConstraint(constraint->GetConstraint().GetPtr());
            constraint->OnRegisteredToWorld(*this);
        }
    }

    physicsSystem->GetBodyInterface().AddBody(
        body.GetId(), activate ? JPH::EActivation::Activate : JPH::EActivation::DontActivate);
    OnPostBodyAdded(body);
}

void PhysicalWorld::DestroyBody(const Ref<PhysicsBody>& body)
{
    if (body == nullptr)
        return;

    auto& bodyInterface = physicsSystem->GetBodyInterface();

    // Destroy constraints
    while (!body->GetConstraints().empty())
    {
        DestroyConstraint(*body->GetConstraints().back());
    }

    if (body->GetBodyControlState() != nullptr)
        DisableBodyControl(*body);

    bodyInterface.RemoveBody(body->GetId());
    body->MarkRemovedFromWorld();

    // Permanently destroy the body
    // TODO: we'll probably want to allow some way to re-add bodies at some point
    bodyInterface.DestroyBody(body->GetId());

    // Remove the extra body reference that we added for the physics system keeping a pointer to the body
    body->Release();
    --bodyCount;

    changesToBodies = true;
}

// ------------------------------------ //
void PhysicalWorld::SetDamping(JPH::BodyID bodyId, float damping, const float* angularDamping /*= nullptr*/)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded())
    {
        LOG_ERROR("Couldn't lock body for setting damping");
        return;
    }

    JPH::Body& body = lock.GetBody();
    auto* motionProperties = body.GetMotionProperties();

    motionProperties->SetLinearDamping(damping);

    if (angularDamping != nullptr)
        motionProperties->SetAngularDamping(*angularDamping);
}

// ------------------------------------ //
void PhysicalWorld::ReadBodyTransform(
    JPH::BodyID bodyId, JPH::RVec3& positionReceiver, JPH::Quat& rotationReceiver) const
{
    JPH::BodyLockRead lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (lock.Succeeded())
    {
        const JPH::Body& body = lock.GetBody();

        positionReceiver = body.GetPosition();
        rotationReceiver = body.GetRotation();
    }
    else
    {
        LOG_ERROR("Couldn't lock body for reading transform");
        std::memset(&positionReceiver, 0, sizeof(positionReceiver));
        std::memset(&rotationReceiver, 0, sizeof(rotationReceiver));
    }
}

void PhysicalWorld::GiveImpulse(JPH::BodyID bodyId, JPH::Vec3Arg impulse)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded())
    {
        LOG_ERROR("Couldn't lock body for giving impulse");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.AddImpulse(impulse);
}

void PhysicalWorld::SetVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded())
    {
        LOG_ERROR("Couldn't lock body for setting velocity");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.SetLinearVelocityClamped(velocity);
}

void PhysicalWorld::SetAngularVelocity(JPH::BodyID bodyId, JPH::Vec3Arg velocity)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded())
    {
        LOG_ERROR("Couldn't lock body for setting angular velocity");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.SetAngularVelocityClamped(velocity);
}

void PhysicalWorld::GiveAngularImpulse(JPH::BodyID bodyId, JPH::Vec3Arg impulse)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded())
    {
        LOG_ERROR("Couldn't lock body for giving angular impulse");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.AddAngularImpulse(impulse);
}

void PhysicalWorld::SetVelocityAndAngularVelocity(
    JPH::BodyID bodyId, JPH::Vec3Arg velocity, JPH::Vec3Arg angularVelocity)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), bodyId);
    if (!lock.Succeeded())
    {
        LOG_ERROR("Couldn't lock body for setting velocity and angular velocity");
        return;
    }

    JPH::Body& body = lock.GetBody();
    body.SetLinearVelocityClamped(velocity);
    body.SetAngularVelocityClamped(angularVelocity);
}

void PhysicalWorld::SetBodyControl(
    PhysicsBody& bodyWrapper, JPH::Vec3Arg movementImpulse, JPH::Quat targetRotation, float rotationRate)
{
    // Used to detect when the target has changed enough to warrant logic change in the control apply. This needs
    // to be relatively large to avoid oscillation
    constexpr auto newRotationTargetAfter = 0.01f;

    if (rotationRate <= 0)
    {
        LOG_ERROR("Invalid rotationRate variable for controlling a body, needs to be positive");
        return;
    }

    BodyControlState* state;

    bool justEnabled = bodyWrapper.EnableBodyControlIfNotAlready();

    state = bodyWrapper.GetBodyControlState();

    if (state == nullptr)
    {
        LOG_ERROR("Logic error in body control state creation (state should have been created)");
        return;
    }

    if (justEnabled)
    {
        // TODO: avoid duplicates if someone else will also add items to this list
        pimpl->bodiesWithPerStepControl.emplace_back(&bodyWrapper);

        state->previousTarget = targetRotation;
        state->targetRotation = targetRotation;
        state->targetChanged = true;
        state->justStarted = true;
    }
    else
    {
        state->targetRotation = targetRotation;

        if (!targetRotation.IsClose(state->previousTarget, newRotationTargetAfter))
        {
            state->targetChanged = true;
            state->previousTarget = state->targetRotation;
        }
    }

    state->movement = movementImpulse;
    state->rotationRate = rotationRate;
}

void PhysicalWorld::DisableBodyControl(PhysicsBody& bodyWrapper)
{
    if (bodyWrapper.DisableBodyControl())
    {
        auto& registeredIn = pimpl->bodiesWithPerStepControl;

        for (auto iter = registeredIn.begin(); iter != registeredIn.end(); ++iter)
        {
            if ((*iter).get() == &bodyWrapper)
            {
                // TODO: if items can be in this vector for multiple reasons this will need to check that
                registeredIn.erase(iter);
                return;
            }
        }

        LOG_ERROR("Didn't find body in internal vector of bodies needing operations for control disable");
    }
}

void PhysicalWorld::SetPosition(JPH::BodyID bodyId, JPH::DVec3Arg position, bool activate)
{
    physicsSystem->GetBodyInterface().SetPosition(
        bodyId, position, activate ? JPH::EActivation::Activate : JPH::EActivation::DontActivate);
}

bool PhysicalWorld::FixBodyYCoordinateToZero(JPH::BodyID bodyId)
{
    decltype(std::declval<JPH::Body>().GetPosition()) position;

    {
        // TODO: maybe there's a way to avoid the double lock here? (setting position takes a lock as well)
        JPH::BodyLockRead lock(physicsSystem->GetBodyLockInterface(), bodyId);
        if (!lock.Succeeded())
        {
            LOG_ERROR("Can't lock body for y-position fix");
            return false;
        }

        const JPH::Body& body = lock.GetBody();

        position = body.GetPosition();
    }

    if (std::abs(position.GetY()) > 0.001f)
    {
        SetPosition(bodyId, {position.GetX(), 0, position.GetZ()}, false);
        return true;
    }

    return false;
}

// ------------------------------------ //
const int32_t* PhysicalWorld::EnableCollisionRecording(
    PhysicsBody& body, CollisionRecordListType collisionRecordingTarget, int maxRecordedCollisions)
{
    body.SetCollisionRecordingTarget(collisionRecordingTarget, maxRecordedCollisions);

    if (body.MarkCollisionRecordingEnabled())
    {
        UpdateBodyUserPointer(body);
    }

    return body.GetRecordedCollisionTargetAddress();
}

void PhysicalWorld::DisableCollisionRecording(PhysicsBody& body)
{
    body.ClearCollisionRecordingTarget();

    if (body.MarkCollisionRecordingDisabled())
    {
        UpdateBodyUserPointer(body);
    }
}

void PhysicalWorld::AddCollisionIgnore(PhysicsBody& body, const PhysicsBody& ignoredBody, bool skipDuplicates)
{
    body.AddCollisionIgnore(ignoredBody, skipDuplicates);

    if (body.MarkCollisionFilterEnabled())
    {
        UpdateBodyUserPointer(body);
    }
}

bool PhysicalWorld::RemoveCollisionIgnore(PhysicsBody& body, const PhysicsBody& noLongerIgnoredBody)
{
    const auto changes = body.RemoveCollisionIgnore(noLongerIgnoredBody);

    if (body.MarkCollisionFilterEnabled())
    {
        UpdateBodyUserPointer(body);
    }

    return changes;
}

void PhysicalWorld::SetCollisionIgnores(PhysicsBody& body, PhysicsBody* const& ignoredBodies, int ignoreCount)
{
    body.SetCollisionIgnores(ignoredBodies, ignoreCount);

    if (body.MarkCollisionFilterEnabled())
    {
        UpdateBodyUserPointer(body);
    }
}

void PhysicalWorld::SetSingleCollisionIgnore(PhysicsBody& body, const PhysicsBody& onlyIgnoredBody)
{
    body.SetSingleCollisionIgnore(onlyIgnoredBody);

    if (body.MarkCollisionFilterEnabled())
    {
        UpdateBodyUserPointer(body);
    }
}

void PhysicalWorld::ClearCollisionIgnores(PhysicsBody& body)
{
    body.ClearCollisionIgnores();

    if (body.MarkCollisionFilterDisabled())
    {
        UpdateBodyUserPointer(body);
    }
}

void PhysicalWorld::SetCollisionDisabledState(PhysicsBody& body, bool disableAllCollisions)
{
    if (!body.SetDisableAllCollisions(disableAllCollisions))
    {
        // No changes
        return;
    }

    if (disableAllCollisions)
    {
        body.MarkCollisionDisableFlagEnabled();
    }
    else
    {
        body.MarkCollisionDisableFlagDisabled();
    }

    UpdateBodyUserPointer(body);
}

// ------------------------------------ //
Ref<TrackedConstraint> PhysicalWorld::CreateAxisLockConstraint(PhysicsBody& body, JPH::Vec3 axis, bool lockRotation)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), body.GetId());
    if (!lock.Succeeded())
    {
        LOG_ERROR("Locking body for adding a constraint failed");
        return nullptr;
    }

    JPH::SixDOFConstraintSettings constraintSettings;

    // This was in an example at https://github.com/jrouwe/JoltPhysics/issues/359 but would require some extra
    // space calculation so this is left out
    // constraintSettings.mSpace = JPH::EConstraintSpace::LocalToBodyCOM;

    if (axis.GetX() != 0)
        constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::TranslationX);

    if (axis.GetY() != 0)
        constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::TranslationY);

    if (axis.GetZ() != 0)
        constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::TranslationZ);

    if (lockRotation)
    {
        if (axis.GetX() != 0)
        {
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationY);
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationZ);
        }

        if (axis.GetY() != 0)
        {
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationX);
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationZ);
        }

        if (axis.GetZ() != 0)
        {
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationY);
            constraintSettings.MakeFixedAxis(JPH::SixDOFConstraintSettings::RotationZ);
        }
    }

    // Needed for precision on the axis lock to actually stay relatively close to the target value
    constraintSettings.mPosition1 = constraintSettings.mPosition2 = lock.GetBody().GetCenterOfMassPosition();

    auto constraintPtr = (JPH::SixDOFConstraint*)constraintSettings.Create(JPH::Body::sFixedToWorld, lock.GetBody());

#ifdef USE_OBJECT_POOLS
    auto trackedConstraint =
        ConstructFromGlobalPool<TrackedConstraint>(JPH::Ref<JPH::Constraint>(constraintPtr), Ref<PhysicsBody>(&body));
#else
    auto trackedConstraint = Ref<TrackedConstraint>(
        new TrackedConstraint(JPH::Ref<JPH::Constraint>(constraintPtr), Ref<PhysicsBody>(&body)));
#endif

    if (body.IsInWorld())
    {
        // Immediately register the constraint if the body is in the world currently

        // TODO: multithreaded adding?
        physicsSystem->AddConstraint(trackedConstraint->GetConstraint().GetPtr());
        trackedConstraint->OnRegisteredToWorld(*this);
    }

    return trackedConstraint;
}

void PhysicalWorld::DestroyConstraint(TrackedConstraint& constraint)
{
    // TODO: allow multithreading
    physicsSystem->RemoveConstraint(constraint.GetConstraint().GetPtr());
    constraint.OnDestroyByWorld(*this);
}

// ------------------------------------ //
std::optional<std::tuple<float, JPH::Vec3, JPH::BodyID>> PhysicalWorld::CastRay(JPH::RVec3 start, JPH::Vec3 endOffset)
{
    // The Jolt samples app has some really nice alternative cast modes that could be added in the future

    JPH::RRayCast ray{start, endOffset};

    // Cast ray
    JPH::RayCastResult hit;

    // TODO: could ignore certain groups
    bool hitSomething = physicsSystem->GetNarrowPhaseQuery().CastRay(ray, hit);

    if (!hitSomething)
        return {};

    const auto resultPosition = ray.GetPointOnRay(hit.mFraction);
    const auto resultFraction = hit.mFraction;
    const auto resultID = hit.mBodyID;

    // Could do something with the hit sub-shape
    // hit.mSubShapeID2

    // Or material
    // JPH::BodyLockRead lock(physicsSystem->GetBodyLockInterface(), hit.mBodyID);
    // if (lock.Succeeded())
    // {
    //     const JPH::Body& resultBody = lock.GetBody();
    //     const JPH::PhysicsMaterial* material = resultBody.GetShape()->GetMaterial(hit.mSubShapeID2);
    // }
    // else
    // {
    //     LOG_ERROR("Failed to get body read lock for ray cast");
    // }

    return std::tuple<float, JPH::Vec3, JPH::BodyID>(resultFraction, resultPosition, resultID);
}

// ------------------------------------ //
void PhysicalWorld::SetGravity(JPH::Vec3 newGravity)
{
    pimpl->gravity = newGravity;

    physicsSystem->SetGravity(pimpl->gravity);
}

void PhysicalWorld::RemoveGravity()
{
    SetGravity(JPH::Vec3(0, 0, 0));
}

// ------------------------------------ //
bool PhysicalWorld::DumpSystemState(std::string_view path)
{
    // Dump a Jolt snapshot to the path
    JPH::Ref<JPH::PhysicsScene> scene = new JPH::PhysicsScene();
    scene->FromPhysicsSystem(physicsSystem.get());

    std::ofstream stream(path.data(), std::ofstream::out | std::ofstream::trunc | std::ofstream::binary);
    JPH::StreamOutWrapper wrapper(stream);

    if (stream.is_open())
    {
        scene->SaveBinaryState(wrapper, true, true);
    }
    else
    {
        LOG_ERROR(std::string("Can't dump physics state to non-writable file at: ") + path.data());
        return false;
    }

    return true;
}

// ------------------------------------ //
void PhysicalWorld::StepPhysics(JPH::JobSystemThreadPool& jobs, float time)
{
    if (changesToBodies)
    {
        if (simulationsToNextOptimization <= 0)
        {
            // Queue an optimization
            simulationsToNextOptimization = simulationsBetweenBroadPhaseOptimization;
        }

        changesToBodies = false;
    }

    // Optimize broadphase (but at most quite rarely)
    if (simulationsToNextOptimization > 0)
    {
        if (--simulationsToNextOptimization <= 0)
        {
            simulationsToNextOptimization = 0;

            // Time to optimize
            physicsSystem->OptimizeBroadPhase();
        }
    }

    // TODO: physics processing time tracking with a high resolution timer (should get the average time over the last
    // second)
    const auto start = TimingClock::now();

    // Per physics step forces are applied in PerformPhysicsStepOperations triggered by the step listener

    const auto result = physicsSystem->Update(time, collisionStepsPerUpdate, tempAllocator.get(), &jobs);

    const auto elapsed = std::chrono::duration_cast<SecondDuration>(TimingClock::now() - start).count();

    switch (result)
    {
        case JPH::EPhysicsUpdateError::None:
            break;
        case JPH::EPhysicsUpdateError::ManifoldCacheFull:
            LOG_ERROR("Physics update error: manifold cache full");
            break;
        case JPH::EPhysicsUpdateError::BodyPairCacheFull:
            LOG_ERROR("Physics update error: body pair cache full");
            break;
        case JPH::EPhysicsUpdateError::ContactConstraintsFull:
            LOG_ERROR("Physics update error: contact constraints full");
            break;
        default:
            LOG_ERROR("Physics update error: unknown");
    }

    latestPhysicsTime = elapsed;

    averagePhysicsTime = pimpl->AddAndCalculateAverageTime(elapsed);
}

void PhysicalWorld::PerformPhysicsStepOperations(float delta)
{
    // Apply per-step physics body state
    // TODO: multithreading if there's a ton of bodies using this
    for (const auto& bodyPtr : pimpl->bodiesWithPerStepControl)
    {
        auto& body = *bodyPtr;

        if (body.GetBodyControlState() != nullptr)
            ApplyBodyControl(body, delta);
    }
}

Ref<PhysicsBody> PhysicalWorld::CreateBody(const JPH::Shape& shape, JPH::EMotionType motionType, JPH::ObjectLayer layer,
    JPH::RVec3Arg position, JPH::Quat rotation /*= JPH::Quat::sIdentity()*/,
    JPH::EAllowedDOFs allowedDegreesOfFreedom /*= JPH::EAllowedDOFs::All*/)
{
#ifndef NDEBUG
    // Sanity check some layer stuff
    if (motionType == JPH::EMotionType::Dynamic && layer == Layers::NON_MOVING)
    {
        LOG_ERROR("Incorrect motion type for layer specified");
        return nullptr;
    }
#endif

    auto creationSettings = JPH::BodyCreationSettings(&shape, position, rotation, motionType, layer);
    creationSettings.mAllowedDOFs = allowedDegreesOfFreedom;

    const auto body = physicsSystem->GetBodyInterface().CreateBody(creationSettings);

    if (body == nullptr)
    {
        LOG_ERROR("Ran out of physics bodies");
        return nullptr;
    }

    changesToBodies = true;

#ifdef USE_OBJECT_POOLS
    return ConstructFromGlobalPool<PhysicsBody>(body, body->GetID());
#else
    return {new PhysicsBody(body, body->GetID())};
#endif
}

Ref<PhysicsBody> PhysicalWorld::OnBodyCreated(Ref<PhysicsBody>&& body, bool addToWorld)
{
    if (body == nullptr)
        return nullptr;

    // Safety check for pointer data alignment
    if (reinterpret_cast<decltype(STUFFED_POINTER_DATA_MASK)>(body.get()) & STUFFED_POINTER_DATA_MASK)
    {
        LOG_ERROR("Allocated PhysicsBody doesn't follow alignment requirements! It uses low bits in the pointer.");
        std::abort();
    }

    if (addToWorld)
    {
        physicsSystem->GetBodyInterface().AddBody(body->GetId(), JPH::EActivation::Activate);
        OnPostBodyAdded(*body);
    }

    return std::move(body);
}

void PhysicalWorld::OnPostBodyAdded(PhysicsBody& body)
{
    body.MarkUsedInWorld();

    // Add an extra reference to the body to keep it from being deleted while in this world
    body.AddRef();
    ++bodyCount;
}

void PhysicalWorld::UpdateBodyUserPointer(const PhysicsBody& body)
{
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterface(), body.GetId());
    if (!lock.Succeeded())
    {
        LOG_ERROR("Can't lock body for updating user pointer bits, the enabled / disabled feature won't apply on it");
    }
    else
    {
        JPH::Body& joltBody = lock.GetBody();
        joltBody.SetUserData(body.CalculateUserPointer());
    }
}

// ------------------------------------ //
void PhysicalWorld::ApplyBodyControl(PhysicsBody& bodyWrapper, float delta)
{
    constexpr auto allowedRotationDifference = 0.0001f;
    constexpr auto overshootDetectWhenAllAnglesLessThan = PI * 0.025f;

#ifdef USE_SLOW_TURN_NEAR_TARGET
    constexpr auto closeToTargetThreshold = 0.20f;
#endif

    // Normalize delta to 60Hz update rate to make gameplay logic not depend on the physics framerate
    float normalizedDelta = delta / (1 / 60.0f);

    BodyControlState* controlState = bodyWrapper.GetBodyControlState();
    const auto bodyId = bodyWrapper.GetId();

    // This method is called by the step listener meaning that all bodies are already locked so this needs to be used
    // like this
    JPH::BodyLockWrite lock(physicsSystem->GetBodyLockInterfaceNoLock(), bodyId);
    if (!lock.Succeeded())
    {
        LOG_ERROR("Couldn't lock body for applying body control");
        return;
    }

    JPH::Body& body = lock.GetBody();
    const auto degreesOfFreedom = body.GetMotionProperties()->GetAllowedDOFs();

    body.AddImpulse(controlState->movement * normalizedDelta);

    const auto& currentRotation = body.GetRotation();

    const auto inversedTargetRotation = controlState->targetRotation.Inversed();
    const auto difference = currentRotation * inversedTargetRotation;

    if (difference.IsClose(JPH::Quat::sIdentity(), allowedRotationDifference))
    {
        // At rotation target, stop rotation
        // TODO: we could allow small velocities to allow external objects to force our rotation off a bit after which
        // this would correct itself
        body.SetAngularVelocity({0, 0, 0});
    }
    else
    {
        // Not currently at the rotation target
        auto differenceAngles = difference.GetEulerAngles();

        // Things break a lot if we add rotation on an axis where rotation is not allowed due to DOF
        if ((degreesOfFreedom & JPH::AllRotationAllowed) != JPH::AllRotationAllowed)
        {
            if (static_cast<int>((degreesOfFreedom & JPH::EAllowedDOFs::RotationX)) == 0)
            {
                differenceAngles.SetX(0);
            }

            if (static_cast<int>((degreesOfFreedom & JPH::EAllowedDOFs::RotationY)) == 0)
            {
                differenceAngles.SetY(0);
            }

            if (static_cast<int>((degreesOfFreedom & JPH::EAllowedDOFs::RotationZ)) == 0)
            {
                differenceAngles.SetZ(0);
            }
        }

        bool setNormalVelocity = true;

        if (!controlState->justStarted && !controlState->targetChanged)
        {
            // Check if we overshot the target and should stop to avoid oscillating

            // Compare the current rotation state with the previous one to detect if we are now on different side of
            // the target rotation than the previous rotation was
            const auto oldDifference = controlState->previousRotation * inversedTargetRotation;
            const auto oldAngles = oldDifference.GetEulerAngles();

            const auto angleDifference = oldAngles - differenceAngles;

            bool potentiallyOvershot = std::signbit(oldAngles.GetX()) != std::signbit(differenceAngles.GetX()) ||
                std::signbit(oldAngles.GetY()) != std::signbit(differenceAngles.GetY()) ||
                std::signbit(oldAngles.GetZ()) != std::signbit(differenceAngles.GetZ());

            // If the signs are different and the angles are close enough (to make sure if we overshoot a ton we
            // correct) then detect an overshoot
            if (potentiallyOvershot && std::abs(angleDifference.GetX()) < overshootDetectWhenAllAnglesLessThan &&
                std::abs(angleDifference.GetY()) < overshootDetectWhenAllAnglesLessThan &&
                std::abs(angleDifference.GetZ()) < overshootDetectWhenAllAnglesLessThan)
            {
                // Overshot and we are within angle limits, reset velocity to 0 to prevent oscillation
                body.SetAngularVelocity({0, 0, 0});
                setNormalVelocity = false;
            }
        }

        if (setNormalVelocity)
        {
#ifdef USE_SLOW_TURN_NEAR_TARGET
            // When near the target slow down rotation
            const bool nearTarget = difference.IsClose(JPH::Quat::sIdentity(), closeToTargetThreshold);

            // It seems as these angles are the distance left, these are hopefully fine to be as-is without any kind
            // of delta adjustment
            if (nearTarget)
            {
                body.SetAngularVelocityClamped(differenceAngles / controlState->rotationRate * 0.5f);
            }
            else
            {
                body.SetAngularVelocityClamped(differenceAngles / controlState->rotationRate);
            }
#else
            body.SetAngularVelocityClamped(differenceAngles / controlState->rotationRate);
#endif // USE_SLOW_TURN_NEAR_TARGET
        }
    }

    controlState->previousRotation = currentRotation;
    controlState->justStarted = false;
    controlState->targetChanged = false;
}

#pragma clang diagnostic push
#pragma ide diagnostic ignored "readability-make-member-function-const"
#pragma ide diagnostic ignored "readability-convert-member-functions-to-static"

void PhysicalWorld::DrawPhysics(float delta)
{
    if (debugDrawLevel < 1)
    {
#ifdef JPH_DEBUG_RENDERER
        contactListener->SetDebugDraw(nullptr);
#endif

        return;
    }

#ifdef JPH_DEBUG_RENDERER
    auto& drawer = DebugDrawForwarder::GetInstance();

    if (!drawer.HasAReceiver())
        return;

    drawer.SetCameraPositionForLOD(pimpl->debugDrawCameraLocation);

    if (!drawer.TimeToRenderDebug(delta))
    {
        // Rate limiting the drawing
        // New contacts will be drawn on the next non-rate limited frame
        contactListener->SetDrawOnlyNewContacts(true);
        return;
    }

    if (debugDrawLevel > 2)
    {
        contactListener->SetDebugDraw(&drawer);
        contactListener->SetDrawOnlyNewContacts(false);
    }
    else
    {
        contactListener->SetDebugDraw(nullptr);
    }

    pimpl->bodyDrawSettings.mDrawBoundingBox = debugDrawLevel > 1;
    pimpl->bodyDrawSettings.mDrawVelocity = debugDrawLevel > 1;

    physicsSystem->DrawBodies(pimpl->bodyDrawSettings, &drawer);

    if (debugDrawLevel > 3)
    {
        contactListener->DrawActiveContacts(drawer);
    }

    if (debugDrawLevel > 4)
        physicsSystem->DrawConstraints(&drawer);

    if (debugDrawLevel > 5)
        physicsSystem->DrawConstraintLimits(&drawer);

    if (debugDrawLevel > 6)
        physicsSystem->DrawConstraintReferenceFrame(&drawer);

    drawer.FlushOutput();
#endif
}

void PhysicalWorld::SetDebugCameraLocation(JPH::Vec3Arg position) noexcept
{
#ifdef JPH_DEBUG_RENDERER
    pimpl->debugDrawCameraLocation = position;
#else
    UNUSED(position);
#endif
}

#pragma clang diagnostic pop

} // namespace Thrive::Physics
