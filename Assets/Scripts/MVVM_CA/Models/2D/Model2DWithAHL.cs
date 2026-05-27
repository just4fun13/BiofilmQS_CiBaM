using CellularAutomaton;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;


namespace Assets.Scripts.MVVM_CA.Models._2D
{
    public class Model2DWithAHL : Model2D
    {

        private readonly Dictionary<ModelType, Vector2[]> GridBaseDic;
        private readonly object bottomRemoveLocker = new object();
        private readonly ParallelOptions biomassParallelOptions;

        private double[,] requestedSubstrate;
        private double[,] deltaAhl;
        private double[,] deltaBiomass;
        private double[,] substrateScale;
        private double[,] Substrate2Dnew;
        private double[,] Ahl2Dnew;


        private object[] nutrientRowLocks;


        // --- Quorum-dependent spreading params ---
        private bool UseGlobalAHLForSpread = false; // false = локальный AHL, true = средний по полю
        private double SpreadProbMin = 0.0;      // минимум
        private double QuorumHillN = 4.0;           // резкость

        // (опционально) влияние кворума на дальность spread
        private bool QuorumAffectsRange = false;
        private double RangeMultMin = 0.7;
        private double RangeMultMax = 2.0;
        private int TotalDivisionCount = 0;
        private int SpreadCount = 0;
        private double AhlProductionRate = 5000.0;
        private double AhlAutoInduction = 2.0;
        private readonly DiffusionSolver nutrientDiffusionSolver;
        private readonly DiffusionSolver ahlDiffusionSolver;

        private const double SquareAverageToLaplacianFactor = 4.0;

        private int[,] depthToFront;
        private long washoutDepthLastUpdateStep = -1;
        public void SetDiffusionSolverMode( DiffusionSolver.DiffusionMode mode = DiffusionSolver.DiffusionMode.Implicit)
        {
            nutrientDiffusionSolver.SetMode(mode);
            ahlDiffusionSolver.SetMode(mode);
        }
        // ===================== PROFILING =====================
        private bool enableProfiling = true;

        private enum ProfileSection
        {
            TotalGrowthStep = 0,
            PrepareStep = 1,
            DiffuseFields = 2,
            BuildConsumptionDeltas = 3,
            ApplyConsumptionDeltas = 4,
            LifetimeCostPhase = 5,
            DivisionPhase = 6,
            RecalcAverageConsume = 7,
            FinalizeStep = 8,
            UpdateStatistics = 9
        }

        private static readonly int ProfileSectionCount = Enum.GetValues(typeof(ProfileSection)).Length;

        private readonly long[] profileTicks = new long[ProfileSectionCount];
        private readonly int[] profileCalls = new int[ProfileSectionCount];
        private readonly long[] profileStarts = new long[ProfileSectionCount];

        private long profiledStepCount = 0;

        private void BeginSection(ProfileSection section)
        {
            if (!enableProfiling) return;
            profileStarts[(int)section] = Stopwatch.GetTimestamp();
        }

        private void EndSection(ProfileSection section)
        {
            if (!enableProfiling) return;

            int idx = (int)section;
            long end = Stopwatch.GetTimestamp();
            profileTicks[idx] += end - profileStarts[idx];
            profileCalls[idx]++;
        }

        private readonly struct ProfileScope : IDisposable
        {
            private readonly Model2DWithAHL owner;
            private readonly ProfileSection section;

            public ProfileScope(Model2DWithAHL owner, ProfileSection section)
            {
                this.owner = owner;
                this.section = section;
                owner.BeginSection(section);
            }

            public void Dispose()
            {
                owner.EndSection(section);
            }
        }

        private ProfileScope Measure(ProfileSection section)
        {
            return new ProfileScope(this, section);
        }

        public void SetProfiling(bool enabled)
        {
            enableProfiling = enabled;
        }

        public void ResetProfiling()
        {
            Array.Clear(profileTicks, 0, profileTicks.Length);
            Array.Clear(profileCalls, 0, profileCalls.Length);
            Array.Clear(profileStarts, 0, profileStarts.Length);
            profiledStepCount = 0;
        }

