#pragma once

#include <array>
#include <cstdint>

#include "Include.h"

namespace Thrive::Physics
{

class PhysicsBody;

/// \brief Recorded physics collision. Must match the memory layout of the C# side PhysicsCollision class.
struct PhysicsCollision
{
public:
    std::array<char, PHYSICS_USER_DATA_SIZE> FirstUserData;

    std::array<char, PHYSICS_USER_DATA_SIZE> SecondUserData;

    const PhysicsBody* FirstBody;

    const PhysicsBody* SecondBody;

    int32_t FirstSubShapeData;

    int32_t SecondSubShapeData;

    float PenetrationAmount;
};

using CollisionRecordListType = PhysicsCollision*;

} // namespace Thrive::Physics
