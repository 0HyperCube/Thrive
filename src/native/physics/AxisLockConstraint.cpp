// ------------------------------------ //
#include "AxisLockConstraint.hpp"

#include "Jolt/Core/StreamIn.h"
#include "Jolt/Core/StreamOut.h"

// ------------------------------------ //
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
    JPH::Constraint(settings), lockAxis(settings.lockAxis), lockRotation(settings.lockRotation)
{
}

AxisLockConstraint::AxisLockConstraint(JPH::Body& body, JPH::Vec3 lockAxis, bool lockRotation) :
    AxisLockConstraint(body, CreateSimpleSettings(lockAxis, lockRotation))
{
}

// ------------------------------------ //
void AxisLockConstraint::NotifyShapeChanged(const JPH::BodyID& inBodyID, JPH::Vec3Arg inDeltaCOM)
{
}

void AxisLockConstraint::SetupVelocityConstraint(float inDeltaTime)
{
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

void AxisLockConstraint::BuildIslands(
    JPH::uint32 inConstraintIndex, JPH::IslandBuilder& ioBuilder, JPH::BodyManager& inBodyManager)
{
}

uint AxisLockConstraint::BuildIslandSplits(JPH::LargeIslandSplitter& ioSplitter) const
{
    return 0;
}

JPH::Ref<JPH::ConstraintSettings> AxisLockConstraint::GetConstraintSettings() const
{
    return JPH::Ref<JPH::ConstraintSettings>();
}

} // namespace Thrive::Physics
