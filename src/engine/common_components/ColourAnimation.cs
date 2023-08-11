namespace Components
{
    using Godot;

    /// <summary>
    ///   Specifies simple colour changing animations
    /// </summary>
    public struct ColourAnimation
    {
        /// <summary>
        ///   The default colour to return to
        /// </summary>
        public Color DefaultColour;

        /// <summary>
        ///   The current colour value that should be displayed. Note that this component by itself is not enough to
        ///   get this to display anywhere.
        /// </summary>
        public Color CurrentColour;

        public Color AnimatedColour;

        public float AnimationDuration;

        /// <summary>
        ///   Needs to be set to true to trigger the animation to happen
        /// </summary>
        public bool Animating;

        /// <summary>
        ///   This shouldn't be changed manually outside the colour animation system
        /// </summary>
        public float AnimationElapsed;
    }
}
