using Assets.Scripts.DiffusionModels;
using Assets.Scripts.DiffusionModels.Geometry;
using Assets.Scripts.MVVM_CA;
using Assets.Scripts.MVVM_CA.Models.ModelParams;
using CellularAutomaton;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class Diffusion2DSimulationController : MonoBehaviour
    {
        public enum ProblemType
        {
            OriginSqr,
            AnalSqr,
            RectangularBiology,
            BoneComsol,
        }

        [SerializeField] private View2D viewDeltas;
        [SerializeField] private View2D viewConcentr;
        [SerializeField] private GridType gridType;
        //[SerializeField] private Base2D base2D;
        private int AreaWidthCells = 100;
        private int AreaHeightCells = 100;
        [SerializeField] public int Nmin = 40;
        [SerializeField] public int Nmax = 40;
        [SerializeField] public int N = 30;
        [SerializeField] public TMP_Text iterText;
        [SerializeField] private double maxTime = 10;
        [SerializeField] private LineRenderer borderLineRenderer;
        [SerializeField] private int timeScaleK = 4;
        [SerializeField] private bool WriteLog = false;
        [SerializeField] private bool DynamicUpdate = false;
        [SerializeField] private bool DrawPictureAtTheEnd = false;


        [SerializeField] private double ADI_deltaTime = 10;

        [SerializeField] private bool UseImprovedDiffusion = false;

        [SerializeField] private ProblemType problemType;
        private IReferenceField referenceField;

        private string comsolOriginSqrUloc = Environment.CurrentDirectory + $@"\Data\Comsol2D_data.csv";
        private string comsolUCircleloc = Environment.CurrentDirectory + $@"\Data\Comsol2D_circle.csv";
        private string comsolAverageBio = Environment.CurrentDirectory + $@"\Data\BioExtendedData.csv";
        private string comsolAverageBioNoFlowNoConc = Environment.CurrentDirectory + $@"\Data\NFNC.csv";
        private string comsolAverageBioNoFlow = Environment.CurrentDirectory + $@"\Data\NF.csv";


        private CellGeometry base2D, deltaBase;
        private Camera mainCam;
        int maxIterCount = 0;
        private double deltaT, simTime, prevSimTime, nextSimTime;
        private double RedrawDelta = 200000.5;
        long iter = 0;
        int iterStep = 1;
        DiffusionModel model;
        AudioMan audioMan;
        int drawStep = 0;
        [SerializeField] private int maxThreadCount = 24;
        private double[,] deltaS;
        private double maxDelta;
        double timeStepForDelta = 1d;//0.5d;
        private int itersToShowDelta = 0;
        protected List<Vector3Int> InitPointsList = new List<Vector3Int>();


        [Header("Model parameters")]
        [SerializeField] float AreaWidth = 1;
        [SerializeField] float AreaHeight = 1;
        [SerializeField] float DiffusionKoef = 1;
        [SerializeField] float FlowVelocity = 1;
        [SerializeField] float ConsumptionRate = 1;



        private System.Diagnostics.Stopwatch timer;


        private async void Awake()
        {
            timer = System.Diagnostics.Stopwatch.StartNew();
            if (Nmax == Nmin)
            {
                PrepareModelAndGo();
                StartCoroutine(RunSimul());
                 //ReadDataComsolObsolete();
                //ReadDataMatlab();
                //StartCoroutine(CalcComsolMatlabDif());
                //StartCoroutine(RunSimul());
            }
            else
                StartCoroutine(DoManyIterations());
        }
        IEnumerator DoManyIterations()
        {
            for (N = Nmin; N <= Nmax; N += 10)
            //N = 100;
            //for (int k = 1; k < 6; k++)
            {
                timer.Restart();
                //if (!UseImprovedDiffusion)
                //    ADI_deltaTime = 1d / N / N;//Math.Pow(10, -k);//

                yield return new WaitForEndOfFrame();
                ResetState();
                yield return new WaitForEndOfFrame();
                PrepareModelAndGo();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                //ReadDataMatlab();
                yield return StartCoroutine(RunSimul());
            }
            while (true)
            {
                audioMan.PlayExplosion();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
            }
        }



        private void ResetState()
        {
            deltaS = null;

            // Сброс счётчиков и времён
            iter = 0;
            simTime = 0;
            maxDelta = 0;
            drawStep = 0;
            iterStep = 1;

            // Удалить старую модель
            if (model != null)
            {
                // Если модель реализует IDisposable — вызвать Dispose()
                model = null;
            }
        }
        private void PrepareModelAndGo()
        {

            simTime = 0;
            prevSimTime = 0;
            nextSimTime = 10;
            iter = 0;
            referenceField ??= CreateReferenceField();
            mainCam = Camera.main;
            audioMan = GameObject.FindObjectOfType<AudioMan>();
            base2D = CreateGeometry(gridType);
            //model = new OriginModel(AreaW, AreaH, base2D, MaxTCount, MaxDCount, DivProbability, _μ);
            /*if (UseImprovedDiffusion)
                model = new DiffusionModelByADI(AreaWidth, AreaHeight, base2D, N, deltaTime);
            else
                model = new DiffusionModel(AreaWidth, AreaHeight, base2D, N);
            */
            ADI_deltaTime = -1;
            if (UseImprovedDiffusion)
                model = new DiffusionModelByADI(AreaWidth, AreaHeight, base2D, N, ADI_deltaTime, DiffusionKoef, FlowVelocity, ConsumptionRate);
            else
                model = new FlowDiffusionModel(AreaWidth, AreaHeight, base2D, N, DiffusionKoef, FlowVelocity, ConsumptionRate);


            model.BoundaryCondition = CreateBoundaryCondition();
            model.SetMaxTreadCount(maxThreadCount);
            AreaWidthCells = model.AreaWidthCells;
            AreaHeightCells = model.AreaHeightCells;
            referenceField?.BindGrid(base2D, AreaWidthCells, AreaHeightCells);
            //Focus2DCamera();
            if (DrawPictureAtTheEnd || DynamicUpdate)
            {
                //viewConcentr.InitForDIffusionGameObjects(AreaWidthCells, AreaHeightCells, base2D);
                int pointsX = 250;
                int pointsY = 50;
                float deltaScale = 0.02f;
                deltaBase = CreateGeometry(GridType.Square);
                viewDeltas.InitForDIffusionGameObjects(pointsX, pointsY, deltaBase);
                //viewDeltas.SetScale(deltaScale);
                viewDeltas.FitToWorldRect(AreaWidth, AreaHeight);
                //viewConcentr.SetScale((float)(model.scale));
                deltaS = new double[pointsX, pointsY];
            }

            deltaT = model.deltaTime;
            itersToShowDelta = (int)(timeStepForDelta / deltaT);

            maxIterCount = (int)(maxTime / deltaT);
            iterStep = 40;// maxIterCount / 5; 
            if (maxIterCount < 10000)
                drawStep = 1;
            else
                drawStep = maxIterCount / 10000;
            model.SetMaxIter(maxIterCount);
            model.SetTimeScaleK(timeScaleK);
            if (problemType == ProblemType.AnalSqr )
                InitWithAnalyticAtT0(referenceField);
            DrawBorderOnView();
        }

        private void CalcDeltaWithReference(bool showInLog, bool showInDebug)
        {
            /*            double newdelta = referenceField.GetDelta(simTime, model.Cn);
                        if (showInDebug)
                            Debug.Log($"Time {simTime}, delta = {newdelta}");
                        if (showInLog && WriteLog)
                        {
                            string n = gridType.ToString();
                            if (UseImprovedDiffusion)
                                n = $"{deltaTime}";
                            MyLogger.WriteLog($"{n}\t{N}\t{simTime:0.0}\t{newdelta}\t{timer.ElapsedMilliseconds}\t{maxThreadCount}");
                        }
                        return;

                        */

            if (referenceField == null)
                return;

            double maxD = double.NegativeInfinity;
            double minD = double.PositiveInfinity;
            double sumD = 0;
            int count = 0;
            double modelVal;
            Array.Clear(deltaS, 0, deltaS.Length);

            List<Vector2Int> filledPoints = new List<Vector2Int>();

            foreach (Vector2 point in referenceField.PointsToCompare)
            {
                if (!referenceField.TryGetValue(point, simTime, out double refVal))
                    continue;
                //double modelVal = model.Cn[cell.x, cell.y];
                Vector2Int cell = GetIdOnUniformGrid(point*1000); // referenceField.GetId(point); 
                if (filledPoints.Contains(cell))
                    Debug.LogError($"Doubling for {point*10000}->{cell}");
                else
                    filledPoints.Add(cell);
                if (gridType != GridType.Hexagone)
                    modelVal = SquareSampler.SampleBilinear(model.Cn, base2D, point, AreaWidthCells, AreaHeightCells);
                else
                {
                    modelVal = HexSampler.SampleBilinearShiftedHex(model.Cn, base2D, point, AreaWidthCells, AreaHeightCells);
                    //modelVal = HexSampler.SampleP1_ByNeighborFan(model.Cn, base2D, point, AreaWidthCells, AreaHeightCells);
                }
                //model.Cn[cell.x, cell.y];
                double delta = Math.Abs(modelVal - refVal);
                //                Debug.Log($"t = {simTime}, point [{base2D.GetPosition(cell)}]->[{cell.x},{cell.y}] = sim:({modelVal} - {refVal}) = {delta}");
                //Debug.Log($"{1000*point}->{cell} = [{delta*1000}]");
                deltaS[cell.x, cell.y] = 100 * delta;


                if (delta > maxD) maxD = delta;
                if (delta < minD) minD = delta;

                sumD += delta;
                count++;
            }
            //Debug.Log($"TotalFiiled {filledPoints.Count}");


            if (count == 0) return;
            double avDelta = sumD / count;

            if (showInLog && WriteLog)
            {
                string n = gridType.ToString()+"Fair";
                if (UseImprovedDiffusion)
                    n = $"{ADI_deltaTime}";
                    MyLogger.WriteLog($"{n}\t{N}\t{simTime:0.00}\t{maxD}\t{avDelta}\t{timer.ElapsedMilliseconds}\t{maxThreadCount}\t{deltaT}" +
                        $"\t{FlowVelocity}\t{ConsumptionRate}");
            }

            if (showInDebug)
            {
                Debug.Log($"Ref delta [{referenceField.Name}]: t={simTime:0.00}, min={minD}, max={maxD}, avg={avDelta}");
            }
        }

        private void DrawBorderOnView()
        {
            if (model == null) return;

            borderLineRenderer.positionCount = 5; // 5 точек, чтобы замкнуть периметр

            // Используем scale для толщины линии
            float scale = (float)model.scale;
            float lineWidth = scale / 5f; // Линия тоньше, чем сама ячейка

            borderLineRenderer.startWidth = lineWidth; // Устанавливаем startWidth
            borderLineRenderer.endWidth = lineWidth;   // Устанавливаем endWidth

            float physicalWidth = AreaWidth;
            float physicalHeight = AreaHeight;

            // Точки по внешним углам области (0,0) - (AreaWidth, AreaHeight)
            Vector3 p0 = Vector3.zero;
            Vector3 p1 = new Vector3(0, physicalHeight);
            Vector3 p2 = new Vector3(physicalWidth, physicalHeight);
            Vector3 p3 = new Vector3(physicalWidth, 0);

            borderLineRenderer.SetPosition(0, p0);
            borderLineRenderer.SetPosition(1, p1);
            borderLineRenderer.SetPosition(2, p2);
            borderLineRenderer.SetPosition(3, p3);
            borderLineRenderer.SetPosition(4, p0); // Замыкаем
        }

        public static Vector2Int GetIdOnUniformGrid(Vector2 pos)
        {
            float dx = 0.02f;
            float dy = 0.02f;



            int i = Mathf.RoundToInt ((pos.x - dx/2f ) / dx);
            int j = Mathf.RoundToInt ((pos.y - dy/2f) / dy);


            return new Vector2Int(i, j);
        }


        IEnumerator RunSimul()
        {
            CalcDeltaWithReference(true, true);
            yield return new WaitForEndOfFrame();
            while (simTime <= maxTime )
            {
                /*                if (model.BotCn >= 0.1902)
                                {
                                    iterText.text = $"{iter}{Environment.NewLine}{simTime.ToString("00.00")}";
                                    MyLogger.WriteLog($"{model.BotCn}");
                                    yield return new WaitForEndOfFrame();
                                    break;
                                }*/
                model.DoGrowthStep();

                if ( iter % iterStep == 0   ) //(iter % (10) == 0)
                {
                    //view.DrawDiffusion(model.Cn);
                    if (DynamicUpdate)
                    {
                        //CalcDeltaComsol(showInLog: WriteLog, showInDebug: true);
                        //CalcDeltas(true, false);
                        //viewDeltas.DrawDelta(deltaS);
                        //viewConcentr.DrawDiffusion(model.Cn);
                        viewDeltas.DrawDiffusion(deltaS);
                        //viewConcentr.DrawDiffusion(model.Cn);
                    }
                    ShowIterTime();    
                    yield return new WaitForEndOfFrame();
                }
                double tPrev = simTime;
                simTime += deltaT;
                iter++;
                if (iter % itersToShowDelta == 0)//tPrev < nextSimTime && simTime >= nextSimTime)
                {
                    //CalcDeltas(true,true);
                    CalcDeltaWithReference(showInLog: WriteLog, showInDebug: true);
                }
                if ((simTime % RedrawDelta) < deltaT)
                {
                    //CalcDeltas(true, true);
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    MyLogger.Screenshot($"{gridType.ToString()}_{N}_{(int)simTime}");
                }
            }
            //CalcDeltas(true, true);
            ShowIterTime();
            if (DrawPictureAtTheEnd)
            {
                viewDeltas.DrawDiffusion(deltaS);
                viewConcentr.DrawDiffusion(model.Cn);
            
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                MyLogger.Screenshot($"{gridType.ToString()}_{N}_{(int)simTime}_mu_{ConsumptionRate}_u_{FlowVelocity}");
            }
            yield return new WaitForEndOfFrame();
            timer.Stop();
            audioMan.PlayExplosion();
            //Debug.Log($"Simul is done = {model.BotCn} for {timer.ElapsedMilliseconds*1f/1000f}");
            Debug.Log($"N={N}, simTime={simTime:F3}, timeStepForDelta={timeStepForDelta:F4}, t * timeStepForDelta = {simTime * timeStepForDelta:F3}");
            Debug.Log($"Итерация:{iter}{Environment.NewLine}Время симуляции:{simTime.ToString("00.00")}{Environment.NewLine}Реальное время:{(timer.ElapsedMilliseconds / 1000f).ToString("0.00")}");
        }
/*        private void Focus2DCamera()
        {
            Vector2 leftDown = base2D.GetPosition(Vector2Int.zero);
            Vector2 rightTop = base2D.GetPosition(new Vector2Int(AreaWidthCells - 1, AreaHeightCells - 1));
            float hSize = (AreaHeight) / 2f;
            float lSize = (AreaWidth) / 2f / 16f * 9f;
            mainCam.orthographicSize = 0.5f;// Mathf.Max(hSize, lSize) + 1 ;
            mainCam.transform.position = (Vector3)(leftDown + (rightTop - leftDown) / 2f) - Vector3.forward * 10;
            //mainCam.ResetProjectionMatrix();
        }*/
        private void Focus2DCamera()
        {
            if (mainCam == null) return;

            float physicalWidth = AreaWidth;
            float physicalHeight = AreaHeight;

            // 1. Рассчитываем позицию центра области
            // Предполагаем, что область симуляции находится в пределах (0,0) - (AreaWidth, AreaHeight)
            Vector3 center = new Vector3(physicalWidth / 2f, physicalHeight / 2f, 0f);

            // 2. Устанавливаем позицию камеры (смещение по Z для просмотра)
            mainCam.transform.position = center - Vector3.forward * 10f; // -10f - стандартная Z для 2D-камеры

            // 3. Рассчитываем Orthographic Size
            float targetAspect = (float)Screen.width / Screen.height;

            // Высота, необходимая для показа всей области по Y
            float requiredSizeY = physicalHeight / 2f;

            // Высота, необходимая для показа всей области по X (с учетом Aspect Ratio)
            float requiredSizeX = (physicalWidth / 2f) / targetAspect;

            float desiredOrthoSize = Mathf.Max(requiredSizeX, requiredSizeY);

            // Добавляем небольшой запас (10%), чтобы границы не прилегали к краю экрана
            desiredOrthoSize *= 1.1f;

            mainCam.orthographicSize = desiredOrthoSize;
        }
        private void ShowIterTime()
        {
            //iterText.text = $"Итерация:{iter}{Environment.NewLine}Время симуляции:{simTime.ToString("00.00")}{Environment.NewLine}Реальное время:{(timer.ElapsedMilliseconds / 1000f).ToString("0.00")}";
            iterText.text = $"Iteration:{iter}{Environment.NewLine}Simulation time:{simTime.ToString("00.00")}{Environment.NewLine}Real time:{(timer.ElapsedMilliseconds / 1000f).ToString("0.00")}";
        }


        private IBoundaryCondition CreateBoundaryCondition()
        {
            switch (problemType)
            {
                case ProblemType.OriginSqr:
                    return new TopSegmentDirichlet { value = 10, xMinFraction = 0.4f, xMaxFraction = 0.6f };

                case ProblemType.AnalSqr:
                    return new AnalSqrBoundaries();
                case ProblemType.RectangularBiology: 
                    return new RectangularBiologicalTask();

                default:
                    return null;
            }
        }
        private IReferenceField CreateReferenceField()
        {
            switch (problemType)
            {
                case ProblemType.OriginSqr:
                    return new ComsolReferenceField(problemType.ToString(), comsolOriginSqrUloc, maxTime);

                case ProblemType.AnalSqr:
                    return new AnalSqr();

                case ProblemType.BoneComsol:
                    return new ComsolReferenceField(problemType.ToString(), comsolOriginSqrUloc, maxTime);

                case ProblemType.RectangularBiology:
                    if (FlowVelocity > 0)
                        return new ComsolReferenceField(problemType.ToString(), comsolAverageBio, maxTime);
                    else
                        if (ConsumptionRate > 0)
                            return new ComsolReferenceField(problemType.ToString(), comsolAverageBioNoFlow, maxTime);
                        else
                            return new ComsolReferenceField(problemType.ToString(), comsolAverageBioNoFlowNoConc, maxTime);

                default:
                    return null;
            }
        }
        private void InitWithAnalyticAtT0(IReferenceField analytic)
        {
            for (int i = 0; i < AreaWidthCells; i++)
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    var cell = new Vector2Int(i, j);
                    if (analytic.TryGetValue(cell, 0.0, out double val))
                        model.Cn[i, j] = val;
                    else
                        model.Cn[i, j] = 0.0;
                }
            }
        }

        private CellGeometry CreateGeometry(GridType gridType)
        {
            switch (gridType) 
            { 
                case GridType.Square:
                    return new SimpleSquare();
                case GridType.ExtendedSquare:
                    return new ExtendedSquare();
                case GridType.Hexagone:
                    return new Hexagon();
                default:
                    Debug.LogError($"Innable to create geometry for grid type <{gridType.ToString()}>");
                    return null;
            }


        }
    }
}
