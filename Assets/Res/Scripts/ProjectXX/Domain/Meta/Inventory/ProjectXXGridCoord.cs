using System;
using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [Serializable]
    public struct ProjectXXGridCoord : IEquatable<ProjectXXGridCoord>
    {
        [SerializeField] private int x;
        [SerializeField] private int y;

        public ProjectXXGridCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int X => x;
        public int Y => y;

        public ProjectXXGridCoord Translate(int deltaX, int deltaY)
        {
            return new ProjectXXGridCoord(x + deltaX, y + deltaY);
        }

        public bool Equals(ProjectXXGridCoord other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is ProjectXXGridCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
}
