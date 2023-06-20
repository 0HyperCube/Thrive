using System;
using System.Collections.Generic;

/// <summary>
///   For use in the prototypes not yet converted to using world simulations
/// </summary>
public class DummyWorldSimulation : IWorldSimulation
{
    public IReadOnlyCollection<IEntityBase> Entities { get; } = new List<IEntityBase>();

    public void AddEntity(IEntityBase entity)
    {
        throw new NotSupportedException("Dummy simulation doesn't support adding entities");
    }

    public bool DestroyEntity(IEntityBase entity)
    {
        return false;
    }

    public void DestroyAllEntities(IEntityBase? skip = null)
    {
        throw new System.NotImplementedException();
    }

    public void Dispose()
    {
    }

    public bool IsEntityInWorld(IEntityBase entity)
    {
        return false;
    }

    public bool IsQueuedForDeletion(IEntityBase entity)
    {
        return false;
    }

    public IEnumerable<IEntityBase> EntitiesWithGroup(string group)
    {
        yield break;
    }
}
