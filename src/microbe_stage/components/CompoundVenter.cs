namespace Components
{
    /// <summary>
    ///   An entity that constantly leaks compounds into the environment. Requires <see cref="CompoundStorage"/>.
    /// </summary>
    public struct CompoundVenter
    {
        /// <summary>
        ///   How much of each compound is vented per second
        /// </summary>
        public float VentEachCompoundPerSecond;

        public bool DestroyOnEmpty;

        /// <inheritdoc cref="DamageOnTouch.UsesMicrobialDissolveEffect"/>
        public bool UsesMicrobialDissolveEffect;
    }
}
