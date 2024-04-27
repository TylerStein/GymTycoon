using GymTycoon.Code.Common;
using System.Collections.Generic;

namespace GymTycoon.Code
{
    public class Zone
    {
        HashSet<int> _zone = [];

        public HashSet<int> Data { get { return _zone; } }

        // TODO: Optimize
        public void CalculateZone(World world, Dictionary<int, int> influences)
        {
            Point3?[] neighbors = new Point3?[6];
            _zone = [];
            // HashSet<int> visited = new HashSet<int>(); // doesn't consider propogation beyond first pass
            Dictionary<int, int> active = new Dictionary<int, int>();
            Dictionary<int, int> pending = new Dictionary<int, int>();

            foreach (var influence in influences)
            {
                Point3 point = world.GetPosition(influence.Key);
                active[influence.Key] = influence.Value;

                while (active.Count > 0)
                {
                    foreach (var kvp in active)
                    {
                        int activeIndex = kvp.Key;
                        _zone.Add(activeIndex);

                        int activeInfluence = kvp.Value;
                        int nextInfluence = kvp.Value - 1;
                        if (nextInfluence == 0)
                        {
                            continue;
                        }

                        point = world.GetPosition(activeIndex);

                        Navigation.FillValidNeighbors(neighbors, world, point, point, true);
                        foreach (var neighbor in neighbors)
                        {
                            if (neighbor == null)
                            {
                                continue;
                            }

                            int index = world.GetIndex(neighbor.Value);
                            if (!pending.ContainsKey(index) || pending[index] < nextInfluence)
                            {
                                pending[index] = nextInfluence;
                            }
                        }
                    }

                    active = new Dictionary<int, int>(pending);
                    pending.Clear();
                }
            }
        }
    }
}
