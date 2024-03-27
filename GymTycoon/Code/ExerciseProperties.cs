using System;
using System.Collections.Generic;

namespace GymTycoon.Code
{
    [Flags]
    public enum ExerciseProperties
    {
        None = 0,
        Cardio = 1,
        Quads = 1 << 2,
        Hams = 1 << 3,
        Abs = 1 << 4,
        Triceps = 1 << 5,
        Biceps = 1 << 6,
        Chest = 1 << 7,
        UpperBack = 1 << 8,
        LowerBack = 1 << 9,
        Shoulders = 1 << 10,
        Glutes = 1 << 11,

        Freeweights = 1 << 12,
        Yoga = 1 << 13,
        Stretches = 1 << 14,

        All = ~0,
    }

    public static class ExercisePropertyGroups
    {
        public static ExerciseProperties Push =
            ExerciseProperties.Triceps
            | ExerciseProperties.Shoulders
            | ExerciseProperties.Chest
            | ExerciseProperties.Abs;

        public static ExerciseProperties Pull =
            ExerciseProperties.Biceps
            | ExerciseProperties.UpperBack
            | ExerciseProperties.LowerBack
            | ExerciseProperties.Abs;

        public static ExerciseProperties UpperBody = Push | Pull;

        public static ExerciseProperties LowerBody =
            ExerciseProperties.Quads
            | ExerciseProperties.Hams
            | ExerciseProperties.LowerBack
            | ExerciseProperties.Glutes;

        public static ExerciseProperties Strength = UpperBody | LowerBody;

        public static ExerciseProperties Cardio =
            ExerciseProperties.Cardio | ExerciseProperties.Stretches;


        public static Dictionary<string, ExerciseProperties> Groups = new()
        {
            { "UpperBody", UpperBody },
            { "LowerBody", LowerBody },
            { "Push", Push },
            { "Pull", Pull },
            { "Strength", Strength  },
            { "Cardio", Cardio },
            { "Yoga", ExerciseProperties.Yoga },
        };
    }
}
