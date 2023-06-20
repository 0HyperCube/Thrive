using System.Collections.Generic;

/// <summary>
///   Anything that supports the entity management (creation, deletion) operations
/// </summary>
public interface IEntityContainer
{
    public IReadOnlyCollection<IEntityBase> Entities { get; }

    /// <summary>
    ///   Adds an entity to this simulation / container
    /// </summary>
    /// <param name="entity">The entity to add. Same instance may not be added multiple times</param>
    public void AddEntity(IEntityBase entity);

    /// <summary>
    ///   Destroys an entity (some simulations will queue destroys and only perform them at the end of the current
    ///   simulation frame)
    /// </summary>
    /// <param name="entity">Entity to destroy</param>
    /// <returns>True when destroyed, false if the entity was not added</returns>
    public bool DestroyEntity(IEntityBase entity);

    /// <summary>
    ///   Destroys all entities in this container
    /// </summary>
    /// <param name="skip">An optional entity to skip deleting</param>
    public void DestroyAllEntities(IEntityBase? skip = null);
}
