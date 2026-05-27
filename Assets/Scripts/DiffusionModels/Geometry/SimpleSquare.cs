using Assets.Scripts.NewGeneration;
using System.Numerics;
using UnityEngine;

namespace Assets.Scripts.DiffusionModels.Geometry
{
    public class SimpleSquare : CellGeometry 
    {
        public SimpleSquare() 
        {
            gridType = CellularAutomaton.GridType.Square;
            NeedShift = false;
            neighbors = new Vector2Int[]
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            };
            HorizontalImpact = new double[] 
            { 
                1, -1, 0, 0,            
            };
            HorizontalImpactSum = 2;
        }

    }
}
