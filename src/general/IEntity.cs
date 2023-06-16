using Godot;
using Newtonsoft.Json;

/// <summary>
///   All Godot-based game entities implement this interface to provide support for needed operations regarding them.
///   For the other type of entity see <see cref="ISimulatedEntity"/>.
/// </summary>
public interface IEntity : IEntityBase
{
    /// <summary>
    ///   The Node that this entity is in the game world as
    /// </summary>
    [JsonIgnore]
    public Spatial EntityNode { get; }
}
