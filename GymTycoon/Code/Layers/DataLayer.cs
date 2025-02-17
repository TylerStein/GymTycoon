using GymTycoon.Code.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymTycoon.Code.Layers
{
    public abstract class DataLayer<T>
    {
        protected bool[] _cache;
        protected T[] _layer;

        private DataLayer() { }

        protected DataLayer(int size)
        {
            _cache = new bool[size];
            _layer = new T[size];
        }

        public void InvalidateCache(int index)
        {
            _cache[index] = false;
        }
        public void InvalidateAllCache()
        {
            _cache = new bool[_layer.Length];
        }

        public void InvalidateCacheInRadius2D(int center, int radius)
        {
            Point3 centerPoint = GameInstance.Instance.World.GetPosition(center);
            InvalidateCacheInRadius2D(centerPoint, radius);
        }

        public void InvalidateCacheInRadius2D(Point3 center, int radius)
        {
            foreach (Point point in GridShapes.GetPointsInCircle((Point)center, radius))
            {
                int index = GameInstance.Instance.World.GetIndex(new Point3(point.X, point.Y, center.Z));
                InvalidateCache(index);
            }
        }

        public void InvalidateCacheInRadius3D(int center, int radius)
        {
            Point3 centerPoint = GameInstance.Instance.World.GetPosition(center);
            InvalidateCacheInRadius3D(centerPoint, radius);
        }

        public void InvalidateCacheInRadius3D(Point3 center, int radius)
        {
            foreach (Point3 point in GridShapes.GetPointsInSphere(center, radius))
            {
                int index = GameInstance.Instance.World.GetIndex(point);
                InvalidateCache(index);
            }
        }

        public T GetValueAt(int index)
        {
            GameInstance.Instance.Performance.Start("DataLayer Eval");
            if (index < 0 || index >= _cache.Length)
            {
                throw new Exception($"Invalid DataLayer index: {index}");
            }

            if (!_cache[index])
            {
                UpdateValueAt(index);
                _cache[index] = true;
            }

            GameInstance.Instance.Performance.Stop();
            return _layer[index];
        }

        protected abstract void UpdateValueAt(int index);
    }
}
