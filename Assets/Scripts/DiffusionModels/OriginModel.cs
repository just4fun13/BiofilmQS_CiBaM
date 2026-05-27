
using Assets.Scripts.MVVM_CA;
using CellularAutomaton;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class OriginModel
    {
        public int AreaWidth { get; private set; } = 160;
        public int AreaHeight { get; private set; } = 90;
        public Base2D GeometryBase;
        public int MaxDivCount { get; private set; } = 30;
        public int MaxTryCount { get;  private set;} = 10;
        public float DivisionProbability { get; private set; } = 0.5f;
        public float μ { get; private set; } = 4.3f;
        public CellState[,] cells { get; private set; }

        private List<Vector2Int> activeCells = new List<Vector2Int>();
        private List<Vector2Int> newCells = new List<Vector2Int>();
        private Dictionary<Vector2Int, CellInfo> cellData = new Dictionary<Vector2Int, CellInfo>();
        private int InitBacCells = 10;
        protected System.Random rng = new System.Random();

        public OriginModel(int w, int h, Base2D b, int maxT, int maxD, float divP, float _μ1)
        {
            AreaWidth = w;
            AreaHeight = h; 
            GeometryBase = b;
            cells = new CellState[AreaWidth, AreaHeight];
            MaxDivCount = maxD;
            MaxTryCount = maxT;
            μ = _μ1;
            DivisionProbability = divP;
            InoculateInitLayer();
        }

        private void InoculateInitLayer()
        {
            for (int i = 0; i < InitBacCells; i++)
            {
                Vector2Int newPos = new Vector2Int(i*AreaWidth/InitBacCells, 0);
                NewCell(newPos, 0);
            }
        }
        private void NewCell(Vector2Int pos, int DivCount)
        {
            CellInfo newCellInfo = new CellInfo(DivCount);
            cells[pos.x, pos.y] = CellState.busyCanDiv;
            cellData.Add(pos, newCellInfo);
            newCells.Add(pos);
        }

        private void TryDivide(Vector2Int pos)
        {
            Vector2Int[] nbrsAround = GeometryBase.GetNbrs(pos);
            foreach (Vector2Int nbr in nbrsAround)
            {
                Vector2Int newPos = nbr + pos;
                if (!InBound(newPos) || cells[newPos.x, newPos.y] != CellState.empty) continue;

                float prob = Mathf.Lerp(DivisionProbability / μ / μ, DivisionProbability, GeometryBase.GetPosition(nbr).y);
                if (rng.NextDouble() <= prob)
                {
                    cellData[pos].divisionCount++;
                    NewCell(pos + nbr, cellData[pos].divisionCount);
                }
            }
        }

        private bool InBound(Vector2Int pos) => pos.x >= 0 && pos.x < AreaWidth && pos.y >= 0 && pos.y < AreaHeight;

        public void DoGrowthStep()
        {
            activeCells.AddRange(newCells);
            newCells.Clear();
            foreach (Vector2Int v in activeCells)
            {
                cellData[v].tryCount++;
                if (cellData[v].tryCount >= MaxTryCount || cellData[v].divisionCount >= MaxDivCount)
                    cells[v.x, v.y] = CellState.busyCanNot;
                else
                    TryDivide(v);
            }
            for (int i = activeCells.Count-1; i >= 0; i--)
                if (cells[activeCells[i].x, activeCells[i].y] == CellState.busyCanNot)
                    activeCells.RemoveAt(i);
        }

        public bool IsDone => activeCells.Count > 0;




    }
}