        public string GetProfilingReport()
        {
            var sb = new StringBuilder();
            double tickToMs = 1000.0 / Stopwatch.Frequency;

            //sb.AppendLine("=== Model2DWithAHL Profiling Report ===");
            //sb.AppendLine($"Profiled growth steps: {profiledStepCount}");


            sb.Append($"{maxThreadCount}\t{profiledStepCount}\t{profileTicks[(int)ProfileSection.DiffuseFields] * tickToMs}\t{BiomassCount}\t" +
                $"{profileTicks[(int)ProfileSection.TotalGrowthStep] * tickToMs}\t{AreaWidth}\t{AreaHeight}\t{AreaHeight*AreaWidth}");
//            return sb.ToString();
            foreach (ProfileSection section in Enum.GetValues(typeof(ProfileSection)))
            {
                int idx = (int)section;
                double totalMs = profileTicks[idx] * tickToMs;
                double avgMs = profileCalls[idx] > 0 ? totalMs / profileCalls[idx] : 0.0;

                //sb.AppendLine($"{section,-24} total = {totalMs,10:F3} ms   avg = {avgMs,8:F6} ms   calls = {profileCalls[idx]}");
                sb.Append($"\t{avgMs,8:F6}");
            }

            return sb.ToString();
        }
        // =================== END PROFILING ===================




        public Model2DWithAHL(int W, int H, float initSub,  ModelType gridT, double TimeStep, int maxThread, int randomSeed = 12345)
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
            Cells2D = new CellState[AreaWidth, AreaHeight];
            Substrate2D = new double[NutrientAreaWidth, NutrientAreaHeight];
            Substrate2Dnew = new double[NutrientAreaWidth, NutrientAreaHeight];
            Ahl2Dnew = new double[NutrientAreaWidth, NutrientAreaHeight];
            nutrientDiffusionSolver = new DiffusionSolver();
            ahlDiffusionSolver = new DiffusionSolver();
            Ahl2D = new double[NutrientAreaWidth, NutrientAreaHeight];
            //rng = new ThreadLocal<System.Random>(() => new System.Random());
            InitRandomSeed(randomSeed);

            U2Dnorm = new double[NutrientAreaWidth, NutrientAreaHeight];
            OnAHL = new double[NutrientAreaWidth, NutrientAreaHeight];
            Bacteria2D = new double[AreaWidth, AreaHeight];

            requestedSubstrate     = new double[NutrientAreaWidth, NutrientAreaHeight];
            deltaAhl           = new double[NutrientAreaWidth, NutrientAreaHeight];
            substrateScale     = new double[NutrientAreaWidth, NutrientAreaHeight];
            deltaBiomass       = new double[AreaWidth, AreaHeight];
            depthToFront = new int[AreaWidth, AreaHeight];

            nutrientRowLocks = new object[NutrientAreaWidth];
            for (int i = 0; i < NutrientAreaWidth; i++)
                nutrientRowLocks[i] = new object();


            biomassParallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };

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
            // инициализируем солверы по диффузии здесь!
            ahlDiffusionSolver.Init(Ahl2D, hStep: 1.0, deltaT: DeltaTime, diffusionKoef: AhlDiffusionKoef / SquareAverageToLaplacianFactor, maxTR: maxThreadCount);
            nutrientDiffusionSolver.Init( Substrate2D, hStep: 1.0, deltaT: DeltaTime, diffusionKoef: DiffusionKoef / SquareAverageToLaplacianFactor, maxTR: maxThreadCount );
            Debug.Log($"AHL D = {AhlDiffusionKoef} Nut D = {DiffusionKoef}");

            if (gridType == ModelType.Hexagon)
            {
                ahlDiffusionSolver.SetHexBackwardEulerOptions(200, 1e-8, 1.0);
                nutrientDiffusionSolver.SetHexBackwardEulerOptions(200, 1e-8, 1.0);
            }

