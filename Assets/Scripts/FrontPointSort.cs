using System.Collections.Generic;
using UnityEngine;

namespace CellularAutomaton
{
    public class FrontPointSort : IComparer<Vector2Int>
    {

        public int Compare(Vector2Int p1, Vector2Int p2)
        {
            Vector2 x = BiofilmGrowth.GetGlobalPos(p1);
            Vector2 y = BiofilmGrowth.GetGlobalPos(p2);
            if (x.x != y.x)
                return x.x.CompareTo(y.x);
            else
                return x.y.CompareTo(y.y);
        }
    }
}
