using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using ImGuiNET;
using System;

namespace GymTycoon.Code.Layers
{
    public class SocialLayer : DataLayer<short>
    {
        public readonly int MaxSocialRadius = 12;
        public readonly short DefaultSocial = 0;

        private float _guestInfluence = 1f;
        private short _maxSocial = 5;
        private short _minSocial = -5;
        private float _socialFalloff = 1.15f;

        
        public SocialLayer(int size) : base(size)
        {
            for (int i = 0; i < size; i++)
            {
                _layer[i] = DefaultSocial;
            }
        }

        public void InvalidateCacheInRadius2D(int center)
        {
            InvalidateCacheInRadius2D(center, MaxSocialRadius);
        }

        public float GetSocialPercentat(int index)
        {
            float value = GetValueAt(index);
            return (value + (_minSocial * -1f)) / (_maxSocial + (_minSocial * -1f));
        }

        protected override void UpdateValueAt(int index)
        {
            _layer[index] = DefaultSocial;
            Point3 layerPosition = GameInstance.Instance.World.GetPosition(index);

            int totalSocial = 0;
            foreach (Guest guest in GameInstance.Instance.Director.ActiveGuests)
            {
                Point3 instPosition = GameInstance.Instance.World.GetPosition(guest.WorldPosition);
                float dist = Point3.Distance(layerPosition, instPosition);

                if (dist > MaxSocialRadius)
                {
                    continue;
                }

                float influence = _guestInfluence / MathF.Pow(_socialFalloff, dist);
                totalSocial += (int)MathF.Round(influence);
            }

            if (totalSocial > _maxSocial)
            {
                totalSocial = _maxSocial;
            } else if (totalSocial < _minSocial)
            {
                totalSocial = _minSocial;
            }

            _layer[index] = (short)totalSocial;
            _cache[index] = true;
        }

        public void DrawImGui()
        {
            bool didChangeSocial = ImGui.DragFloat($"SOCIAL FALLOFF", ref _socialFalloff, 0.01f, 1.01f, 2f);
            int minSocial = _minSocial;
            int maxSocial = _maxSocial;

            didChangeSocial = ImGui.DragInt($"MAX SOCIAL", ref maxSocial, 1, 1, 100) || didChangeSocial;
            didChangeSocial = ImGui.DragInt($"MIN SOCIAL", ref minSocial, 1, -100, 0) || didChangeSocial;

            if (didChangeSocial)
            {
                _minSocial = (short)minSocial;
                _maxSocial = (short)maxSocial;
            }

            if (ImGui.Button("Invalidate Social Cache") || didChangeSocial)
            {
                InvalidateAllCache();
            }
        }
    }
}
