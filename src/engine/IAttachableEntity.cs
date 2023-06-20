using Godot;

/// <summary>
///   Entity type that can be attached physically to another entity
/// </summary>
[UseThriveSerializer]
[JSONAlwaysDynamicType]
public interface IAttachableEntity : ISimulatedEntity
{
    /// <summary>
    ///   Position relative to the parent entity (not valid when not attached)
    /// </summary>
    public Vector3 RelativePosition { get; }

    /// <summary>
    ///   Rotation relative to the parent entity (not valid when not attached)
    /// </summary>
    public Quat RelativeRotation { get; }

    public bool AttachedToAnEntity { get; }
}
