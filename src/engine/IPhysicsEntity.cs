using Godot;
using Newtonsoft.Json;

/// <summary>
///   Custom simulated physics entity. Note that the <see cref="ISimulatedEntity.Position"/> and <see cref="Rotation"/>
///   are overridden from the physics state on each update.
/// </summary>
public interface IPhysicsEntity : ISimulatedEntity
{
    [JsonProperty]
    public Quat Rotation { get; set; }

    [JsonIgnore]
    public PhysicsBody? Body { get; }

    [JsonIgnore]
    public PhysicalWorld? BodyCreatedInWorld { get; }

    /// <summary>
    ///   When the body is disabled the body state is no longer read into the position variables allowing custom
    ///   control
    /// </summary>
    public bool BodyDisabled { get; }
}
