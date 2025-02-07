using System;
using System.Collections.Generic;
using System.Diagnostics;
using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using GymTycoon.Code.Layers;
using ImGuiNET;

namespace GymTycoon.Code
{
    public class World
    {
        /// <summary>
        /// 3d points flattened to an array of flyweight tile data references
        /// </summary>
        private TileType[] _tiles;

        /// <summary>
        /// dictionary of all dynamic objects in the world to ID
        /// </summary>
        private Dictionary<int, DynamicObjectInstance> _dynamicObjects;

        /// <summary>
        /// dynamic object ids by world index location
        /// </summary>
        private List<int>[] _dynamicObjectLocations;

        public BeautyLayer BeautyLayer;
        public SocialLayer SocialLayer;

        /// <summary>
        /// Behaviors posted by objects in the world
        /// </summary>
        private List<AdvertisedBehavior> _advertisedBehaviors;

        private int _width;
        private int _height;
        private int _layers;

        public int GetWidth() { return _width; }
        public int GetHeight() { return _height; }
        public int GetLayers() { return _layers; }
        
        public IEnumerable<DynamicObjectInstance> GetAllDynamicObjects() { return _dynamicObjects.Values; }
        public int GetDynamicObjectCount() { return _dynamicObjects.Count; }
        public DynamicObjectInstance GetDynamicObjectById(int id) {  return _dynamicObjects[id]; }

        public World()
        {
        }
       
