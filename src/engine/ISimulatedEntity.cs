using Godot;

/// <summary>
///   Interface for non-Godot entity types. Godot-based entities use <see cref="IEntity"/>
/// </summary>
public interface ISimulatedEntity : IEntityBase
{
    /// <summary>
    ///   All entities added to a game world must have a position (even if this field doesn't really matter for them).
    ///   This is similar to how the Godot-based entities must have a <see cref="Spatial"/> node associated with them.
    /// </summary>
    public Vector3 Position { get; set; }

    public void OnAddedToSimulation(WorldSimulation simulation);
}
