using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Interface for non-Godot entity types. Godot-based entities use <see cref="IEntity"/>
/// </summary>
[JSONAlwaysDynamicType]
public interface ISimulatedEntity : IEntityBase
{
    /// <summary>
    ///   All entities added to a game world must have a position (even if this field doesn't really matter for them).
    ///   This is similar to how the Godot-based entities must have a <see cref="Spatial"/> node associated with them.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     In case of attached entities this is the world position. For relative position see <see cref=""/>
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public Vector3 Position { get; set; }

    /// <summary>
    ///   Custom groups this entity is in. The groups by default don't do anything, only what code checking and adding
    ///   groups to entities will do anything with this.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If refactoring to use an ECS for Thrive then probably destroying and creating components rather
    ///     infrequently could be used to alternatively encode this information.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public HashSet<string> EntityGroups { get; }

    public void OnAddedToSimulation(IWorldSimulation simulation);

    // TODO: remove from world callback (or use the OnDestroyed?) and maybe a separate detach callback
}