        public void Set(int width, int height, int layers, TileType fill)
        {
            _width = width;
            _height = height;
            _layers = layers;
            _tiles = new TileType[_width * _height * _layers];
            _advertisedBehaviors = [];
            _dynamicObjectLocations = new List<int>[_width * _height * _layers];
            _dynamicObjects = [];
            BeautyLayer = new BeautyLayer(_width * _height * _layers);
            SocialLayer = new SocialLayer(_width * _height * _layers);

            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i] = fill;
                _dynamicObjectLocations[i] = new List<int>();
            }
        }

        public Point3 GetSize()
        {
            return new Point3(_width, _height, _layers);
        }

        public TileType GetTile(int index)
        {
            return _tiles[index];
        }

        public void SetTile(TileType tileType, int index)
        {
            _tiles[index] = tileType;
        }

        public IEnumerable<KeyValuePair<int, TileType>> GetAllTiles()
        {
            int index = 0;
            while (index < _tiles.Length)
            {
                yield return new KeyValuePair<int, TileType>(index, _tiles[index++]);
            }
        }

        public int GetTileCount()
        {
            return _tiles.Length;
        }

        public Point3 GetPosition(int index)
        {
            return IsoGrid.IndexToPoint3(index, _width, _height);
        }

        public int GetIndex(Point3 position)
        {
            return IsoGrid.Point3ToIndex(position, _width, _height);
        }

        public void Update(float deltaTime)
        {
            foreach (var obj in _dynamicObjects)
            {
                foreach (var spr in obj.Value.Sprites)
                {
                    spr.Value.Update(deltaTime);
                }
            }
        }

        public void DrawImGui()
        {
            ImGui.Begin("[DEBUG] World");

            ImGui.Text($"SIZE ({_width}, {_height}, {_layers})");
            BeautyLayer.DrawImGui();
            ImGui.Separator();
            SocialLayer.DrawImGui();

            ImGui.End();
        }

        public List<int> FindTilesOfType(TileType tileType)
        {
            List<int> result = [];
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i] == tileType)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public List<int> FindTilesWithProperties(TileProperties properties, int limit = int.MaxValue)
        {
            List<int> result = [];
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].HasProperty(properties))
                {
                    result.Add(i);
                    if (result.Count == limit)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public int FindDynamicObjectsOfCategory(DynamicObjectCategory category, List<DynamicObjectInstance> dynamicObjects)
        {
            dynamicObjects.Clear();
            foreach (var obj in _dynamicObjects.Values)
            {
                if (obj.Data.Category == category && !obj.Held && obj.FindOpenGuestClaimSlot() != -1)
                {
                    dynamicObjects.Add(obj);
                }
            }
            return dynamicObjects.Count;
        }

        public bool CanBuildOnTile(int position)
        {
            foreach (var id in _dynamicObjectLocations[position])
            {
                if (!_dynamicObjects[id].Navigable)
                {
                    return false;
                }
            }

            TileType tile = GetTile(position);
            if (tile != null && (!tile.Properties.HasFlag(TileProperties.Navigable) || tile.Properties.HasFlag(TileProperties.Spawn)))
            {
                // tile cannot be built on
                return false;
            }

            return true;
        }

        public bool CanPlaceDynamicObject(DynamicObject type, int position, Direction direction)
        {
            if (!CanBuildOnTile(position))
            {
                return false;
            }

            Point3 worldPos = GetPosition(position);
            foreach (Point3 pos in type.GetGuestSlots(direction))
            {
                Point3 slot = worldPos + pos;
                int slotIndex = GetIndex(slot);
                if (!CanBuildOnTile(slotIndex))
                {
                    return false;
                }
            }

            foreach (Point3 pos in type.GetStaffSlots(direction))
            {
                Point3 slot = worldPos + pos;
                int slotIndex = GetIndex(slot);
                if (!CanBuildOnTile(slotIndex))
                {
                    return false;
                }
            }


            return true;
        }

        private void AddDynamicObjectLocation(int position, int id)
        {
            _dynamicObjectLocations[position].Add(id);
        }

        private void RemoveDynamicObjectLocation(int position, int id)
        {
            if (!_dynamicObjectLocations[position].Remove(id))
            {
                throw new Exception($"RemoveDynamicObjectlocation called with nonexistant values (position = {position}, id = {id})");
            }
        }

        public void AddDynamicObject(DynamicObjectInstance dynamicObject)
        {
            AddDynamicObjectLocation(dynamicObject.WorldPosition, dynamicObject.Id);
            _dynamicObjects[dynamicObject.Id] = dynamicObject;
            BeautyLayer.InvalidateCacheInRadius2D(dynamicObject.WorldPosition);
        }

        public bool RemoveDynamicObject(DynamicObjectInstance dynamicObject)
        {
            RemoveDynamicObjectLocation(dynamicObject.WorldPosition, dynamicObject.Id);
            if (!_dynamicObjects.Remove(dynamicObject.Id))
            {
                throw new Exception($"Attempted to remove nonexistant dynamic object with id {dynamicObject.Id}");
            }

            BeautyLayer.InvalidateCacheInRadius2D(dynamicObject.WorldPosition);
            return true;
        }

        public void UpdateDynamicObjectLocation(int id, int oldPosition, int newPosition)
        {
            RemoveDynamicObjectLocation(oldPosition, id);
            AddDynamicObjectLocation(newPosition, id);
            BeautyLayer.InvalidateCacheInRadius2D(oldPosition);
            BeautyLayer.InvalidateCacheInRadius2D(newPosition);
        }

        public short GetBeautyAt(int worldIndex)
        {
            if (worldIndex < 0 || worldIndex >= _tiles.Length)
            {
                return BeautyLayer.DefaultBeauty;
            }

            return BeautyLayer.GetValueAt(worldIndex);
        }

        public void UpdateAdvertisedBehaviors()
        {
            _advertisedBehaviors.Clear();

            // TODO: Don't instantiate every frame, reuse!

            _advertisedBehaviors.Add(GameInstance.Instance.Instances.GetAdvertisedBehavior(new ScopedName("Behavior.Default.Leave"))); // default leave option (float.MinValue if guest should not leave)
            _advertisedBehaviors.Add(GameInstance.Instance.Instances.GetAdvertisedBehavior(new ScopedName("Behavior.Default.Wander"))); // default idle option (float.MinValue+1 as fallback)

            foreach (var obj in _dynamicObjects.Values)
            {
                if (obj.CanAdvertiseBehaviors())
                {
                    _advertisedBehaviors.AddRange(obj.GetAdvertisedBehaviors());
                }
            }
        }

        public List<AdvertisedBehavior> GetAdvertisedBehaviors()
        {
            return _advertisedBehaviors;
        }

        public void DeleteDynamicObject(DynamicObjectInstance obj)
        {
            if (!_dynamicObjects.ContainsKey(obj.Id))
            {
                throw new Exception("Tried to delete object that no longer exists!");
            }

            if (obj.Parent != null)
            {
                Debug.WriteLine("Cannot delete child objects, must select parent");
                return;
            }

            foreach (var child in obj.Children)
            {
                child.ClearAllClaims();

                if (child.Held)
                {
                    child.HeldBy.RemoveHeldObj(obj);
                }


                if (child.Racked)
                {
                    if (!_dynamicObjects.Remove(child.Id))
                    {
                        throw new Exception($"Attempted to remove nonexistant child object from world with ID {obj.Id}");
                    }
                }
                else
                {
                    RemoveDynamicObjectLocation(child.WorldPosition, child.Id);
                    child.AddBurst(EBurstType.Money);
                }
            }

            obj.ClearAllClaims();
            RemoveDynamicObjectLocation(obj.WorldPosition, obj.Id);

            GameInstance.Instance.Economy.Transaction(obj.GetRefundValue(), TransactionType.Refund);
            obj.AddBurst(EBurstType.Money);
            if (!_dynamicObjects.Remove(obj.Id))
            {
                throw new Exception($"Attempted to remove nonexistant object from world with ID {obj.Id}");
            }
            BeautyLayer.InvalidateCacheInRadius2D(obj.WorldPosition);
        }

        internal bool HasBlockingObjectsAtLocation(int to)
        {
            if (to < 0 || to >= _tiles.Length)
            {
                return false;
            }

            foreach (int id in _dynamicObjectLocations[to])
            {
                if (!_dynamicObjects[id].Navigable)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
