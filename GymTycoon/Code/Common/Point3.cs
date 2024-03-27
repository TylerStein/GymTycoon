using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Markup;
using Microsoft.Xna.Framework;

namespace GymTycoon.Code.Common
{
    public struct Point3 : IEquatable<Point3>
    {
        private static readonly Point3 zeroPoint = new(0, 0, 0);
        private static readonly Point3 upPoint = new(0, 0, 1);
        private static readonly Point3 downPoint = new(0, 0, -1);
        private static readonly Point3 forwardPoint = new(0, 1, 0);
        private static readonly Point3 backwardPoint = new(0, -1, 0);
        private static readonly Point3 leftPoint = new(1, 0, 0);
        private static readonly Point3 rightPoint = new(-1, 0, 0);

        public int X;
        public int Y;
        public int Z;

        public static Point3 Zero => zeroPoint;
        public static Point3 Up => upPoint;
        public static Point3 Down => downPoint;
        public static Point3 Forward => forwardPoint;
        public static Point3 Backward => backwardPoint;
        public static Point3 Left => leftPoint;
        public static Point3 Right => rightPoint;

        internal readonly string DebugDisplayString => X + "  " + Y + " " + Z;

        public Point3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3(int value)
        {
            X = value;
            Y = value;
            Z = value;
        }

        public Point3(Point point)
        {
            X = point.X;
            Y = point.Y;
            Z = 0;
        }

        public Point3(Vector3 vector)
        {
            X = (int)vector.X;
            Y = (int)vector.Y;
            Z = (int)vector.Z;
        }

        public Point3(int[] values)
        {
            X = values[0];
            Y = values[1];
            Z = values[2];
        }

        public static Point3 operator +(Point3 value1, Point3 value2)
        {
            return new Point3(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z);
        }

        public static Point3 operator +(Point3 value1, Point value2)
        {
            return new Point3(value1.X + value2.X, value1.Y + value2.Y, value1.Z);
        }

        public static Point3 operator -(Point3 value1, Point3 value2)
        {
            return new Point3(value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z);
        }

        public static Point3 operator -(Point3 value1, Point value2)
        {
            return new Point3(value1.X - value2.X, value1.Y - value2.Y, value1.Z);
        }

        public static Point3 operator *(Point3 value1, Point3 value2)
        {
            return new Point3(value1.X * value2.X, value1.Y * value2.Y, value1.Z * value2.Z);
        }

        public static Point3 operator /(Point3 source, Point3 divisor)
        {
            return new Point3(source.X / divisor.X, source.Y / divisor.Y, source.Z / divisor.Z);
        }

        public static bool operator ==(Point3 a, Point3 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Point3 a, Point3 b)
        {
            return !a.Equals(b);
        }

        public static explicit operator Point(Point3 p)
        {
            return new Point(p.X, p.Y);
        }

        public static explicit operator Point3(Point p)
        {
            return new Point3(p.X, p.Y, 0);
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is Point3 point)
            {
                return Equals(point);
            }

            return false;
        }

        public readonly bool Equals(Point3 other)
        {
            return
                X == other.X
                && Y == other.Y
                && Z == other.Z
            ;
        }

        public override readonly int GetHashCode()
        {
            return (17 * 23 + X.GetHashCode()) * 23 + Y.GetHashCode() * 23 + Z.GetHashCode();
        }

        public override readonly string ToString()
        {
            return "{X:" + X + " Y:" + Y + " Z:" + Z + "}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public readonly void Deconstruct(out int x, out int y, out int z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public readonly float Magnitude()
        {
            return MathF.Sqrt(SqrMagnitude());
        }

        public readonly float SqrMagnitude()
        {
            return X * X + Y * Y + Z * Z;
        }

        public static float Distance(Point3 a, Point3 b)
        {
            return (a - b).Magnitude();
        }

        public static float DistanceSqr(Point3 a, Point3 b)
        {
            return (a - b).SqrMagnitude();
        }

        public static float DistanceXY(Point3 a, Point3 b)
        {
            return ((Point)a - (Point)b).Magnitude();
        }
    }
}
