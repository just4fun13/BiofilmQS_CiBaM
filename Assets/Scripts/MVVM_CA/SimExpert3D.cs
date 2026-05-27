using Assets.Scripts.MVVM_CA.Models.ModelParams;
using Assets.Scripts.MVVM_CA.Utils;
using Assets.Scripts.Utils;
using CellularAutomaton;
using Cinemachine;
using DG.Tweening.Plugins.Core.PathCore;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static CellularAutomaton.BoxCountingMachine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts.MVVM_CA
{
    public class SimExpert3D : MonoBehaviour
    {

        private ViewModel viewModel;
        private ProgressUI progressUI;

        [SerializeField] private ImageToArrayLoader imageToArrayLoader;

        [SerializeField] private bool DinamycUpdate = true;
        [SerializeField] private ModelType modelType;
        [SerializeField] private double TimeStep => ModelParameters.mainParameters.TimeStep;
        [SerializeField] private View3D view3D;
        [SerializeField] private View2D view2D;

        [SerializeField] private int RedrawFrequency = 50;
        [SerializeField] private int MaxThreadCount = 1;
        [SerializeField] private bool MultipleSimulations = false;
        [SerializeField][Range(0.0001f, 0.01f)] private float PercentageOfUpdateAmount = 0.001f;



        private UIinput uiInput;
        private AudioMan audioMan;
        private int RefreshAmount = 0;
        private int maxStepsWithoutProgress = 50;
        private long iterationId = 0;
        private int stepsWithoutProgress = 0;
        private int CellsCount = 0;
        private int NotRefreshedCellCount = 0;
        private int simulStartTime;
        private int programStartTime;

        private string imagesLoc = Environment.CurrentDirectory + $@"\Log\Pictures\";
        private string logLoc = Environment.CurrentDirectory + $@"\Log\";

        private float nutrStart = 0.1f;
        private float nutrEnd = 1.05f;
        private float nutrDelta = 0.05f;
          
        private float totalSteps;
        private float nutrStep;
        private int CellCountToRefreshTheDraw;

        private Stopwatch watch;


        //[SerializeField] 
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

        public void RestartScene()
        {
            SceneMan.Instance.ReloadScene();
        }

        private CultureInfo fl = System.Globalization.CultureInfo.InvariantCulture;

        private float MaxTimeStep = -10f;
        private float MinTimeStep = 100000f;
        private float AverageTimeStep = 0f;


        private int MaxIterCount = 80000;
        private int ReprintImage = 36000000;
        private string PictureName = "";
        float MainProgress = 0f;
        private Vector4 InitPoint = new Vector4(0.2383198f, 0.03572778f, 0.00081308f, 10);
        private Vector4 NewPoint = new Vector4(0.2383198f, 0.03572778f, 0.00081308f, 10);
        private float InitError = 1.0499f;
        private float NewError = 0;
        private bool ViewInited = false;
        private bool SimulIsDone(Model model) => iterationId == MaxIterCount;//false;

                                                                             //model.averageSubstrate <= 0.05f;// stepsWithoutProgress >= maxStepsWithoutProgress;      
        private void Awake()
        {
            uiInput = FindObjectOfType<UIinput>();
            audioMan = FindObjectOfType<AudioMan>();
            progressUI = FindObjectOfType<ProgressUI>();
            uiInput.SetInputType(UIinput.InputType._3d);
            uiInput.SetInputs(new float[] {3, 30, 30, 30, 0.0264f, 0.0264f, 0.01f});
        }
        private void RefreshInitPoint(Model model, float er)
        {
            InitPoint = new Vector4((float)model.DiffusionKoef, (float)model.InitSubstrateCount, (float)model.μmax, model.InoculationCount);
            InitError = er;
        }
        private void SetModelParametersWithUI(Model model)
        {
            // W, H, L, Type, Dnutr, Dahl, alpha, betta, degr, AHK_k, Uth_k
            model.SetMainPars(uiInput.GetMainParsInputs());
            model.SetSize3D(uiInput.GetSize3);
            model.SetAhlPars(uiInput.GetAhlParsInputs());
            // model.SetSpreadProb(uiInput.GetAhlParsInputs()[1]);
            //  model.SetInocCount((int)uiInput.GetAhlParsInputs()[0]);
            model.SetRandomAmount(0.8f);
        }
        private void SetModelParametersRandom(Model model)
        {

            float[] mainPars = new float[] {
                UnityEngine.Random.Range(0.055f,0.75f)    ,       // diffusion K
                UnityEngine.Random.Range(0.005f,0.05f)    ,       // init nutrient
                UnityEngine.Random.Range(0.00001f,0.001f) ,       // muMax
            };
            model.SetInocCount(UnityEngine.Random.Range(1, 40));
            model.SetMainPars(mainPars);
            Vector4 v4 = new Vector4((float)model.DiffusionKoef, (float) model.InitSubstrateCount, (float)model.μmax, model.InoculationCount);
            Debug.Log($"Inited model with v4 : {v4.x}, {v4.y}, {v4.z}, {v4.w}");
            // W, H, Type, Dnutr, Dahl, alpha, betta, degr, AHK_k, Uth_k
        }
        private Vector4 GetV4(int dir, float stepMult)
        {
            NewPoint = InitPoint;
            if (dir > 0)
                NewPoint.x *= stepMult;
            else
                NewPoint.x /= stepMult;
            return NewPoint;
        }
        private void SetModelParametersWithV4(Model model, Vector4 v4)
        {

            float[] mainPars = new float[] { v4.x, v4.y, v4.z };
            model.SetInocCount((int)( v4.w));
            model.SetMainPars(mainPars);
            Debug.Log($"Inited model with v4 : {v4.x}, {v4.y}, {v4.z}, {v4.w}");
            // W, H, Type, Dnutr, Dahl, alpha, betta, degr, AHK_k, Uth_k
        }
        public async void StartSimul()
        {
            uiInput.SaveData();
            StartCoroutine(StartSimulation());
            await Task.Yield();
            uiInput.gameObject.SetActive(false);
        }
        private IEnumerator StartSimulation()
        {
            watch = new Stopwatch();
            programStartTime = NowTime();
            totalSteps = 20; ;// ((nutrEnd - nutrStart) / nutrDelta) * AreaSets.Length * 3;
            nutrStep = 1f;// ((nutrEnd - nutrStart) / nutrDelta);
            yield return new WaitForEndOfFrame();

            if (!MultipleSimulations)
            {
                Debug.Log("Solo sim mode activated");
                progressUI?.Hide();
                Model model = PrepareModelSolo();
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
                float totalT = 10;
                int k = 0;
                int KforSame = 10;
                int direction = 1;
                float stepMult = 1.05f;
                while (k <= totalT)
                {
                    /*if (NewError != 0 && k % KforSame == 0)
                    {
                        if (NewError < InitError)
                        {
                            Debug.Log($"NewError for 10 {NewError} is better, refreshing point.");
                            InitError = NewError;
                            InitPoint = NewPoint;
                        }
                        else
                        {
                            Debug.Log($"NewError for 10 {NewError} is worse, Oppositioning.");
                            direction *= -1;
                            stepMult /= 2f;
                        }
                        GetV4(direction, stepMult);
                        Debug.Log($"Getting new Point v4 {InitPoint}->{NewPoint}");
                        NewError = 0;
                    }*/

                    MainProgress = k / totalT * 1000;
                    progressUI.Progress(MainProgress); 
                    iterationId = 0;
                    //Debug.Log($"Nutr:{MaxNutrT}/{maxNurtTop}, ahl:{ahlT}/{ahlTtop}");
                    // LoadParsAndShowProg(new Vector2(MaxNutrT, ahlT), new Vector2(maxNurtTop, ahlTtop));
                    Model model = PrepareModelMulti();
                    model.SetRandomAmount(k*1f/10f);
                    k++;
                    StartCoroutine(RunSimul(model));
                    while (!SimulIsDone(model))
                    {
                        //progressUI.RefreshTime(NowTime() - programStartTime);
                        yield return new WaitForSeconds(0.5f);
                    }
                    NewError += (float) (CalcErAndWriteLog(model)/10d);
                }
                yield return new WaitForEndOfFrame();
                audioMan.PlayExplosion();
            }
        }
        private void InitView(Model model)
        {
            view3D.Init(modelType, model.GetPos, (model as Model3D).BiomassCells3D);
            //view2D.InitWithOffset(ModelType.SimpleSquare, GetPos, AreaWidth, AreaHeight, InitNutrient, view2DsideOffset);
            CameraFocusMan.Focus3DCamera(uiInput.GetSize3);
        }
        private void LoadParsAndShowProg(Vector2 paramsV, Vector2 maxParamsV)
        {
            float prog = paramsV.x * paramsV.y / maxParamsV.x / maxParamsV.y;
            progressUI.Progress(prog);
        }
        public Vector2 GetPos(Vector2Int coord)
        {
                return coord.x * Vector2.right + coord.y * Vector2.up;
        }
        private Model PrepareModelSolo()
        {
            Model model;
            ResetParams();
            //Vector2Int sizeFromImg = imageToArrayLoader.GetSize();
            int AreaWidth  = uiInput.GetSize3.x;//sizeFromImg.x;// 
            int AreaHeight = uiInput.GetSize3.y;
            int AreaLength = uiInput.GetSize3.z;//sizeFromImg.y;// 
            modelType = (ModelType)(uiInput.GetGridType + 4);
            Debug.Log($"Grid type is {modelType.ToString()}");
            float InitNutrient = uiInput.GetMainParsInputs()[1];
            model = new Model3DWithAHL(AreaWidth, AreaHeight, AreaLength, InitNutrient, modelType, TimeStep, MaxThreadCount);
            SetModelParametersWithUI(model);
            //model.ShowModelParamsList();
            model.InitInoculate();
            //model.InitInoculateFromFile(imageToArrayLoader.BottomLayerToDraw(AreaWidth));
            //imageToArrayLoader.InitModelWithImage(model);
            //            model.InitInoculateFromFile(ImageToArray.GreenFromImage(initBacImage, 12), 0.009f);

            if (DinamycUpdate)
            {
                InitView(model);
                //view3D.InitOneLayerMode(AreaWidth, AreaLength,  modelType, model.GetPos);
            }
            CellsCount = model.CellCount;
            CellCountToRefreshTheDraw = (int)(PercentageOfUpdateAmount * AreaHeight * AreaWidth * AreaLength);
            viewModel = new ViewModel(model, view3D);
            return model;
        }

        private Model PrepareModelMulti()
        {
            Model model;
            ResetParams();
            int AreaWidth  = uiInput.GetSize3.x;//sizeFromImg.x;// 
            int AreaHeight = uiInput.GetSize3.y;
            int AreaLength = uiInput.GetSize3.z;//sizeFromImg.y;// 
            float InitNutrient = uiInput.GetMainParsInputs()[1];
            modelType = ModelType.TruncOct;
            model = new Model3DWithAHL(AreaWidth, AreaHeight, AreaLength, InitNutrient, modelType, TimeStep, MaxThreadCount);
            //SetModelParametersWithV4(model, NewPoint);
            SetModelParametersWithUI(model);
            model.InitInoculate();
            CellsCount = model.CellCount;
            CellCountToRefreshTheDraw = (int)(PercentageOfUpdateAmount * AreaHeight * AreaWidth * AreaLength);
            //            viewModel = new ViewModel(model, view2D, view3D);
            viewModel = new ViewModel(model, view3D);
            return model;
        }
        private void ResetParams()
        {
            RefreshAmount = 0;
            maxStepsWithoutProgress = 1000;
            iterationId = 0;
            stepsWithoutProgress = 0;
            CellsCount = 0;
            NotRefreshedCellCount = 0;
            simulStartTime = NowTime();
        }

        private void ShowTimeProg()
        {
            progressUI.ShowTimeWithProgress(new Vector3(NowTime() - programStartTime, (int)iterationId, MainProgress));
        }
        private void CreateDir()
        {
            try
            {
                // Create the directory
                string path = PictureName + $@"Log\Pictures\{uiInput.GetMainParsInputs()[0].ToString("0.0000")}_" +
                    $@"{uiInput.GetMainParsInputs()[1].ToString("0.00")}_{uiInput.GetMainParsInputs()[2].ToString("0.0000")}";
                DirectoryInfo di = Directory.CreateDirectory(path);
                Debug.Log("Directory created successfully at: " + di.FullName);
            }
            catch (Exception ex)
            {
                Debug.Log("An error occurred: " + ex.Message);
            }
        }
        private IEnumerator RunSimul(Model model)
        {
            XavierResultsComparer.ShowExp();
            while (!SimulIsDone(model))
            {
                if (!MultipleSimulations)
                    ShowTime();
                else
                    ShowTimeProg();
                DoSimulStep(model);


                if (false && model.BottomLayerCountRemain() == 0)
                {
                    Debug.LogError($"Model growth is too much, termintated");
                    break;
                }

                //                if (iterationId % ReprintImage == 0 && ! DinamycUpdate)
                //if ( iterationId % 1000 == 0)
                //    ImageToArrayLoader.ShowModelError3D(model);
                //                ImageToArrayLoader.ShowModelErr(model);

                if (iterationId > 43200)
                    model.SetSpreadProb(0.05f);


                if (false && iterationId % (MaxIterCount / 10) == 0)
                {
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    //Vector2 frc = model.GetFractalDimension();
                    //string fracDim = frc.x.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    //DoThePicture(PictureName + $"-FracDim-{fracDim}_NR_{model.AverageNutrientRemain}");
                    DoThePicture(PictureName + $@"{uiInput.GetMainParsInputs()[0].ToString("0.0000")}_" +
                    $@"{uiInput.GetMainParsInputs()[1].ToString("0.00")}_{uiInput.GetMainParsInputs()[2].ToString("0.0000")}\{iterationId}");
                    yield return new WaitForEndOfFrame();
                }
                /*
                if (!MultipleSimulations && NotRefreshedCellCount > CellCountToRefreshTheDraw)
                {
                    if (DinamycUpdate)
                        Redraw();
                    NotRefreshedCellCount = 0;
                    yield return new WaitForEndOfFrame();
                    //DoThePicture();
                }
                else*/
                if (iterationId % 1800 == 0)
                {
                    XavierResultsComparer.CalcAndShotDiffExp1(model, iterationId);
                }
                if (iterationId % 50 == 0 )
                {
                    //WritePopDyn(model);
                    if (DinamycUpdate)
                        Redraw(model );
                    yield return new WaitForEndOfFrame();
                }
            }
            //if (!MultipleSimulations)
            {
                //if (!ViewInited)
                InitView(model);
                Redraw(model);
                DoThePicture(PictureName + $@"{uiInput.GetMainParsInputs()[0].ToString("0.0000")}_" +
                $@"{uiInput.GetMainParsInputs()[1].ToString("0.00")}_{uiInput.GetMainParsInputs()[2].ToString("0.0000")}_{iterationId}id_{System.DateTime.Now.Ticks}");
                audioMan.PlayExplosion();
                // WriteSimulationDynamics(model);
            }
        }
        private double CalcErAndWriteLog(Model model)
        {
            double er = ImageToArrayLoader.ShowModelError3D((model as Model3D));
            if (er < InitError)
                RefreshInitPoint(model, (float)er);
            Vector4 modelStat = model.ModelStats3D();
            int bottomSize = (model as Model3D).C3D.GetLength(0) * (model as Model3D).C3D.GetLength(2);
            string resStr = $"{uiInput.GetSize3.x},{uiInput.GetSize3.y},{uiInput.GetSize3.z}," +
                $"{model.DiffusionKoef},{model.InitSubstrateCount},{model.μmax}," +
                $"{model.InoculationCount},{model.randomDIrectionDIvideProbability},{model.AHLthreshold},{model.randomDIrectionDIvideProbability}" +
                $"{er},{Environment.NewLine}" +
                $"Fd = {model.GetFractalDimension().x}, Hmax = {modelStat.x}, Hfull = {modelStat.y}, Cav = {modelStat.z}, CountDivBot = {modelStat.w}" +
                $"BottomFill = { 1 - model.BottomLayerCountRemain()*1f/bottomSize}";
            MyLogger.WriteLog(resStr);
            Debug.Log(resStr);
            return er;
        }
        private void ShowTime()
        {
            progressUI.ShowTime(new Vector2Int(NowTime() - programStartTime, (int)iterationId));
        }
        private void DoSimulStep(Model model)
        {
            iterationId++;
            watch.Reset();
            watch.Start();
            viewModel.UpdateModel();
            watch.Stop();
            RecalcTimeStep(watch.ElapsedMilliseconds);
            /*            int newCellsCount = (model.CellCount) - CellsCount;
                        NotRefreshedCellCount += newCellsCount;
                        if (newCellsCount == 0)
                            stepsWithoutProgress++;
            */
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
        private void Redraw(Model model)
        {
            viewModel.UpdateView3D();
            //view2D.UpdateVisualAHL_Slice(model);
        }
        private void RedrawFromScratch()
        {
            viewModel.UpdateView3DFromScratch();
        }
        private void ClearMemory() => viewModel.ClearMemory();
        private int NowTime()
        {
            var nowTime = DateTime.Now;
            return 3600 * nowTime.Hour + 60 * nowTime.Minute + nowTime.Second;
        }
        public async void DoThePicture(string picName)
        {
            progressUI.Hide();
            if (!DinamycUpdate)
            {
                RedrawFromScratch();
                await Task.Yield();
                await Task.Yield();
            }
            //            Screenshot($"{modelType.ToString()}_{AreaWidth}_{AreaHeight}_{InitNutrient * 100}_({NowTime() - simulStartTime}sec)");
            Screenshot($"{picName}");
            audioMan.PlayPhotShot();
            await Task.Yield();
            await Task.Yield();
            //ClearMemory();
            if (MultipleSimulations)
                progressUI.Show();
//            Debug.Log("Picture is done");
        }        
/*        public async void DoThePicturesLayers(string picName)
        {
            progressUI.Hide();
            for (int h = 1; h <= 20; h++)
            {
                TopViewCamT.position = new Vector3(TopViewCamT.position.x, h, TopViewCamT.position.z);
                if (!DinamycUpdate)
                {
                    RedrawFromScratch();
                    await Task.Yield();
                    await Task.Yield();
                }
                //            Screenshot($"{modelType.ToString()}_{AreaWidth}_{AreaHeight}_{InitNutrient * 100}_({NowTime() - simulStartTime}sec)");
                Screenshot($"{picName}_{h}");
                await Task.Yield();
                await Task.Yield();
            }
            audioMan.PlayPhotShot();
            //ClearMemory();
            if (MultipleSimulations)
                progressUI.Show();
        }*/
        private void Screenshot(string name, bool addNowTime = false)
        {
            string path = imagesLoc + name + "_" + ".jpg";
            if (addNowTime)
                path = imagesLoc + name + "_" + NowTime().ToString() + ".jpg";
            ScreenCapture.CaptureScreenshot(path);
        }
        public void Quit()
        {
            Application.Quit();
        }
        private void WriteSimulationDynamics(Model model)
        {
            int AreaWidth = (int)uiInput.GetSize3.x;//sizeFromImg.x;// 
            int AreaHeight = (int)uiInput.GetSize3.y;
            int AreaLength = (int)uiInput.GetSize3.z;//sizeFromImg.y;// 
            float InitNutrient = uiInput.GetMainParsInputs()[1];
            Vector2 frc = model.GetFractalDimension();
            string fracDim = frc.x.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string name = $"{AreaWidth}_{AreaHeight}_{AreaLength}_{model.SpreadProbMax}_{model.InoculationCount}_{InitNutrient}";
            MyLogger.WriteLog($"{AreaWidth};{AreaHeight};{AreaLength};{InitNutrient};{model.SpreadProbMax};{model.InoculationCount};{fracDim};" +
                $"{model.BiomassCount};{model.AverageNutrientRemain};{AreaWidth*AreaLength-model.BottomLayerCountRemain()}");
            Debug.Log($"Simul is done {iterationId}  for {NowTime() - simulStartTime} seconds; G = {model.G}; FracDim = {fracDim} for ");
            DoThePicture(name);
        }
        private void WritePopDyn(Model model)
        {
            int AreaWidth = (int)uiInput.GetSize3.x;//sizeFromImg.x;// 
            int AreaHeight = (int)uiInput.GetSize3.y;
            int AreaLength = (int)uiInput.GetSize3.z;//sizeFromImg.y;// 
            float InitNutrient = uiInput.GetMainParsInputs()[1];
            MyLogger.WriteLog($"{AreaWidth};{AreaHeight};{AreaLength};{InitNutrient};{model.SpreadProbMax};{model.InoculationCount};" +
                $"{model.BiomassCount};{model.AverageNutrientRemain};{AreaWidth * AreaLength - model.BottomLayerCountRemain()};{iterationId}");
        }

    }
}
