// ------------------------------------ //
#include "PhysicsBody.hpp"

#include "Jolt/Physics/Body/Body.h"

#include "core/Logger.hpp"

#include "TrackedConstraint.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

PhysicsBody::PhysicsBody(JPH::Body* body, JPH::BodyID bodyId) noexcept : id(bodyId)
{
    body->SetUserData(reinterpret_cast<uint64_t>(this));
}

PhysicsBody::~PhysicsBody() noexcept
{
    if (inWorld)
        LOG_ERROR("PhysicsBody deleted while it is still in the world, this is going to cause memory corruption!");
}

// ------------------------------------ //
PhysicsBody* PhysicsBody::FromJoltBody(const JPH::Body* body) noexcept
{
    const auto rawValue = body->GetUserData();

    if (rawValue == 0)
        return nullptr;

    return reinterpret_cast<PhysicsBody*>(rawValue);
}

PhysicsBody* PhysicsBody::FromJoltBody(uint64_t bodyUserData) noexcept
{
    if (bodyUserData == 0)
        return nullptr;

    return reinterpret_cast<PhysicsBody*>(bodyUserData);
}

// ------------------------------------ //
void PhysicsBody::MarkUsedInWorld() noexcept
{
    if (inWorld)
        LOG_ERROR("PhysicsBody marked used when already in use");

    inWorld = true;
}

void PhysicsBody::MarkRemovedFromWorld() noexcept
{
    if (!inWorld)
        LOG_ERROR("PhysicsBody marked removed from world when it wasn't used in the first place");

    inWorld = false;
}

void PhysicsBody::NotifyConstraintAdded(TrackedConstraint& constraint) noexcept
{
    constraintsThisIsPartOf.emplace_back(&constraint);

    // To save on performance this doesn't check on duplicate constraint adds
}

void PhysicsBody::NotifyConstraintRemoved(TrackedConstraint& constraint) noexcept
{
    for (auto iter = constraintsThisIsPartOf.begin(); iter != constraintsThisIsPartOf.end(); ++iter)
    {
        if (iter->get() == &constraint)
        {
            constraintsThisIsPartOf.erase(iter);
            return;
        }
    }

    LOG_ERROR("PhysicsBody notified of removed constraint that this wasn't a part of");
}

} // namespace Thrive::Physics
