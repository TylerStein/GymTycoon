using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace GymTycoon.Code
{
    // -X (W)     -Y (N)
    //   X X X X X
    //   X X X X X 
    //   X X X X X 
    //   X X X X X
    // +Y (S)     +X (E)

    public enum Direction : int
    {
        NORTH = 0,
        SOUTH = 1,
        EAST = 2,
        WEST = 3
    }
}
