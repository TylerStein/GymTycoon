using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using GymTycoon.Code.Common;
using System.IO;
using GymTycoon.Code.Data;
using GymTycoon.Code.AI;

namespace GymTycoon.Code
{

    /// <summary>
    /// The Resources class is responsible for loading storing data for various types.
    /// It also tries to resolve references, such as DynamicObjects requiring specific Sprites or Behaviors.
    /// Most data is stored in dictionaries with ScopedName keys for lookup.
    /// It does not create or manage any instances of resources.
    /// </summary>
    public class Resources
    {
        private readonly Dictionary<ScopedName, Sprite> _sprites = [];
        private readonly Dictionary<ushort, TileType> _tileTypes = [];
        private readonly Dictionary<ushort, string> _texturePathsPendingLoad = [];
        private readonly Dictionary<ushort, Texture2D> _tileTextures = [];
        private readonly Dictionary<ScopedName, DynamicObject> _dynamicObjects = [];
        private readonly Dictionary<ScopedName, Behavior> _behaviors = [];
        private readonly Dictionary<ScopedName, Scenario> _scenarios = [];

        public IEnumerable<ScopedName> DynamicObjectTypes => _dynamicObjects.Keys;
        

        private Texture2D _debugCube;
        public Texture2D GetDebugCube() {  return _debugCube; }


        private Texture2D _guestTexture;
        public Texture2D GetGuestTexture() { return _guestTexture; }

        private List<Texture2D> _guestExerciseTextures = [];
        public List<Texture2D> GetGuestExerciseTextures() { return _guestExerciseTextures; }

        private Dictionary<EBurstType, Texture2D> _burstTextures = [];
        public Texture2D GetBurstTexture(EBurstType type) { return _burstTextures[type]; }

        public void AddTexturePendingLoad(ushort tileId, string path)
        {
            if (!_tileTextures.ContainsKey(tileId))
            {
                _texturePathsPendingLoad[tileId] = path;
            }
        }

        public void AddTileType(TileType tileType)
        {
            _tileTypes[tileType.ID] = tileType;
        }

        public TileType GetTileType(ushort tileId)
        {
            return _tileTypes[tileId];
        }

        public TileType FindTileTypeByName(string name)
        {
            foreach(TileType tileType in _tileTypes.Values)
            {
                if (tileType.Name.HasName(name))
                {
                    return tileType;
                }
            }

            return null;
        }

        public List<TileType> FindTileTypesByProperties(TileProperties properties)
        {
            List<TileType> tileTypes = [];
            foreach (TileType tileType in _tileTypes.Values)
            {
                if (tileType.HasProperty(properties))
                {
                    tileTypes.Add(tileType);
                }
            }

            return tileTypes;
        }

        public void LoadContent(ContentManager content)
        {
            ResourceLoader loader = new ResourceLoader();
            loader.LoadAllMetadata(Path.Combine(Directory.GetCurrentDirectory(), content.RootDirectory, "Defs"));

            foreach (var data in loader.SpriteData)
            {
                Sprite sprite = Sprite.Load(data, content);
                _sprites.Add(sprite.Name, sprite);
            }

            foreach (var data in loader.DynamicObjectData)
            {
                DynamicObject obj = DynamicObject.Load(data, content);
                _dynamicObjects.Add(obj.Name, obj);
            }

            foreach (var data in loader.BehaviorData)
            {
                Behavior behavior = Behavior.Load(data, content);
                _behaviors.Add(behavior.Name, behavior);
            }

            foreach (var data in loader.ScenarioData)
            {
                Scenario scenario = Scenario.Load(data, content);
                _scenarios.Add(scenario.Name, scenario);
            }

            foreach (var kvp in _texturePathsPendingLoad)
            {
                _tileTextures[kvp.Key] = content.Load<Texture2D>(kvp.Value);
            }

            _texturePathsPendingLoad.Clear();
            _debugCube = content.Load<Texture2D>("Textures\\Debug\\Cube");
            _guestTexture = content.Load<Texture2D>("Textures\\Sprites\\Guest_Walk");
            _guestExerciseTextures.Add(content.Load<Texture2D>("Textures\\Sprites\\Guest_Floor_Exercise_1"));
            _guestExerciseTextures.Add(content.Load<Texture2D>("Textures\\Sprites\\Guest_Floor_Exercise_2"));

            //foreach (var kvp in DefaultObjects)
            //{
            //    _dynamicObjectTypes.Add(kvp.Value.ID, kvp.Value);
            //    string texturePath = kvp.Value.DynamicObjectData.Sprite.GetFullName('\\');
            //    _dynamicObjectTextures[kvp.Value.ID] = content.Load<Texture2D>(texturePath);
            //}

            _burstTextures.Add(EBurstType.Money, content.Load<Texture2D>("Textures\\Sprites\\Burst\\Money"));
            _burstTextures.Add(EBurstType.Fitness, content.Load<Texture2D>("Textures\\Sprites\\Burst\\Fitness"));
        }

        public Sprite GetSprite(ScopedName spriteName)
        {
            Sprite sprite;
            if (_sprites.TryGetValue(spriteName, out sprite))
            {
                return sprite;
            }

            return null;
        }


        public Texture2D GetTileTexture(ushort tileId)
        {
            return _tileTextures[tileId];
        }

        public void DrawImGui()
        {
            ImGui.Begin("[DEBUG] Resources");

            ImGui.Text($"TYPES ({_tileTypes.Count})");
            ImGui.Text($"TEXTURES ({_tileTextures.Count})");

            ImGui.End();
        }

        public DynamicObject GetDynamicObjectType(ScopedName dynamicObjectName)
        {
            DynamicObject type;
            if (_dynamicObjects.TryGetValue(dynamicObjectName, out type)) {
                return type;
            }

            return null;
        }

        public Behavior GetBehavior(ScopedName behaviorName)
        {
            Behavior behavior;
            if (_behaviors.TryGetValue(behaviorName, out behavior))
            {
                return behavior;
            }

            return null;
        }
    }
}
