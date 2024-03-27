using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
namespace GymTycoon.Code.Common
{
    public static class GridShapes
    {
        public static IEnumerable<Point> GetPointsInCircle(Point center, int radius)
        {
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                for (int y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) < radius * radius)
                    {
                        yield return new Point(x, y);
                    }
                }
            }
        }

        public static IEnumerable<Point3> GetPointsInSphere(Point3 center, int radius)
        {
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                for (int y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    for (int z = center.Z - radius; z <= center.Z + radius; z++)
                    {
                        if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) + (z - center.Z) * (z - center.Z) < radius * radius)
                        {
                            yield return new Point3(x, y, z);
                        }
                    }
                }
            }
        }

        public static int GetPointsInCircle(Point center, int radius, Point[] points, int max)
        {
            int i = 0;
            foreach (var point in GetPointsInCircle(center, radius))
            {
                points[i++] = point;
                if (i >= max)
                {
                    break;
                }
            }

            return i;
        }

        public static int GetPointsInSphere(Point3 center, int radius, Point3[] points, int max)
        {
            int i = 0;
            foreach (var point in GetPointsInSphere(center, radius))
            {
                points[i++] = point;
                if (i >= max)
                {
                    break;
                }
            }

            return i;
        }
    }
}
