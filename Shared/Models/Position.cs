using System;
using System.Collections.Generic;

namespace Shared.Models
{
    public struct Position : IEquatable<Position>
    {
        public int Y { get; set; }

        public int X { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Position)
            {
                return Equals((Position)obj);
            }

            return false;
        }

        public bool Equals(Position other)
        {
            return Y == other.Y &&
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
