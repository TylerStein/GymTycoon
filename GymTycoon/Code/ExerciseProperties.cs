using GymTycoon.Code.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code
{
    //[Flags]
    //public enum ExerciseProperties
    //{
    //    None = 0,
    //    Cardio = 1,
    //    Quads = 1 << 2,
    //    Hams = 1 << 3,
    //    Abs = 1 << 4,
    //    Triceps = 1 << 5,
    //    Biceps = 1 << 6,
    //    Chest = 1 << 7,
    //    UpperBack = 1 << 8,
    //    LowerBack = 1 << 9,
    //    Shoulders = 1 << 10,
    //    Glutes = 1 << 11,

    //    Freeweights = 1 << 12,
    //    Yoga = 1 << 13,
    //    Stretches = 1 << 14,

    //    All = ~0,
    //}

    public static class ExercisePropertyGroups
    {
        public static List<Tag> Push = ["Triceps", "Shoulders", "Chest", "Abs"];
        public static List<Tag> Pull = ["Biceps", "UpperBack", "LowerBack", "Abs"];
        public static List<Tag> UpperBody = [.. Push, .. Pull];
        public static List<Tag> LowerBody = ["Quads", "Hams", "LowerBack", "Glutes"];
        public static List<Tag> Strength = [..UpperBody, ..LowerBody];
        public static List<Tag> Cardio = ["Cardio", "Stretches"];
        public static List<Tag> Yoga = ["Yoga"];

        public static Dictionary<string, List<Tag>> Groups = new()
        {
            { "UpperBody", UpperBody },
            { "LowerBody", LowerBody },
            { "Push", Push },
            { "Pull", Pull },
            { "Strength", Strength  },
            { "Cardio", Cardio },
            { "Yoga", Yoga },
        };

        public static List<Tag> AllDefaultExercises = new HashSet<Tag>([
            ..Strength,
            ..Cardio,
            ..Yoga
        ]).ToList();
    }
}
