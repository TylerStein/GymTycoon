using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GymTycoon.Code.Data
{
    public class BehaviorExercisesData
    {
        [JsonProperty]
        public string Name;

        [JsonProperty]
        public int Efficiency;

        [JsonProperty]
        public int Fun;

        [JsonProperty]
        public string[] Sprites;

        [JsonProperty]
        public Dictionary<string, int> Fitness;
    }

    public class BehaviorData
    {
        [JsonProperty]
        public string Name;

        [JsonProperty]
        public string Script;

        [JsonProperty]
        public string[] RequiredOwnerSpriteAliases;

        [JsonProperty]
        public BehaviorExercisesData Exercise;

        public MetaData MetaData;
    }
    public class Exercise
    {
        public string Name;
        public int Efficiency;
        public int Fun;
        public ScopedName[] Sprites;
        public Dictionary<ExerciseProperties, int> Fitness;
        public ExerciseProperties AvailableExerciseProperties;

        public static Exercise Load(BehaviorExercisesData data, ContentManager content)
        {
            ExerciseProperties availableExerciseProps = ExerciseProperties.None;
            Dictionary<ExerciseProperties, int> fitness = [];
            foreach (var item in data.Fitness)
            {
                ExerciseProperties prop;
                if (Enum.TryParse(item.Key, out prop))
                {
                    fitness[prop] = item.Value;
                    availableExerciseProps |= prop;
                }
            }

            Exercise exercise = new Exercise()
            {
                Name = data.Name,
                Efficiency = data.Efficiency,
                Fun = data.Fun,
                Sprites = data.Sprites.Select((sprite) => new ScopedName(sprite)).ToArray(),
                AvailableExerciseProperties = availableExerciseProps,
                Fitness = fitness,

            };

            return exercise;
        }
    }

    public class Behavior
    {
        public ScopedName Name;
        public ScopedName Script;
        public ScopedName[] RequiredOwnerSpriteAliases;
        public Exercise Exercise;

        public static Behavior Load(BehaviorData data, ContentManager content)
        {
            Behavior obj = new Behavior()
            {
                Name = new ScopedName(new string[] { data.MetaData.Type, data.MetaData.Package, data.Name }),
                Script = new ScopedName(data.Script),
                Exercise = data.Exercise == null ? null : Exercise.Load(data.Exercise, content)
            };

            return obj;
        }

        public override string ToString()
        {
            return Name.GetFullName();
        }
    }
}
