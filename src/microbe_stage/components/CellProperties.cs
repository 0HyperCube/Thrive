namespace Components
{
    /// <summary>
    ///   Base properties of a microbe (separate from the species info as early multicellular species object couldn't
    ///   work there)
    /// </summary>
    public struct CellProperties
    {
        public int HexCount;

        public float EngulfSize;

        public float UnadjustedRadius;

        public float RotationSpeed;

        // public float MassFromOrganelles

        public bool IsBacteria;

        public float Radius => IsBacteria ? UnadjustedRadius * 0.5f : UnadjustedRadius;
    }
}
