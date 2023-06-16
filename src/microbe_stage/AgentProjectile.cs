using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   This is a shot agent projectile, does damage on hitting a cell of different species
/// </summary>
[JSONAlwaysDynamicType]
// TODO: reimplement inspectable
public class AgentProjectile : SimulatedPhysicsEntity, ISimulatedEntityWithDirectVisuals, ITimedLife /*, IInspectableEntity*/
{
#pragma warning disable CA2213
    private Particles particles = null!;
#pragma warning restore CA2213

    public float TimeToLiveRemaining { get; set; }
    public float Amount { get; set; }
    public AgentProperties? Properties { get; set; }
    public EntityReference<SimulatedPhysicsEntity> Emitter { get; set; } = new();

    [JsonIgnore]
    public string ReadableName => Properties?.ToString() ?? TranslationServer.Translate("N_A");

    [JsonProperty]
    private float? FadeTimeRemaining { get; set; }

    [JsonIgnore]
    public Spatial VisualNode { get; private set; }

    public override void OnAddedToSimulation(WorldSimulation simulation)
    {
        if (Properties == null)
            throw new InvalidOperationException($"{nameof(Properties)} is required");

        base.OnAddedToSimulation(simulation);

        var emitterNode = Emitter.Value?.EntityNode;

        if (emitterNode != null)
            AddCollisionExceptionWith(emitterNode);

        Connect("body_shape_entered", this, nameof(OnContactBegin));

        particles = GD.Load<PackedScene>("res://src/microbe_stage/AgentProjectile.tscn").Instance<Particles>();
        VisualNode = particles;
    }



    public void OnTimeOver()
    {
        if (FadeTimeRemaining == null)
            BeginDestroy();
    }

    private void OnContactBegin(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;
        _ = localShape;

        if (body is not Microbe microbe)
            return;

        if (microbe.Species == Properties!.Species)
            return;

        // If more stuff needs to be damaged we could make an IAgentDamageable interface.
        var target = microbe.GetMicrobeFromShape(bodyShape);

        if (target == null)
            return;

        Invoke.Instance.Perform(
            () => target.Damage(Constants.OXYTOXY_DAMAGE * Amount, Properties.AgentType));

        if (FadeTimeRemaining == null)
        {
            // We should probably get some *POP* effect here.
            BeginDestroy();
        }
    }

    /// <summary>
    ///   Stops particle emission and destroys the object after 5 seconds.
    /// </summary>
    private void BeginDestroy()
    {
        particles.Emitting = false;

        // Disable collisions and stop this entity
        // This isn't the recommended way (disabling the collision shape), but as we don't have a reference to that here
        // this should also work for disabling the collisions
        CollisionLayer = 0;
        CollisionMask = 0;
        LinearVelocity = Vector3.Zero;

        // Timer that delays despawn of projectiles
        FadeTimeRemaining = Constants.PROJECTILE_DESPAWN_DELAY;
    }
}
