using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   <see cref="SimulatedEntity"/> that has a <see cref="PhysicsBody"/> to interact things with physically
/// </summary>
[JSONAlwaysDynamicType]
public class SimulatedPhysicsEntity : SimulatedEntity, IPhysicsEntity
{
    public delegate void OnCollidedWith(PhysicsBody body, int collidedSubShapeDataOurs, int collidedSubShapeDataTheirs);

    /// <summary>
    ///   Latest rotation updated from the physics
    /// </summary>
    [JsonProperty]
    public Quat Rotation { get; set; } = Quat.Identity;

    /// <summary>
    ///   The density of this physics body. Note that this doesn't apply retroactively after creation. The Jolt physics
    ///   uses a density value instead of mass, which is not the same.
    /// </summary>
    [JsonProperty]
    public float Density { get; set; } = 1000;

    [JsonIgnore]
    public PhysicsBody? Body { get; protected set; }

    [JsonIgnore]
    public PhysicalWorld? BodyCreatedInWorld { get; protected set; }

    [JsonProperty]
    public bool BodyDisabled { get; set; }

    protected bool AllCollisionsDisabled { get; private set; }

    public override void OnAddedToSimulation(IWorldSimulation simulation)
    {
        base.OnAddedToSimulation(simulation);

        // TODO: physics body creation (and destruction when removed from a world)
        throw new NotImplementedException();
    }

    public void RegisterCollisionCallback(OnCollidedWith onCollided)
    {
        throw new NotImplementedException();
    }

    public void SetVelocityToZero()
    {
        if (Body == null || !CheckWeHaveWorldReference())
            return;

        BodyCreatedInWorld!.SetBodyVelocity(Body, Vector3.Zero, Vector3.Zero);
    }

    public void DisableAllCollisions()
    {
        if (AllCollisionsDisabled)
            return;

        if (Body == null || !CheckWeHaveWorldReference())
            return;

        AllCollisionsDisabled = true;

        BodyCreatedInWorld!.SetBodyCollisionsEnabledState(Body, false);
    }

    public void EnableCollisions()
    {
        if (!AllCollisionsDisabled)
            return;

        if (Body == null || !CheckWeHaveWorldReference())
            return;

        AllCollisionsDisabled = false;

        BodyCreatedInWorld!.SetBodyCollisionsEnabledState(Body, false);
    }

    public void DisableCollisionsWith(PhysicsBody otherBody)
    {
        if (Body == null || !CheckWeHaveWorldReference())
            return;

        try
        {
            BodyCreatedInWorld!.BodyIgnoreCollisionsWithBody(Body, otherBody);
        }
        catch (Exception e)
        {
            GD.PrintErr("Cannot ignore collisions with another body: ", e);
        }
    }

    protected bool CheckWeHaveWorldReference()
    {
        if (BodyCreatedInWorld == null)
        {
            GD.PrintErr("Physics entity doesn't have a known physics world, can't apply an operation");
            return false;
        }

        return true;
    }
}
