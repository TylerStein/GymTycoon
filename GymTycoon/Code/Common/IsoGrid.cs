using System;
using Microsoft.Xna.Framework;

namespace GymTycoon.Code.Common
{
    internal static class IsoGrid
    {
        public static Point ScreenToGrid(int x, int y, int w, int h)
        {
            return new Point(
                 (int)MathF.Round((x / w + y / h) / 2f),
                 (int)MathF.Round((y / h - x / w) / 2f)
            );
        }

        public static Point GridToScreen(int x, int y, int w, int h)
        {
            return new Point(
                (int)MathF.Round((x - y) * (w / 2f)),
                (int)MathF.Round((x + y) * (h / 2f))
            );
        }

        public static Point GridToScreen(float x, float y, int w, int h)
        {
            return new Point(
                (int)MathF.Round((x - y) * (w / 2f)),
                (int)MathF.Round((x + y) * (h / 2f))
            );
        }

        public static Point ScreenToGrid(Point screen, Point offset, Point tileSize)
        {
            return ScreenToGrid(
                (screen.X - offset.X) * 2,
                (screen.Y - offset.Y) * 2,
                tileSize.X,
                tileSize.Y
            );
        }

        public static Point GridToScreen(Point grid, Point offset, Point tileSize)
        {
            return GridToScreen(grid.X, grid.Y, tileSize.X, tileSize.Y) + offset;
        }

        public static Point GridToScreen(Vector2 grid, Point offset, Point tileSize)
        {
            return GridToScreen(grid.X, grid.Y, tileSize.X, tileSize.Y) + offset;
        }

        public static Point3 IndexToPoint3(int index, int width, int height)
        {
            int area = width * height;
            int z = (int)MathF.Floor((float)index / (float)area);
            int indexInLayer = index % area;
            int x = indexInLayer % width;
            int y = (int)MathF.Floor((float)indexInLayer / (float)height);
            return new Point3(x, y, z);
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

    }
}
