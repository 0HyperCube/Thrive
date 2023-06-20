﻿/// <summary>
///   The most fundamental entity type in the game. The highest hierarchy level before the split into
///   <see cref="IEntity"/> and <see cref="ISimulatedEntity"/>
/// </summary>
[JSONAlwaysDynamicType]
public interface IEntityBase : IAliveTracked
{
}
