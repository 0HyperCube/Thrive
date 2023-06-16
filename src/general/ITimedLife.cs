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

    public void OnTimeOver();
}
