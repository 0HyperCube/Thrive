#pragma once

#include <cstring>
#include <memory>

#include "Jolt/Core/Reference.h"
#include "Jolt/Physics/Body/BodyID.h"

#include "core/ForwardDefinitions.hpp"

namespace JPH
{
class BodyID;
class Body;
class Shape;
} // namespace JPH

namespace Thrive::Physics
{

class PhysicalWorld;
class BodyControlState;

/// \brief Our physics body wrapper that has extra data
class PhysicsBody : public RefCounted<PhysicsBody>
{
    friend PhysicalWorld;
    friend BodyActivationListener;
    friend TrackedConstraint;

protected:
#ifndef USE_OBJECT_POOLS
    PhysicsBody(JPH::Body* body, JPH::BodyID bodyId) noexcept;
#endif

public:
#ifdef USE_OBJECT_POOLS
    /// Even though this is public this should only be called by PhysicalWorld, so any other code sohuld ask the world
    /// to make new bodies
    PhysicsBody(JPH::Body* body, JPH::BodyID bodyId, ReleaseCallback deleteCallback) noexcept;
#endif

    ~PhysicsBody() noexcept override;

    PhysicsBody(const PhysicsBody& other) = delete;
    PhysicsBody(PhysicsBody&& other) = delete;

    PhysicsBody& operator=(const PhysicsBody& other) = delete;
    PhysicsBody& operator=(PhysicsBody&& other) = delete;

    /// \brief Retrieves an instance of this class from a physics body user data
    [[nodiscard]] static PhysicsBody* FromJoltBody(const JPH::Body* body) noexcept;

    [[nodiscard]] static PhysicsBody* FromJoltBody(uint64_t bodyUserData) noexcept;

    [[nodiscard]] inline bool IsActive() const noexcept
    {
        return active;
    }

    [[nodiscard]] inline bool IsInWorld() const noexcept
    {
        return inWorld;
    }

    [[nodiscard]] inline JPH::BodyID GetId() const
    {
        return id;
    }

    [[nodiscard]] const inline auto& GetConstraints() const noexcept
    {
        return constraintsThisIsPartOf;
    }

    [[nodiscard]] inline BodyControlState* GetBodyControlState() const noexcept
    {
        return bodyControlStateIfActive.get();
    }

    [[nodiscard]] inline bool HasUserData() const noexcept
    {
        return userDataLength > 0;
    }

    inline bool SetUserData(const char* data, int length) noexcept
    {
        static_assert(PHYSICS_USER_DATA_SIZE < std::numeric_limits<int>::max());

        // Fail if too much data given
        if (length > static_cast<int>(userData.size()))
        {
            userDataLength = 0;
            return false;
        }

        // Data clearing
        if (data == nullptr)
        {
            userDataLength = 0;
            return true;
        }

        // New data is set
        std::memcpy(userData.data(), data, length);
        userDataLength = length;
        return true;
    }

protected:
    bool EnableBodyControlIfNotAlready() noexcept;
    bool DisableBodyControl() noexcept;

    void MarkUsedInWorld() noexcept;
    void MarkRemovedFromWorld() noexcept;

    void NotifyConstraintAdded(TrackedConstraint& constraint) noexcept;
    void NotifyConstraintRemoved(TrackedConstraint& constraint) noexcept;

    inline void NotifyActiveStatus(bool newActiveValue) noexcept
    {
        active = newActiveValue;
    }

private:
    std::array<char, PHYSICS_USER_DATA_SIZE> userData;

    const JPH::BodyID id;

    std::vector<Ref<TrackedConstraint>> constraintsThisIsPartOf;

    std::unique_ptr<BodyControlState> bodyControlStateIfActive;

    int userDataLength = 0;

    /// A pointer to this is passed out for users of the collision recording array
    int activeRecordedCollisionCount = 0;

    bool inWorld = false;
    bool active = true;
};

} // namespace Thrive::Physics
