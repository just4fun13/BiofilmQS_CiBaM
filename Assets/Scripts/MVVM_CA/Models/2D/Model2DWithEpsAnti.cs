using CellularAutomaton;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Scripts.MVVM_CA.SimExpert2D;
using Random = UnityEngine.Random;

namespace Assets.Scripts.MVVM_CA.Models._2D
{
    public class Model2DWithEpsAnti : Model2D
    {

        private Vector2 MinBounds => new Vector2(MinX, MinY);
        private Vector2 MaxBounds => new Vector2(MaxX, MaxY);
        private Dictionary<ModelType, Vector2[]> GridBaseDic;

        private int bottomLayerHeight = 0;
        object divLocker = new object();
        object lockerNutr = new object();
        object bottomRemoveLocker = new object();


        private int MaxSpreadCount = 10;
        private double AHLscaler = 45;
        private readonly DiffusionSolver substrateDiffusionSolver;
        private readonly DiffusionSolver epsDiffusionSolver;
        private readonly DiffusionSolver lactonasDiffusionSolver;
        public Model2DWithEpsAnti(int W, int H, float initSub,  ModelType gridT, double TimeStep, int maxThread)
        {
            gridType = gridT;
            AreaWidth = W;
            AreaHeight = H;
            NutrientAreaWidth = AreaWidth / NutrGridSimpl;
            NutrientAreaHeight = AreaHeight / NutrGridSimpl;
            maxThreadCount = maxThread;
            BiomassCells2D = new List<Vector2Int>();
            BiomassCells2D.Clear();
            NewBiomassCells.Clear();
            newFrontCells2D.Clear();
            bottomLayer.Clear();
            DeltaTime = TimeStep;
            InitSubstrateCount = initSub;
            rng = new ThreadLocal<System.Random>(() => new System.Random());
            Cells2D = new CellState[AreaWidth, AreaHeight];
            Substrate2D = new double[NutrientAreaWidth, NutrientAreaHeight];
            Lactonas2D = new double[NutrientAreaWidth, NutrientAreaHeight];
            Eps2D = new double[NutrientAreaWidth, NutrientAreaHeight];
            Bacteria2D = new double[AreaWidth, AreaHeight];
            substrateDiffusionSolver = new DiffusionSolver();
            substrateDiffusionSolver.Init(Substrate2D, 1, TimeStep, DiffusionKoef, maxThreadCount);
            epsDiffusionSolver = new DiffusionSolver();
            epsDiffusionSolver.Init(Eps2D, 1, TimeStep, AhlDiffusionKoef, maxThreadCount);
            lactonasDiffusionSolver = new DiffusionSolver();
            lactonasDiffusionSolver.Init(Lactonas2D, 1, TimeStep, DiffusionKoef, maxThreadCount);
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
                {ModelType.Hexagon,        HexagonNbrs },
                {ModelType.ExtendedSquare, SquareExtendedNbrs }
            };
            SetInitSubstrate();
            //SetInitAntibio();
        }
        public override void InitInoculate()
        {
            InoculateInitialBacterialLayer();
        }
        private void SetInitSubstrate()
        {
            for (int i = 0; i < NutrientAreaWidth; i++)
                for (int j = 0; j < NutrientAreaHeight; j++)
                    Substrate2D[i, j] = InitSubstrateCount * NutrGridSimpl * NutrGridSimpl;//rng.Value.NextDouble();
        }
        public override void AddAntiSubstance()
        {
            for (int i = 0; i < NutrientAreaWidth; i++)
                    Lactonas2D[i, AreaHeight-1] = 30;//rng.Value.NextDouble();
        }

        private void DiffuseSubstrateAndAHL()
        {
            double[] sums = new double[NutrientAreaWidth];
            double[] maxsUhl = new double[NutrientAreaWidth];
            int[] sumCounts = new int[NutrientAreaWidth];
            double AverageNutr = 0;
            double[] sumsNutr = new double[NutrientAreaWidth];
            double[,] S2Dnew = new double[NutrientAreaWidth, NutrientAreaHeight];
            double[,] E2Dnew = new double[NutrientAreaWidth, NutrientAreaHeight];
            double[,] A2Dnew = new double[NutrientAreaWidth, NutrientAreaHeight];
            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };

