using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.MVVM_CA.Models._2D
{
    public class Model2D : Model
    { 
        public double[,] Substrate2D { get; protected set; }
        public double[,] Bacteria2D { get; protected set; }
        public double[,] Ahl2D { get; protected set; }
        public double[,] Eps2D { get; protected set; }
        public double[,] Lactonas2D { get; protected set; }


        protected Vector2[] SquareBase =
        {
                new Vector2( 1f, 0f),
                new Vector2( 0f, 1f),
        };
        protected Vector2[] SquareExtendedBase =
        {
                new Vector2( 1f, 0f),
                new Vector2( 0f, 1f),
            };
        protected Vector2[] HexBase =
        {
                new Vector2( 1f,    0f),
                new Vector2( 0f, 0.8660254f),
            };
        protected Vector2[] DiamondBase =
        {
                new Vector2( 1f,    0f),
                new Vector2( 0f, 0.8660254f),
            };

        protected Vector2Int[] SquareNbrs =
        {
                new Vector2Int( 0, 1),
                new Vector2Int( 1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int( 0,-1),
        };
        protected Vector2Int[] SquareExtendedNbrs =
        {
                new Vector2Int( 0, 1),
                new Vector2Int( 1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int( 0,-1),
                new Vector2Int( 1, 1),
                new Vector2Int( 1, -1),
                new Vector2Int(-1, 1),
                new Vector2Int( -1,-1),
        };
        protected Vector2Int[] HexagonNbrs =
        {
                new Vector2Int( 1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int( 0,-1),
                new Vector2Int( 0, 1),
                new Vector2Int( 1,-1),
                new Vector2Int( 1, 1),
        };
        protected Vector2Int[] HexagonNegNbrs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( -1,-1),
            new Vector2Int( -1, 1),
            new Vector2Int( 0,-1),
            new Vector2Int( 0, 1),
        };
        protected Vector2Int[] DiamondNbrs =
        {
                new Vector2Int( 1, 1),
                new Vector2Int( 0,-1),
                new Vector2Int( 1,-1),
                new Vector2Int( 0, 1),
        };
        protected Vector2Int[] DiamondNegNbrs =
        {
            new Vector2Int( 0, 1),
            new Vector2Int( -1,-1),
            new Vector2Int( 0,-1),
            new Vector2Int( -1, 1),
        };
        protected List<Vector2Int> NewBiomassCells = new List<Vector2Int>();
        protected List<Vector2Int> newFrontCells2D = new List<Vector2Int>();
        protected List<Vector2Int> deadCells2D = new List<Vector2Int>();
        protected List<int> bottomLayer = new List<int>();
        protected Dictionary<ModelType, Vector2Int[]> GridNbrsDic;
        protected object lockerRemoveFront = new object();

        protected bool setTopNutrient = false;

        public double Biomass2DVolume()
        {
            double totalVol = 0;
            foreach (double c in Bacteria2D)
                if (c > 0)
                    totalVol += c;
            return totalVol;
        }
        protected bool IsLegal(Vector2Int v) => v.x >= 0 && v.y >= 0 && v.x < AreaWidth && v.y < AreaHeight;
        protected void TryBounds(Vector2 v)
        {
            if (v.x < MinX) MinX = v.x;
            if (v.x > MaxX) MaxX = v.x;
            if (v.y < MinY) MinY = v.y;
            if (v.y > MaxY) MaxY = v.y;
        }
        protected Vector2Int[] GetNbrs(int y)
        {
            if (gridType != ModelType.Hexagon || y % 2 != 0)
                return GridNbrsDic[gridType];
            return HexagonNegNbrs;
        }
        protected void SetInitSubstrate()
        {
            for (int i = 0; i < NutrientAreaWidth; i++)
                for (int j = 0; j < NutrientAreaHeight; j++)
                    Substrate2D[i, j] = InitSubstrateCount * NutrGridSimpl * NutrGridSimpl;//rng.Value.NextDouble();
        }
        protected void SetTopNutrient(double nutrVal)
        {
            for (int i = 0; i < NutrientAreaWidth; i++)
            {
                int j = NutrientAreaHeight - 1;
                Substrate2D[i, j] = nutrVal * NutrGridSimpl * NutrGridSimpl;//rng.Value.NextDouble();
            }
        }
        protected void NewCellBlank(Vector2Int cellIndex)
        {
            Bacteria2D[cellIndex.x, cellIndex.y] = ConcToDivide / 2d;
            Vector2 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            NewBiomassCells.Add(cellIndex);
            CellCount++;
            Cells2D[cellIndex.x, cellIndex.y] = CellState.busyCanDiv;
            Vector2Int[] nbrs = GetNbrs(cellIndex.y);
            foreach (Vector2Int v in nbrs)
            {
                Vector2Int nbrPos = v + cellIndex;
                if (IsLegal(nbrPos) && Cells2D[nbrPos.x, nbrPos.y] == CellState.empty)
                    newFrontCells2D.Add(v + cellIndex);
            }
        }
        protected void NewCell(Vector2Int cellIndex, double Bac)
        {
            Bacteria2D[cellIndex.x, cellIndex.y] = Bac;
            Vector2 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            NewBiomassCells.Add(cellIndex);
            CellCount++;
            Cells2D[cellIndex.x, cellIndex.y] = CellState.busyCanDiv;
            lock (lockerRemoveFront)
            {
                if (frontCells2D.Contains(cellIndex))
                    frontCells2D.Remove(cellIndex);
                if (cellIndex.y == 0 && bottomLayer.Contains(cellIndex.x))
                    bottomLayer.Remove(cellIndex.x);
            }
            Vector2Int[] nbrs = GetNbrs(cellIndex.y);
            foreach (Vector2Int v in nbrs)
            {
                Vector2Int nbrPos = v + cellIndex;
                if (IsLegal(nbrPos) && Cells2D[nbrPos.x, nbrPos.y] == CellState.empty)
                    newFrontCells2D.Add(v + cellIndex);
            }
        }

        public CellState[,] Cells2D { get; protected set; }

        public List<Vector2> frontPoints2D { get; protected set; }
        public List<Vector2Int> frontCells2D { get; protected set; }
        public List<Vector2Int> BiomassCells2D { get; protected set; }

    }
}
