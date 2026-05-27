using CellularAutomaton;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using UnityEngine;
using static Assets.Scripts.MVVM_CA.SimExpert2D;
using Random = UnityEngine.Random;

namespace Assets.Scripts.MVVM_CA.Models._2D
{
    public class FiveStarModel : Model2D
    {

        private Dictionary<ModelType, Vector2[]> GridBaseDic;
        object lockerNutr = new object();
        object bottomRemoveLocker = new object();

        private double[,] Substrate2Dnew;
        private double[,] Ahl2Dnew;
        private double[,] Eps2Dnew;
        private double[,] Lactonas2Dnew;


        private double AHLscaler = 45;
        private readonly DiffusionSolver diffusionSolver;
        public FiveStarModel(int W, int H, float initSub, ModelType gridT, double TimeStep, int maxThread)
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
            Ahl2D       = new double[NutrientAreaWidth, NutrientAreaHeight];
            Eps2D       = new double[NutrientAreaWidth, NutrientAreaHeight];
            Lactonas2D  = new double[NutrientAreaWidth, NutrientAreaHeight];
            Bacteria2D = new double[AreaWidth, AreaHeight];

            Substrate2Dnew = new double[NutrientAreaWidth, NutrientAreaHeight];
            Ahl2Dnew       = new double[NutrientAreaWidth, NutrientAreaHeight];
            Eps2Dnew       = new double[NutrientAreaWidth, NutrientAreaHeight];
            Lactonas2Dnew  = new double[NutrientAreaWidth, NutrientAreaHeight];


            OnAHL = new double[NutrientAreaWidth, NutrientAreaHeight];

            diffusionSolver = new DiffusionSolver();
            diffusionSolver.Init(Substrate2D, 1, 1, DiffusionKoef, maxThreadCount);

            U2Dnorm = new double[NutrientAreaWidth, NutrientAreaHeight];

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
        }
        public override void InitInoculate()
        {
            InoculateInitialBacterialLayer();
        }

        private void DiffuseFieldsParallel()
        {
            var opt = new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount };

