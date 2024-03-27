using GymTycoon.Code.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymTycoon.Code
{
    public enum EBurstType
    {
        Money,
        Fitness,
    }

    public class Burst
    {
        public Point3 WorldPos;
        public float MaxLife;
        public float CurrentLife;
        public EBurstType BurstType;
        public bool PendingRemoval = false;
        public float Alpha = 1f;
        public float Offset = 0f;


        public Burst(EBurstType type, Point3 worldPosition, float maxLife)
        {
            BurstType = type;
            WorldPos = worldPosition;
            MaxLife = maxLife;
            CurrentLife = MaxLife;
        }

        public void Update(float deltaTime)
        {
            CurrentLife -= deltaTime;
            if (CurrentLife < 0)
            {
                CurrentLife = 0f;
                PendingRemoval = true;
            }

            float progress = CurrentLife / MaxLife;
            Alpha = progress;
            Offset = 1f - progress;
        }
    }
}
