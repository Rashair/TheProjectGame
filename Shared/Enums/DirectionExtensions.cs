using System;

namespace Shared.Enums
{
    public static class DirectionExtensions
    {
        public static (Direction dir, int y, int x)[] GetCoordinatesAroundCenter((int y, int x) center)
        {
            Direction[] directions = (Direction[])Enum.GetValues(typeof(Direction));
            (Direction, int y, int x)[] coordinates = new (Direction, int, int)[directions.Length];
            for (int i = 0; i < directions.Length; ++i)
            {
                (int y, int x) = directions[i].GetCoordinates(center);
                coordinates[i] = (directions[i], y, x);
            }

            return coordinates;
        }

        public static (Direction dir, int y, int x)[] GetCoordinatesAroundCenter(int[] center)
        {
            return GetCoordinatesAroundCenter((center[0], center[1]));
        }

        public static (int y, int x) GetCoordinates(this Direction dir, (int y, int x) center)
        {
            switch (dir)
            {
                case Direction.NW:
                    return (center.y + 1, center.x - 1);
                case Direction.N:
                    return (center.y + 1, center.x);
                case Direction.NE:
                    return (center.y + 1, center.x + 1);
                case Direction.W:
                    return (center.y, center.x - 1);
                case Direction.FromCurrent:
                    return (center.y, center.x);
                case Direction.E:
                    return (center.y, center.x + 1);
                case Direction.SW:
                    return (center.y - 1, center.x - 1);
                case Direction.S:
                    return (center.y - 1, center.x);
                case Direction.SE:
                    return (center.y - 1, center.x + 1);

                default:
                    return (0, 0);
            }
        }

        public static (int y, int x) GetCoordinates(this Direction dir, int[] center)
        {
            return dir.GetCoordinates((center[0], center[1]));
        }

        public static Direction GetOppositeDirection(this Direction dir)
        {
            switch (dir)
            {
                case Direction.NW:
                    return Direction.SE;
                case Direction.N:
                    return Direction.S;
                case Direction.NE:
                    return Direction.SW;
                case Direction.W:
                    return Direction.E;
                case Direction.FromCurrent:
                    return Direction.FromCurrent;
                case Direction.E:
                    return Direction.W;
                case Direction.SW:
                    return Direction.NE;
                case Direction.S:
                    return Direction.N;
                case Direction.SE:
                    return Direction.NW;

                default:
                    return Direction.FromCurrent;
            }
        }

        public static (Direction right, Direction left) GetPerpendicularDirections(this Direction dir)
        {
            switch (dir)
            {
                case Direction.NW:
                    return (Direction.NE, Direction.SW);
                case Direction.N:
                    return (Direction.E, Direction.W);
                case Direction.NE:
                    return (Direction.SE, Direction.NW);
                case Direction.W:
                    return (Direction.N, Direction.S);
                case Direction.FromCurrent:
                    return (Direction.FromCurrent, Direction.FromCurrent);
                case Direction.E:
                    return (Direction.S, Direction.N);
                case Direction.SW:
                    return (Direction.NW, Direction.SE);
                case Direction.S:
                    return (Direction.W, Direction.E);
                case Direction.SE:
                    return (Direction.SW, Direction.NE);

                default:
                    return (Direction.FromCurrent, Direction.FromCurrent);
            }
        }
    }
}
