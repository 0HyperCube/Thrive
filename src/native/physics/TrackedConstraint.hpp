#pragma once

#include "Jolt/Core/Reference.h"

#include "core/Logger.hpp"

namespace JPH
{
class Constraint;
} // namespace JPH

namespace Thrive::Physics
{

/// \brief Tracks an existing constraint. This is needed as the physics engine doesn't track the existing constraints
/// itself
class TrackedConstraint : public RefCounted
{
    friend class PhysicalWorld;

public:
    /// \brief Constraint between a single body and the world
    TrackedConstraint(const JPH::Ref<JPH::Constraint>& constraint, const Ref<PhysicsBody>& body1);

    /// \brief Constraint between two bodies
    TrackedConstraint(
        const JPH::Ref<JPH::Constraint>& constraint, const Ref<PhysicsBody>& body1, const Ref<PhysicsBody>& body2);

    ~TrackedConstraint();

    [[nodiscard]] bool IsCreatedInWorld() const noexcept
    {
        return createdInWorld != nullptr;
    }

    [[nodiscard]] bool IsAttachedToBodies() const noexcept
    {
        return attachedToBodies;
    }

    [[nodiscard]] const inline JPH::Ref<JPH::Constraint>& GetConstraint() const noexcept
    {
        return constraintInstance;
    }

protected:
    inline void OnRegisteredToWorld(PhysicalWorld& world)
    {
        createdInWorld = &world;
    }

    inline void OnRemoveFromWorld(PhysicalWorld& world)
    {
        if (createdInWorld != &world)
        {
            LOG_ERROR("Constraint tried to be removed from world it is not in");
            return;
        }

        createdInWorld = nullptr;
    }

    // TODO: method to delete the constraint from the bodies

private:
    const Ref<PhysicsBody> firstBody;
    const Ref<PhysicsBody> optionalSecondBody;
    const JPH::Ref<JPH::Constraint> constraintInstance;

    bool attachedToBodies = true;

    PhysicalWorld* createdInWorld = nullptr;
};

} // namespace Thrive::Physics
