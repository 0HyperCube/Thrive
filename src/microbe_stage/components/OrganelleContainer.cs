namespace Components
{
    using System.Collections.Generic;

    /// <summary>
    ///   Entity that contains <see cref="PlacedOrganelle"/>
    /// </summary>
    public struct OrganelleContainer
    {
        // probably can do with just having the CommandSignaler component
        // public bool HasSignalingAgent;

        public Dictionary<Enzyme, int>? AvailableEnzymes;
    }
}
