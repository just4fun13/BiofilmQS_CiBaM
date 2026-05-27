
using CellularAutomaton;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Assets.Scripts.MVVM_CA.Models._2D
{
    public class Model2DClassic : Model2D
    {
        private Vector2 MinBounds => new Vector2(MinX, MinY);
        private Vector2 MaxBounds => new Vector2(MaxX, MaxY);
        private Dictionary<ModelType, Vector2[]> GridBaseDic;
        private Dictionary<ModelType, Vector2Int[]> GridNbrsDic;
        private List<Vector2Int> BiomassCells = new List<Vector2Int>();
        object locker = new object();
        object lockerNutr = new object();
        object lockerRemoveFront = new object();


        private int maxTryCount = 50;
        private int maxDivCount = 50;
        private int[,] tryCount;
        private int[,] divCount;
        private float divProb = 0.5f;


        public Model2DClassic(int W, int H, float initSub, ModelType gridT, double TimeStep, int maxThread)
        {
            gridType = gridT;
            AreaWidth = W;
            AreaHeight = H;
            NutrientAreaWidth = AreaWidth / NutrGridSimpl;
            NutrientAreaHeight = AreaHeight / NutrGridSimpl;
            maxThreadCount = maxThread;

            DeltaTime = TimeStep;
            InitSubstrateCount = initSub;
            rng = new ThreadLocal<System.Random>(() => new System.Random());
            Cells2D = new CellState[AreaWidth, AreaHeight];
            divCount = new int[NutrientAreaWidth, NutrientAreaHeight];
            Bacteria2D = new double[AreaWidth, AreaHeight];
            frontPoints2D = new List<Vector2>();
            frontCells2D = new List<Vector2Int>();
            GridBaseDic = new Dictionary<ModelType, Vector2[]>
            {
                {ModelType.SimpleSquare,   SquareBase  },
                {ModelType.Hexagon, HexBase },
                {ModelType.ExtendedSquare, SquareExtendedBase }
            };
            GridNbrsDic = new Dictionary<ModelType, Vector2Int[]>
            {
                {ModelType.SimpleSquare,   SquareNbrs  },
                {ModelType.Hexagon, HexagonNbrs },
                {ModelType.ExtendedSquare, SquareExtendedNbrs }
            };
            InoculateInitialBacterialLayer();
        }
        private void InoculateInitialBacterialLayer()
        {
            int xMin = 0;
            int xMax = AreaWidth;
            int n = (int)(AreaWidth * 0.12f);
            int d = (xMax - xMin) / n;


            for (int i = n; i < AreaWidth; i += n)
                NewCell(new Vector2Int(i, 0), maxDivCount);

            /*            while (n > 0)
                        {
                            n--;
                            int k = Random.Range(0, AreaWidth);
                            if (Cells2D[k, 0] == CellState.empty)
                                NewCellBlank(new Vector2Int(k, 0));
                        }
            */
            BiomassCells.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
        }
        public override Vector2 GetPos(Vector2Int coord)
        {
            if (gridType != ModelType.Hexagon)
            {
                Vector2 v = coord.x * GridBaseDic[gridType][0] + coord.y * GridBaseDic[gridType][1];
                return v;
            }
            Vector2 v2 = coord.x * HexBase[0] + coord.y * HexBase[1];
            if (coord.y % 2 != 0)
                v2 += 0.5f * Vector2.right;
            return v2;
        }
        private void NewCell(Vector2Int cellIndex, int divC)
        {
            divCount[cellIndex.x, cellIndex.y] = divC;
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
                    frontCells2D.Add(v + cellIndex);
            }
        }
        //////////////////////////////////////////////////////////////////////
        private void Divide(Vector2Int pos)
        {
            divCount[pos.x, pos.y]--;
            if (divCount[pos.x, pos.y] == 0)
                Cells2D[pos.x, pos.y] = CellState.busyCanNot;
            List<Vector2Int> dirs = new List<Vector2Int>();
            /*          List<Vector2Int> farNbrs = new List<Vector2Int>();
                      List<Vector2Int> farNbrs2 = new List<Vector2Int>();
            */
            Vector2Int[] CellNbrs = GetNbrs(pos.y);
            int emtpyNbrsCount = 0;
            foreach (Vector2Int nbr in CellNbrs)
            {
                Vector2Int p = pos + nbr;
                if (IsLegal(p))
                {
                    if (Cells2D[p.x, p.y] == CellState.empty)
                    {
                        dirs.Add(p);
                        emtpyNbrsCount++;
                    }
                }
            }
            if (dirs.Count > 0)
            {
                SelectDirAndDivide(pos, dirs);
            }
            else
            {
                Cells2D[pos.x, pos.y] = CellState.busyCanNot;
            }
        }
        //////////////////////////////////////////////////////////////////////
        private void SelectDirAndDivide(Vector2Int pos, List<Vector2Int> dirs)
        {
            lock (locker)
            {
                Vector2Int dir;
                /**/
                dir = dirs[rng.Value.Next(dirs.Count)];
                Bacteria2D[pos.x, pos.y] /= 2f;// C[pos.x, pos.y] - ConcToDivide;
                NewCell(dir, divCount[pos.x, pos.y]);
            }
        }
        public override void DoGrowthStep()
        {
            //        var watch = new Stopwatch();
            NewBiomassCells.Clear();
            newFrontCells2D.Clear();
            //Parallel.ForEach(BiomassCells, (cellPos) =>
            averageConsume = 0f;
            //foreach (Vector2Int cellPos in BiomassCells)

            // Create a ParallelOptions object with a MaxDegreeOfParallelism of 4
            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
            //            for (int i = 0; i < difCOunt; i++)
            Parallel.ForEach(BiomassCells, parallelOptions, cellPos =>
            //foreach (Vector2Int cellPos in BiomassCells)
            {
                if (Cells2D[cellPos.x, cellPos.y] == CellState.busyCanDiv && rng.Value.NextDouble() > divProb)
                    Divide(cellPos);
            }
            );
            BiomassCells.AddRange(NewBiomassCells);
            frontCells2D.AddRange(newFrontCells2D);
        }
        private bool HasFreeSpaceAround(Vector2Int coord)
        {
            Vector2Int[] nbrs = GetNbrs(coord.y);
            foreach (Vector2Int v in nbrs)
            {
                Vector2Int sum = coord + v;
                if (IsLegal(sum) && Cells2D[sum.x, sum.y] == CellState.empty)
                    return true;
            }
            return false;
        }
        public override Vector2 GetFractalDimension()
        {
            frontCells2D.Clear();
            frontPoints2D.Clear();
            foreach (Vector2Int v in BiomassCells)
                if (HasFreeSpaceAround(v))
                {
                    frontCells2D.Add(v);
                    AddPointToFront(GetPos(v));
                }
            return BoxCountingMachine.GetFractalDimension(new Vector4(MinX, MinY, MaxX, MaxY), frontPoints2D);
        }
        private void AddPointToFront(Vector2 Pos)
        {
            if (gridType == ModelType.Hexagon)
            {
                TryAddFrontPoint(Pos + new Vector2(0, 0.5f));
                TryAddFrontPoint(Pos + new Vector2(0, -0.5f));
                TryAddFrontPoint(Pos + new Vector2(0.5f, 0.25f));
                TryAddFrontPoint(Pos + new Vector2(-0.5f, 0.25f));
                TryAddFrontPoint(Pos + new Vector2(0.5f, -0.25f));
                TryAddFrontPoint(Pos + new Vector2(-0.5f, -0.25f));
            }
            else
            {
                TryAddFrontPoint(Pos + 0.5f * Vector2.one);
                TryAddFrontPoint(Pos - 0.5f * Vector2.one);
                TryAddFrontPoint(Pos + 0.5f * new Vector2(1, -1));
                TryAddFrontPoint(Pos + 0.5f * new Vector2(-1, 1));
            }
        }
        private void TryAddFrontPoint(Vector2 point)
        {
            if (!frontPoints2D.Contains(point))
                frontPoints2D.Add(point);
        }
        public override void AddNutrientAtHighLevel()
        {
            Debug.Log($"Added nutrient at HIGH !");
            for (int i = 0; i < NutrientAreaWidth; i++)
                Substrate2D[i, NutrientAreaHeight - 1] = 10 * InitSubstrateCount * NutrGridSimpl * NutrGridSimpl;//rng.Value.NextDouble();
        }
    }
}
