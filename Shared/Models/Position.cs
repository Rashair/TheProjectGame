using System;
using System.Collections.Generic;

namespace Shared.Models
{
    public class Position : IEquatable<Position>
    {
        public int Y { get; set; }

        public int X { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Position);
        }

        public bool Equals(Position other)
        {
            return other != null &&
                   Y == other.Y &&
                   X == other.X;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Y, X);
        }

        public static bool operator ==(Position left, Position right)
        {
            return EqualityComparer<Position>.Default.Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        public static implicit operator Position(int[] pos) => new Position
        {
            Y = pos[0],
            X = pos[1],
        };
    }
}
