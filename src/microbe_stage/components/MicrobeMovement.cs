namespace Components
{
    using Godot;

    /// <summary>
    ///   Control variables for specifying how a microbe wants to move
    /// </summary>
    public struct MicrobeMovement
    {
        /// <summary>
        ///   The point towards which the microbe will move to point to
        /// </summary>
        public Vector3 LookAtPoint;

        /// <summary>
        ///   The direction the microbe wants to move. Doesn't need to be normalized
        /// </summary>
        public Vector3 MovementDirection;

        /// <summary>
        ///   Whether this microbe is currently being slowed by environmental slime
        /// </summary>
        private bool SlowedBySlime;

        public MicrobeMovement(Vector3 startingPosition)
        {
            LookAtPoint = startingPosition + new Vector3(0, 0, -1);
            MovementDirection = new Vector3(0, 0, 0);
            SlowedBySlime = false;
        }
    }
}
