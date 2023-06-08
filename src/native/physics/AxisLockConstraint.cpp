// ------------------------------------ //
#include "AxisLockConstraint.hpp"

#include "Jolt/Core/StreamIn.h"
#include "Jolt/Core/StreamOut.h"
#include "Jolt/ObjectStream/TypeDeclarations.h"

#ifdef JPH_DEBUG_RENDERER
#include "Jolt/Renderer/DebugRenderer.h"
#endif // JPH_DEBUG_RENDERER

// ------------------------------------ //
namespace Thrive::Physics
{
using namespace JPH;

JPH_SUPPRESS_WARNING_PUSH
JPH_SUPPRESS_WARNINGS

JPH_IMPLEMENT_SERIALIZABLE_VIRTUAL(AxisLockConstraintSettings)
{
    JPH_ADD_BASE_CLASS(AxisLockConstraintSettings, ConstraintSettings)

    JPH_ADD_ATTRIBUTE(AxisLockConstraintSettings, lockAxis)
    JPH_ADD_ATTRIBUTE(AxisLockConstraintSettings, lockRotation)
}

JPH_SUPPRESS_WARNING_POP
} // namespace Thrive::Physics

namespace Thrive::Physics
{

void AxisLockConstraintSettings::SaveBinaryState(JPH::StreamOut& inStream) const
{
    ConstraintSettings::SaveBinaryState(inStream);

    inStream.Write(lockAxis);
    inStream.Write(lockRotation);
}

AxisLockConstraint* AxisLockConstraintSettings::Create(JPH::Body& body) const
{
    return new AxisLockConstraint(body, lockAxis, lockRotation);
}

void AxisLockConstraintSettings::RestoreBinaryState(JPH::StreamIn& inStream)
{
    ConstraintSettings::RestoreBinaryState(inStream);

    inStream.Read(lockAxis);
    inStream.Read(lockRotation);
}

// ------------------------------------ //
// AxisLockConstraint
AxisLockConstraintSettings CreateSimpleSettings(JPH::Vec3 lockAxis, bool lockRotation)
{
    AxisLockConstraintSettings settings{};

    settings.lockAxis = lockAxis;
    settings.lockRotation = lockRotation;

    return settings;
}

AxisLockConstraint::AxisLockConstraint(JPH::Body& body, const AxisLockConstraintSettings& settings) :
    JPH::Constraint(settings), bodyId(body.GetID()), body(&body), lockAxis(settings.lockAxis), lockRotation(settings.lockRotation)
{
}

AxisLockConstraint::AxisLockConstraint(JPH::Body& body, JPH::Vec3 lockAxis, bool lockRotation) :
    AxisLockConstraint(body, CreateSimpleSettings(lockAxis, lockRotation))
{
}

// ------------------------------------ //
void AxisLockConstraint::NotifyShapeChanged(const BodyID& inBodyID, JPH::Vec3Arg inDeltaCOM)
{
    UNUSED(inDeltaCOM);

    if (inBodyID == bodyId){
        // We don't actually have anything to do here
    }
}

void AxisLockConstraint::SetupVelocityConstraint(float inDeltaTime)
{
    Mat44 rotation1 = Mat44::sRotation(body->GetRotation());
    axisConstraintPart.CalculateConstraintProperties(*body, rotation1, *mBody2, rotation2)
}

void AxisLockConstraint::WarmStartVelocityConstraint(float inWarmStartImpulseRatio)
{
}

bool AxisLockConstraint::SolveVelocityConstraint(float inDeltaTime)
{
    return false;
}

bool AxisLockConstraint::SolvePositionConstraint(float inDeltaTime, float inBaumgarte)
{
    return false;
}

// ------------------------------------ //
void AxisLockConstraint::BuildIslands(uint32 inConstraintIndex, IslandBuilder& ioBuilder, BodyManager& inBodyManager)
{
}

uint AxisLockConstraint::BuildIslandSplits(LargeIslandSplitter& ioSplitter) const
{
    return 0;
}

// ------------------------------------ //
#ifdef JPH_DEBUG_RENDERER
void AxisLockConstraint::DrawConstraint(DebugRenderer* inRenderer) const
{
}
#endif // JPH_DEBUG_RENDERER

// ------------------------------------ //
void AxisLockConstraint::SaveState(StateRecorder& inStream) const
{
    Constraint::SaveState(inStream);
}

void AxisLockConstraint::RestoreState(StateRecorder& inStream)
{
    Constraint::RestoreState(inStream);
}

// ------------------------------------ //
JPH::Ref<ConstraintSettings> AxisLockConstraint::GetConstraintSettings() const
{
    auto settings = new AxisLockConstraintSettings();
    auto result = JPH::Ref<ConstraintSettings>(settings);

    settings->lockAxis = lockAxis;
    settings->lockRotation = lockRotation;

    return result;
}

} // namespace Thrive::Physics