            Parallel.For(0, NutrientAreaWidth, opt, (Action<int>)(i =>
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    // -------- Nutrient diffusion (blocked by EPS) --------
                    // EPS без диффузии, поэтому Eps2D — на nutr-grid (как у тебя сейчас)
                    // Блокировку делаем экспонентой: Deff = D * exp(-gamma * EPS)
                    double block = 1.0 / (1.0 + EpsDifReducion * Eps2D[i, j]);
                    double Deff = DiffusionKoef * block; 
                    //double Deff = DiffusionKoef * Math.Exp(-EpsDifReducion * eps);
                    double Navg = AverageSubstanceAround(new Vector2Int(i, j), Substrate2D);

                    double Nval = Substrate2D[i, j] + Deff * (Navg - Substrate2D[i, j]) * DeltaTime;
                    if (Nval < 0) Nval = 0;
                    Substrate2Dnew[i, j] = Nval;

                    // -------- AHL diffusion + decay + quenching by lactonase --------
                    double Uavg = AverageSubstanceAround(new Vector2Int(i, j), Ahl2D);
                    double Uval = Ahl2D[i, j] + AhlDiffusionKoef * (Uavg - Ahl2D[i, j]) * DeltaTime;

                    // деградация AHL (пер-шаговый множитель)
                    Uval *= AHLdegrPerHour;

                    // quenching: U -= LacQs * L * U * dt
                    double Lhere = Lactonas2D[i, j];
                    // quenching rate: Q = kq * L * U/(Kq+U)
                    double Q = Lac_k_q * Lhere * (Uval / (LacKq + Uval));
                    Uval -= Q * DeltaTime;

                    if (Uval < 1e-12) Uval = 0;
                    Ahl2Dnew[i, j] = Uval;

                    // -------- Lactonase diffusion + decay --------
                    double Lavg = AverageSubstanceAround(new Vector2Int(i, j), Lactonas2D);
                    double Lval = Lactonas2D[i, j] + LacDiffusionKoef * (Lavg - Lactonas2D[i, j]) * DeltaTime;

                    // деградация лактоназы (пер-шаговый множитель)
                    Lval *= LacDegr;

                    // saturation
                    if (Lval < 1e-12) Lval = 0;
                    if (Lval > LacMax) Lval = LacMax;
                    Lactonas2Dnew[i, j] = Lval;

                    // -------- EPS: no diffusion, only decay --------
                    Eps2Dnew[i, j] = Eps2D[i, j] * EpsDegr;
                    //if (Eval < 1e-12) Eval = 0;
                    //if (Eval > EpsMax) Eval = EpsMax;
                    //Eps2Dnew[i, j] = Eval;
                }
            }));

            // commit
            Parallel.For(0, NutrientAreaWidth, opt, i =>
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    Substrate2D[i, j] = Substrate2Dnew[i, j];
                    Ahl2D[i, j] = Ahl2Dnew[i, j];
                    Lactonas2D[i, j] = Lactonas2Dnew[i, j];
                    Eps2D[i, j] = Eps2Dnew[i, j];
                    U2Dnorm[i, j] = Ahl2D[i, j] / AHLscaler;
                }
            });
        }

        private double AverageSubstanceAround(Vector2Int pos, double[,] substance)
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
            if (k == 0)
                Debug.LogError($"0 neighbors sitation occurs in av substance around for cell {pos}");
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
                return;
            }

        }

        private void CellStep(Vector2Int pos)
        {
            int ni = pos.x / NutrGridSimpl;
            int nj = pos.y / NutrGridSimpl;
            if (!IsLegalNutr(new Vector2Int(ni, nj)))
                return;

            double B = Bacteria2D[pos.x, pos.y];
            double N = Substrate2D[ni, nj];
            double U = Ahl2D[ni, nj];
            double E = Eps2D[ni, nj];

            // 1) Nutrient consumption (Monod) + QS decrease
            double consume = GetConsume(B, N);
            consume -= GetConsumeDecrease(consume, U);
            if (consume < 0) consume = 0;

            lock (lockerNutr)
            {
                double take = Math.Min(Substrate2D[ni, nj], consume);
                Substrate2D[ni, nj] -= take;
                if (Substrate2D[ni, nj] < 0) Substrate2D[ni, nj] = 0;

                Bacteria2D[pos.x, pos.y] += take / Yxs;
            }

            // 2) AHL production (как раньше)
            Ahl2D[ni, nj] += GetAHLprod(Bacteria2D[pos.x, pos.y], Ahl2D[ni, nj]);

            // 3) EPS production (по новым параметрам)
            double epsProd = EPSProduction(Bacteria2D[pos.x, pos.y], Ahl2D[ni, nj]);
            if (epsProd > 0)
            {
                double prod = EpsAlpha * B + EpsBetta * B * epsProd;   //epsProd * (1.0 - E / EpsMax);
                Eps2D[ni, nj] = Math.Min(EpsMax, E + prod);
            }

            // 4) Lactonase production (по новым параметрам)
            double lacProd = LactonaseProduction(Bacteria2D[pos.x, pos.y], Ahl2D[ni, nj]);
            if (lacProd > 0)
            {
                Lactonas2D[ni, nj] += lacProd;
                if (Lactonas2D[ni, nj] > LacMax) Lactonas2D[ni, nj] = LacMax;
            }

            // 5) lifetime cost (важно: *DeltaTime, если LifetimeCost это "в единицу времени")

            // 6) death
            if (Bacteria2D[pos.x, pos.y] < AntiDieTh)
            {
                CellDie(pos);
                return;
            }

            // 7) QS -> division threshold (как у тебя было, но лучше без повторного TryDivide ниже)
            double th = Math.Max(1e-9, AHLthreshold);
            double u2 = Ahl2D[ni, nj] * Ahl2D[ni, nj];
            double q = u2 / (u2 + th * th);
            

            if (Bacteria2D[pos.x, pos.y] > ConcToDivide)
                TryDivide(pos);
        }
        private void CellDie(Vector2Int pos)
        {
            Cells2D[pos.x, pos.y] = CellState.empty;

            int ni = pos.x / NutrGridSimpl;
            int nj = pos.y / NutrGridSimpl;
            if (IsLegalNutr(new Vector2Int(ni, nj)))
            {
                lock (lockerNutr)
                {
                    Substrate2D[ni, nj] += Bacteria2D[pos.x, pos.y]; // возвращаем биомассу в питание (опционально)
                }
            }

            Bacteria2D[pos.x, pos.y] = 0;
            deadCells2D.Add(pos);
            Vector2Int[] nbrs = GetNbrs(pos.y);
            foreach (var v in nbrs)
            {
                var p = pos + v;
                if (IsLegal(p) && Cells2D[p.x, p.y] == CellState.busyCanDiv)
                    newFrontCells2D.Add(p);
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
            int range = 2 * pos.y + 2;
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
            //DiffuseSubstances();
            DiffuseFieldsParallel();
            //            Parallel.ForEach(BiomassCells, parallelOptions, cellPos =>
            foreach (Vector2Int cellPos in BiomassCells2D)
            {
                if (Cells2D[cellPos.x, cellPos.y] == CellState.busyCanDiv)
                    CellStep(cellPos);
            }

            // process dead cells
            if (deadCells2D.Count > 0)
            {
                foreach (var d in deadCells2D)
                    BiomassCells2D.Remove(d);
                deadCells2D.Clear();
            }

            averageConsume /= BiomassCells2D.Count;
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
            List<Vector2Int> dirs = new List<Vector2Int>();
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
            List<Vector2> points = new List<Vector2>();
            foreach (Vector2Int v in BiomassCells2D)
                points.Add(GetPos(v));

            return BoxCountingMachine.GetFractalDimension(new Vector4(MinX, MinY, MaxX, MaxY), points);
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
        public override int BiomassCount => BiomassCells2D.Count;
        public override int BottomLayerCountRemain()
        {
            return bottomLayer.Count;
        }
    }
}
