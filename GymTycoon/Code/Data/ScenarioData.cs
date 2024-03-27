using GymTycoon.Code.Common;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymTycoon.Code.Data
{
    public class ScenarioData
    {
        public MetaData MetaData;

        [JsonProperty]
        public string Name;

        [JsonProperty]
        public int StartingCapital;

        [JsonProperty]
        public string Map;

        [JsonProperty]
        public bool InfiniteMoney;

        [JsonProperty]
        public int StartingYear;

        [JsonProperty]
        public int StartingMonth;

        [JsonProperty]
        public int StartingDay;

        [JsonProperty]
        public int InitialGymRating;

        [JsonProperty]
        public float InitialBoost;

        [JsonProperty]
        public float InitialBoostDecayRate;

        [JsonProperty]
        public float MarketingDecayRate;

        [JsonProperty]
        public float NewEquipmentDecayRate;
    }

   public class Scenario
    {
        public ScopedName Name;
        public int StartingCapital;
        public string Map;
        public bool InfiniteMoney;
        public DateOnly StartDate;
        public int InitialGymRating;
        public float InitialBoost;
        public float InitialBoostDecayRate;
        public float MarketingDecayRate;
        public float NewEquipmentDecayRate;

        public static Scenario Load(ScenarioData data, ContentManager content)
        {
            Scenario scenario = new Scenario()
            {
                Name = new ScopedName(new string[] { data.MetaData.Type, data.MetaData.Package, data.Name }),
                StartingCapital = data.StartingCapital,
                Map = data.Map,
                InfiniteMoney = data.InfiniteMoney,
                StartDate = new DateOnly(data.StartingYear, data.StartingMonth, data.StartingDay),
                InitialGymRating = data.InitialGymRating,
                InitialBoost = data.InitialBoost,
                InitialBoostDecayRate = data.InitialBoostDecayRate,
                MarketingDecayRate = data.MarketingDecayRate,
                NewEquipmentDecayRate = data.NewEquipmentDecayRate
    };

            return scenario;
        }
    }
}
