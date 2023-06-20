using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base for game entities that don't use Godot. For the Godot variant see <see cref="IEntity"/>
/// </summary>
[JSONAlwaysDynamicType]
public abstract class SimulatedEntity : ISimulatedEntity
{
    [JsonProperty]
    public virtual Vector3 Position { get; set; }

    [JsonProperty]
    public HashSet<string> EntityGroups { get; private set; } = new();

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    public virtual void OnAddedToSimulation(IWorldSimulation simulation)
    {
    }

    public virtual void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }
}
