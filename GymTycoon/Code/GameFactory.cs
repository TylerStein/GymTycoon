using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using TiledCS;

namespace GymTycoon.Code
{
    internal class GameFactory
    {
        public static GameInstance CreateTiledMapGameInstance(Scenario scenario)
        {
            GameInstance game = new GameInstance(scenario);
            string path = Path.Combine(game.Content.RootDirectory, scenario.Map);
            TiledMap tiledMap = new TiledMap(path);
            if (tiledMap == null)
            {
                throw new FileNotFoundException($"Tiledmap not found at {path}");
            }

            Dictionary<int, TiledTileset> tilesets = tiledMap.GetTiledTilesets(path);
            PopulateTiledMapResources(game, tilesets);
            PopulateTiledMapWorld(game, tiledMap);
            return game;
        }

        public static void PopulateTiledMapWorld(GameInstance game, TiledMap tiledMap)
        {
            int mapWidth = tiledMap.Width + tiledMap.Layers.Length;
            int mapHeight = tiledMap.Height + tiledMap.Layers.Length;

            game.World.Set(mapWidth, mapHeight, tiledMap.Layers.Length, TileType.Empty);
            for (int z = 0; z < tiledMap.Layers.Length; z++)
            {
                for (int i = 0; i < tiledMap.Layers[z].data.Length; i++)
                {
                    Point layerPoint = IsoGrid.IndexToPoint(i, tiledMap.Width);
                    // Tiled layers move points along the XY to give the illusion of verticality - since we want our 3D grid to simulate actual space, we need to adjust for it
                    layerPoint += new Point(z, z);
                    int index3D = IsoGrid.Point3ToIndex(layerPoint.X, layerPoint.Y, z, mapWidth, mapHeight);
                    ushort id = (ushort)tiledMap.Layers[z].data[i];
                    TileType tileType = game.Resources.GetTileType(id);
                    game.World.SetTile(tileType, index3D);
                }
            }
        }

        public static void PopulateTiledMapResources(GameInstance game, Dictionary<int, TiledTileset> tilesets)
        {
            game.Resources.AddTileType(TileType.Empty); // 0 tile is empty
            foreach (var kvp in tilesets)
            {
                int gid = kvp.Key;
                var tileset = kvp.Value;
                foreach (var tile in tileset.Tiles)
                {
                    var tileType = CreateTiledTileType(gid, tileset, tile);
                    if (tileType == TileType.Empty)
                    {
                        continue;
                    }

                    string relativeSource = tile.image.source;
                    string extension = Path.GetExtension(tile.image.source);
                    string adjustedSource = relativeSource.Replace("../", "").Replace("/", "\\").Replace(extension, "");
                    game.Resources.AddTexturePendingLoad(tileType.ID, adjustedSource);
                    game.Resources.AddTileType(tileType);
                }
            }
        }

        public static TileType CreateTiledTileType(int gid, TiledTileset tileset, TiledTile tile)
        {
            TileProperties properties = TileProperties.Visible;
            foreach (var property in tile.properties)
            {
                properties = ParseTiledTileProperty(properties, property.name, property.value);
            }

            string tileName = Path.GetFileNameWithoutExtension(tile.image.source);
            TileType tileType = new TileType(
                (ushort)(gid + tile.id),
                new ScopedName(new string[] { tileset.Name, tileName }),
                properties
            );

            return tileType;
        }

        public static TileProperties ParseTiledTileProperty(TileProperties properties, string propertyKey, string propertyValue)
        {
            switch (propertyKey)
            {
                case "Nav":
                    return properties | (propertyValue == "true" ? TileProperties.Navigable : TileProperties.None);
                case "Spawn":
                    return properties | (propertyValue == "true" ? TileProperties.Spawn : TileProperties.None);
                case "Editor":
                    return properties | (propertyValue == "true" ? TileProperties.Editor : TileProperties.None);
                case "Transparency":
                    return properties | (propertyValue == "true" ? TileProperties.Transparency : TileProperties.None);
            }

            // return properties & ~TileProperties.Visible; // to remove a prop
            return properties;
        }
    }
}
