/// <summary>
///   System that deletes nodes that are in the timed group after their lifespan expires.
/// </summary>
public class TimedLifeSystem
{
    private readonly IEntityContainer world;

    public TimedLifeSystem(IEntityContainer world)
    {
        this.world = world;
    }

    public void Process(float delta)
    {
        foreach (var entity in world.Entities)
        {
            if (entity is not ITimedLife timed)
                continue;

            // Fading timing is now also handled by this system
            if (timed.FadeTimeRemaining != null)
            {
                timed.FadeTimeRemaining -= delta;

                if (timed.FadeTimeRemaining <= 0)
                {
                    // Fade time ended
                    world.DestroyEntity(entity);
                }

                continue;
            }

            timed.TimeToLiveRemaining -= delta;

            if (timed.TimeToLiveRemaining <= 0.0f)
            {
                timed.OnTimeOver();

                if (timed.FadeTimeRemaining != null && timed.FadeTimeRemaining.Value > 0)
                {
                    // It wants a bit of extra time to fade

                    // Consider it already dead to not have it be saved
                    timed.AliveMarker.Alive = false;
                }
                else
                {
                    // Entity doesn't want to fade
                    world.DestroyEntity(entity);
                }
            }
        }
    }

    /// <summary>
    ///   Despawns all timed entities
    /// </summary>
    public void DespawnAll()
    {
        foreach (var entity in world.Entities)
        {
            if (entity is not ITimedLife)
                continue;

            world.DestroyEntity(entity);
        }
    }
}
