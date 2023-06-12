#pragma once

namespace Thrive::Physics
{

/// \brief Variables needed for tracking body control over multiple physics updates to make sure the control is stable
class BodyControlState
{
public:
    BodyControlState() = default;

    JPH::Quat previousRotation = {};
    JPH::Quat previousTarget = {};

    bool justStarted = true;
};

} // namespace Thrive::Physics
