#pragma once

#include <cstring>
#include <memory>

#include "Jolt/Core/Reference.h"
#include "Jolt/Physics/Body/Body.h"
#include "Jolt/Physics/Body/BodyID.h"

#include "Include.h"
#include "core/ForwardDefinitions.hpp"

#include "PhysicsCollision.hpp"

#ifdef USE_SMALL_VECTOR_POOLS
#include "boost/pool/pool_alloc.hpp"
#endif

namespace JPH
{
class Shape;
} // namespace JPH

namespace Thrive::Physics
{

class PhysicalWorld;
class BodyControlState;

// Flags to put in the physics user data field as a stuffed pointer, max count is UNUSED_POINTER_BITS
constexpr uint64_t PHYSICS_BODY_COLLISION_FLAG = 0x1;
constexpr uint64_t PHYSICS_BODY_RECORDING_FLAG = 0x2;
constexpr uint64_t PHYSICS_BODY_DISABLE_COLLISION_FLAG = 0x4;

#ifdef USE_SMALL_VECTOR_POOLS
using IgnoredCollisionList = std::vector<JPH::BodyID, boost::pool_allocator<JPH::BodyID>>;
#else
using IgnoredCollisionList = std::vector<JPH::BodyID>;
#endif

/// \brief Our physics body wrapper that has extra data
class alignas(STUFFED_POINTER_ALIGNMENT) PhysicsBody : public RefCounted<PhysicsBody>
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
    /// Even though this is public this should only be called by PhysicalWorld, so any other code should ask the world
    /// to make new bodies
    PhysicsBody(JPH::Body* body, JPH::BodyID bodyId, ReleaseCallback deleteCallback) noexcept;
#endif

    ~PhysicsBody() noexcept override;

    PhysicsBody(const PhysicsBody& other) = delete;
    PhysicsBody(PhysicsBody&& other) = delete;

    PhysicsBody& operator=(const PhysicsBody& other) = delete;
    PhysicsBody& operator=(PhysicsBody&& other) = delete;

    /// \brief Retrieves an instance of this class from a physics body user data
    [[nodiscard]] FORCE_INLINE static PhysicsBody* FromJoltBody(const JPH::Body* body) noexcept
    {
        return FromJoltBody(body->GetUserData());
    }

    [[nodiscard]] FORCE_INLINE static PhysicsBody* FromJoltBody(uint64_t bodyUserData) noexcept
    {
        bodyUserData &= STUFFED_POINTER_POINTER_MASK;

#ifdef NULL_HAS_UNUSUAL_REPRESENTATION
        if (bodyUserData == 0)
            return nullptr;
#endif

        return reinterpret_cast<PhysicsBody*>(bodyUserData);
    }

    // ------------------------------------ //
    // Recording
    void SetCollisionRecordingTarget(CollisionRecordListType target, int maxCount) noexcept;
    void ClearCollisionRecordingTarget() noexcept;

    const inline int32_t* GetRecordedCollisionTargetAddress() const noexcept
    {
        return &(this->activeRecordedCollisionCount);
    }

    // ------------------------------------ //
    // Collision ignores

    bool AddCollisionIgnore(const PhysicsBody& ignoredBody, bool skipDuplicates) noexcept;
    bool RemoveCollisionIgnore(const PhysicsBody& noLongerIgnored) noexcept;

    void SetCollisionIgnores(PhysicsBody* const& ignoredBodies, int ignoreCount) noexcept;
    void SetSingleCollisionIgnore(const PhysicsBody& ignoredBody) noexcept;

    void ClearCollisionIgnores() noexcept;

    inline bool IsBodyIgnored(JPH::BodyID bodyId) const noexcept
    {
        for (const auto& ignored : ignoredCollisions)
        {
            if (ignored == bodyId)
                return true;
        }

        return false;
    }

    // ------------------------------------ //
    // State flags

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

    // ------------------------------------ //
    // User pointer flags

    inline bool MarkCollisionFilterEnabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags |= PHYSICS_BODY_COLLISION_FLAG;

        return old != activeUserPointerFlags;
    }

    inline bool MarkCollisionFilterDisabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags &= ~PHYSICS_BODY_COLLISION_FLAG;

        return old != activeUserPointerFlags;
    }

    inline bool MarkCollisionRecordingEnabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags |= PHYSICS_BODY_RECORDING_FLAG;

        return old != activeUserPointerFlags;
    }

    inline bool MarkCollisionRecordingDisabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags &= ~PHYSICS_BODY_RECORDING_FLAG;

        return old != activeUserPointerFlags;
    }

    /// \brief Just a simple way to store this one bool separately in this class, used by PhysicalWorld
    inline bool SetDisableAllCollisions(bool newValue) noexcept
    {
        if (allCollisionsDisabled == newValue)
            return false;

        allCollisionsDisabled = newValue;
        return true;
    }

    inline bool MarkCollisionDisableFlagEnabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags |= PHYSICS_BODY_DISABLE_COLLISION_FLAG;

        return old != activeUserPointerFlags;
    }

    inline bool MarkCollisionDisableFlagDisabled() noexcept
    {
        const auto old = activeUserPointerFlags;

        activeUserPointerFlags &= ~PHYSICS_BODY_DISABLE_COLLISION_FLAG;

        return old != activeUserPointerFlags;
    }

    [[nodiscard]] inline uint64_t CalculateUserPointer() const noexcept
    {
        return reinterpret_cast<uint64_t>(this) & static_cast<uint64_t>(activeUserPointerFlags);
    }

    // ------------------------------------ //
    // Collision callback user data (C# side's)

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

    IgnoredCollisionList ignoredCollisions;

    /// This is memory not owned by us where recorded collisions are written to
    CollisionRecordListType collisionRecordingTarget = nullptr;

    std::vector<Ref<TrackedConstraint>> constraintsThisIsPartOf;

    const JPH::BodyID id;

    std::unique_ptr<BodyControlState> bodyControlStateIfActive;

    int userDataLength = 0;

    int maxCollisionsToRecord = 0;

    /// A pointer to this is passed out for users of the collision recording array
    int32_t activeRecordedCollisionCount = 0;

    /// Used to detect when a new batch of collisions begins and old ones should be cleared
    uint32_t lastRecordedPhysicsStep = -1;

    uint8_t activeUserPointerFlags = 0;

    bool inWorld = false;
    bool active = true;
    bool allCollisionsDisabled = false;
};

} // namespace Thrive::Physics
