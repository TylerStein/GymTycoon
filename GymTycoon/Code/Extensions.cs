using GymTycoon.Code.Common;
using Microsoft.Xna.Framework;
using System;

namespace GymTycoon.Code
{
    public static class Extensions
    {
        public static float Magnitude(this Point point)
        {
            return MathF.Sqrt(point.SqrMagnitude());
        }

        public static float SqrMagnitude(this Point point)
        {
            return point.X * point.X + point.Y * point.Y;
        }
    }
}
