using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base for game entities that don't use Godot. For the Godot variant see <see cref="IEntity"/>
/// </summary>
public abstract class SimulatedEntity : ISimulatedEntity
{
    [JsonProperty]
    public virtual Vector3 Position { get; set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    public virtual void OnAddedToSimulation(WorldSimulation simulation)
    {
    }

    public virtual void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }
}
