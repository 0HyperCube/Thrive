// ------------------------------------ //
#include "PhysicsBody.hpp"

#include "Jolt/Physics/Body/Body.h"
// ------------------------------------ //
namespace Thrive::Physics
{

PhysicsBody::PhysicsBody(JPH::Body* body, JPH::BodyID bodyId) : id(bodyId)
{
    body->SetUserData(reinterpret_cast<uint64_t>(this));
}

PhysicsBody::~PhysicsBody()
{
}

// ------------------------------------ //
PhysicsBody* PhysicsBody::FromJoltBody(const JPH::Body* body)
{
    const auto rawValue = body->GetUserData();

    if (rawValue == 0)
        return nullptr;

    return reinterpret_cast<PhysicsBody*>(rawValue);
}

}
