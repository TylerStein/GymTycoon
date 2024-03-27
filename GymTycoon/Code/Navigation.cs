using GymTycoon.Code.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GymTycoon.Code
{
    internal enum NavDirection
    {
        UNSET = 0,
        NORTH = 1,
        EAST = 2,
        SOUTH = 3,
        WEST = 4
    }

    public class Navigation
    {
        internal static Point3 DirectionPoint(NavDirection dir)
        {
            return dir switch
            {
                NavDirection.NORTH => new Point3(0, -1, 0),
                NavDirection.SOUTH => new Point3(0, 1, 0),
                NavDirection.WEST => new Point3(-1, 0, 0),
                NavDirection.EAST => new Point3(1, 0, 0),
                _ => new Point3(0, 0, 0),
            };
        }

        public static int MaxPathIterations = 5000;
        private static void CreatePath(Dictionary<Point3, Point3> cameFrom, Point3 current, Stack<Point3> path)
        {
            path.Push(current);
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Push(current);
            }
        }

        public static bool IsTileNavigable(World world, int index)
        {
            TileType tileType = world.GetTile(index);
            return (tileType.HasProperty(TileProperties.Navigable) && !world.HasBlockingObjectsAtLocation(index));
        }

        public static bool IsValidNeighbor(World world, int from, int to, int finalDest)
        {
            if (to == finalDest)
            {
                return true;
            }

            return IsTileNavigable(world, to);
        }

        public static void FillValidNeighbors(Point3?[] neighbors, World world, Point3 current, Point3 dest)
        {
            // TODO: Stair/ramp logic
            Point3 next;
            int index;

            int destIndex = world.GetIndex(dest);
            int fromIndex = world.GetIndex(current);

            // forward
            next = current + Point3.Forward;
            index = world.GetIndex(next);
            neighbors[0] = IsValidNeighbor(world, fromIndex, index, destIndex) ? next : null;

            // right
            next = current + Point3.Right;
            index = world.GetIndex(next);
            neighbors[1] = IsValidNeighbor(world, fromIndex, index, destIndex) ? next : null;

            // backward
            next = current + Point3.Backward;
            index = world.GetIndex(next);
            neighbors[2] = IsValidNeighbor(world, fromIndex, index, destIndex) ? next : null;

            // left
            next = current + Point3.Left;
            index = world.GetIndex(next);
            neighbors[3] = IsValidNeighbor(world, fromIndex, index, destIndex) ? next : null;

        }

        public static float TraverseCost(World world, Point3 current, Point3 neighbor)
        {
            return 1; // TODO: Traverse costs
        }

        public static float Heuristic(Point3 start, Point3 end)
        {
            return (start - end).Magnitude();
        }

        private static HashSet<Point3> _open = [];
        private static Dictionary<Point3, Point3> _cameFrom = [];
        private static Dictionary<Point3, float> _gScore = [];
        private static Dictionary<Point3, float> _fScore = [];
        private static Point3?[] _neighbors = new Point3?[4];

        public static bool Pathfinding(World world, Point3 start, Point3 end, Stack<Point3> path)
        {
            path.Clear();
            _cameFrom.Clear();
            _open.Clear();
            _gScore.Clear();
            _fScore.Clear();

            _open.Add(start);
            _gScore[start] = 0;
            _fScore[start] = Heuristic(start, end);

            int iterations = 0;
            while (_open.Count > 0)
            {
                if (++iterations >= MaxPathIterations)
                {
                    Debug.Print($"Pathfinding hit max iterations ({iterations}");
                    return false;
                }

                Point3? current = null;

                float lowestOpenFScore = float.MaxValue;
                foreach (Point3 openVoxel in _open)
                {
                    float currentFscore = _fScore[openVoxel];
                    if (currentFscore < lowestOpenFScore)
                    {
                        current = openVoxel;
                        lowestOpenFScore = currentFscore;
                    }
                }

                if (current.Value == end)
                {
                    CreatePath(_cameFrom, current.Value, path);
                    return true;
                }

                _open.Remove(current.Value);

                FillValidNeighbors(_neighbors, world, current.Value, end);

                Nullable<Point3> neighbor;
                for (int i = 0; i < 4; i++)
                {
                    neighbor = _neighbors[i];
                    if (neighbor == null)
                    {
                        continue;
                    }

                    float neighborGScore = _gScore.ContainsKey(neighbor.Value) ? _gScore[neighbor.Value] : float.MaxValue;
                    float nextGScore = _gScore[current.Value] + TraverseCost(world, current.Value, neighbor.Value);
                    if (nextGScore < neighborGScore)
                    {
                        _cameFrom[neighbor.Value] = current.Value;
                        _gScore[neighbor.Value] = nextGScore;
                        _fScore[neighbor.Value] = nextGScore + Heuristic(start, end);

                        if (!_open.Contains(neighbor.Value))
                        {
                            _open.Add(neighbor.Value);
                        }
                    }
                }
            }

            return false;
        }
    }
}
