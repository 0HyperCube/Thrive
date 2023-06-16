using Godot;

/// <summary>
///   System that deletes nodes that are in the timed group after their lifespan expires.
/// </summary>
public class TimedLifeSystem
{
    private readonly Node worldRoot;

    public TimedLifeSystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
    }

    public void Process(float delta)
    {
        foreach (var entity in worldRoot.GetChildrenToProcess<Node>(Constants.TIMED_GROUP))
        {
            var timed = entity as ITimedLife;

            if (timed == null)
            {
                GD.PrintErr("A node has been put in the timed group but it isn't derived from ITimedLife");
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
            }
        }
        
        // TODO: implement fading
        public override void _Process(float delta)
        {
            if (FadeTimeRemaining == null)
                return;

            FadeTimeRemaining -= delta;
            if (FadeTimeRemaining <= 0)
                this.DestroyDetachAndQueueFree();
        }
    }
    

    /// <summary>
    ///   Despawns all timed entities
    /// </summary>
    public void DespawnAll()
    {
        foreach (var entity in worldRoot.GetChildrenToProcess<Node>(Constants.TIMED_GROUP))
        {
            if (entity.IsQueuedForDeletion())
                continue;

            var asProperEntity = entity as IEntity;

            if (asProperEntity == null)
            {
                entity.DetachAndQueueFree();
                continue;
            }

            asProperEntity.DestroyDetachAndQueueFree();
        }
    }
}
