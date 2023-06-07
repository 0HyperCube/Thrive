#pragma once

#include "Jolt/Physics/Body/Body.h"
#include "Jolt/Physics/Constraints/Constraint.h"

#include "CustomConstraintTypes.hpp"

namespace Thrive::Physics
{

class AxisLockConstraint;

/// \brief AxisLockConstraint settings, used to create them
class JPH_EXPORT AxisLockConstraintSettings final : public JPH::ConstraintSettings
{
public:
    JPH_DECLARE_SERIALIZABLE_VIRTUAL(JPH_EXPORT, AxisLockConstraintSettings)

    // See: ConstraintSettings::SaveBinaryState
    void SaveBinaryState(JPH::StreamOut& inStream) const override;

    /// Create an an instance of this constraint
    AxisLockConstraint* Create(JPH::Body& body) const;

    JPH::Vec3 lockAxis = JPH::Vec3::sAxisY();
    bool lockRotation = false;

protected:
    /// \copydoc JPH::ConstraintSettings::RestoreBinaryState
    void RestoreBinaryState(JPH::StreamIn& inStream) override;
};

/// \brief Constraints a physics body to not allow it to move on an axis
class AxisLockConstraint : public JPH::Constraint
{
public:
    explicit AxisLockConstraint(JPH::Body& body, const AxisLockConstraintSettings& settings);
    AxisLockConstraint(JPH::Body& body, JPH::Vec3 lockAxis, bool lockRotation);
    ~AxisLockConstraint() override = default;

    JPH::EConstraintSubType GetSubType() const override
    {
        return ConstraintTypes::AxisLock;
    }

    void NotifyShapeChanged(const JPH::BodyID& inBodyID, JPH::Vec3Arg inDeltaCOM) override;
    void SetupVelocityConstraint(float inDeltaTime) override;
    void WarmStartVelocityConstraint(float inWarmStartImpulseRatio) override;
    bool SolveVelocityConstraint(float inDeltaTime) override;
    bool SolvePositionConstraint(float inDeltaTime, float inBaumgarte) override;
    void BuildIslands(
        JPH::uint32 inConstraintIndex, JPH::IslandBuilder& ioBuilder, JPH::BodyManager& inBodyManager) override;
    uint BuildIslandSplits(JPH::LargeIslandSplitter& ioSplitter) const override;
    JPH::Ref<JPH::ConstraintSettings> GetConstraintSettings() const override;

#ifdef JPH_DEBUG_RENDERER
    // Drawing interface
    virtual void DrawConstraint(DebugRenderer* inRenderer) const = 0;
#endif

private:
    JPH::Vec3 lockAxis;
    bool lockRotation;
};

} // namespace Thrive::Physics
