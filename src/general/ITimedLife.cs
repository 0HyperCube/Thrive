using Newtonsoft.Json;

/// <summary>
///   All nodes that despawn after some time need to implement this.
/// </summary>
public interface ITimedLife : IAliveTracked
{
    [JsonProperty]
    public float TimeToLiveRemaining { get; set; }

    [JsonProperty]
    public float? FadeTimeRemaining { get; set; }

    /// <summary>
    ///   Called when the time to live runs out. If this doesn't set <see cref="FadeTimeRemaining"/> the timed life
    ///   system will destroy this entity immediately
    /// </summary>
    public void OnTimeOver();
}
