using System;
using System.Collections.Generic;

/// <summary>
///   Interface for <see cref="WorldSimulation{TEntity}"/> to solve circular dependencies regarding callback types etc.
///   with some flexibility
/// </summary>
public interface IWorldSimulation : IEntityContainer, IDisposable
{
    /// <summary>
    ///   Checks that the entity is in this world and is not being deleted
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True when the entity is in this world and is not queued for deletion</returns>
    public bool IsEntityInWorld(IEntityBase entity);

    /// <summary>
    ///   Returns true when the given entity is queued for destruction
    /// </summary>
    public bool IsQueuedForDeletion(IEntityBase entity);

    /// <summary>
    ///   Filters entities to just ones that have the specified group
    /// </summary>
    /// <param name="group">The group that must be in the entity's <see cref="ISimulatedEntity.EntityGroups"/></param>
    /// <returns>Entities matching the group</returns>
    public IEnumerable<IEntityBase> EntitiesWithGroup(string group);
}
