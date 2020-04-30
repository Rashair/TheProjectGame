namespace Shared.Models
{
    public class Position
    {
        public int Y { get; set; }

        public int X { get; set; }

        public static implicit operator Position(int[] pos) => new Position
        {
            Y = pos[0],
            X = pos[1],
        };
    }
}
