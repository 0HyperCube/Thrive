// ------------------------------------ //
#include "CInterop.h"

#include "physics/PhysicalWorld.hpp"

#pragma clang diagnostic push
#pragma ide diagnostic ignored "cppcoreguidelines-pro-type-reinterpret-cast"
// ------------------------------------ //
extern "C"
{
    PhysicalWorld* CreatePhysicalWorld()
    {
        return reinterpret_cast<PhysicalWorld*>(new Thrive::Physics::PhysicalWorld());
    }

    void DestroyPhysicalWorld(PhysicalWorld* physicalWorld)
    {
        if (physicalWorld == nullptr)
            return;

        delete reinterpret_cast<Thrive::Physics::PhysicalWorld*>(physicalWorld);
    }
}

#pragma clang diagnostic pop
