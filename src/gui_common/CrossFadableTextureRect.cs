using Godot;

/// <summary>
///   Displays image that can be changed smoothly with fade.
/// </summary>
public partial class CrossFadableTextureRect : TextureRect
{
    private Texture2D? image;

#pragma warning disable CA2213
    private Tween tween = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void FadedEventHandler();

    /// <summary>
    ///   Image to be displayed. This fades the texture rect. To change the image without fading use
    ///   <see cref="TextureRect.Texture2D"/>.
    /// </summary>
    public Texture2D? Image
    {
        get => image;
        set
        {
            image = value;
            UpdateImage();
        }
    }

    [Export]
    public float FadeDuration { get; set; } = 0.5f;

    public override void _Ready()
    {
        tween = GetNode<Tween>("Tween");
    }

    private void UpdateImage()
    {
        // Initial image display shouldn't fade
        if (Texture == null)
        {
            Texture = Image;
            return;
        }

        tween.TweenProperty(this, "modulate", Colors.Black, FadeDuration);
        tween.Play();

        tween.CheckAndConnect(
            "tween_completed", this, nameof(OnFaded), null, (uint)ConnectFlags.OneShot);
    }

    private void OnFaded(GodotObject @object, NodePath key)
    {
        _ = @object;
        _ = key;

        Texture = Image;
        EmitSignal(nameof(Faded));

        tween.TweenProperty(this, "modulate", Colors.White, FadeDuration);
        tween.Play();
    }
}
