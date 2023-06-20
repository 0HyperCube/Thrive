using System.Collections.Generic;
using Godot;

/// <summary>
///   World simulation that uses the external physics engine in the native code module
/// </summary>
/// <typeparam name="TEntity">Type of entity handled by this simulation</typeparam> 
public abstract class WorldSimulationWithExternalPhysics<TEntity> : WorldSimulation<TEntity>,
    IWorldSimulationWithPhysics
    where TEntity : class, ISimulatedEntity
{
    protected readonly PhysicalWorld physics;

    /// <summary>
    ///   All created physics bodies. Must be tracked to correctly destroy them all
    /// </summary>
    protected readonly List<PhysicsBody> createdBodies = new();

    protected WorldSimulationWithExternalPhysics()
    {
        physics = PhysicalWorld.Create();
    }

    ~WorldSimulationWithExternalPhysics()
    {
        Dispose(false);
    }

    public PhysicalWorld PhysicalWorld => physics;

    public PhysicsBody CreateMovingBody(PhysicsShape shape, Vector3 position, Quat rotation)
    {
        var body = physics.CreateMovingBody(shape, position, rotation);
        createdBodies.Add(body);
        return body;
    }

    public void DestroyBody(PhysicsBody body)
    {
        if (!createdBodies.Remove(body))
        {
            GD.PrintErr("Can't destroy body not in simulation");
            return;
        }

        physics.DestroyBody(body);
    }

    protected override void WaitForStartedPhysicsRun()
    {
        // TODO: implement multithreading
    }

    protected override bool RunPhysicsIfBehind()
    {
        // TODO: implement this once multithreaded running is added
        return false;
    }

    protected override void OnStartPhysicsRunIfTime(float delta)
    {
        physics.ProcessPhysics(delta);
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            physics.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ReleaseUnmanagedResources()
    {
        foreach (var createdBody in createdBodies)
        {
            physics.DestroyBody(createdBody);
        }

        createdBodies.Clear();
    }
}
