namespace Shared.Enums
{
    // Add 'distance' in front of each enum to generate valid message
    public enum Direction
    {
        /// <summary> y + 1, x - 1 </summary>
        NW,

        /// <summary> y + 1, x </summary>
        N,

        /// <summary> y + 1, x + 1 </summary>
        NE,

        /// <summary> y, x - 1 </summary>
        W,

        /// <summary> y, x </summary>
        FromCurrent,

        /// <summary> y, x + 1 </summary>
        E,

        /// <summary> y - 1, x - 1 </summary>
        SW,

        /// <summary> y - 1, x </summary>
        S,

        /// <summary> y - 1, x + 1 </summary>
        SE,
    }
}
