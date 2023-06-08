#pragma once

#include "Jolt/Physics/Body/Body.h"
#include "Jolt/Physics/Constraints/Constraint.h"
#include "Jolt/Physics/Constraints/ConstraintPart/DualAxisConstraintPart.h"
#include "Jolt/Physics/Constraints/ConstraintPart/PointConstraintPart.h"
#include "Jolt/Physics/Constraints/ConstraintPart/RotationEulerConstraintPart.h"

#include "CustomConstraintTypes.hpp"

namespace Thrive::Physics
{
class AxisLockConstraint;

using namespace JPH;

JPH_SUPPRESS_WARNING_PUSH
JPH_SUPPRESS_WARNINGS

/// \brief AxisLockConstraint settings, used to create them
class JPH_EXPORT AxisLockConstraintSettings final : public JPH::ConstraintSettings
{
public:
    JPH_DECLARE_SERIALIZABLE_VIRTUAL(JPH_EXPORT, AxisLockConstraintSettings)

    void SaveBinaryState(JPH::StreamOut& inStream) const override;

    AxisLockConstraint* Create(JPH::Body& body) const;

    JPH::Vec3 lockAxis = JPH::Vec3::sAxisY();
    bool lockRotation = false;

protected:
    /// \copydoc JPH::ConstraintSettings::RestoreBinaryState
    void RestoreBinaryState(JPH::StreamIn& inStream) override;
};

JPH_SUPPRESS_WARNING_POP

} // namespace Thrive::Physics

namespace Thrive::Physics
{

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

    void BuildIslands(uint32 inConstraintIndex, IslandBuilder& ioBuilder, BodyManager& inBodyManager) override;
    uint BuildIslandSplits(LargeIslandSplitter& ioSplitter) const override;

#ifdef JPH_DEBUG_RENDERER
    // Drawing interface
    void DrawConstraint(DebugRenderer* inRenderer) const override;
#endif

    void SaveState(JPH::StateRecorder& inStream) const override;
    void RestoreState(JPH::StateRecorder& inStream) override;

    JPH::Ref<ConstraintSettings> GetConstraintSettings() const override;

private:
    JPH::BodyID bodyId;
    JPH::Body* body;

    JPH::Vec3 lockAxis;
    bool lockRotation;

    // Ephemeral runtime info
    JPH::DualAxisConstraintPart			axisConstraintPart;

    // TODO: needs a custom 2 axis rotation support
};

} // namespace Thrive::Physics
