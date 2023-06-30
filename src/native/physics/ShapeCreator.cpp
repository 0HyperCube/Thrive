// ------------------------------------ //
#include "ShapeCreator.hpp"

#include "Jolt/Math/Trigonometry.h"
#include "Jolt/Physics/Collision/Shape/ConvexHullShape.h"
#include "Jolt/Physics/Collision/Shape/MeshShape.h"
#include "Jolt/Physics/Collision/Shape/MutableCompoundShape.h"
#include "Jolt/Physics/Collision/Shape/StaticCompoundShape.h"

#include "core/Logger.hpp"

// ------------------------------------ //
namespace Thrive::Physics
{

JPH::RefConst<JPH::Shape> ShapeCreator::CreateConvex(const JPH::Array<JPH::Vec3>& points, float density /*= 1000*/,
    float convexRadius /*= 0.01f*/, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    auto settings = JPH::ConvexHullShapeSettings(points, convexRadius, material);
    settings.SetDensity(density);

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateStaticCompound(
    const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes)
{
    JPH::StaticCompoundShapeSettings settings;

    for (const auto& shape : subShapes)
    {
        settings.AddShape(std::get<1>(shape), std::get<2>(shape), std::get<0>(shape), std::get<3>(shape));
    }

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateMutableCompound(
    const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes)
{
    JPH::MutableCompoundShapeSettings settings;

    for (const auto& shape : subShapes)
    {
        settings.AddShape(std::get<1>(shape), std::get<2>(shape), std::get<0>(shape), std::get<3>(shape));
    }

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateMesh(
    JPH::Array<JPH::Float3>&& vertices, JPH::Array<JPH::IndexedTriangle>&& triangles)
{
    // Create torus
    JPH::MeshShapeSettings mesh;

    // TODO: materials support (each triangle can have a different one)
    // mesh.mMaterials

    mesh.mTriangleVertices = std::move(vertices);
    mesh.mIndexedTriangles = std::move(triangles);

    return mesh.Create().Get();
}

// ------------------------------------ //
JPH::RefConst<JPH::Shape> ShapeCreator::CreateMicrobeShapeConvex(
    JVecF3* points, uint32_t pointCount, float density, float scale, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    if (pointCount < 1)
    {
        LOG_ERROR("Microbe shape point count is 0");
        return nullptr;
    }

    // We don't use any of the explicit constructors as we want to do any needed type and scale conversions when
    // actually copying data to the array in the settings
    auto settings = JPH::ConvexHullShapeSettings();
    settings.mMaxConvexRadius = JPH::cDefaultConvexRadius;

    auto& pointTarget = settings.mPoints;
    pointTarget.reserve(pointCount + 2);

    // Add a center and a top point to ensure some volume for the shape without duplicating all of the points
    pointTarget.emplace_back(0, 0, 0);
    pointTarget.emplace_back(0, 1, 0);

    if (scale != 1)
    {
        for (uint32_t i = 0; i < pointCount; ++i)
        {
            const auto& sourcePoint = points[i];

            pointTarget.emplace_back(sourcePoint.X * scale, sourcePoint.Y * scale, sourcePoint.Z * scale);
        }
    }
    else
    {
        for (uint32_t i = 0; i < pointCount; ++i)
        {
            const auto& sourcePoint = points[i];

            pointTarget.emplace_back(sourcePoint.X, sourcePoint.Y, sourcePoint.Z);
        }
    }

    if (material != nullptr)
        settings.mMaterial = material;

    settings.SetDensity(density);

    return settings.Create().Get();
}

JPH::RefConst<JPH::Shape> ShapeCreator::CreateMicrobeShapeSpheres(
    JVecF3* points, uint32_t pointCount, float density, float scale, const JPH::PhysicsMaterial* material /*= nullptr*/)
{
    if (pointCount < 1)
    {
        LOG_ERROR("Microbe shape point count is 0");
        return nullptr;
    }

    const auto sphereShape = SimpleShapes::CreateSphere(1 * scale, density, material);

    JPH::StaticCompoundShapeSettings settings;

    const auto rotation = JPH::Quat::sIdentity();

    for (uint32_t i = 0; i < pointCount; ++i)
    {
        const auto& sourcePoint = points[i];

        settings.AddShape(
            {sourcePoint.X * scale, sourcePoint.Y * scale, sourcePoint.Z * scale}, rotation, sphereShape.GetPtr(), 0);
    }

    // Individual materials and densities are set in the sub shapes, hopefully that is enough

    return settings.Create().Get();
}

} // namespace Thrive::Physics
