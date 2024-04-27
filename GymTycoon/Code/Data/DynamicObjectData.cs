using GymTycoon.Code.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;

namespace GymTycoon.Code.Data
{
    public enum DynamicObjectCategory
    {
        None,               /** Unset */
        Decoration,         /** No special behavior */
        Equipment,          /** Interactable equipment */
        Vendor,             /** Vendor that does not require staff to operate */

        Reception,          /** A place guests check in and buy memberships */

        Locker,             /** Locker for guests to store things */
        Toilet,             /** Toilet for guests to releive themselves */
        Bench,              /** A place guests can sit and relax */

        Litter,             /** Something to be cleaned up */
        TrashBin            /** A place where litter should be dropped */
    }

    public class DynamicObjectData
    {
        public MetaData MetaData;

        [JsonProperty]
        public readonly string Name;

        [JsonProperty]
        public readonly int Beauty;

        [JsonProperty]
        public readonly int Width;

        [JsonProperty]
        public readonly int Height;

        [JsonProperty]
        public readonly bool Holdable;

        [JsonProperty]
        public readonly bool Navigable;

        [JsonProperty]
        public readonly int BuildCost;

        [JsonProperty]
        public readonly bool Purchaseable;

        [JsonProperty]
        public readonly string[] Behaviors;

        [JsonProperty]
        public readonly string Sprite;

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public readonly DynamicObjectCategory Category;

        /** Equipment Properties */

        [JsonProperty]
        [JsonConverter(typeof(PointConverter))]
        public readonly Point[] GuestSlots;

        [JsonProperty]
        public int FunModifier;

        [JsonProperty]
        public readonly int FitnessModifier;

        [JsonProperty]
        public readonly int DifficultyModifier;

        /** Vendor Properties */

        [JsonProperty]
        [JsonConverter(typeof(PointConverter))]
        public readonly Point[] StaffSlots;

        [JsonProperty]
        public readonly IDictionary<string, string> Sprites;

        [JsonProperty]
        public readonly string[] Dispenses = [];

        [JsonProperty]
        public readonly int[] DispenseQuantity = [];
    }

    public class DynamicObject
    {
        public ScopedName Name;

        public int Width;
        public int Height;

        public bool Holdable;
        public bool Navigable;
        public bool Purchaseable;

        public int Beauty;
        public int FitnessModifier;
        public int FunModifier;
        public int DifficultyModifier;
        public int BuildCost;

        public DynamicObjectCategory Category;

        private Point[][] _guestSlots;
        private Point[][] _staffSlots;

        public ScopedName[] Behaviors;
        public ScopedName[] DispensedObjects;
        public int[] DispenseQuantity;

        public Dictionary<string, ScopedName> SpriteAliases;

        /** Common Properties */

        public static DynamicObject Load(DynamicObjectData data, ContentManager content)
        {
            Dictionary<string, ScopedName> spriteAliases = [];
            foreach (var item in data.Sprites)
            {
                spriteAliases[item.Key] = new ScopedName(item.Value);
            }

            DynamicObject obj = new DynamicObject()
            {
                Name = new ScopedName(new string[] { data.MetaData.Type, data.MetaData.Package, data.Name }),
                Width = data.Width,
                Height = data.Height,
                Holdable = data.Holdable,
                Navigable = data.Navigable,
                Beauty = data.Beauty,
                FitnessModifier = data.FitnessModifier,
                FunModifier = data.FunModifier,
                DifficultyModifier = data.DifficultyModifier,
                BuildCost = data.BuildCost,
                Category = data.Category,
                _guestSlots = GenerateSlots(data.GuestSlots),
                _staffSlots = GenerateSlots(data.StaffSlots),
                Behaviors = data.Behaviors.Select((behavior) => new ScopedName(behavior)).ToArray(),
                SpriteAliases = spriteAliases,
                DispensedObjects = data.Dispenses.Select((dynamicObject) => new ScopedName(dynamicObject)).ToArray(),
                DispenseQuantity = data.DispenseQuantity
            };

            return obj;
        }

        public static Point[][] GenerateSlots(Point[] southSlots)
        {
            Point[][] points =
            [
                // north (-Y)
                southSlots.Select((dir) => new Point(-dir.X, -dir.Y)).ToArray(),
                // south (+Y)
                southSlots,
                // west (-X)
                southSlots.Select((dir) => new Point(dir.Y, -dir.X)).ToArray(),
                // east (+X)
                southSlots.Select((dir) => new Point(-dir.Y, dir.X)).ToArray(),
            ];

            return points;
        }

        public Point[] GetGuestSlots(Direction direction)
        {
            return _guestSlots[(int)direction];
        }

        public Point[] GetStaffSlots(Direction direction)
        {
            return _staffSlots[(int)direction];
        }

        public int GetNumGuestSlots()
        {
            return _guestSlots[0].Length;
        }

        public override string ToString()
        {
            return GetFullName();
        }

        public string GetFullName()
        {
            return Name.GetFullName();
        }

        public string GetName()
        {
            return Name.GetName();
        }
    }
}
