using Assets.Scripts.NewGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.DiffusionModels.Geometry
{
    public class ExtendedSquare : CellGeometry
    {
        public ExtendedSquare()
        {
            gridType = CellularAutomaton.GridType.ExtendedSquare;
            NeedShift = false;
            neighbors = new Vector2Int[]
            {
                    Vector2Int.right,
                    Vector2Int.left,
                    Vector2Int.up,
                    Vector2Int.down,
                    new Vector2Int(1, 1),
                    new Vector2Int(-1, -1),
                    new Vector2Int(1, -1),
                    new Vector2Int(-1, 1),
            };
            double sqr12 = Math.Pow(0.5, 0.5);
            HorizontalImpact = new double[]
            {
//                    1, -1, 0, 0, 0, 0, 0, 0, 
//                    1, -1, 0, 0, sqr12, -sqr12, sqr12, -sqr12
                    1, -1, 0, 0, 1, -1, 1, -1
            };
            HorizontalImpactSum = 6;// 2 + 4 * sqr12;
        }
    }
}

