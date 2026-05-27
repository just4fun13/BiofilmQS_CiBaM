using Assets.Scripts.ImageAnalyze;
using Assets.Scripts.MVVM_CA.Analytics;
using Assets.Scripts.MVVM_CA.Models._2D;
using Assets.Scripts.MVVM_CA.Utils;
using CellularAutomaton;
using Cinemachine;
using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts.MVVM_CA
{
    public class SimExpert2D : MonoBehaviour
    {
        private ViewModel viewModel;
        private ProgressUI progressUI;

        [SerializeField] private Camera simCam;
        [SerializeField] private int OptStepsPerFrame = 500; // 200..5000 подбирается
        [SerializeField] private float OptLogEverySeconds = 1.0f;
        [SerializeField] private bool UseAHL = false;

        [SerializeField] private bool DynamicUpdate = true;
        [SerializeField] private ModelType modelType;
        [SerializeField] private int AreaWidth = 320;
        [SerializeField] private int AreaHeight = 180;
        [SerializeField] private int AreaLength = 180;
        [SerializeField] private float InitNutrient = 0.5f;
        [SerializeField] private View2D view2D, view2Dahl, view2Dbac, view2Dnut;
        [SerializeField] private View3D view3D;
        [SerializeField] private int RedrawFrequency = 10;
        [SerializeField] private int MaxThreadCount = 1;
        [SerializeField] private bool MultipleSimulations = false;
        [SerializeField][Range(0.0001f, 0.01f)] private float PercentageOfUpdateAmount = 0.001f;

        [SerializeField] private int NutrGridSimp = 2;
        [SerializeField] private bool WriteLog = false;

        private int MaxSimTimeSeconds;

        private UIinput uiInput;
        private AudioMan audioMan;
        private int RefreshAmount = 0;
        private int maxStepsWithoutProgress = 50;
        private long iterationId = 0;
        private float simulationTime = 0;
        private int stepsWithoutProgress = 0;
        private int CellsCount = 0;
        private int simulStartTime;
        private int programStartTime;
        private float TimeStep => uiInput.DeltaTimeValue;
        private float MaxSimTimeHours => uiInput.TotalHours;

        private string imagesLoc = Environment.CurrentDirectory + $@"\Log\Pictures\";
        private string logLoc = Environment.CurrentDirectory + $@"\Log\";

        private float totalSteps;
        private float nutrStep;
        private int CellCountToRefreshTheDraw;


        private readonly Stopwatch fullStepWatch = new Stopwatch();
        private readonly Stopwatch modelWatch = new Stopwatch();
        private readonly Stopwatch redrawWatch = new Stopwatch();
        private readonly Stopwatch logWatch = new Stopwatch();
        private readonly Stopwatch screenshotWatch = new Stopwatch();

        private double fullStepMsSum = 0;
        private double modelMsSum = 0;
        private double redrawMsSum = 0;
        private double logMsSum = 0;
        private double screenshotMsSum = 0;

        private int fullStepCount = 0;
        private int redrawCount = 0;
        private int logCount = 0;
        private int screenshotCount = 0;
        private int RandomSeed => uiInput.RandomSeed;

        private CurveComparer slopeMatcher;

        private Vector3Int[] AreaSets =
        {
            new Vector3Int(50, 50, 10),
            new Vector3Int(200, 200, 10),
            new Vector3Int(500, 500, 10),
        };

        private int[] Threads =
        {
            1, 4, 12, 24,
        };

        private CultureInfo fl = System.Globalization.CultureInfo.InvariantCulture;

        private float MaxTimeStep = -10f;
        private float MinTimeStep = 100000f;
        private float AverageTimeStep = 0f;

        private float MainProgress = 0f;
        private int ReprintImage = 2400000;
        private string PictureName = "";
        private int LogEvery = 500;
        private int hourRefresh = -1;

        private bool SimulIsDone(Model model) => simulationTime >= MaxSimTimeSeconds;
        private void ResetTopLevelProfiling()
        {
            fullStepMsSum = 0;
            modelMsSum = 0;
            redrawMsSum = 0;
            logMsSum = 0;
            screenshotMsSum = 0;

            fullStepCount = 0;
            redrawCount = 0;
            logCount = 0;
            screenshotCount = 0;
        }
        private string PrintTopLevelProfilingReport(string tag = "Untagged")
        {
            double avgFull = fullStepCount > 0 ? fullStepMsSum / fullStepCount : 0.0;
            double avgModel = fullStepCount > 0 ? modelMsSum / fullStepCount : 0.0;

            // ВАЖНО: две разные метрики
            double avgRedrawPerCall = redrawCount > 0 ? redrawMsSum / redrawCount : 0.0;
            double avgRedrawPerStep = fullStepCount > 0 ? redrawMsSum / fullStepCount : 0.0;

            return $"{tag}\t{fullStepCount}\t{avgFull:F3}\t{avgModel:F3}\t" +
                   $"{avgRedrawPerCall:F3}\t{avgRedrawPerStep:F4}\t" +
                   $"{redrawCount}\t{RedrawFrequency}";
        }
        public void RestartScene()
        {
            SceneMan.Instance.ReloadScene();
        }

        private List<Vector2> modelAhlCurve = new List<Vector2>();
        private List<Vector2> modelBiomassCurve = new List<Vector2>() ;


        private void Awake()
        {
            uiInput = FindObjectOfType<UIinput>();
            audioMan = FindObjectOfType<AudioMan>();

            uiInput.SetInputs(new float[] {
                1, 90, 50, 50,
                0.0264f, 0.4f, 0.01f,
                0.02f, 0.9999f, 2.5f, 1, 0.5f, 0.7f, 0.05f, 0.01f, 0.01f,
                0, 0, 0
            });
            slopeMatcher = new CurveComparer(BuddrusCurves.Ahl, BuddrusCurves.Biomass);
            // сравниваем форму, а не абсолютную шкалу
            MyLogger.InitLogger();
            slopeMatcher.Reset();

            // XavierResultsComparer.ShowExp();
        }

        private void SetModelParametersWithUI(Model model)
        {
            // W, H, Type, Dnutr, Dahl, alpha, betta, degr, AHK_k, Uth_k
            model.SetMainPars(uiInput.GetMainParsInputs());

            if (model is Model2DWithAHL || model is FiveStarModel)
            {
                model.SetAhlPars(uiInput.GetAhlParsInputs());
                model.SetWashPars(uiInput.GetWashParsInputs());

                if (model is FiveStarModel)
                {
                    model.SetEpsPars(uiInput.GetEpsParsInputs());
                    model.SetLactonasePars(uiInput.GetLactonaseParsInputs());
                }
                if (model.gridType == ModelType.Hexagon)
                    (model as Model2DWithAHL).SetDiffusionSolverMode(DiffusionSolver.DiffusionMode.HexADI);
                else
                    (model as Model2DWithAHL).SetDiffusionSolverMode(DiffusionSolver.DiffusionMode.Implicit);
            }

            //model.SetSize(uiInput.GetSize);
            model.SetNutrGridSimp(NutrGridSimp);
        }

        private void SetMoveParametersMultiVariant(Model model, int inoc, float ahlTH, float redCons)
        {
            model.SetMainPars(uiInput.GetMainParsInputs());
            model.SetAhlPars(uiInput.GetAhlParsInputs());
            model.SetReduceCons(redCons);
            model.SetAHLThreshold(ahlTH);
            model.SetInocCount(inoc);
            model.SetSize(new Vector2(AreaWidth, AreaHeight));
        }

        private void SetModelParametersRandom(Model model)
        {
            float[] mainPars = new float[] {
                UnityEngine.Random.Range(0.01f,0.7f),     // diffusion K
                UnityEngine.Random.Range(0.001f,0.01f),   // init nutrient
                UnityEngine.Random.Range(0.00001f,0.01f), // muMax
            };

            model.SetMainPars(mainPars);

            if (model is Model2DWithAHL)
                model.SetAhlPars(uiInput.GetAhlParsInputs());

            model.SetSize(uiInput.GetSize);
        }

        private void SetMainParsUI(Model model) => model.SetMainPars(uiInput.GetMainParsInputs());
        private void SetAhlParsUI(Model model) => model.SetAhlPars(uiInput.GetMainParsInputs());
        private void SetSizeParsUI(Model model) => model.SetSize(uiInput.GetSize);

        public async void StartSimul()
        {
            uiInput.SaveData();

            StartCoroutine(StartSimulation());
            await Task.Yield();
        }

        private IEnumerator StartSimulation()
        {
            MaxSimTimeSeconds = (int) (MaxSimTimeHours * 3600);
            programStartTime = NowTime();
            progressUI = FindObjectOfType<ProgressUI>();
            uiInput = FindObjectOfType<UIinput>();
            yield return new WaitForEndOfFrame();

            if (!MultipleSimulations)
            {
                Debug.Log("Solo sim mode activated");
                Model2D model = PrepareModelSolo();
                Redraw(model);
                StartCoroutine(RunSimul(model));
                while (!SimulIsDone(model))
                {
                    progressUI.RefreshTime(NowTime() - programStartTime);
                    yield return new WaitForSeconds(0.5f);
                }
            }
            else
            {
                Debug.Log($"Mutliple sim mode activated, model type {modelType.ToString()}");

                int[] thx = new int[] { 4, 8 };
                int[] szh = new int[] { 200, 400, 800, 1000, 1200 };
                

                double a1 = 0.10,a2 = 0.92, da = 0.01;          // порог АНЛ
                double b1 = 0.2,    b2 = 0.2, db = 0.01;    // % от ширины для нач. посева
                double c1 = 0.09, c2 = 0.09, dc = 0.01;      // интенсивнность промывки
                double d1 = 0.1078, d2 = 0.1078, dd = 0.0001; 
                double e1 = 0.0055, e2 = 0.0085, de = 0.0005; 
                double f1 = 0.000, f2 = 0.005, df = 0.001;    



                int totCount = (int)((a2 - a1 + da) / da) * (int)((b2 - b1 + db) / db) * (int)((c2 - c1 + dc) / dc);
                //int totCount = (int)((d2 - d1 + dd) / dd) * (int)((e2 - e1 + de) / de) * (int)((f2 - f1 + df) / df);


                int counter = 0;

                int prevTime = NowTime();
                int avRunTime = 0;
                int sumTime = 0;

                WriteHeader();
                for (double ahlTh = a1; ahlTh <= a2; ahlTh += da)
                    for (double inocCount = b1; inocCount <= b2; inocCount += db)
                        for (double liqWashOut = c1; liqWashOut <= c2; liqWashOut += dc)
                    //for (double liwash = d1; liwash <= d2; liwash += dd)
                    //    for (double bwash = e1; bwash <= e2; bwash += de)
                    //        for (double nutin = f1; nutin <= f2; nutin += df)
                    {
                        modelBiomassCurve.Clear();
                            modelAhlCurve.Clear();
                            slopeMatcher.Reset();
                            MainProgress = counter * 1f / totCount;
                            ShowProg(MainProgress);
                            counter++;
                            iterationId = 0;
                            simulationTime = 0;
                            Model2D model = PrepareModelMulti(ahlTh, inocCount, liqWashOut);
                            //StartCoroutine(RunSimul(model));
                            yield return RunSimulBatch(model);
                            sumTime += NowTime() - prevTime;
                            avRunTime = sumTime / counter;
                            prevTime = NowTime();
                            int remains = avRunTime * (totCount - counter);
                            Debug.Log($"{ahlTh}/{a2}; {inocCount}/{b2}; {liqWashOut}/{c2}    prog = {MainProgress}, averageTime {avRunTime / 60} min, " +
                        $"remains ~ {remains / 3600} hrs {(remains % 3600) / 60} mins");
//                    Debug.Log($"  prog = {MainProgress}, averageTime {avRunTime / 60} min, " +
//                        $"remains ~ {(avRunTime * (totCount - counter)) / 3600} hrs {(avRunTime * (totCount - counter)) / 60} mins ");
                }
                Debug.Log($"Simul is donE!");
                yield return new WaitForEndOfFrame();
                audioMan.PlayExplosion();
            }
        }

        enum gridSize {small, big}

        gridSize size = gridSize.big;
        private double GetMaxAhlFromNutrient(double nutr)
        {
            if (size == gridSize.big)
            {
                if (Math.Abs(nutr - 0.001) < 1e-5)
                    return 0.0042;
                if (Math.Abs(nutr - 0.011) < 1e-5)
                    return 0.1153;
                if (Math.Abs(nutr - 0.021) < 1e-5)
                    return 0.3007;
                if (Math.Abs(nutr - 0.031) < 1e-5)
                    return 0.5668;

                return 0.8550;
            }
            else
            {
                if (Math.Abs(nutr - 0.001) < 1e-5)
                    return 0.0042;
                if (Math.Abs(nutr - 0.011) < 1e-5)
                    return 0.5363;
                if (Math.Abs(nutr - 0.021) < 1e-5)
                    return 1.4244;
                if (Math.Abs(nutr - 0.031) < 1e-5)
                    return 2.7037;
                return 4.0914;
            }
        }

        private IEnumerator RunSimulBatch(Model2D model)
        {
            float localSimulationTime = 0f;
            long localIterationId = 0;

            while (localSimulationTime < MaxSimTimeSeconds)
            {
                localSimulationTime += TimeStep;
                localIterationId++;

                model.DoGrowthStep();

                if (localIterationId % LogEvery == 0)
                    //WritePerformance(model as Model2DWithAHL);//
                    WriteBiomassDynamics(model, localSimulationTime, localIterationId);
                if (localIterationId % 100 == 0)
                    yield return null;
            }
            WriteBiomassDynamics(model, localSimulationTime, localIterationId, true);
        }
        private void WriteBiomassDynamics(Model2D model, float localSimulationTime, long locIter, bool lastFrame = false)
        {
            double spreadValue = (model as Model2DWithAHL)?.SpreadValue ?? 0.0;
            (double rough, double coverage, double thickness) = XavierResultsComparer.CalcPars(model);
            double hours = localSimulationTime / 3600d;

            
            slopeMatcher.AddFrame(hours, model.AverageAhl, model.Biomass2DVolume());

            string modelInfoString = $"{localSimulationTime:F1}\t{hours:F3}\t{locIter}\t{model.BiomassCount}\t{model.Biomass2DVolume().ToString("F2")}\t" +
                $"{model.BottomLayerCountRemain()}\t{(model.AverageNutrientRemain).ToString("F4")}\t{model.AverageAhl:F9}\t" +
                $"{model.maxAhl:F9}\t{rough}\t" +
                $"{coverage.ToString("F4")}\t{thickness.ToString("F4")}\t{spreadValue}\t{model.AHLthreshold}\t" +
                $"{model.gridType}\t{model.AreaWidth}\t{model.AreaHeight}\t{TimeStep}\t{model.DiffusionKoef}" +
                $"\t{model.InitSubstrateCount}\t{model.μmax}\t{model.InoculationCount}\t{model.SpreadProb}\t{model.Ks}" +
                $"\t{model.Yxs}\t{model.AhlDiffusionKoef}\t{model.AhlAlpha}\t{model.AhlBetta}\t{model.AHLdegrPerHour}" +
                $"\t{model.AHLpowerK}\t{model.AHLthreshold}\t{model.AHLscaler}\t{model.LiquidDilutionRatePerHour:F4}\t{model.FrontBiomassWashoutRatePerHour:F4}\t{model.InflowSubstrate:F4}";

            modelAhlCurve.Add(new Vector2((float)hours, (float)model.AverageAhl));
            modelBiomassCurve.Add(new Vector2((float)hours, (float)model.Biomass2DVolume()));

            if (DynamicUpdate)
            {
                //DrawLineTool.DrawLines34(modelBiomassCurve, modelAhlCurve, (float)model.AHLthreshold);
                XChartLineTool.AddModelPoint(new Vector3((float)model.AverageAhl, (float)model.Biomass2DVolume(), (float)model.AverageNutrientRemain), (float)hours);
            }
            slopeMatcher.GetBestExperiment();
            modelInfoString += $"\t{slopeMatcher.BestExperimentName}\t{slopeMatcher.BestExperimentIndex}" +
            $"\t{slopeMatcher.BestBiomassError:F4}\t{slopeMatcher.BestAhlError:F4}\t{slopeMatcher.BestTotalError:F4}";
            if (!WriteLog) return;
            MyLogger.WriteLog(modelInfoString);
            if (lastFrame)
            {
                Debug.Log($"Ber = {slopeMatcher.BestBiomassError}\tUer = {slopeMatcher.BestAhlError}\t Ter{slopeMatcher.BestTotalError}");
               // Debug.Log($"Max average AHL = {modelAhlCurve}");
            }


        }


        private void WritePerformance(Model2DWithAHL m)
        {
            logWatch.Restart();
            //Debug.Log(m.GetProfilingReport());
            MyLogger.WriteLog($"adi\t{PrintTopLevelProfilingReport()}\t{m.GetProfilingReport()}" +
                $"\t{GC.GetTotalMemory(false)}\t{Process.GetCurrentProcess().WorkingSet64}\t{Process.GetCurrentProcess().PrivateMemorySize64}" +
                $"\t{PerformanceMemoryTracker.PeakPrivateMemoryMB()}");
            m.ResetProfiling();
            logWatch.Stop();

            logMsSum += logWatch.Elapsed.TotalMilliseconds;
            logCount++;
        }

        private void ShowProg(float prog)
        {
            progressUI.Progress(prog);
        }

        private bool ModelTypeIs2D =>
           (modelType == ModelType.SimpleSquare || modelType == ModelType.ExtendedSquare || modelType == ModelType.Hexagon || modelType == ModelType.Diamond);

        private Model2D PrepareModelSolo()
        {
            Model2D model;
            ResetParams();

            AreaWidth = (int)uiInput.GetSize.x;
            AreaHeight = (int)uiInput.GetSize.y;
            modelType = (ModelType)uiInput.GetGridType;
            InitNutrient = uiInput.GetMainParsInputs()[1];

            if (!UseAHL)
                model = new Model2DnoAHL(AreaWidth, AreaHeight, InitNutrient, modelType, TimeStep, MaxThreadCount);
            else
                model = new Model2DWithAHL(AreaWidth, AreaHeight, InitNutrient, modelType, TimeStep, MaxThreadCount, RandomSeed);

            SetModelParametersWithUI(model);
//            Debug.Log("Config initialCells = " + Config.initialCells);
            Debug.Log("Model InoculationCount before init = " + model.InoculationCount);
            model.InitInoculate();
            slopeMatcher.AddFrame(0.0, model.AverageAhl, model.Biomass2DVolume() );
            if (!MultipleSimulations)
            {
                view2D.Init(modelType, model.GetPos, AreaWidth, AreaHeight, InitNutrient, UseTexMode: false);
                FocusSimulationCamera();
                
            }

            CellsCount = model.CellCount;
            CellCountToRefreshTheDraw = (int)(PercentageOfUpdateAmount * AreaHeight * AreaWidth);
            viewModel = new ViewModel(model, view2D, view2Dnut, view2Dbac, view2Dahl, view3D);

            return model;
        }

        private Model2D PrepareModelMulti(double ahlTHe, double inocc, double liwash)
        {
            Model2D model;

            int thCount = 12;
            AreaWidth = (int)uiInput.GetSize.x;
            AreaHeight = (int)uiInput.GetSize.y;
            modelType = (ModelType)uiInput.GetGridType;
            //InitNutrient = (float)c00;

            if (!UseAHL)
                  model = new Model2DnoAHL(AreaWidth, AreaHeight, InitNutrient, modelType, TimeStep, thCount);
            else
                model = new Model2DWithAHL(AreaWidth, AreaHeight, InitNutrient, modelType, TimeStep, thCount);

            SetModelParametersWithUI(model);   // ОБЯЗАТЕЛЬНО
            model.SetInocCount(inocc);
            model.SetAHLThreshold(ahlTHe);
            model.SetLiquidWash(liwash);
            Debug.Log($"{model.AreaWidth} x {model.AreaHeight} with {thCount} threads");

            model.InitInoculate();
            slopeMatcher.AddFrame( 0.0, model.AverageAhl, model.Biomass2DVolume() );
            return model;
        }

        private void ResetParams()
        {
            RefreshAmount = 0;
            maxStepsWithoutProgress = 1000;
            iterationId = 0;
            simulationTime = 0;
            stepsWithoutProgress = 0;
            CellsCount = 0;
            simulStartTime = NowTime();
        }

        private void WriteHeader()
        {
            MyLogger.WriteLog("sec\thour\titer\tdx\tBcount\tBvol\tCovLeft\tNleft\tAvAhl\tMaxAhL\tRough\tCoverage\tThicknes\tSpreadVal\t" +
                "ALHth\tType\tW\tH\ttimestep\tDiffusion\tc0\tmuMax\tinocCount\tSpreadMax\tKs\tYxs\tD_u\tAlpha\tBetta\tDegr\tPowerK\tUth\tUscale" +
                "\tliwash\tbwash\tnutin\tex\tex id\tb_er\tahl_er\ttot_er");
        }

        private IEnumerator RunSimul(Model2D model)
        {

            Vector2 frc;
            ResetTopLevelProfiling();
            while (!SimulIsDone(model))
            {
                if (Paused)
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                fullStepWatch.Restart();

                if (!MultipleSimulations)
                    ShowTime();
                else
                    ShowTimeProg();

                DoSimulStep(model);



                if (iterationId % ReprintImage == 0)
                {
                    yield return new WaitForEndOfFrame();
                    Redraw(model);
                    yield return new WaitForEndOfFrame();
                    frc = model.GetFractalDimension();
                    //string fracDim = frc.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    DoThePicture(PictureName + $"-FracDim-fracDim_NR_{model.AHLthreshold}_{model.AverageNutrientRemain.ToString("0.00")}", model);
                    yield return new WaitForEndOfFrame();
                }

                if (DynamicUpdate && iterationId % RedrawFrequency == 0)
                {
                    redrawWatch.Restart();
                    Redraw(model);
                    redrawWatch.Stop();

                    redrawMsSum += redrawWatch.Elapsed.TotalMilliseconds;
                    redrawCount++;
                    yield return new WaitForEndOfFrame();
                }

                if (Math.Floor(simulationTime /3600d) >= hourRefresh+1)
                {
                    hourRefresh = (int)Math.Floor(simulationTime / 3600d);
                    WriteBiomassDynamics(model, simulationTime, iterationId);
                }
                yield return new WaitForEndOfFrame();
                /*
                                                if (iterationId % 100 == 0 && model is Model2DWithAHL m)
                                                {
                                                    logWatch.Restart();
                                                    //Debug.Log(m.GetProfilingReport());
                                                    //MyLogger.WriteLog(m.GetProfilingReport());
                                                    MyLogger.WriteLog($"{AreaWidth}\t{AreaHeight}\t{PrintTopLevelProfilingReport()}\t{m.GetProfilingReport()}");
                                                    m.ResetProfiling();
                                                    logWatch.Stop();

                                                    logMsSum += logWatch.Elapsed.TotalMilliseconds;
                                                    logCount++;
                                                }
                                */


                fullStepWatch.Stop();
                fullStepMsSum += fullStepWatch.Elapsed.TotalMilliseconds;
                fullStepCount++;
            }
            WriteBiomassDynamics(model, simulationTime, iterationId, true);


            /*
            frc = model.GetFractalDimension();
            string fracDim = frc.x.ToString(System.Globalization.CultureInfo.InvariantCulture);

            Redraw(model);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            DoThePicture(PictureName + $"All-FracDim-{fracDim}_NR_{model.AverageNutrientRemain}", model);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            view2D.ShowAhl(true);
            view2D.ShowNutr(false);
            view2D.ShowBac(false);
            Redraw(model);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            DoThePicture(PictureName + $"Ahl-FracDim-{fracDim}_NR_{model.AverageNutrientRemain}", model);

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            view2D.ShowAhl(false);
            view2D.ShowNutr(true);
            view2D.ShowBac(false);
            Redraw(model);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            DoThePicture(PictureName + $"Nut-FracDim-{fracDim}_NR_{model.AverageNutrientRemain}", model);

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            view2D.ShowAhl(false);
            view2D.ShowNutr(false);
            view2D.ShowBac(true);
            Redraw(model);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            DoThePicture(PictureName + $"Bac-FracDim-{fracDim}_NR_{model.AverageNutrientRemain}", model);

            Debug.Log($"Simul is done {iterationId}  for {NowTime() - simulStartTime} seconds; " +
                $"G = {model.G}; FracDim = {fracDim};  ");

             */

            audioMan.PlayExplosion();
        }

        private void FocusSimulationCamera()
        {
            FindObjectOfType<CinemachineVirtualCamera>()?.gameObject.SetActive(false);

            simCam.orthographic = true;
            simCam.transform.rotation = Quaternion.identity;
            simCam.ResetProjectionMatrix();

            float centerX = AreaWidth / 2f;
            float centerY = AreaHeight / 2f;

            float targetWidth = AreaWidth;
            float targetHeight = AreaHeight;

            float aspect = 1f;

            if (simCam.targetTexture != null)
            {
                aspect = (float)simCam.targetTexture.width / simCam.targetTexture.height;
            }
            else
            {
                aspect = (float)Screen.width / Screen.height;
            }

            float sizeByHeight = targetHeight / 2f;
            float sizeByWidth = targetWidth / (2f * aspect);

            simCam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth) + 0.5f;
            simCam.transform.position = new Vector3(centerX, centerY, -10f);
        }
        private void ShowTime()
        {
            progressUI.ShowTime(new Vector2Int(NowTime() - programStartTime, (int)simulationTime));
        }

        private void ShowTimeProg()
        {
            progressUI.ShowTimeWithProgress(new Vector3(NowTime() - programStartTime, (int)iterationId, MainProgress));
        }

        private void DoSimulStep(Model model)
        {
            simulationTime += TimeStep;
            iterationId++;


            modelWatch.Restart();
            model.DoGrowthStep();
            modelWatch.Stop();
            modelMsSum += modelWatch.Elapsed.TotalMilliseconds;

            CellsCount = model.CellCount;
        }

        private void RecalcTimeStep(long stepTime)
        {
            if (stepTime > MaxTimeStep)
                MaxTimeStep = stepTime;
            if (stepTime < MinTimeStep)
                MinTimeStep = stepTime;
            AverageTimeStep += stepTime;
        }

        private void Redraw(Model2D model)
        {
            view2D.UpdateVisual(model);
        }

        private int NowTime()
        {
            var nowTime = DateTime.Now;
            return 3600 * nowTime.Hour + 60 * nowTime.Minute + nowTime.Second;
        }

        public async void DoThePicture(string picName, Model model)
        {
            Model2DWithAHL m2 = model as Model2DWithAHL;
            (double rough, double coverage, double thickness) = XavierResultsComparer.CalcPars(m2);
            string Na = $"iter={iterationId}_bCount={model.BiomassCount}_nR={(model.AverageNutrientRemain).ToString("F4")}" +
                $"_avU={(model.AverageAhl).ToString("F4")}_maxU={(model.maxAhl).ToString("F4")}_r={rough.ToString("F4")}" +
                $"_cov={coverage.ToString("F4")}_th={thickness.ToString("F4")}_sp={m2.SpreadValue}_uTh={model.AHLthreshold}";

            await Task.Yield();
            await Task.Yield();
            Screenshot($"{picName}_{Na}", model);
            await Task.Yield();
            await Task.Yield();
            audioMan.PlayPhotShot();

            if (MultipleSimulations)
                progressUI.Show();
        }

        public void DoPicture()
        {
            Screenshot(Guid.NewGuid().ToString() + ".jpg");
        }

        // === ADD THIS: header + params log (call once per run) ===   Math.Log10
        private void Screenshot(string name, Model model)
        {
            string path = imagesLoc + name + "AHLthresh_" + model.AHLthreshold.ToString("0.00") +
                "_Cdec" + model.ConsumeDescreaseVal.ToString("0.00") +
                "_SB" + model.SpreadProbMax.ToString("0.00") +
                "_" + NowTime().ToString() + ".jpg";

            ScreenCapture.CaptureScreenshot(path);
        }

        private void Screenshot(string name)
        {
            string path = imagesLoc + name + ".jpg";
            ScreenCapture.CaptureScreenshot(path);
        }

        private bool Paused = false;
        public void Pause()
        {
            Paused = !Paused;
        }
        public void Quit()
        {
            Application.Quit();
        }

        private void OnGUINo()
        {
            if (MultipleSimulations)
                return;
            if (iterationId <= 0) return;

            int i, j;
            Vector2 v = Vector2.zero;//mainCam.ScreenToWorldPoint(Input.mousePosition);
            if (modelType == ModelType.Hexagon)
                v.y /= 0.75f;

            i = (int)v.x;
            j = (int)v.y;

            float[] modelData = viewModel.GetDat(new Vector2Int(i, j));
            if (i >= 0 && j >= 0 && i < AreaWidth && j < AreaHeight)
                GUI.Label(new Rect(20, Screen.height - 250, 650, 250),
                    $"<size=38>(pos[ {i}, {j} ]    Bacteria = {modelData[0]:F4}, {Environment.NewLine}" +
                    $"Substrate = {modelData[1]:F4}, Ahl = {modelData[2]:F4}, {Environment.NewLine}" +
                    $"{Environment.NewLine} {viewModel.GetFun(new Vector2Int(i, j))}</size>");
        }




    }
}
