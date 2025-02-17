using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GymTycoon.Code.Common
{
    internal static class IsoGrid
    {
        public enum Direction
        {
            MIN = 0,
            NE = 1,
            SE = 2,
            SW = 3,
            NW = 4,
            MAX = 5
        }

        public static Point3 ScreenToGridRotation(int screenX, int screenY, int z, int s, Direction direction)
        {
            Point3 grid = ScreenToGrid(screenX, screenY, z, s);

            switch (direction)
            {
                case Direction.NE:
                    return grid;
                case Direction.NW:
                    return new Point3(-grid.X, grid.Y, grid.Z);
                case Direction.SE:
                    return new Point3(grid.X, -grid.Y, grid.Z);
                case Direction.SW:
                    return new Point3(-grid.X, -grid.Y, grid.Z);
                default:
                    return grid;
            }
        }

        public static Point GridToScreenRotation(int x, int y, int z, int s, Direction direction)
        {
            switch (direction)
            {
                case Direction.SE:
                    return GridToScreen(x, -y, z, s);
                case Direction.SW:
                    return GridToScreen(-x, -y, z, s);
                case Direction.NW:
                    return GridToScreen(-x, y, z, s);
                default:
                    return GridToScreen(x, y, z, s);
            }
        }

        public static Point3 ScreenToGrid(int x, int y, int z, int s)
        {
            return new Point3(
                (int)MathF.Round((x + 2 * y) / (float)s),
                (int)MathF.Round((2 * y - x) / (float)s),
                z
            );
        }

        public static Point GridToScreen(int x, int y, int z, int s)
        {
            return new Point(
                (int)MathF.Round((x - y) * (s / 2)),
                (int)MathF.Round(((x + y) * (s / 2) - (z * s)) / 2)
            );
        }

        public static Point GridToScreen(float x, float y, float z, int s)
        {
            return new Point(
                (int)MathF.Round((x - y) * (s / 2f)),
                (int)MathF.Round(((x + y) * (s / 2f) - (z * s)) / 2f)
            );
        }

        public static Point3 ScreenToGrid(Point screen, Point offset, int z, int tileSize)
        {
            return ScreenToGrid(
                (screen.X - offset.X),
                (screen.Y - offset.Y),
                z,
                tileSize
            );
        }

        public static Point GridToScreen(Point3 grid, Point offset, int tileSize)
        {
            return GridToScreen(grid.X, grid.Y, grid.Z, tileSize) + offset;
        }

        public static Point GridToScreen(Vector3 grid, Point offset, int tileSize)
        {
            return GridToScreen(grid.X, grid.Y, grid.Z, tileSize) + offset;
        }

        // TODO: Non-static caching
        private static Dictionary<int, Point3> _indexToPoint3Cache = new Dictionary<int, Point3>();
        public static Point3 IndexToPoint3(int index, int width, int height)
        {
            Point3 result;
            if (!_indexToPoint3Cache.TryGetValue(index, out result))
            {
                int area = width * height;
                int z = (int)MathF.Floor((float)index / (float)area);
                int indexInLayer = index % area;
                int x = indexInLayer % width;
                int y = (int)MathF.Floor((float)indexInLayer / (float)width);
                result = new Point3(x, y, z);
            }
            return result;
        }

        public static int Point3ToIndex(Point3 position, int width, int height)
        {
            return Point3ToIndex(position.X, position.Y, position.Z, width, height);
        }

        public static int Point3ToIndex(int x, int y, int z, int width, int height)
        {
            return x + y * width + z * width * height;
        }

        public static Point IndexToPoint(int index, int width)
        {
            return new Point(index % width, (int)MathF.Floor(index / (float)width));
        }

        public static int PointToIndex(Point point, int width)
        {
            return PointToIndex(point.X, point.Y, width);
        }

        public static int PointToIndex(int x, int y, int width)
        {
            return x + y * width;
        }

        public static float GetDepth(Point3 pos, Point3 rotatedBounds, Point3 bounds)
        {
            float lengthSquared = bounds.X * bounds.X + bounds.Y * bounds.Y + bounds.Z * bounds.Z;
            float distSquared = (pos.X - rotatedBounds.X) * (pos.X - rotatedBounds.X) + (pos.Y - rotatedBounds.Y) * (pos.Y - rotatedBounds.Y) + (pos.Z - rotatedBounds.Z) * (pos.Z - rotatedBounds.Z);
            float depth = (distSquared / lengthSquared);
            return Math.Clamp(depth, 0f, 1f);
        }

        public static float GetDepth(Vector3 pos, Point3 rotatedBounds, Point3 bounds)
        {
            float lengthSquared = bounds.X * bounds.X + bounds.Y * bounds.Y + bounds.Z * bounds.Z;
            float distSquared = (pos.X - rotatedBounds.X) * (pos.X - rotatedBounds.X) + (pos.Y - rotatedBounds.Y) * (pos.Y - rotatedBounds.Y) + (pos.Z - rotatedBounds.Z) * (pos.Z - rotatedBounds.Z);
            float depth = (distSquared / lengthSquared);
            return Math.Clamp(depth, 0f, 1f);
        }
    }
}