            InoculateInitialBacterialLayer();
        }
        private void InoculateInitialBacterialLayer()
        {
            Debug.Log($"Inoc count = {InoculationCount}");
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

        private void DiffuseSubstrateAndAHLImproved()
        {
            ahlDiffusionSolver.RefreshAndDoStep(Ahl2D, AhlDiffusionKoef);
            for (int i = 0; i < Ahl2D.GetLength(0); i++)
                for (int j = 0; j < Ahl2D.GetLength(1); j++)
                {
                    Ahl2D[i, j] = ahlDiffusionSolver.u[i, j] * AHLdegrPerHour;
                    U2Dnorm[i, j] = Ahl2D[i, j] / AHLscaler;
                }

            nutrientDiffusionSolver.RefreshAndDoStep(Substrate2D, DiffusionKoef);
            for (int i = 0; i < Substrate2D.GetLength(0); i++)
                for (int j = 0; j < Substrate2D.GetLength(1); j++)
                    Substrate2D[i, j] = nutrientDiffusionSolver.u[i, j];
        }
        private void DiffuseFieldsParallel(double dt)
        {
            var opt = new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount };

            Parallel.For(0, NutrientAreaWidth, opt, i =>
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    // -------- Nutrient diffusion (blocked by EPS) --------
                    double navg = AverageSubstanceAround(i, j, Substrate2D);
                    double nval = Substrate2D[i, j] + DiffusionKoef * (navg - Substrate2D[i, j]) * dt;

                    if (nval < 0.0) nval = 0.0;
                    Substrate2Dnew[i, j] = nval;

                    // -------- AHL diffusion + decay --------
                    double uavg = AverageSubstanceAround(i, j, Ahl2D);
                    double uval = Ahl2D[i, j] + AhlDiffusionKoef * (uavg - Ahl2D[i, j]) * dt;

                    // деградация AHL
                    uval *= AHLdegrPerHour;

                    if (uval < 1e-12) uval = 0.0;
                    Ahl2Dnew[i, j] = uval;
                }
            });

            Parallel.For(0, NutrientAreaWidth, opt, i =>
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    Substrate2D[i, j] = Substrate2Dnew[i, j];
                    Ahl2D[i, j] = Ahl2Dnew[i, j];
                    U2Dnorm[i, j] = Ahl2D[i, j] / AHLscaler;
                }
            });
        }

        private double AverageSubstanceAround(int x, int y, double[,] substance)
        {
            double sum = 0.0;
            int k = 0;

            Vector2Int[] nbrs = GetNbrs(y);

            for (int idx = 0; idx < nbrs.Length; idx++)
            {
                int nx = x + nbrs[idx].x;
                int ny = y + nbrs[idx].y;

                if (IsLegalNutr(nx, ny))
                {
                    sum += substance[nx, ny];
                    k++;
                }
            }

            if (k == 0)
            {
                Debug.LogError($"0 neighbors situation occurs in AverageSubstanceAround for cell ({x},{y})");
                return substance[x, y];
            }

            return sum / k;
        }
        private bool IsLegalNutr(int x, int y)
        {
            return x >= 0 && x < NutrientAreaWidth &&
                   y >= 0 && y < NutrientAreaHeight;
        }
        private void CalcAverageAhlAndNutr()
        {
            double sumU = 0, sumN = 0, avU, avN;
            double maxU = -1000;
            int uCount = 0;
            for (int i = 0; i < NutrientAreaWidth; i++)
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    sumN += Substrate2D[i, j];
                    if (Bacteria2D[i, j] > 0)
                    {
                        sumU += Ahl2D[i, j];
                        uCount++;
                        if (Ahl2D[i, j] > maxU)
                            maxU = Ahl2D[i, j];
                    }
                }
            }
            avU = sumU * 1d / uCount;
            avN = sumN * 1d / (NutrientAreaHeight * NutrientAreaWidth);
            if (avN > AverageNutrientRemain + 1e-9)
                Debug.LogWarning($"Nutrient level error, level increased was {AverageNutrientRemain}->{avN}");
            AverageNutrientRemain = avN;
            AverageAhl = avU;
            maxAhl = maxU;
        }
        private double GetLocalQuorumNorm(Vector2Int pos)
        {
            int i = pos.x / NutrGridSimpl;
            int j = pos.y / NutrGridSimpl;
            if (i < 0 || j < 0 || i >= NutrientAreaWidth || j >= NutrientAreaHeight) return 0;
            return U2Dnorm[i, j];

        }
        private double GetGlobalQuorumNorm()
        {
            return AverageAhl / AHLscaler;
        }
        private double SpreadProbByQuorum(Vector2Int pos)
        {
            double q = UseGlobalAHLForSpread ? GetGlobalQuorumNorm() : GetLocalQuorumNorm(pos);
            double h = Hill01(q, AHLthreshold, QuorumHillN);
            return SpreadProbMin + (SpreadProbMax - SpreadProbMin) * h;
        }
        private int SpreadRangeByQuorum(Vector2Int pos, int baseRange)
        {
            if (!QuorumAffectsRange) return baseRange;
            double q = UseGlobalAHLForSpread ? GetGlobalQuorumNorm() : GetLocalQuorumNorm(pos);
            double h = Hill01(q, AHLthreshold, QuorumHillN);
            double mult = RangeMultMin + (RangeMultMax - RangeMultMin) * h;
            int r = (int)Math.Round(baseRange * mult);
            return Math.Max(1, r);
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
        private void BuildConsumptionDeltas()
        {
            Parallel.For(0, BiomassCells2D.Count, biomassParallelOptions, idx =>
            {
                Vector2Int pos = BiomassCells2D[idx];
                if (Cells2D[pos.x, pos.y] != CellState.busyCanDiv)
                    return;

                int i = pos.x / NutrGridSimpl;
                int j = pos.y / NutrGridSimpl;



                double b = Bacteria2D[pos.x, pos.y];
                double s = Substrate2D[i, j];


                double requested = Math.Max(0.0, GetConsume(b, s));


                double dU = 0.0;
                //double dB = Math.Max(0.0, GetBuddrusBiomassGrowth(b, s));
                //double dS = Math.Max(0.0, GetBuddrusSubstrateConsumption(b, s));


                /*                if (dS > 0.0)
                                {
                                    double q = U2Dnorm[i, j];

                                    dU = AhlProductionRate * dS * (1.0 + AhlAutoInduction * Hill01(q, AHLthreshold, QuorumHillN));
                                    double h = Hill01(q, AHLthreshold, AHLpowerK);
                                    dU = b * (AhlAlpha + AhlBetta * h) * dS * DeltaTime;
                                }
                */

                double q = U2Dnorm[i, j]; // normalized AHL
                double sPow = Math.Pow(s, 1.3);
                double kPow = Math.Pow(Ks, 1.3);
                //double monod = s / (Ks + s + 1e-12);
                double monod = sPow / (kPow + sPow + 1e-12);
                double dB = μmax * b * monod * DeltaTime;
                double dS = dB / Yxs;
                double h = Hill01(q, AHLthreshold, AHLpowerK);
                dU = b * (AhlAlpha + AhlBetta * h) * dS ;


                lock (nutrientRowLocks[i])
                {
                    requestedSubstrate[i, j] += dS;
                    //requestedSubstrate[i, j] += requested;
                    deltaAhl[i, j] += dU;
                }

                deltaBiomass[pos.x, pos.y] = dB;
                //deltaBiomass[pos.x, pos.y] = requested / Yxs;
            });
        }
        private void ApplyConsumptionDeltas()
        {
            Parallel.For(0, NutrientAreaWidth, biomassParallelOptions, i =>
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    double available = Substrate2D[i, j];
                    double requested = requestedSubstrate[i, j];

                    double scale = 1.0;
                    if (requested > available && requested > 0.0)
                        scale = available / requested;

                    substrateScale[i, j] = scale;

                    double substrateDelta = requested * scale;
                    Substrate2D[i, j] = Math.Max(0.0, available - substrateDelta);
                    Ahl2D[i, j] += deltaAhl[i, j] * scale;
                    U2Dnorm[i, j] = Ahl2D[i, j] / AHLscaler;
                    OnAHL[i, j] = (U2Dnorm[i, j] >= AHLthreshold) ? 1.0 : 0.0;
                }
            });

            Parallel.For(0, BiomassCells2D.Count, biomassParallelOptions, idx =>
            {
                Vector2Int pos = BiomassCells2D[idx];
                if (Cells2D[pos.x, pos.y] != CellState.busyCanDiv)
                    return;

                int i = pos.x / NutrGridSimpl;
                int j = pos.y / NutrGridSimpl;
                Bacteria2D[pos.x, pos.y] += deltaBiomass[pos.x, pos.y] * substrateScale[i, j];
            });
        }
        private void RecalcAverageConsume()
        {
            double totalConsumed = 0.0;
            for (int i = 0; i < NutrientAreaWidth; i++)
                for (int j = 0; j < NutrientAreaHeight; j++)
                    totalConsumed += requestedSubstrate[i, j] * substrateScale[i, j];

            averageConsume = BiomassCells2D.Count > 0 ? (float)(totalConsumed / BiomassCells2D.Count) : 0f;
        }

        //////////////////////////////////////////////////////////////////////
        private void TryDivideOld(Vector2Int pos)
        {
            //List<Vector2Int> freeDirsAround = FreeDirsAround(pos);
            //bool HasFreeSpaceAround = (freeDirsAround.Count > 0);
            bool hasFreeSpaceAround = TryGetRandomFreeDir(pos, out Vector2Int freeDir);
            bool hasSpreadSpace = (bottomLayer.Count > 0);

            // --- Quorum-dependent spread ---
            if (hasSpreadSpace)
            {
                //double pSpread = SpreadProbByQuorum(pos);
                double pSpread = SpreadProbByQuorum(pos);

                if (rng.Value.NextDouble() < pSpread)
                {
                    lock (bottomRemoveLocker)
                    {
                        if (RangedSpreadDivide(pos))
                        {
                            SpreadCount++;
                            TotalDivisionCount++;
                            return;
                        }
                    }
                }
            }

            if (hasFreeSpaceAround)
            {
                TotalDivisionCount++;
                Divide(pos, freeDir);
                return;
            }
            else
            {
               // PushDivde(pos);
            }
        }

        private void TryDivide(Vector2Int pos)
        {
            bool hasSpreadSpace = (bottomLayer.Count > 0);

            // --- Quorum-dependent spread ---
            if (hasSpreadSpace)
            {
                double pSpread = SpreadProbByQuorum(pos);

                if (rng.Value.NextDouble() < pSpread)
                {
                    lock (bottomRemoveLocker)
                    {
                        if (RangedSpreadDivide(pos))
                        {
                            SpreadCount++;
                            TotalDivisionCount++;
                            return;
                        }
                    }
                }
            }

            // 1. Обычное локальное деление
            if (TryGetRandomFreeDir(pos, out Vector2Int freeDir))
            {
                if (TryDivideIntoEmpty(pos, freeDir))
                    TotalDivisionCount++;

                return;
            }

            // 2. Если вокруг родителя нет места — пробуем вынести дочь на свободный фронт
            if (TryPushDivide(pos))
            {
                TotalDivisionCount++;
                return;
            }

            // 3. Если места вообще нет — клетка просто не делится на этом шаге
        }
        private bool TryDivideIntoEmpty(Vector2Int fromPos, Vector2Int toPos)
        {
            if (!IsLegal(fromPos) || !IsLegal(toPos))
                return false;

            if (Cells2D[fromPos.x, fromPos.y] != CellState.busyCanDiv)
                return false;

            // Ключевая защита: нельзя делить в занятую клетку
            if (Cells2D[toPos.x, toPos.y] != CellState.empty)
            {
                Debug.LogError($"Attempt to divide into occupied cell: from={fromPos}, to={toPos}");
                return false;
            }

            double childBiomass = Bacteria2D[fromPos.x, fromPos.y] * 0.5;

            Bacteria2D[fromPos.x, fromPos.y] -= childBiomass;

            NewCell(toPos, childBiomass);

            return true;
        }
        private bool TryPushDivide(Vector2Int pos)
        {
            // frontCells2D может быть устаревшим, поэтому лучше сначала обновить
            RebuildFrontCells2D();

            if (frontCells2D.Count == 0)
                return false;

            int start = rng.Value.Next(frontCells2D.Count);

            for (int attempt = 0; attempt < frontCells2D.Count; attempt++)
            {
                int idx = (start + attempt) % frontCells2D.Count;
                Vector2Int frontCell = frontCells2D[idx];

                if (!IsLegal(frontCell))
                    continue;

                if (Cells2D[frontCell.x, frontCell.y] != CellState.busyCanDiv)
                    continue;

                // Ищем свободную позицию рядом с фронтовой клеткой
                if (!TryGetRandomFreeDir(frontCell, out Vector2Int freeTarget))
                    continue;

                // Родитель делится, но дочь кладётся в свободную клетку на фронте
                return TryDivideIntoEmpty(pos, freeTarget);
            }

            return false;
        }
        private void RebuildFrontCells2D()
        {
            frontCells2D.Clear();

            foreach (Vector2Int cell in BiomassCells2D)
            {
                if (!IsLegal(cell))
                    continue;

                if (Cells2D[cell.x, cell.y] != CellState.busyCanDiv)
                    continue;

                if (HasFreeSpaceAround(cell))
                    frontCells2D.Add(cell);
            }
        }
        private void PushDivde(Vector2Int pos)
        {
            Vector2Int randomCellOnFront;
            randomCellOnFront = frontCells2D[rng.Value.Next(0, frontCells2D.Count)];
            Divide(pos, randomCellOnFront);
        }
        public double MeanAHL()
        {
            double s = 0;
            foreach (double u in Ahl2D)
                s += u;
            return s * 1d / (Ahl2D.GetLength(0) * Ahl2D.GetLength(1));
        }
        public double SpreadValue => TotalDivisionCount > 0 ? SpreadCount * 1d / TotalDivisionCount : 0.0;
        private bool RangedSpreadDivideOld(Vector2Int pos)
        {
            int baseRange = 2 * pos.y + 2;
            int range = SpreadRangeByQuorum(pos, baseRange);

            int count = 0;
            foreach (int xVal in bottomLayer)
            {
                if (xVal >= pos.x - range && xVal <= pos.x + range)
                    count++;
            }

            if (count == 0)
                return false;

            int target = rng.Value.Next(count);
            int idx = 0;
            int chosenX = -1;

            foreach (int xVal in bottomLayer)
            {
                if (xVal >= pos.x - range && xVal <= pos.x + range)
                {
                    if (idx == target)
                    {
                        chosenX = xVal;
                        break;
                    }
                    idx++;
                }
            }

            if (chosenX < 0)
                return false;

            bottomLayer.Remove(chosenX);
            Divide(pos, new Vector2Int(chosenX, 0));
            return true;    
        }
        private bool RangedSpreadDivide(Vector2Int pos)
        {
            int baseRange = 2 * pos.y + 2;
            int range = SpreadRangeByQuorum(pos, baseRange);

            int count = 0;
            foreach (int xVal in bottomLayer)
            {
                if (xVal >= pos.x - range && xVal <= pos.x + range)
                    count++;
            }

            if (count == 0)
                return false;

            int target = rng.Value.Next(count);
            int idx = 0;
            int chosenX = -1;

            foreach (int xVal in bottomLayer)
            {
                if (xVal >= pos.x - range && xVal <= pos.x + range)
                {
                    if (idx == target)
                    {
                        chosenX = xVal;
                        break;
                    }
                    idx++;
                }
            }

            if (chosenX < 0)
                return false;

            Vector2Int toPos = new Vector2Int(chosenX, 0);

            if (!TryDivideIntoEmpty(pos, toPos))
                return false;

            bottomLayer.Remove(chosenX);
            return true;
        }
        private void Divide(Vector2Int fromPos, Vector2Int toPos)
        {
            Bacteria2D[fromPos.x, fromPos.y] /= 2f;
            NewCell(toPos, Bacteria2D[fromPos.x, fromPos.y]);
        }
      
        /*
        public override void DoGrowthStep()
        {
            PrepareStep();
            DiffuseFields();
            ProcessBiomassCells();
            FinalizeStep();
            UpdateStepStatistics();
        }
        */

        public override void DoGrowthStep()
        {
            using (Measure(ProfileSection.TotalGrowthStep))
            {
                using (Measure(ProfileSection.PrepareStep))
                    PrepareStep();

                using (Measure(ProfileSection.DiffuseFields))
                    DiffuseFields();

                using (Measure(ProfileSection.BuildConsumptionDeltas))
                    BuildConsumptionDeltas();

                using (Measure(ProfileSection.ApplyConsumptionDeltas))
                    ApplyConsumptionDeltas();

                using (Measure(ProfileSection.DivisionPhase))
                {
                    foreach (Vector2Int cellPos in BiomassCells2D)
                    {
                        if (CanCellDivide(cellPos))
                            TryDivide(cellPos);
                    }
                }

                using (Measure(ProfileSection.RecalcAverageConsume))
                    RecalcAverageConsume();

                using (Measure(ProfileSection.FinalizeStep))
                {
                    FinalizeStep();
                    ApplyWashout();
                }

                using (Measure(ProfileSection.UpdateStatistics))
                    UpdateStepStatistics();
            }

            profiledStepCount++;
        }


        private void PrepareStep()
        {
            ApplyPreviousStepBottomLayerUpdates();

            NewBiomassCells.Clear();
            newFrontCells2D.Clear();

            averageConsume = 0f;
            ClearDeltaBuffers();
        }
        private void ClearDeltaBuffers()
        {
            Array.Clear(requestedSubstrate, 0, requestedSubstrate.Length);
            Array.Clear(deltaAhl, 0, deltaAhl.Length);
            Array.Clear(substrateScale, 0, substrateScale.Length);
            Array.Clear(deltaBiomass, 0, deltaBiomass.Length);
        }
        private void ApplyPreviousStepBottomLayerUpdates()
        {
            foreach (Vector2Int frontCell in newFrontCells2D)
            {
                if (frontCell.y == 0 )
                    bottomLayer.Remove(frontCell.x);
            }
        }
        private void DiffuseFieldsWithSolver()
        {
            double nutrientSolverD = DiffusionKoef / SquareAverageToLaplacianFactor;
            double ahlSolverD = AhlDiffusionKoef / SquareAverageToLaplacianFactor;

            nutrientDiffusionSolver.RefreshAndDoStep(Substrate2D, nutrientSolverD);
            ahlDiffusionSolver.RefreshAndDoStep(Ahl2D, ahlSolverD);

            Parallel.For(0, NutrientAreaWidth, biomassParallelOptions, i =>
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    double nval = nutrientDiffusionSolver.u[i, j];

                    if (nval < 1e-12)
                        nval = 0.0;

                    Substrate2D[i, j] = nval;

                    double uval = ahlDiffusionSolver.u[i, j];

                    double kDeg_s = AHLdegrPerHour / 3600.0;
                    double ahlDegrStep = Math.Exp(-kDeg_s * DeltaTime);
                    uval *= ahlDegrStep;
                    // AHLdegr теперь применяется один раз за полный шаг модели

                    if (uval < 1e-12)
                        uval = 0.0;

                    Ahl2D[i, j] = uval;
                    U2Dnorm[i, j] = Ahl2D[i, j] / AHLscaler;
                }
            });
        }
        private void DiffuseFields()
        {
            DiffuseFieldsWithSolver();
            //DiffuseSubstrateAndAHLImproved();
            //DiffuseFieldsParallel();
        }
        private void ProcessBiomassCells()
        {
            BuildConsumptionDeltas();
            ApplyConsumptionDeltas();


            foreach (Vector2Int cellPos in BiomassCells2D)
                if (CanCellDivide(cellPos))
                    TryDivide(cellPos);

            RecalcAverageConsume();
        }
        private bool CanCellDivide(Vector2Int cellPos)
        {
            return Bacteria2D[cellPos.x, cellPos.y] > ConcToDivide;
        }
        private void FinalizeStep()
        {
            BiomassCells2D.AddRange(NewBiomassCells);
            frontCells2D.AddRange(newFrontCells2D);
        }
        private void UpdateStepStatistics()
        {
            CalcAverageAhlAndNutr();
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
        private bool TryGetRandomFreeDir(Vector2Int coord, out Vector2Int result)
        {
            result = default;
            Vector2Int[] nbrs = GetNbrs(coord.y);

            int freeCount = 0;
            for (int k = 0; k < nbrs.Length; k++)
            {
                Vector2Int sum = coord + nbrs[k];
                if (IsLegal(sum) && Cells2D[sum.x, sum.y] == CellState.empty)
                    freeCount++;
            }

            if (freeCount == 0)
                return false;

            int target = rng.Value.Next(freeCount);
            int idx = 0;

            for (int k = 0; k < nbrs.Length; k++)
            {
                Vector2Int sum = coord + nbrs[k];
                if (IsLegal(sum) && Cells2D[sum.x, sum.y] == CellState.empty)
                {
                    if (idx == target)
                    {
                        result = sum;
                        return true;
                    }
                    idx++;
                }
            }

            return false;
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

        private void RecalculateDepthToFront()
        {
            const int INF = int.MaxValue / 4;

            for (int x = 0; x < AreaWidth; x++)
            {
                for (int y = 0; y < AreaHeight; y++)
                {
                    depthToFront[x, y] = INF;
                }
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            // Все поверхностные клетки получают depth = 0.
            foreach (Vector2Int pos in BiomassCells2D)
            {
                if (Cells2D[pos.x, pos.y] != CellState.busyCanDiv)
                    continue;

                if (HasFreeSpaceAround(pos))
                {
                    depthToFront[pos.x, pos.y] = 0;
                    queue.Enqueue(pos);
                }
            }

            // Распространяем расстояние только внутри занятых клеток.
            while (queue.Count > 0)
            {
                Vector2Int pos = queue.Dequeue();
                int currentDepth = depthToFront[pos.x, pos.y];

                Vector2Int[] nbrs = GetNbrs(pos.y);

                for (int k = 0; k < nbrs.Length; k++)
                {
                    int nx = pos.x + nbrs[k].x;
                    int ny = pos.y + nbrs[k].y;

                    if (!IsLegal(new Vector2Int(nx, ny)))
                        continue;

                    if (Cells2D[nx, ny] != CellState.busyCanDiv)
                        continue;

                    int nextDepth = currentDepth + 1;

                    if (nextDepth < depthToFront[nx, ny])
                    {
                        depthToFront[nx, ny] = nextDepth;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }
        private void ApplyLiquidWashoutAndInflow()
        {
            if (LiquidDilutionRatePerHour <= 0.0)
                return;

            double D_s = LiquidDilutionRatePerHour / 3600.0;
            double wash = Math.Exp(-D_s * DeltaTime);

            Parallel.For(0, NutrientAreaWidth, biomassParallelOptions, i =>
            {
                for (int j = 0; j < NutrientAreaHeight; j++)
                {
                    // Питание стремится к входной концентрации.
                    Substrate2D[i, j] = InflowSubstrate + (Substrate2D[i, j] - InflowSubstrate) * wash;

                    if (Substrate2D[i, j] < 0.0)
                        Substrate2D[i, j] = 0.0;

                    // AHL вымывается.
                    Ahl2D[i, j] *= wash;

                    if (Ahl2D[i, j] < 1e-12)
                        Ahl2D[i, j] = 0.0;

                    U2Dnorm[i, j] = Ahl2D[i, j] / AHLscaler;
                    OnAHL[i, j] = U2Dnorm[i, j] >= AHLthreshold ? 1.0 : 0.0;
                }
            });
        }

        private double GetDepthDependentBiomassWashoutRatePerHour(int depth)
        {
            if (depth < 0 || depth >= int.MaxValue / 8)
                return DeepBiomassWashoutRatePerHour;

            if (depth >= WashoutDepthCells)
                return DeepBiomassWashoutRatePerHour;

            double q = depth / (double)WashoutDepthCells;

            return DeepBiomassWashoutRatePerHour + (FrontBiomassWashoutRatePerHour - DeepBiomassWashoutRatePerHour) * (1.0 - q);
        }

        private void ApplyDepthDependentBiomassWashout()
        {
            if (FrontBiomassWashoutRatePerHour <= 0.0 &&
                DeepBiomassWashoutRatePerHour <= 0.0)
            {
                return;
            }

            if (washoutDepthLastUpdateStep < 0 ||
                rep - washoutDepthLastUpdateStep >= WashoutDepthUpdateEverySteps)
            {
                RecalculateDepthToFront();
                washoutDepthLastUpdateStep = rep;
            }

            List<Vector2Int> survivors = new List<Vector2Int>(BiomassCells2D.Count);

            foreach (Vector2Int pos in BiomassCells2D)
            {
                if (Cells2D[pos.x, pos.y] != CellState.busyCanDiv)
                    continue;

                int depth = depthToFront[pos.x, pos.y];

                double k_h = GetDepthDependentBiomassWashoutRatePerHour(depth);
                double k_s = k_h / 3600.0;

                double remain = Math.Exp(-k_s * DeltaTime);

                double newB = Bacteria2D[pos.x, pos.y] * remain;

                if (newB <= BiomassRemoveThreshold)
                {
                    Bacteria2D[pos.x, pos.y] = 0.0;
                    Cells2D[pos.x, pos.y] = CellState.empty;

                    if (pos.y == 0 && !bottomLayer.Contains(pos.x))
                        bottomLayer.Add(pos.x);

                    continue;
                }

                Bacteria2D[pos.x, pos.y] = newB;
                survivors.Add(pos);
            }

            BiomassCells2D.Clear();
            BiomassCells2D.AddRange(survivors);

            // Чтобы старые front lists не содержали удалённые клетки.
            //frontCells2D.RemoveAll(p => !IsLegal(p) || Cells2D[p.x, p.y] == CellState.empty);
            //newFrontCells2D.RemoveAll(p => !IsLegal(p) || Cells2D[p.x, p.y] == CellState.empty);
        }

        private void ApplyWashout()
        {
            if (CurrentWashoutMode == WashoutMode.None)
                return;

            if (CurrentWashoutMode == WashoutMode.LiquidOnly ||
                CurrentWashoutMode == WashoutMode.ChemostatLike ||
                CurrentWashoutMode == WashoutMode.DepthDependent)
            {
                ApplyLiquidWashoutAndInflow();
            }

            if (CurrentWashoutMode == WashoutMode.DepthDependent)
            {
                ApplyDepthDependentBiomassWashout();
            }
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