            Parallel.For(0, NutrientAreaWidth, parallelOptions, i =>
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    sumsNutr[i] += Substrate2D[i, j];
                    S2Dnew[i, j] = Substrate2D[i, j] + (DiffusionKoef       * (AverageSubstanceAround(new Vector2Int(i, j), Substrate2D) - Substrate2D[i, j])) * DeltaTime;
                    E2Dnew[i, j] = Eps2D[i, j] + (AhlDiffusionKoef * (AverageSubstanceAround(new Vector2Int(i, j), Eps2D) - Eps2D[i, j])) * DeltaTime;
                    A2Dnew[i, j] = Lactonas2D[i, j] + (DiffusionKoef * (AverageSubstanceAround(new Vector2Int(i, j), Lactonas2D) - Lactonas2D[i, j])) * DeltaTime;
                    if (E2Dnew[i, j] <= 0.1)
                        E2Dnew[i, j] = 0;
                    else
                        E2Dnew[i, j] = E2Dnew[i, j] * AHLdegrPerHour;
                }
            });

            int sumCount = 0;
            maxAhl = 0;
            averageUHL = 0;

            Parallel.For(0, NutrientAreaWidth, parallelOptions, i =>
            {

                //for (int i = 0; i < NutrientAreaWidth; i++)
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    Substrate2D[i, j] = S2Dnew[i, j];
                    Eps2D[i, j] = E2Dnew[i, j];
                    Lactonas2D[i, j] = A2Dnew[i, j];
                    //NormalizeAHL(U2D[i, j]/maxUhl);
                }
            });

            int sumCountBac = 0;
            for (int i = 0; i < NutrientAreaWidth; i++)
            {
                AverageNutr += sumsNutr[i];
            }
            AverageNutr /= NutrientAreaWidth* NutrientAreaHeight;
            AverageNutrientRemain = AverageNutr;
