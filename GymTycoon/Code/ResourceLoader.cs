using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using GymTycoon.Code.Data;

namespace GymTycoon.Code
{
    public class MetaData
    {
        [JsonProperty]
        public string Type;
        [JsonProperty]
        public string Package;
        [JsonProperty]
        public JObject Data;
    }

    public class ResourceLoader
    {
        private List<string> _metadataPaths;
        public List<SpriteData> SpriteData;
        public List<DynamicObjectData> DynamicObjectData;
        public List<BehaviorData> BehaviorData;
        public List<ScenarioData> ScenarioData;

        public HashSet<SpriteData> Names;

        public ResourceLoader()
        {
            _metadataPaths = [];

            SpriteData = [];
            DynamicObjectData = [];
            BehaviorData = [];
            ScenarioData = [];
        }

        public void LoadAllMetadata(string root)
        {
            _metadataPaths.Clear();

            SpriteData.Clear();
            DynamicObjectData.Clear();
            BehaviorData.Clear();
            ScenarioData.Clear();

            if (Directory.Exists(root))
            {
                foreach(string path in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
                {
                    _metadataPaths.Add(path);
                }
            }

            foreach (var path in _metadataPaths)
            {
                string text = File.ReadAllText(path);
                MetaData metadata = JsonConvert.DeserializeObject<MetaData>(text);
                switch (metadata.Type)
                {
                    case "Sprite":
                        SpriteData spriteData = metadata.Data.ToObject<SpriteData>();
                        spriteData.MetaData = metadata;
                        SpriteData.Add(spriteData);
                        break;
                    case "DynamicObject":
                        DynamicObjectData dynamicObjectData = metadata.Data.ToObject<DynamicObjectData>();
                        dynamicObjectData.MetaData = metadata;
                        DynamicObjectData.Add(dynamicObjectData);
                        break;
                    case "Behavior":
                        BehaviorData behaviorData = metadata.Data.ToObject<BehaviorData>();
                        behaviorData.MetaData = metadata;
                        BehaviorData.Add(behaviorData);
                        break;
                    case "Scenario":
                        ScenarioData scenarioData = metadata.Data.ToObject<ScenarioData>();
                        scenarioData.MetaData = metadata;
                        ScenarioData.Add(scenarioData);
                        break;
                    default:
                        throw new Exception($"Unexpected data Type '{metadata.Type}'");

                }
            }
        }
    }
}
