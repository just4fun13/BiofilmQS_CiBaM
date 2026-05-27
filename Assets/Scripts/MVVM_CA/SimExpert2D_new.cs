using Assets.Scripts.MVVM_CA.Models._2D;
using Assets.Scripts.MVVM_CA.Models.ModelParams;
using CellularAutomaton;
using Cinemachine;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CellularAutomaton.BoxCountingMachine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts.MVVM_CA
{
    public class SimExpert2D_new : MonoBehaviour
    {
        private ProgressUI progressUI;
        private Camera mainCam;

        private bool DinamycUpdate => ModelParameters.mainParameters.DynamicUpdate;
        private ModelType modelType => ModelParameters.geometryParameters.gridType;
        private int AreaWidth => ModelParameters.geometryParameters.AreaWidth;
        private int AreaHeight = ModelParameters.geometryParameters.AreaHeight;
        private double InitNutrient => ModelParameters.nutrientParameters.InitialDensity;
        private double TimeStep => ModelParameters.mainParameters.TimeStep;

        private int MaxSimTime => (int) (ModelParameters.mainParameters.MaxTimeInHours/TimeStep*3600);

        [SerializeField] private View2D view2D;
        [SerializeField] private int RedrawFrequency = 50;
        [SerializeField] private int MaxThreadCount = 1;
        [SerializeField] private bool MultipleSimulations = false;
        [SerializeField][Range(0.0001f, 0.01f)] private float PercentageOfUpdateAmount = 0.001f;

        private AudioMan audioMan;
        private int RefreshAmount = 0;
        private int maxStepsWithoutProgress = 50;
        private long iterationId = 0;
        private double simulationTime = 0;
        private int stepsWithoutProgress = 0;
        private int CellsCount = 0;
        private int NotRefreshedCellCount = 0;
        private int simulStartTime;
        private int programStartTime;

        private string imagesLoc = Environment.CurrentDirectory + $@"\Log\Pictures\";
        private string logLoc = Environment.CurrentDirectory + $@"\Log\";

        private Model2D model;

          
        private float totalSteps;
        private float nutrStep;
        private int CellCountToRefreshTheDraw;

        private Stopwatch watch;

        public void RestartScene()
        {
            SceneMan.Instance.ReloadScene();
        }

        private CultureInfo fl = System.Globalization.CultureInfo.InvariantCulture;

        private float MaxTimeStep = -10f;
        private float MinTimeStep = 100000f;
        private float AverageTimeStep = 0f;

        private float MainProgress = 0f;
        private int ReprintImage = 2; // in hours
        private string PictureName = "";
        private bool SimulIsDone(Model model) => simulationTime == MaxSimTime;//false;
                                                                             //model.averageSubstrate <= 0.05f;// stepsWithoutProgress >= maxStepsWithoutProgress;      

        private void Awake()
        {
            mainCam = Camera.main;
            audioMan = FindObjectOfType<AudioMan>();
            StartSimul();
        }


        public async void StartSimul()
        {
            //Screenshot("Screen");
            StartCoroutine(StartSimulation());
            await Task.Yield();
        }

        private IEnumerator StartSimulation()
        {
            watch = new Stopwatch();
            programStartTime = NowTime();
            progressUI = FindObjectOfType<ProgressUI>();
            yield return new WaitForEndOfFrame();
            Debug.Log("Solo sim mode activated");
            progressUI?.Hide();
            Model2D model = PrepareModelSolo();
            StartCoroutine(RunSimul(model));
            while (!SimulIsDone(model))
            {
                progressUI.RefreshTime(NowTime() - programStartTime);
                yield return new WaitForSeconds(0.5f);
            }
        }
        private void ShowProg(float prog)
        {
            progressUI.Progress(prog);
        }
        private Model2D PrepareModelSolo()
        {
            
            ResetParams();
            if (ModelParameters.aHLParameters.UseAHL)
                model = new Model2DWithAHL(AreaWidth, AreaHeight, (float)InitNutrient, modelType, TimeStep, MaxThreadCount);
            else
                model = new Model2DnoAHL(AreaWidth, AreaHeight, (float)InitNutrient, modelType, TimeStep, MaxThreadCount);

            ModelParameters.ShowInDebug();
            model.SetDifK(ModelParameters.nutrientParameters.NutrientDiffusion);
            model.Setμmax(ModelParameters.bacteriaParameters.mUmax);
            model.SetKs(ModelParameters.bacteriaParameters.kS);
            model.SetInocCount(ModelParameters.bacteriaParameters.InitialInoculationCount);
            model.SetYxs(ModelParameters.bacteriaParameters.Yxs);
            model.SetConcToDivide(ModelParameters.bacteriaParameters.ConcToDivide);
            model.InitInoculate();
            ReprintImage *= (int) (3600 / TimeStep);
            if (!MultipleSimulations)
            {
                view2D.Init(modelType, model.GetPos, AreaWidth, AreaHeight, InitNutrient );
                Focus2DCamera();
            }
            CellsCount = model.CellCount;
            CellCountToRefreshTheDraw = (int)(PercentageOfUpdateAmount * AreaHeight * AreaWidth);
            return model;
        }
        private Model2D PrepareModelMulti()
        {
            Model2D model;
            model = new Model2DWithAHL(AreaWidth, AreaHeight, (float)InitNutrient, modelType, TimeStep, MaxThreadCount);
            //SetModelParametersRandom(model);
            //SetMoveParametersMultiVariant(model, inoc, ahlTH, redCons);
            model.InitInoculate();
            CellsCount = model.CellCount;
            CellCountToRefreshTheDraw = (int)(PercentageOfUpdateAmount * AreaHeight * AreaWidth);

            //            viewModel = new ViewModel(model, view2D, view3D);
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
            NotRefreshedCellCount = 0;
            simulStartTime = NowTime();
        }
        private void Focus2DCamera()
        {
            FindObjectOfType<CinemachineVirtualCamera>()?.gameObject.SetActive(false);
//            mainCam.Reset();
//            mainCam.clearFlags = CameraClearFlags.Color;
//            mainCam.backgroundColor= Color.white;
            mainCam.orthographic = true;
/*            foreach (Camera cm in FindObjectsOfType<Camera>())
                if (cm != mainCam)
                    cm.gameObject.SetActive(false);*/
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.ResetProjectionMatrix();

            float wx = AreaWidth / 2f ;
            float hy = AreaHeight * 0.75f;
            if (modelType != ModelType.Hexagon)
                hy = AreaHeight;
            mainCam.orthographicSize = Mathf.Max(wx / Screen.width * Screen.height, hy / 2f) + 0.5f;
            mainCam.transform.position = new Vector3( wx, hy/2f - 0.25f, -10f);
//            mainCam.orthographicSize = 67;
//            mainCam.transform.position = new Vector3(AreaWidth / 2f - 0.25f, 66, -10f);

//            mainCam.orthographicSize = 34;
//            mainCam.transform.position = new Vector3(80, 30, -10f);
        }
        private IEnumerator RunSimul(Model2D model)
        {
            while (!SimulIsDone(model))
            {
                if (!MultipleSimulations)
                    ShowTime();
                else
                    ShowTimeProg();

                DoSimulStep(model);

                if (iterationId % ReprintImage == 0)
                {
                    yield return new WaitForEndOfFrame();
                    Redraw();
                    yield return new WaitForEndOfFrame();
                    Vector2 frc = model.GetFractalDimension();
                    string fracDim = frc.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                    DoThePicture(PictureName + $"-FracDim-{fracDim}_NR_{model.AverageNutrientRemain.ToString("0.00")}", model);
                    yield return new WaitForEndOfFrame();
                }
                if (!MultipleSimulations && NotRefreshedCellCount > CellCountToRefreshTheDraw)
                {
                    if (DinamycUpdate)
                        Redraw();
                    NotRefreshedCellCount = 0;
                    yield return new WaitForEndOfFrame();
                    //DoThePicture();
                }
                else
                if (iterationId % 50 == 0)
                {
                    if (DinamycUpdate && iterationId % RedrawFrequency == 0)
                        Redraw();
                    yield return new WaitForEndOfFrame();
                }
            }
            //Redraw();
            //DoThePicture();
            if (!MultipleSimulations)
            {

                Vector2 frc = model.GetFractalDimension();
                string fracDim = frc.x.ToString(System.Globalization.CultureInfo.InvariantCulture);
                DoThePicture(PictureName + $"-FracDim-{fracDim}_NR_{model.AverageNutrientRemain}", model);
                audioMan.PlayExplosion();
                Debug.Log($"Simul is done {iterationId}  for {NowTime() - simulStartTime} seconds; " +
                    $"G = {model.G}; FracDim = {fracDim}; Model error = {ImageToArrayLoader.ShowModelError2D(model)} ");
            }
            else
            {/*
                string resStr = $"{uiInput.GetSize.x},{uiInput.GetSize.y}," +
                    $"{model.DiffusionKoef},{model.InitSubstrateCount},{model.μmax}," +
                    $"{model.InoculationCount},{model.SpreadProb}," +
                    $"{ImageToArrayLoader.ShowModelError2D(model)}";
                MyLogger.WriteLog(resStr);
                Debug.Log(resStr);*/
            }
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
            watch.Reset();
            watch.Start();
            
            UpdateModel();
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
        private void Redraw()
        {
            UpdateView();
        }
        private int NowTime()
        {
            var nowTime = DateTime.Now;
            return 3600 * nowTime.Hour + 60 * nowTime.Minute + nowTime.Second;
        }
        public async void DoThePicture(string picName, Model model)
        {
            progressUI.Hide();
            await Task.Yield();
            await Task.Yield();
//            Screenshot($"{modelType.ToString()}_{AreaWidth}_{AreaHeight}_{InitNutrient * 100}_({NowTime() - simulStartTime}sec)");
            Screenshot($"{picName}_ALL", model);
            await Task.Yield();
            await Task.Yield();
/*
            view2D.ShowAHL = false;
            view2D.ShowBacteria = false;
            view2D.ShowNutrient = true;
            Redraw();
            await Task.Yield();
            await Task.Yield();
            Screenshot($"{picName}_S", model);
            await Task.Yield();
            await Task.Yield();

            view2D.ShowNutrient = false;
            view2D.ShowBacteria = true;
            Redraw();

            await Task.Yield();
            await Task.Yield();
            Screenshot($"{picName}_C", model);
            await Task.Yield();
            await Task.Yield();

            view2D.ShowAHL = true;
            view2D.ShowBacteria = false;
            Redraw();

            await Task.Yield();
            await Task.Yield();
            Screenshot($"{picName}_U", model);
            await Task.Yield();
            await Task.Yield();

            view2D.ShowAHL = true;
            view2D.ShowBacteria = true;
            view2D.ShowNutrient = true;


            await Task.Yield();
            await Task.Yield();
            Screenshot($"{picName}_U", model);*/

           // MyLogger.WriteLog($"{AreaWidth};{AreaHeight};{model.ConsumeDescreaseVal};{model.SpreadProb};{model.AHLthreshold};{model.BiomassCount};{model.Biomass2DVolume()}");
            audioMan.PlayPhotShot();


            if (MultipleSimulations)
                progressUI.Show();
//            Debug.Log("Picture is done");
        }
        private void WriteBiomassDynamics(Model2D model)
        {
            MyLogger.WriteLog($"{AreaWidth};{AreaHeight};{model.ConsumeDescreaseVal};{model.SpreadProbMax};{model.AHLthreshold};{model.InoculationCount};" +
                $"{model.BiomassCount};{model.Biomass2DVolume()};{model.BottomLayerCountRemain()};{Math.Log10(model.AverageNutrientRemain)};{iterationId}");
        }
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

        private void OnGUI()
        {
            if (iterationId <= 0) return;
            int i, j;
            Vector2 v = mainCam.ScreenToWorldPoint(Input.mousePosition);
            if (modelType == ModelType.Hexagon)
                v.y /= 0.75f;
            i = (int)v.x;
            j = (int)v.y;
            if (i >= 0 && j >= 0 && i < AreaWidth && j < AreaHeight)
            {
                if (model is Model2DWithAHL)
                {
                    Vector3 modelData = GetDat3(new Vector2Int(i, j));
                    GUI.Label(new Rect(20, Screen.height - 250, 650, 250),
                        $"<size=38>(C = {modelData.x}, S = {modelData.y}, U = {modelData.z}){Environment.NewLine}" +
                         $"</size>");
                }
                else
                {
                    Vector2 modelData = GetDat2(new Vector2Int(i, j));
                    GUI.Label(new Rect(20, Screen.height - 250, 650, 250),
                        $"<size=38>(C = {modelData.x}, S = {modelData.y}){Environment.NewLine}" +
                         $"</size>");
                }


            }            
        }
    

        private void UpdateModel() => model.DoGrowthStep();
    
        private void UpdateView() => view2D.UpdateVisual(model);

        public Vector2 GetDat2(Vector2Int vi)
        {
            Model2D model2 = (Model2D)model;
            if (model2.Substrate2D == null)
                return Vector2.zero;
            if (vi.x < 0 || vi.y < 0 || vi.x >= model2.Substrate2D.GetLength(0) || vi.y >= model2.Substrate2D.GetLength(1))
                return Vector2.zero;
            int ni, nj;
            ni = vi.x / model2.NutrGridSimpl;
            nj = vi.y / model2.NutrGridSimpl;
            return new Vector2(
                (float)(model2.Bacteria2D[vi.x, vi.y]),
                (float)(model2.Substrate2D[ni, nj]));
        }
        public Vector3 GetDat3(Vector2Int vi)
        {
            Model2D model2 = (Model2D)model;
            if (model2.Substrate2D == null)
                return Vector3.zero;
            if (vi.x < 0 || vi.y < 0 || vi.x >= model2.Substrate2D.GetLength(0) || vi.y >= model2.Substrate2D.GetLength(1))
                return Vector3.zero;
            int ni, nj;
            ni = vi.x / model2.NutrGridSimpl;
            nj = vi.y / model2.NutrGridSimpl;
            return new Vector3(
                (float)(model2.Bacteria2D[vi.x, vi.y]),
                (float)(model2.Substrate2D[ni, nj]),
                (float)(model2.Ahl2D[ni, nj]) );
        }
        public Vector4 GetDat4(Vector2Int vi)
        {
            Model2D model2 = (Model2D)model;
            if (model2.Substrate2D == null)
                return Vector4.zero;
            if (vi.x < 0 || vi.y < 0 || vi.x >= model2.Substrate2D.GetLength(0) || vi.y >= model2.Substrate2D.GetLength(1))
                return Vector4.zero;
            int ni, nj;
            ni = vi.x / model2.NutrGridSimpl;
            nj = vi.y / model2.NutrGridSimpl;
            return new Vector4(
                (float)(model2.Bacteria2D[vi.x, vi.y]),
                (float)(model2.Substrate2D[ni, nj]),
                (float)(model2.Ahl2D[ni, nj]),
                (float)(model2.Ahl2D[ni, nj]));
        }

        public void BackToMenu()
        {
            SceneManager.LoadScene(0);
        }

        public void Quit()
        {
            Application.Quit();
        }
    }
}
    