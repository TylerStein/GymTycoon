using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymTycoon.Code.Common
{
    public class Tag
    {
        private static Dictionary<int, string> _tags = [];
        public static Tag EMPTY = new Tag("");

        public static IEnumerable<string> All()
        {
            foreach (var tag in _tags)
            {
                yield return tag.Value;
            }
        }

        public static List<string> GetAllTags()
        {
            return _tags.Values.ToList();
        }

        public static int CountAllTags()
        {
            return _tags.Count;
        }

        private int _id;
        public Tag(string value)
        {
            _id = value.GetHashCode();
            _tags[_id] = value;
        }

        public override bool Equals(object obj)
        {
            return obj is Tag && (obj as Tag)._id == _id;
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public override string ToString()
        {
            return _tags[_id];
        }

        public static bool operator ==(Tag a, Tag b)
        {
            return a._id == b._id;
        }
        
        public static bool operator !=(Tag a, Tag b)
        {
            return a._id != b._id;
        }

        public static implicit operator string(Tag tag) => tag.ToString();
        public static implicit operator Tag(string value) => new Tag(value);
    }
}
