namespace Components
{
    using DefaultEcs;
    using Godot;

    /// <summary>
    ///   Sends and receivers command signals (signalling agent). Requires a <see cref="WorldPosition"/> to function
    ///   as the origin of the signalling command.
    /// </summary>
    public struct CommandSignaler
    {
        /// <summary>
        ///   Stores the position the command signal was received from. Only valid if <see cref="ReceivedCommand"/> is
        ///   not <see cref="MicrobeSignalCommand.None"/>.
        /// </summary>
        public Vector3 ReceivedCommandSource;

        /// <summary>
        ///   Entity that sent the detected signal. Not valid if <see cref="ReceivedCommand"/> is not set (see
        ///   documentation on <see cref="ReceivedCommandSource"/>).
        /// </summary>
        public Entity ReceivedCommandFromEntity;

        /// <summary>
        ///   Used to limit signals reaching entities they shouldn't. In the microbe stage this contains the entity's
        ///   species ID to allow species-wide signalling.
        /// </summary>
        public ulong SignallingChannel;

        /// <summary>
        ///   Because AI is ran in parallel thread, if it wants to change the signalling, it needs to do it through this
        /// </summary>
        public MicrobeSignalCommand? QueuedSignallingCommand;

        public MicrobeSignalCommand Command;

        public MicrobeSignalCommand ReceivedCommand;
    }
}
