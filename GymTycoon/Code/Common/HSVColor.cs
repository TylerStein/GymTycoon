using System;
using Microsoft.Xna.Framework;

/**
 * https://axonflux.com/handy-rgb-to-hsl-and-rgb-to-hsv-color-model-c
 */

namespace GymTycoon.Code.Common
{
    public class HSVColor
    {
        private float[] _values = new float[3];

        public float H
        {
            get { return _values[0]; }
            set { _values[0] = value; }
        }

        public float S
        {
            get { return _values[1]; }
            set { _values[1] = value;}
        }

        public float V
        {
            get { return _values[2]; }
            set { _values[2] = value; }
        }

        public HSVColor(float h, float s, float v) {
            H = h;
            S = s;
            V = v;
        }

        public HSVColor(Color color)
        {
            HSVColor hsvColor = FromColor(color);
            H = hsvColor.H;
            S = hsvColor.S;
            V = hsvColor.V;
        }

        public HSVColor(float[] hsv)
        {
            if (hsv.Length != 3)
            {
                throw new Exception("HSVColor constructor expects a 3-float array input");
            }

            _values.CopyTo(hsv, 0);
        }

        public override bool Equals(object obj)
        {
            if (obj is HSVColor)
            {
                return (obj as HSVColor).H == H
                    && (obj as HSVColor).S == S
                    && (obj as HSVColor).V == V;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (H * 1000).GetHashCode();
            hash = hash * 23 + (S * 1000).GetHashCode();
            hash = hash * 23 + (V * 1000).GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            return $"{H}, {S}, {V}";
        }

        public Color ToColor(float alpha = 1f)
        {
            float r = 0f;
            float g = 0f;
            float b = 0f;

            float i = MathF.Floor(H * 6);
            float f = H * 6 - i;
            float p = V * (1 - S);
            float q = V * (1 - f * S);
            float t = V * (1 - (1 - f) * S);

            switch (i % 6)
            {
                case 0: r = V; g = t; b = p; break;
                case 1: r = q; g = V; b = p; break;
                case 2: r = p; g = V; b = t; break;
                case 3: r = p; g = q; b = V; break;
                case 4: r = t; g = p; b = V; break;
                case 5: r = V; g = p; b = q; break;
            }

            return new Color(r, g, b, alpha);
        }

        public static HSVColor FromColor(Color color)
        {
            float r = (float)color.R / 255f;
            float g = (float)color.G / 255f;
            float b = (float)color.B / 255f;

            float max = MathF.Max(MathF.Max(r, g), b);
            float min = MathF.Min(MathF.Min(r, g), b);

            float h = max;
            float v = max;

            float d = max - min;

            float s = max == 0 ? 0 : d / max;

            if (max == min)
            {
                h = 0; // achromatic
            }
            else if (max == r)
            {
                h = (g - b) / d + (g < b ? 6 : 0);
            }
            else if (max == g)
            {
                h = (b - r) / d + 2;
            }
            else if (max == b)
            {
                h = (r - g) / d + 4;
            }

            h /= 6;

            return new HSVColor(h, s, v);
        }
    }
}
