using GymTycoon.Code.Common;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using System.Linq;

namespace GymTycoon.Code.Data
{
    public class SpriteLayerData
    {
        [JsonProperty]
        public readonly string Name;
        [JsonProperty]
        public readonly int Sort;
        [JsonProperty]
        public readonly string[] SpriteSheets;
    }

    public class SpriteSheetData
    {
        [JsonProperty]
        public readonly string Name;
        [JsonProperty]
        public readonly string TextureSouth;
        [JsonProperty]
        public readonly string TextureEast;
        [JsonProperty]
        public readonly string TextureWest;
        [JsonProperty]
        public readonly string TextureNorth;
        [JsonProperty]
        public readonly string TextureSheet;
        [JsonProperty]
        public readonly float FrameRate;
        [JsonProperty]
        public readonly int Width;
        [JsonProperty]
        public readonly int Height;
        [JsonProperty]
        public readonly string Type;
    }

    public class SpriteData
    {
        public MetaData MetaData;

        [JsonProperty]
        public readonly string Name;
        [JsonProperty]
        public readonly SpriteSheetData[] SpriteSheets;
        [JsonProperty]
        public readonly SpriteLayerData[] Layers;
        [JsonProperty]
        public readonly bool Shared;

        public SpriteData(
            string name,
            SpriteSheetData[] spriteSheets,
            SpriteLayerData[] layers,
            bool shared)
        {
            Name = name;
            SpriteSheets = spriteSheets;
            Layers = layers;
            Shared = shared;
        }
    }

    public class SpriteLayer
    {
        public ScopedName Name = null;
        public ScopedName[] SpriteSheets = null;
        public int Sort = 0;
        public bool IsSheet = false;

        public static SpriteLayer Load(SpriteLayerData data)
        {
            SpriteLayer layer = new SpriteLayer()
            {
                Name = new ScopedName(data.Name),
                SpriteSheets = data.SpriteSheets.Select((name) => new ScopedName(name)).ToArray(),
                Sort = data.Sort
            };
            return layer;
        }
    }

    public class SpriteSheet
    {
        public ScopedName Name = null;
        public Texture2D[] Textures = null;
        public bool IsSheet = false;
        public float FrameRate = 1f;
        public int Width = 1;
        public int Height = 1;
        public int Frames = 1;

        public static SpriteSheet Load(SpriteSheetData data, ContentManager content)
        {
            bool isSheet = data.TextureSheet != null;
            SpriteSheet sheet = new SpriteSheet()
            {
                Name = new ScopedName(data.Name),
                Textures =
                    isSheet
                    ? [ content.Load<Texture2D>(data.TextureSheet) ]
                    : [
                        content.Load<Texture2D>(data.TextureNorth),
                        content.Load<Texture2D>(data.TextureSouth),
                        content.Load<Texture2D>(data.TextureEast),
                        content.Load<Texture2D>(data.TextureWest)
                    ],
                FrameRate = data.FrameRate,
                Width = data.Width,
                Height = data.Height,
                Frames = data.Width * data.Height,
                IsSheet = isSheet
            };

            return sheet;
        }

        public Texture2D GetTexture(Direction direction)
        {
            return Textures[(int)direction];
        }

        public Texture2D GetTexture(int frame, Direction direction, out Rectangle sourceRectangle)
        {
            if(IsSheet)
            {
                sourceRectangle = GetSourceRectangle(frame, (int)direction);
                return Textures[0];
            }

            sourceRectangle = GetSourceRectangle(frame, 0);
            return GetTexture(direction);
        }

        public Rectangle GetSourceRectangle(int frame, int direction)
        {
            int x = frame * WorldRenderer.SpriteSize;
            int y = direction * WorldRenderer.SpriteSize;
            return new Rectangle(x, y, WorldRenderer.SpriteSize, WorldRenderer.SpriteSize);
        }

        public int WrapFrame(int nextFrame)
        {
            int wrapped = nextFrame % Frames;
            if (wrapped < 0)
            {
                wrapped += Frames;
            }

            return wrapped;
        }
    }
}
