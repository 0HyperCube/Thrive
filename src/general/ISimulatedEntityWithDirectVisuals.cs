using Godot;

/// <summary>
///   Simulated entity that has directly a Godot <see cref="Spatial"/> node for displaying its visuals
/// </summary>
public interface ISimulatedEntityWithDirectVisuals : ISimulatedEntity
{
    public Spatial VisualNode { get; }
}
