using GymTycoon.Code.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

namespace GymTycoon.Code.AI
{
    public enum NeedCategory
    {
        Basic = 0,
        Environmental = 1
    }

    public struct HappinessFunction
    {
        private float _a;
        private float _b;
        private float _c;
        public static HappinessFunction Flat => new HappinessFunction(1f, 0f, -1f);
        public static HappinessFunction Linear => new HappinessFunction(1000f, 1f, 0f);
        public static HappinessFunction Exponential => new HappinessFunction(1000f, 2f, 0f);

        public HappinessFunction(float a, float b, float c)
        {
            _a = a;
            _b = b;
            _c = c;
        }

        public float Evaluate(float x)
        {
            return MathF.Pow(x / _a, _b) + _c;
        }

    }


    public struct NeedDef
    {
        public string Name;
        public NeedCategory Category;

        public int IdleChangeRate;
        public int MinValue = Needs.MinValue;
        public int MaxValue = Needs.MaxValue;

        public HappinessFunction HappinessFunction;
        
        public NeedDef()
        {
            Name = string.Empty;
            Category = NeedCategory.Basic;
            IdleChangeRate = 0;
            HappinessFunction = HappinessFunction.Flat;
        }
        
        public NeedDef(string name, NeedCategory category, HappinessFunction happinessFn, int idleChangeRate = 0, int minValue = Needs.MinValue, int maxValue = Needs.MaxValue)
        {
            Name = name;
            Category = category;
            HappinessFunction = happinessFn;
            IdleChangeRate = idleChangeRate;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }

    public class NeedsManager
    {
        Dictionary<Tag, NeedDef> _needDefs = [];

        public static List<NeedDef> DefaultNeedDefs = [
            ..ExercisePropertyGroups.AllDefaultExercises.Select((tag) => new NeedDef(tag, NeedCategory.Basic, HappinessFunction.Flat)),

            new NeedDef("Toilet", NeedCategory.Basic, HappinessFunction.Exponential, 1, 0, Needs.MaxValue),
            new NeedDef("Rest", NeedCategory.Basic, HappinessFunction.Linear, -1, 0, Needs.MaxValue),
            new NeedDef("Thirst", NeedCategory.Basic, HappinessFunction.Linear, -1, 0, Needs.MaxValue),

            new NeedDef("Beauty", NeedCategory.Environmental, HappinessFunction.Linear, 0),
            new NeedDef("Social", NeedCategory.Environmental, HappinessFunction.Linear, 1),
        ];

        public void AddNeedDefs(ICollection<NeedDef> needDefs)
        {
            foreach (var needDef in needDefs)
            {
                AddNeedDef(needDef);
            }
        }

        public void AddNeedDef(NeedDef needDef)
        {
            Debug.Assert(_needDefs.ContainsKey(needDef.Name) == false, $"NeedDef {needDef.Name} is already registered!");
            _needDefs[needDef.Name] = needDef;
        }
        public Needs CreateNeeds()
        {
            return new Needs(_needDefs);
        }

        public int GetIdleChangeRate(Tag name)
        {
            return _needDefs[name].IdleChangeRate;
        }

        public NeedCategory GetCategory(Tag name)
        {
            return _needDefs[name].Category;
        }

        public float EvaluateHappinessFunction(Tag name, float value)
        {
            return _needDefs[name].HappinessFunction.Evaluate(value);
        }

        public int Clamp(Tag name, int value)
        {
            return MathHelper.Clamp(value, _needDefs[name].MinValue, _needDefs[name].MaxValue);
        }
    }

    public class Needs
    {
        public const int MinValue = int.MinValue;
        public const int MaxValue = int.MaxValue;

        private Dictionary<Tag, int> _values = [];
        private Dictionary<Tag, int> _lastDelta = [];

        public int this[Tag key]
        {
            get => _values[key];
            set => SetValue(key, value);
        }

        public Needs(Dictionary<Tag, NeedDef> needDefinitions)
        {
            _values = [];
            _lastDelta = [];
            foreach (var kvp in needDefinitions)
            {
                _values[kvp.Key] = 0;
                _lastDelta[kvp.Key] = 0;
            }
        }

        public void SetValue(Tag key, int value)
        {
            int lastValue = _values[key];
            _values[key] = MathHelper.Clamp(value, MinValue, MaxValue);

            int delta = (_values[key] - lastValue);
            if (_lastDelta.ContainsKey(key))
            {
                _lastDelta[key] += delta;
            }
            else
            {
                _lastDelta[key] = delta;
            }
        }

        public bool TryGetValue(Tag key, out int value)
        {
            return _values.TryGetValue(key, out value);
        }

        public void AddValue(Tag key, int value)
        {
            SetValue(key, _values[key] + value);
        }

        public int GetLastDelta(Tag key)
        {
            return _lastDelta[key];
        }

        public void ClearLastDeltas()
        {
            _lastDelta.Clear();
        }

        public bool HasNeed(Tag key)
        {
            return _values.ContainsKey(key);
        }

        public IEnumerable<KeyValuePair<Tag, int>> All()
        {
            foreach (var kvp in _values)
            {
                yield return kvp;
            }
        }
    }
}
