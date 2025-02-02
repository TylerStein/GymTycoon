using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code.AI
{
    public class Blackboard
    {
        public Dictionary<string, dynamic> Dictionary = [];
        public Stack<dynamic> Stack = [];

        public dynamic this[string key]
        {
            get => Dictionary[key];
            set => Dictionary[key] = value;
        }

        public void Clear()
        {
            Dictionary.Clear();
            Stack.Clear();
        }

        public bool ContainsKey(string key)
        {
            return Dictionary.ContainsKey(key);
        }

        public void SetValue(string key, dynamic value)
        {
            Dictionary[key] = value;
        }

        public void ClearValue(string key)
        {
            Dictionary.Remove(key);
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            dynamic obj;
            if (Dictionary.TryGetValue(key, out obj))
            {
                if (obj is T)
                {
                    value = (T)obj;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public T GetValueWithDefault<T>(string key, T defaultValue)
        {
            T value;
            if (TryGetValue(key, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public IEnumerable<KeyValuePair<string, dynamic>> GetDictValues()
        {
            foreach (KeyValuePair<string, dynamic> item in Dictionary)
            {
                yield return item;
            }
        }

        public void PushValue(dynamic value)
        {
            Stack.Push(value);
        }

        public bool TryPopValue<T>(out T value)
        {
            if (Stack.Count > 0)
            {
                dynamic obj = Stack.Pop();
                if (obj is T)
                {
                    value = (T)obj;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public Stack<dynamic>.Enumerator GetStackEnumerator()
        {
            return Stack.GetEnumerator();
        }
    }

}
