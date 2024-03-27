using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace GymTycoon.Code
{
    public class Sprite
    {
        public ScopedName Name;
        public Dictionary<ScopedName, SpriteSheet> Sheets = [];
        public List<SpriteLayer> Layers = [];
        public bool Shared = false;

        public static Sprite Load(SpriteData data, ContentManager content)
        {
            Sprite sprite = new Sprite();
            sprite.Name = new ScopedName(new string[]{ data.MetaData.Type, data.MetaData.Package, data.Name });
            sprite.Shared = data.Shared;
            foreach (var sheetData in data.SpriteSheets)
            {
                SpriteSheet sheet = SpriteSheet.Load(sheetData, content);
                sprite.Sheets.Add(sheet.Name, sheet);
            }

            foreach (var layerData in data.Layers)
            {
                SpriteLayer layer = SpriteLayer.Load(layerData);
                sprite.Layers.Add(layer);
            }

            return sprite;
        }
    }

    public class SpriteInstance
    {
        Sprite Sprite;
        SpriteSheet[] ActiveLayerSpriteSheets;
        int[] ActiveLayerIndex;
        int[] Frames;
        float[] Timers;
        bool[] Hidden;

        public SpriteInstance(Sprite data)
        {
            Sprite = data;
            Frames = new int[data.Layers.Count];
            ActiveLayerSpriteSheets = new SpriteSheet[data.Layers.Count];
            ActiveLayerIndex = new int[data.Layers.Count];
            Timers = new float[data.Layers.Count];
            Hidden = new bool[data.Layers.Count];

            for (int i = 0; i < data.Layers.Count; i++)
            {
                Frames[i] = 0;
                ActiveLayerSpriteSheets[i] = Sprite.Sheets[data.Layers[i].SpriteSheets[0]];
                ActiveLayerIndex[i] = 0;
                Timers[i] = 0;
                Hidden[i] = false;
            }
        }

        public SpriteSheet GetLayerSpriteSheet(int layer)
        {
            return ActiveLayerSpriteSheets[layer];
        }

        public SpriteSheet[] GetLayerSpriteSheets()
        {
            return ActiveLayerSpriteSheets;
        }

        public Texture2D GetTexture(int layer, Direction direction, out Rectangle sourceRectangle)
        {
            return GetLayerSpriteSheet(layer).GetTexture(Frames[layer], direction, out sourceRectangle);
        }

        public int GetNumLayers()
        {
            return ActiveLayerSpriteSheets.Length;
        }

        public bool LayerIsHidden(int layer)
        {
            return Hidden[layer];
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < ActiveLayerSpriteSheets.Length; i++)
            {
                Timers[i] += deltaTime;
                if (Timers[i] >= 1f / ActiveLayerSpriteSheets[i].FrameRate)
                {
                    Timers[i] = 0f;
                    NextFrame(i);
                }
            }
        }

        public void PrevFrame(int layer)
        {
            int prev = ActiveLayerSpriteSheets[layer].WrapFrame(Frames[layer] - 1);
            Frames[layer] = prev;
        }

        public void NextFrame(int layer)
        {
            int next = ActiveLayerSpriteSheets[layer].WrapFrame(Frames[layer] + 1);
            Frames[layer] = next;
        }

        public void SetActiveLayerSheet(ScopedName layerName, ScopedName sheetName, bool reset = false)
        {
            int layerIndex = Sprite.Layers.FindIndex((layer) => layer.Name == layerName);
            for (int i = 0; i < Sprite.Layers[layerIndex].SpriteSheets.Length; i++)
            {
                if (Sprite.Layers[layerIndex].SpriteSheets[i] == sheetName)
                {
                    SetActiveLayerSheet(layerName, i, reset);
                    return;
                }
            }
        }
        public void SetActiveLayerSheet(ScopedName layerName, int sheetIndex, bool reset = false)
        {
            int layerIndex = Sprite.Layers.FindIndex((layer) => layer.Name == layerName);

            if ((reset || ActiveLayerIndex[layerIndex] != sheetIndex))
            {
                SpriteLayer spriteLayer = Sprite.Layers[layerIndex];
                ActiveLayerSpriteSheets[layerIndex] = Sprite.Sheets[spriteLayer.SpriteSheets[sheetIndex]];
                ActiveLayerIndex[layerIndex] = sheetIndex;
                Frames[layerIndex] = 0;
                Hidden[layerIndex] = false;
            }
        }

        public void HideActiveLayerSheet(ScopedName layerName)
        {
            int layerIndex = Sprite.Layers.FindIndex((layer) => layer.Name == layerName);
            Hidden[layerIndex] = true;
        }
    }
}
