using GymTycoon.Code.Common;
using ImGuiNET;
using System;

namespace GymTycoon.Code.Layers
{
    public class BeautyLayer : DataLayer<short>
    {
        public readonly int MaxBeautyRadius = 12;
        public readonly short DefaultBeauty = 0;

        private short _maxBeauty = 5;
        private short _minBeauty = -5;
        private float _beautyFalloff = 1.15f;

        
        public BeautyLayer(int size) : base(size)
        {
            for (int i = 0; i < size; i++)
            {
                _layer[i] = DefaultBeauty;
            }
        }

        public void InvalidateCacheInRadius2D(int center)
        {
            InvalidateCacheInRadius2D(center, MaxBeautyRadius);
        }

        public float GetBeautyPercentAt(int index)
        {
            float value = GetValueAt(index);
            return (value + (_minBeauty * -1f)) / (_maxBeauty + (_minBeauty * -1f));
        }

        protected override void UpdateValueAt(int index)
        {
            _layer[index] = DefaultBeauty;
            Point3 layerPosition = GameInstance.Instance.World.GetPosition(index);

            int totalBeauty = 0;
            foreach (DynamicObjectInstance inst in GameInstance.Instance.World.GetAllDynamicObjects())
            {
                if (inst.Data.Beauty == 0)
                {
                    continue;
                }

                Point3 instPosition = GameInstance.Instance.World.GetPosition(inst.WorldPosition);
                float dist = Point3.Distance(layerPosition, instPosition);

                if (dist > MaxBeautyRadius)
                {
                    continue;
                }

                float influence = inst.Data.Beauty / MathF.Pow(_beautyFalloff, dist);
                totalBeauty += (int)MathF.Round(influence);
            }

            if (totalBeauty > _maxBeauty)
            {
                totalBeauty = _maxBeauty;
            } else if (totalBeauty < _minBeauty)
            {
                totalBeauty = _minBeauty;
            }

            _layer[index] = (short)totalBeauty;
            _cache[index] = true;
        }

        public void DrawImGui()
        {
            bool didChangeBeauty = ImGui.DragFloat($"BEAUTY FALLOFF", ref _beautyFalloff, 0.01f, 1.01f, 2f);
            int minBeauty = _minBeauty;
            int maxBeauty = _maxBeauty;

            didChangeBeauty = ImGui.DragInt($"MAX BEAUTY", ref maxBeauty, 1, 1, 100) || didChangeBeauty;
            didChangeBeauty = ImGui.DragInt($"MIN BEAUTY", ref minBeauty, 1, -100, 0) || didChangeBeauty;

            if (didChangeBeauty)
            {
                _minBeauty = (short)minBeauty;
                _maxBeauty = (short)maxBeauty;
            }

            if (ImGui.Button("Invalidate Beauty Cache") || didChangeBeauty)
            {
                InvalidateAllCache();
            }
        }
    }
}
