using GymTycoon.Code.Common;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace GymTycoon.Code
{
    [Flags]
    public enum TileProperties
    {
        None = 0,
        Visible = 1,
        Navigable = 1 << 1,
        Spawn = 1 << 2,
        Editor = 1 << 3,
        Transparency = 1 << 4,
    }

    /// <summary>
    /// Flyweight class intended to populate the object grid.
    /// </summary>
    public class TileType : IComparable<TileType>, IEquatable<TileType>
    {
        private static TileType _Empty = new TileType(0, new ScopedName("TileType.Empty"), TileProperties.None);
        public static TileType Empty => _Empty;

        public readonly ushort ID;
        public readonly ScopedName Name;
        public readonly TileProperties Properties;

        public TileType(
            ushort id,
            ScopedName name,
            TileProperties properties)
        {
            ID = id;
            Name = name;
            Properties = properties;
        }

        public int CompareTo(TileType other)
        {
            return ID - other.ID;
        }

        public bool Equals(TileType other)
        {
            return ID == other.ID;
        }
        public override int GetHashCode()
        {
            return ID;
        }

        public override string ToString()
        {
            return "{ID:" + ID + " Name:" + Name.GetFullName() + "}";
        }

        public string GetFullName()
        {
            return Name.GetFullName();
        }

        public string GetName()
        {
            return Name.GetName();
        }

        public bool HasProperty(TileProperties property)
        {
            return (Properties & property) != 0;
        }
    }
}
