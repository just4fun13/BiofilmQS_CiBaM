using Assets.Scripts.NewGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.DiffusionModels.Geometry
{
    public class Hexagon : CellGeometry
    {
        private double sqrt3 = Math.Sqrt(3);
        private Vector2 offset = Vector2.left * 0.5f;
        public Hexagon()
        {
            BaseV[0] = Vector2.right;
            BaseV[1] = new Vector2(0, (float) (sqrt3 / 2d) );
            gridType = CellularAutomaton.GridType.Hexagone;
            NeedShift = true;
            neighbors = new Vector2Int[]
            {
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 1),
                    new Vector2Int(0, -1),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, -1),
            };
            shiftN = new Vector2Int[]
            {
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(-1, -1),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, -1),
            };


            double sqr12 = Math.Pow(0.5, 0.5);
            HorizontalImpact = new double[]
            {
                    1, -1, 0.5, -0.5, -0.5, 0.5,
            };
            HorizontalImpactSum = 3;
        }
        public override double GetFaceLength()
        {
            return HorizontalStep / sqrt3;
        }

        public override double GetCellArea()
        {
            return HorizontalStep * HorizontalStep * sqrt3 / 2;
        }
        public override Vector2 GetPosition(Vector2Int id)
        {
            Vector2 p = BaseV[0] * id.x + BaseV[1] * id.y;

            if ((id.y % 2 != 0))
                p += offset;

            return scale * p;
        }
        public override Vector2Int GetIdOfPoint(Vector2 pos)
        {
            int x, y;

            // "Гекс": чётные строки без сдвига, нечётные — со сдвигом offset.

            // сначала оцениваем индекс по вертикали
            float iy = pos.y / (scale * BaseV[1].y);
            y = Mathf.RoundToInt(iy);

            float xNorm = pos.x / scale;

            if (y % 2 == 0)
            {
                // чётная строка: pos.x ≈ scale * (i * BaseV[0].x)
                float ix = xNorm / BaseV[0].x;
                x = Mathf.RoundToInt(ix);
            }
            else
            {
                // нечётная строка: pos.x ≈ scale * (i * BaseV[0].x + offset.x)
                float ix = (xNorm - offset.x) / BaseV[0].x;
                x = Mathf.RoundToInt(ix);
            }

            return new Vector2Int(x, y);
        }
    }


}
