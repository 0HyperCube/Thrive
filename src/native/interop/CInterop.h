#pragma once

#include <cstdint>

#include "Include.h"

#include "interop/CStructures.h"

/// \file Defines all of the API methods that can be called from C#

extern "C"
{
    typedef void (*OnLogMessage)(const char* message, int32_t messageLength, int8_t logLevel);

    // ------------------------------------ //
    // General

    /// \returns The API version the native library was compiled with, if different from C# the library should not be
    /// used
    [[maybe_unused]] THRIVE_NATIVE_API int32_t CheckAPIVersion();

    /// \brief Prepares the native library for use, must be called first (right after the version check)
    [[maybe_unused]] THRIVE_NATIVE_API int32_t InitThriveLibrary();

    /// \brief Prepares the native library for shutdown should be called before the process is ended and after all
    /// other calls to the library have been performed
    [[maybe_unused]] THRIVE_NATIVE_API void ShutdownThriveLibrary();

    // ------------------------------------ //
    // Logging

    [[maybe_unused]] THRIVE_NATIVE_API void SetLogLevel(int8_t level);
    [[maybe_unused]] THRIVE_NATIVE_API void SetLogForwardingCallback(OnLogMessage callback);

    // ------------------------------------ //
    // Physics world

    [[maybe_unused]] THRIVE_NATIVE_API PhysicalWorld* CreatePhysicalWorld();
    [[maybe_unused]] THRIVE_NATIVE_API void DestroyPhysicalWorld(PhysicalWorld* physicalWorld);

    [[maybe_unused]] THRIVE_NATIVE_API bool ProcessPhysicalWorld(PhysicalWorld* physicalWorld, float delta);

    [[maybe_unused]] THRIVE_NATIVE_API PhysicsBody* PhysicalWorldCreateMovingBody(PhysicalWorld* physicalWorld,
        PhysicsShape* shape, JVec3 position, JQuat rotation = QuatIdentity, bool addToWorld = true);
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsBody* PhysicalWorldCreateStaticBody(PhysicalWorld* physicalWorld,
        PhysicsShape* shape, JVec3 position, JQuat rotation = QuatIdentity, bool addToWorld = true);

    [[maybe_unused]] THRIVE_NATIVE_API void PhysicalWorldAddBody(
        PhysicalWorld* physicalWorld, PhysicsBody* body, bool activate);

    [[maybe_unused]] THRIVE_NATIVE_API void DestroyPhysicalWorldBody(PhysicalWorld* physicalWorld, PhysicsBody* body);

    [[maybe_unused]] THRIVE_NATIVE_API void ReadPhysicsBodyTransform(
        PhysicalWorld* physicalWorld, PhysicsBody* body, JVec3* positionReceiver, JQuat* rotationReceiver);

    [[maybe_unused]] THRIVE_NATIVE_API float PhysicalWorldGetPhysicsLatestTime(PhysicalWorld* physicalWorld);
    [[maybe_unused]] THRIVE_NATIVE_API float PhysicalWorldGetPhysicsAverageTime(PhysicalWorld* physicalWorld);

    // ------------------------------------ //
    // Body functions
    [[maybe_unused]] THRIVE_NATIVE_API void PhysicsBodyAddAxisLock(PhysicsBody* body, JVecF3 axis);
    [[maybe_unused]] THRIVE_NATIVE_API void ReleasePhysicsBodyReference(PhysicsBody* body);

    // ------------------------------------ //
    // Physics shapes
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateBoxShape(float halfSideLength);
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateBoxShapeWithDimensions(JVecF3 halfDimensions);
    [[maybe_unused]] THRIVE_NATIVE_API PhysicsShape* CreateSphereShape(float radius);

    [[maybe_unused]] THRIVE_NATIVE_API void ReleaseShape(PhysicsShape* shape);
}
