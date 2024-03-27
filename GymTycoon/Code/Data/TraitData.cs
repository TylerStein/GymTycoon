using GymTycoon.Code.AI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code.Data
{

    public struct NeedModifier
    {
        public Dictionary<NeedType, float> Modifier = [];

        public NeedModifier()
        {
        }

        public static NeedModifier operator +(NeedModifier value1, NeedModifier value2)
        {
            return value1 + value2.Modifier;
        }

        public static NeedModifier operator +(NeedModifier value1, Dictionary<NeedType, float> value2)
        {
            NeedModifier mod = new();
            foreach (var kvp in value1.Modifier)
            {
                if (value2.ContainsKey(kvp.Key))
                {
                    mod.Modifier[kvp.Key] = kvp.Value + value2[kvp.Key];
                }
            }

            return mod;
        }
    }

    public struct Preferences
    {
        public float Beauty = 1f;
        public float Tidy = 1f;
        public float Speed = 1f;
        public float Social = 1f;
        public Dictionary<ExerciseProperties, float> Exercise = new Dictionary<ExerciseProperties, float>()
        {
            { ExerciseProperties.Cardio, 1f },
            { ExerciseProperties.Quads, 1f },
            { ExerciseProperties.Hams, 1f },
            { ExerciseProperties.Abs, 1f },
            { ExerciseProperties.Triceps, 1f },
            { ExerciseProperties.Biceps, 1f },
            { ExerciseProperties.Chest , 1f },
            { ExerciseProperties.UpperBack, 1f },
            { ExerciseProperties.LowerBack, 1f },
            { ExerciseProperties.Shoulders, 1f },
            { ExerciseProperties.Glutes, 1f },
            { ExerciseProperties.Freeweights, 1f },
            { ExerciseProperties.Yoga, 1f },
            { ExerciseProperties.Stretches, 1f },
        };

        public Preferences() { }

        public static Preferences operator *(Preferences value1, Preferences value2)
        {
            Preferences res = new();
            res.Beauty = value1.Beauty * value2.Beauty;
            res.Tidy = value1.Tidy * value2.Tidy;
            res.Speed = value1.Speed * value2.Speed;
            foreach (var item in res.Exercise)
            {
                res.Exercise[item.Key] = value1.Exercise[item.Key] * value2.Exercise[item.Key];
            }
            return res;
        }

        public static Preferences operator +(Preferences value1, Preferences value2)
        {
            Preferences res = new();
            res.Beauty = value1.Beauty + value2.Beauty;
            res.Tidy = value1.Tidy + value2.Tidy;
            res.Speed = value1.Speed + value2.Speed;
            foreach (var item in res.Exercise)
            {
                res.Exercise[item.Key] = value1.Exercise[item.Key] + value2.Exercise[item.Key];
            }
            return res;
        }
    }

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
        public static Dictionary<string, ExerciseProperties[]> DefaultRoutines = new()
        {
            { "UpperLower", new ExerciseProperties[]{ ExercisePropertyGroups.UpperBody, ExercisePropertyGroups.LowerBody } },
            { "PushPullLower", new ExerciseProperties[]{ ExercisePropertyGroups.Push, ExercisePropertyGroups.Pull, ExercisePropertyGroups.LowerBody } },
            { "FulLBody", new ExerciseProperties[] { ExercisePropertyGroups.Strength } },
            { "Cardio", new ExerciseProperties[] { ExercisePropertyGroups.Cardio } },
            { "Yoga", new ExerciseProperties[] { ExerciseProperties.Yoga } },
        };

        List<ExerciseProperties> Data;
        public Routine(string defaultRoutine) : this(DefaultRoutines[defaultRoutine]) { }

        public Routine(ExerciseProperties[] routine)
        {
            Data = new List<ExerciseProperties>(routine);
        }
    }

    public class TraitData
    {
        public string Name;
        public int Cost;
        public Preferences? PreferenceModifier;
        public Dictionary<NeedType, float> NeedModifier;
        public Schedule? Schedule;
        public Routine? Routine;
        public WealthTier? WealthTier;
    }

    public static class DefaultTraits
    {
        public static Dictionary<string, List<TraitData>> TraitGroups = new()
        {
            { "Social", new List<TraitData>() { TExtrovert, TIntrovert } },
            { "Speed", new List<TraitData>() { TFast, TSlow} },
            { "Tidy", new List<TraitData>() { TTidy, TMessy} },
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
                Name = $"Routine({key})",
                Cost = 0,
                Schedule = Schedule.DefaultTimeWindows[key],
            };
        }

        public static TraitData TIntrovert = new TraitData()
        {
            Name = "Introvert",
            Cost = 1,
            PreferenceModifier = new() { Social = -1f },
        };

        public static TraitData TExtrovert = new TraitData()
        {
            Name = "Extrovert",
            Cost = 1,
            PreferenceModifier = new() { Social = 2f },
        };

        public static TraitData TFast = new TraitData()
        {
            Name = "Fast",
            Cost = 1,
            PreferenceModifier = new() { Speed = 1.5f },
        };

        public static TraitData TSlow = new TraitData()
        {
            Name = "Slow",
            Cost = 1,
            PreferenceModifier = new() { Speed = 0.5f },
        };

        public static TraitData TMessy = new TraitData()
        {
            Name = "Messy",
            Cost = 1,
            PreferenceModifier = new() { Tidy = -1f },
        };

        public static TraitData TTidy = new TraitData()
        {
            Name = "Tidy",
            Cost = 1,
            PreferenceModifier = new() { Tidy = 1.5f },
        };
    }
}
