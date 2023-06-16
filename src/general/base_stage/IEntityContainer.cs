using System.Collections.Generic;

/// <summary>
///   Anything that supports the entity management (creation, deletion) operations
/// </summary>
/// <typeparam name="TEntity">Type of entity handled by this container</typeparam>
public interface IEntityContainer<TEntity>
    where TEntity : class, IEntityBase
{
    public IReadOnlyCollection<TEntity> Entities { get; }

    /// <summary>
    ///   Adds an entity to this simulation / container
    /// </summary>
    /// <param name="entity">The entity to add. Same instance may not be added multiple times</param>
    public void AddEntity(TEntity entity);

    /// <summary>
    ///   Destroys an entity (some simulations will queue destroys and only perform them at the end of the current
    ///   simulation frame)
    /// </summary>
    /// <param name="entity">Entity to destroy</param>
    /// <returns>True when destroyed, false if the entity was not added</returns>
    public bool DestroyEntity(TEntity entity);

    /// <summary>
    ///   Destroys all entities in this container
    /// </summary>
    /// <param name="skip">An optional entity to skip deleting</param>
    public void DestroyAllEntities(TEntity? skip = null);
}