//            Debug.Log($"Average UHL Level = {averageUHL} to max {maxUhl} (SumCount)={sumCount}, Average BAC Level = {AverageBac}");
        }
        private void DiffuseSubstrateAndAHLImproved()
        {
            substrateDiffusionSolver.RefreshAndDoStep(Substrate2D, AhlDiffusionKoef);
            CopyArray(substrateDiffusionSolver.u, Substrate2D);
//            S2D = DiffusionSolver.u;
            epsDiffusionSolver.RefreshAndDoStep(Eps2D, DiffusionKoef);
//            E2D = DiffusionSolver.u;
            CopyArray(epsDiffusionSolver.u, Eps2D);
            lactonasDiffusionSolver.RefreshAndDoStep(Lactonas2D, DiffusionKoef);
//            A2D = DiffusionSolver.u;
            CopyArray(lactonasDiffusionSolver.u, Lactonas2D);
        }
        private void CopyArray(double[,] from, double[,] to)
        {
            for (int i = 0; i < from.GetLength(0);i++)
                for (int j = 0; j < from.GetLength(1);j++)
                    to[i, j] = from[i, j];
        }

        private double AverageSubstanceAround(Vector2Int pos, Double[,] substance)
        {
            double av = 0;
            int k = 0;
            Vector2Int[] nbrs = GetNbrs(pos.y);
            foreach (Vector2Int nbr in nbrs)
            {
                Vector2Int v = pos + nbr;
                if (IsLegalNutr(v))
                {
                    k++;
                    av += substance[v.x, v.y];
                }
            }
            av = av * 1d / k;
            return av;
        }

        private void InoculateInitialBacterialLayer()
        {
            for (int i = 0; i < AreaWidth; i++)
                bottomLayer.Add(i);

            int h = (AreaWidth / (InoculationCount + 1)) + 1;


            for (int i = h; i < AreaWidth; i += h)
            {
                NewCellBlank(new Vector2Int(i, 0));
                bottomLayer.Remove(i);
            }
            BiomassCells2D.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
        }
        private bool IsLegalNutr(Vector2Int v) => v.x >= 0 && v.y >= 0 && v.x < NutrientAreaWidth && v.y < NutrientAreaHeight;

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


        private void ConsumeSubstrateAndProduceEPS(Vector2Int pos)
        {

            int i = (pos.x / NutrGridSimpl);
           int j = (pos.y / NutrGridSimpl);
           double v = GetConsume(Bacteria2D[pos.x, pos.y], Substrate2D[i, j]);
            if (v < 0)
                v = 0;
            if (v > 1 || v < 0)
                Debug.LogError($"Unpredictable consume value {v} for {pos} position debug : {GetConsumeDebug(Bacteria2D[pos.x, pos.y], Substrate2D[i, j])} ");
            // double v = U2D[i, j] > AHLthreshold ? GetConsumeExtend(C2D[pos.x, pos.y], S2D[i, j], U2D[i, j]) : GetConsume(C2D[pos.x, pos.y], S2D[i, j]);
            //averageConsume += v;

           lock (lockerNutr)
           {
                /*if (Cells2D[i, j] == CellState.busyCanDiv || Cells2D[i, j] == CellState.busyCanNot)
                    E2D[i, j] += C2D[i, j] * 0.1;
               */
                Substrate2D[i, j] -= v;
                if (Substrate2D[i, j] < 0)
                    Substrate2D[i, j] = 0;
               Bacteria2D[pos.x, pos.y] += v;
               
                if (Lactonas2D[i, j] > 0)
                {
                    Bacteria2D[i, j] -= Lactonas2D[i, j];
                    Lactonas2D[i, j] = 0;
                }



               if (Bacteria2D[i, j] < AntiDieTh)
                {
                    CellDie(new Vector2Int(i, j));
                }
           }
        }

        private void CellDie(Vector2Int pos)
        {
            int i = pos.x;
            int j = pos.y;
            Substrate2D[i, j] += Bacteria2D[i, j];
            Bacteria2D[i, j] = 0;
            Cells2D[i, j] = CellState.empty;
            deadCells2D.Add(pos);
        }

        //////////////////////////////////////////////////////////////////////
        private void TryDivide(Vector2Int pos)
        {
            List<Vector2Int> freeDirsAround = FreeDirsAround(pos);
            List<Vector2Int> horDirsAround = FreeHorizontalDirsAround(pos);
            List<Vector2Int> verDirsAround = FreeVerticalDirsAround(pos);
            bool HasFreeSpaceAround = (freeDirsAround.Count > 0);
            bool HasSpreadSpace = (bottomLayer.Count > 0);
            // try Spread
            if (HasSpreadSpace)
            {
                if (rng.Value.NextDouble() < SpreadProbMax)
                {
                    lock (bottomRemoveLocker)
                    {
                        RangedSpreadDivide(pos);
                        return;
                    }
                }
            }
            double dirFocusProbability = 1;
            if (HasFreeSpaceAround)
            {

                Divide(pos, freeDirsAround[rng.Value.Next(0, freeDirsAround.Count)]);
            }

        }



        private void SpreadDivide(Vector2Int pos)
        {
            int x = bottomLayer[rng.Value.Next(0, bottomLayer.Count)];
            Vector2Int dirToDiv = new Vector2Int(x, 0);
            bottomLayer.Remove(x);
            Divide(pos, dirToDiv);
        }
        private void RangedSpreadDivide(Vector2Int pos)
        {
            List<Vector2Int> nodesInRange = new List<Vector2Int>();
            int range = 2*pos.y + 2;
            foreach (int xVal in bottomLayer) 
            {
                if (xVal >= pos.x - range && xVal <= pos.x + range)
                    nodesInRange.Add(new Vector2Int(xVal, 0));
            }
            if (nodesInRange.Count == 0)
                return;
            Vector2Int dirToDiv = nodesInRange[rng.Value.Next(0, nodesInRange.Count)];
            bottomLayer.Remove(dirToDiv.x);
            Divide(pos, dirToDiv);
        }
        private void PushDivde(Vector2Int pos)
        {
            Vector2Int randomCellOnFront;
            randomCellOnFront = frontCells2D[rng.Value.Next(0, frontCells2D.Count)];
            Divide(pos, randomCellOnFront);
        }
        private void Divide(Vector2Int fromPos, Vector2Int toPos)
        {
            Bacteria2D[fromPos.x, fromPos.y] /= 2f;
            NewCell(toPos, Bacteria2D[fromPos.x, fromPos.y]);
        }
 /// <summary>
 /// ////////////////////////////////////////////////////////////////////////////////
 /// </summary>
        public override void DoGrowthStep()
        {
            foreach (Vector2Int frontCell in newFrontCells2D)
            {
                if (frontCell.y == 0 && bottomLayer.Contains(frontCell.x))
                    bottomLayer.Remove(frontCell.x);
            }

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
            //DiffuseSubstrateAndAHLImproved();
            DiffuseSubstrateAndAHL();
            //            Parallel.ForEach(BiomassCells, parallelOptions, cellPos =>
            foreach (Vector2Int cellPos in BiomassCells2D)
            {
                if (Cells2D[cellPos.x, cellPos.y] == CellState.busyCanDiv)
                    ConsumeSubstrateAndProduceEPS(cellPos);
                Bacteria2D[cellPos.x, cellPos.y] -= LifetimeCost;
                if (Bacteria2D[cellPos.x, cellPos.y] > ConcToDivide )
                    TryDivide(cellPos);
            }
            //);
            averageConsume /= BiomassCells2D.Count;
            foreach (Vector2Int v in deadCells2D)
            BiomassCells2D.Remove(v);
            deadCells2D.Clear();
            BiomassCells2D.AddRange(NewBiomassCells);
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
        private List<Vector2Int> FreeDirsAround(Vector2Int coord)
        {
            List <Vector2Int> dirs = new List<Vector2Int>();
            Vector2Int[] nbrs = GetNbrs(coord.y);
            foreach (Vector2Int v in nbrs)
            {
                Vector2Int sum = coord + v;
                if (IsLegal(sum) && Cells2D[sum.x, sum.y] == CellState.empty)
                    dirs.Add(sum);
            }
            return dirs;
        }
        private List<Vector2Int> FreeHorizontalDirsAround(Vector2Int coord)
        {
            List<Vector2Int> dirs = new List<Vector2Int>();
            Vector2Int[] nbrs = GetNbrs(coord.y);
            foreach (Vector2Int v in nbrs)
            {
                if (v.y > 0)
                    continue;
                Vector2Int sum = coord + v;
                if (IsLegal(sum) && Cells2D[sum.x, sum.y] == CellState.empty)
                    dirs.Add(sum);
            }
            return dirs;
        }
        private List<Vector2Int> FreeVerticalDirsAround(Vector2Int coord)
        {
            List<Vector2Int> dirs = new List<Vector2Int>();
            Vector2Int[] nbrs = GetNbrs(coord.y);
            foreach (Vector2Int v in nbrs)
            {
                if (v.y == 0)
                    continue;
                Vector2Int sum = coord + v;
                if (IsLegal(sum) && Cells2D[sum.x, sum.y] == CellState.empty)
                    dirs.Add(sum);
            }
            return dirs;
        }
        public override Vector2 GetFractalDimension()
        {
            frontCells2D.Clear();
            frontPoints2D.Clear();
            foreach (Vector2Int v in BiomassCells2D)
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
               Substrate2D[i, NutrientAreaHeight-1] = 10 * InitSubstrateCount * NutrGridSimpl * NutrGridSimpl;//rng.Value.NextDouble();
        }
        public override int BiomassCount => BiomassCells2D.Count;
        public override int BottomLayerCountRemain()
        {
            return bottomLayer.Count;
        }
    }
}
