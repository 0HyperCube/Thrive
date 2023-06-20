/// <summary>
///   All nodes that can be spawned with the spawn system must implement this interface
/// </summary>
public interface ISpawned : ISimulatedEntity
{
    /// <summary>
    ///   If the squared distance to the player of this object is
    ///   greater than this, it is despawned.
    /// </summary>
    public int DespawnRadiusSquared { get; set; }

    /// <summary>
    ///   How much this entity contributes to the entity limit relative to a single node
    /// </summary>
    public float EntityWeight { get; }

    /// <summary>
    ///   Set to true when despawning is disallowed (for example the player entity)
    /// </summary>
    public bool DisallowDespawning { get; }
}
