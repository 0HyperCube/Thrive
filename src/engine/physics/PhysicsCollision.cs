﻿using System;
using DefaultEcs;

/// <summary>
///   Info regarding a physics collision in an entity simulation
/// </summary>
public readonly struct PhysicsCollision
{
    /// <summary>
    ///   First colliding body, this is not wrapped in a <see cref="NativePhysicsBody"/> to avoid extra reference
    ///   counting and object allocations
    /// </summary>
    public readonly IntPtr FirstBody;

    public readonly Entity FirstEntity;
    public readonly int FirstSubShapeData;

    public readonly IntPtr SecondBody;
    public readonly Entity SecondEntity;
    public readonly int SecondSubShapeData;

    public readonly float PenetrationAmount;

    public PhysicsCollision(IntPtr body1, int subShapeData1, Entity entity1, IntPtr body2, int subShapeData2,
        Entity entity2, float penetration)
    {
        FirstBody = body1;
        FirstSubShapeData = subShapeData1;
        FirstEntity = entity1;

        SecondBody = body2;
        SecondSubShapeData = subShapeData2;
        SecondEntity = entity2;

        PenetrationAmount = penetration;
    }
}
