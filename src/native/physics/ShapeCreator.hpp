#pragma once

#include "SimpleShapes.hpp"

namespace Thrive::Physics
{

/// \brief More advanced shape creation helper class than SimpleShapes
class ShapeCreator
{
public:
    ShapeCreator() = delete;

    /// \brief Create a convex shape from a list of points
    /// \param convexRadius Used convex radius for this shape, should be lower than the default value used for other
    /// shapes
    static JPH::RefConst<JPH::Shape> CreateConvex(const JPH::Array<JPH::Vec3>& points, float convexRadius = 0.01f,
        const JPH::PhysicsMaterial* material = nullptr);

    /// \brief Creates a shape composed of multiple other shapes that cannot change after creation
    /// \todo Figure out how to use physics materials here
    static JPH::RefConst<JPH::Shape> CreateStaticCompound(
        const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes);

    /// \brief Variant of the compound shape that is allowed to be modified (but has lower performance than static)
    static JPH::RefConst<JPH::Shape> CreateMutableCompound(
        const std::vector<std::tuple<JPH::RefConst<JPH::Shape>, JPH::Vec3, JPH::Quat, uint32_t>>& subShapes);

    // TODO: mesh (but note the performance caveats)
};

} // namespace Thrive::Physics
