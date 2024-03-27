using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code.Common
{
    public class ScopedName : IEquatable<ScopedName>, IComparable<ScopedName>
    {
        private readonly string[] _parts;
        private string _cachedFullName;

        public ScopedName(string name, char delimiter = '.')
        {
            _parts = name.Split(delimiter);
        }

        public ScopedName(string[] parts)
        {
            _parts = parts;
        }

        public ScopedName(List<string> parts)
        {
            _parts = [.. parts];
        }

        public ScopedName(List<ScopedName> parts)
        {
            _parts = parts.SelectMany((item) => item._parts).ToArray();
        }

        public static ScopedName operator+(ScopedName left, ScopedName right)
        {
            return new ScopedName(left._parts.Concat(right._parts).ToArray());
        }

        public static bool operator ==(ScopedName left, ScopedName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ScopedName left, ScopedName right)
        {
            return !(left.Equals(right));
        }

        public string GetFullName(char delimeter = '.')
        {
            if (_cachedFullName == null)
            {
                _cachedFullName = string.Join(delimeter, _parts);
            }

            return _cachedFullName;
        }

        public string GetName()
        {
            return _parts[^1];
        }

        public bool HasName(string name)
        {
            return _parts[^1] == name;
        }

        public bool Equals(ScopedName other)
        {
            return GetFullName() == other.GetFullName();
        }

        public int CompareTo(ScopedName other)
        {
            return GetFullName().CompareTo(other.GetFullName());
        }

        public override int GetHashCode()
        {
            return GetFullName().GetHashCode();
        }

        public override string ToString()
        {
            return GetFullName();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            throw new NotImplementedException();
        }

        public static bool operator <(ScopedName left, ScopedName right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(ScopedName left, ScopedName right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(ScopedName left, ScopedName right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(ScopedName left, ScopedName right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }
    }
}
