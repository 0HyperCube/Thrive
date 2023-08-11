namespace Components
{
    using System.Collections.Generic;
    using DefaultEcs;

    /// <summary>
    ///   Entity that can engulf <see cref="Engulfable"/>s
    /// </summary>
    public struct Engulfer
    {
        /// <summary>
        ///   Tracks entities this already engulfed.
        /// </summary>
        public List<Entity>? EngulfedObjects;

        /// <summary>
        ///   Tracks entities this has previously engulfed. This is used to not constantly attempt to re-engulf
        ///   something this cannot fully engulf
        /// </summary>
        public List<Entity>? ExpelledObjects;
    }
}
