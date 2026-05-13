using System;
using UnityEngine;

namespace ProjectXX.Domain.Inventory
{
    [Serializable]
    public struct ProjectXXGridSize : IEquatable<ProjectXXGridSize>
    {
        [SerializeField, Min(1)] private int width;
        [SerializeField, Min(1)] private int height;

        public ProjectXXGridSize(int width, int height)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
        }

        public int Width => Mathf.Max(1, width);
        public int Height => Mathf.Max(1, height);
        public int Area => Width * Height;
        public ProjectXXGridSize Rotated => new ProjectXXGridSize(Height, Width);

        public static ProjectXXGridSize One => new ProjectXXGridSize(1, 1);

        public bool Equals(ProjectXXGridSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is ProjectXXGridSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}
