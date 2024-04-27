using GymTycoon.Code.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code.Data
{
    public struct Schedule
    {
        public static Dictionary<string, Schedule> DefaultTimeWindows = new()
        {
            // { "EarlyMorning", new Schedule(2, 4) },
            { "Morning", new Schedule(5, 8) },
            { "Midday", new Schedule(11, 12 + 2) },
            { "Afternoon", new Schedule(4, 12 + 8) },
            { "Evening", new Schedule(12 + 7, 12 + 10) },
            // { "Midnight", new Schedule(12 + 11, 12 + 1) },
            { "Daytime", new Schedule(6, 12 + 6) },
            // { "Nighttime", new Schedule(12 + 7, 2) },
        };

        public int TimeWindowFrom = 8;
        public int TimeWindowTo = 12 + 8;

        public Schedule(int timeWindowFrom, int timeWindowTo)
        {
            TimeWindowTo = timeWindowFrom;
            TimeWindowTo = timeWindowTo;
        }

        public Schedule(Tuple<int, int> timeWindow) : this(timeWindow.Item1, timeWindow.Item2) { }

        public static bool TimeInWindow(int hour, int from, int to)
        {
            if (from >= to)
            {
                return hour >= from || hour <= to;
            }
            else
            {
                return hour >= from && hour <= to;
            }

        }

        public bool TimeInWindow(int hour)
        {
            return TimeInWindow(hour, TimeWindowFrom, TimeWindowTo);
        }
    }

    public struct Routine
    {
        public static Dictionary<string, List<Tag>[]> DefaultRoutines = new()
        {
            { "UpperLower", new List<Tag>[] { ExercisePropertyGroups.UpperBody, ExercisePropertyGroups.LowerBody } },
            { "PushPullLower", new List<Tag>[] { ExercisePropertyGroups.Push, ExercisePropertyGroups.Pull, ExercisePropertyGroups.LowerBody } },
            { "FulLBody", new List<Tag>[] { ExercisePropertyGroups.Strength } },
            { "Cardio", new List<Tag>[] { ExercisePropertyGroups.Cardio } },
            { "Yoga", new List<Tag>[] { ExercisePropertyGroups.Yoga } },
        };

        private List<Tag>[] _data;
        private int _index = 0;

        public Routine(string defaultRoutine) : this(DefaultRoutines[defaultRoutine]) { }

        public Routine(List<Tag>[] routine)
        {
            _data = routine;
            _index = 0;
        }

        public List<Tag> Pop()
        {
            int idx = _index;
            _index++;
            if (_index >= _data.Length)
            {
                _index = 0;
            }
            return _data[idx];
        }

        public void RandomIndex()
        {
            _index = Random.Shared.Next(0, _data.Length - 1);
        }
    }

    public class NeedFilter
    {
        public List<Tag> Tags = [];
        public float Modifier = 0;
    }

    public class TraitData
    {
        public string Name;
        public int Cost;
        public Schedule? Schedule;
        public Routine? Routine;
        public WealthTier? WealthTier;
        public float? SpeedModifier;
        public float? TidynessModifier;
        public Dictionary<string, float> BasicNeedModifiers;
        public Dictionary<string, int> SpawnNeedModifiers;
        public Dictionary<string, NeedFilter> AdvancedNeedModifiers;
        public Dictionary<string, float> DecayRateModifiers;
    }

    public static class DefaultTraits
    {
        public static Dictionary<string, List<TraitData>> TraitGroups = new()
        {
            { "Social", new List<TraitData>() { TExtrovert, TIntrovert } },
            { "Speed", new List<TraitData>() { TFast, TSlow } },
            { "Tidy", new List<TraitData>() { TTidy, TMessy } },
            { "Energy", new List<TraitData>() { TEnergetic, TSluggish } },
        };

        public static TraitData GetRandomScheduleTraitForTime(int hour)
        {
            List<string> opts = [];
            foreach (var kvp in Schedule.DefaultTimeWindows)
            {
                if (kvp.Value.TimeInWindow(hour))
                {
                    opts.Add(kvp.Key);
                }
            }

            if (opts.Count == 0)
            {
                throw new Exception($"No schedule exists containing the time: {hour}");
            }

            string key = opts[Random.Shared.Next(0, opts.Count)];

            return new TraitData()
            {
                Name = $"Schedule: {key}",
                Cost = 0,
                Schedule = Schedule.DefaultTimeWindows[key],
            };
        }

        public static TraitData GetRandomRoutine()
        {
            int rng = Random.Shared.Next(0, Routine.DefaultRoutines.Count);
            string key = Routine.DefaultRoutines.Keys.ToList()[rng];
            Routine routine = new Routine(key);
            return new TraitData()
            {
                Name = $"Routine: {key}",
                Cost = 0,
                Routine = routine,
            };
        }

        public static TraitData GetRandomWealthTier()
        {
            WealthTier tier = (WealthTier)Random.Shared.Next((int)WealthTier.Low, (int)WealthTier.Premium);
            return new TraitData()
            {
                Name = $"Wealth: {tier}",
                Cost = 0,
                WealthTier = tier,
            };
        }

        public static TraitData TIntrovert = new TraitData()
        {
            Name = "Introvert",
            Cost = 1,
            BasicNeedModifiers = new() { { "Social", -0.5f } }, // social scales at -1x (negative influence)
            SpawnNeedModifiers = new() { { "Social", -100 } } // spawn with -100 social
        };

        public static TraitData TExtrovert = new TraitData()
        {
            Name = "Extrovert",
            Cost = 1,
            BasicNeedModifiers = new() { { "Social", 1.5f } }, // social scales at 1.5x
            SpawnNeedModifiers = new() { { "Social", 100 } } // spawn with +100 social
        };

        public static TraitData TFast = new TraitData()
        {
            Name = "Fast",
            Cost = 1,
            SpeedModifier = 1.5f,
        };

        public static TraitData TSlow = new TraitData()
        {
            Name = "Slow",
            Cost = 1,
            SpeedModifier = 0.5f
        };

        public static TraitData TMessy = new TraitData()
        {
            Name = "Messy",
            Cost = 1,
            TidynessModifier = 0.5f
        };

        public static TraitData TTidy = new TraitData()
        {
            Name = "Tidy",
            Cost = 1,
            TidynessModifier = 1.5f
        };

        public static TraitData TEnergetic = new TraitData()
        {
            Name = "Energetic",
            Cost = 1,
            DecayRateModifiers = new() { { "Rest", 1.5f } }, // Rest decreases at 1.5x (idle, resting)
            BasicNeedModifiers = new() { { "Rest", 0.75f } }, // Rest increases at 0.75x (exercising)
        };

        public static TraitData TSluggish = new TraitData()
        {
            Name = "Sluggish",
            Cost = 1,
            DecayRateModifiers = new() { { "Rest", 0.75f } }, // Rest decreases at 0.75x (idle, resting)
            BasicNeedModifiers = new() { { "Rest", 1.75f } }, // Rest increases at 1.5x (exercising)
        };
    }
}
