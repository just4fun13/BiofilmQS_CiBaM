using CellularAutomaton;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class CellGeometry
    {
        public string name => gridType.ToString();
        public GridType gridType { get; protected set; }
        // Сдвиг для нечётных строк при NeedShift = true
        // (по умолчанию горизонтальный сдвиг на полклетки влево)
        public double HorizontalStep = 0;
        // Базисные векторы (по умолчанию ортонормальные)
        public Vector2[] BaseV { get; protected set; } = new Vector2[2]
        {
            Vector2.right,
            Vector2.up
        };
        public bool NeedShift { get; protected set; } = false; // false = прямоугольная; true = "гекс" со сдвигом строк
        public Vector2Int[] neighbors { get; protected set; } // для прямоугольной / чётной строки
        public Vector2Int[] shiftN    { get; protected set; } // для нечётной строки при NeedShift
        public double MinSqrWeight { get; private set; } = 10d;
        public double[] HorizontalImpact { get; protected set; }
        public double HorizontalImpactSum { get; protected set; }
        public Dictionary<Vector2Int, double> DirSqrWeight { get; private set; } = new Dictionary<Vector2Int, double>();
        public float scale { get; protected set; } = 1f;
        public void SetScale(double s)
        {
            scale = (float)s;

            DirSqrWeight.Clear();
            MinSqrWeight = double.MaxValue;

            // Достаём всевозможные направления соседей из пары точек (1,1) и (2,2)
            Vector2Int pos = new Vector2Int(0, 0);
            for (int i = 0; i < 2; i++)
            {
                pos += Vector2Int.one;
                foreach (Vector2Int nbr in GetNbrs(pos))
                {
                    if (DirSqrWeight.ContainsKey(nbr))
                        continue;

                    double sqrWeight =
                        (GetPosition(pos + nbr) - GetPosition(pos)).sqrMagnitude;

                    if (sqrWeight < MinSqrWeight)
                        MinSqrWeight = sqrWeight;

                    DirSqrWeight.Add(nbr, sqrWeight);
                }
            }
            HorizontalStep = (GetPosition(Vector2Int.zero) - GetPosition(Vector2Int.right) ).magnitude;
        }

        public double GetSqrWeight(Vector2Int nbr) => DirSqrWeight[nbr];

        public Vector2Int[] GetNbrs(Vector2Int pos)
        {
            if (!NeedShift)
                return neighbors;

            // Гекс-сетка: чётные строки — neighbors, нечётные — shiftN
            if (pos.y % 2 == 0)
                return neighbors;
            else
                return shiftN;
        }


        public virtual Vector2 GetPosition(Vector2Int id)
        {
            Vector2 p = BaseV[0] * id.x + BaseV[1] * id.y;
            return scale * p;
        }


        public virtual Vector2Int GetIdOfPoint(Vector2 pos)
        {
            int x, y;

            // Обычная регулярная решётка:
            // pos = scale * (i * e1 + j * e2), e1=(1,0), e2=(0,1)
            // i ~ x/scale, j ~ y/scale
            float ix = pos.x / (scale * BaseV[0].x);
            float iy = pos.y / (scale * BaseV[1].y);

            x = Mathf.RoundToInt(ix);
            y = Mathf.RoundToInt(iy);


            return new Vector2Int(x, y);
        }

        public virtual double GetCellArea()
        {
            return HorizontalStep * HorizontalStep;
        }
        public virtual double GetFaceLength()
        {
            return HorizontalStep;
        }
    }
}
