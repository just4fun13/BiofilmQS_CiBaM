using Assets.Scripts.MVVM_CA.Models.ModelParams;
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
using UnityEngine.SceneManagement;
using static CellularAutomaton.BoxCountingMachine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts.MVVM_CA
{
    public class SimExpert3D_new : MonoBehaviour
    {

        private ViewModel viewModel;
        private ProgressUI progressUI;


        [SerializeField] private bool DinamycUpdate = true;
        [SerializeField] private ModelType modelType;
        [SerializeField] private float TimeStep = 0.1f;
        [SerializeField] private View3D view3D;

        [SerializeField] private int RedrawFrequency = 50;
        [SerializeField] private int MaxThreadCount = 1;
        [SerializeField] private bool MultipleSimulations = false;
        [SerializeField][Range(0.0001f, 0.01f)] private float PercentageOfUpdateAmount = 0.001f;



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


        private int MaxIterCount = 144000;
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
            audioMan = FindObjectOfType<AudioMan>();
            progressUI = FindObjectOfType<ProgressUI>();
            StartSimul();
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
            StartCoroutine(StartSimulation());
            await Task.Yield();
        }
        private IEnumerator StartSimulation()
        {
            watch = new Stopwatch();
            programStartTime = NowTime();
            totalSteps = 20; ;// ((nutrEnd - nutrStart) / nutrDelta) * AreaSets.Length * 3;
            nutrStep = 1f;// ((nutrEnd - nutrStart) / nutrDelta);
            yield return new WaitForEndOfFrame();

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
        private void InitView(Model3D model)
        {
            view3D.Init(modelType, model.GetPos, model.BiomassCells3D);
            //view2D.InitWithOffset(ModelType.SimpleSquare, GetPos, AreaWidth, AreaHeight, InitNutrient, view2DsideOffset);
            CameraFocusMan.Focus3DCamera(new Vector3Int(ModelParameters.geometryParameters.AreaWidth,
                                                        ModelParameters.geometryParameters.AreaHeight,
                                                        ModelParameters.geometryParameters.AreaLength ));
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
            int AreaWidth  = ModelParameters.geometryParameters.AreaWidth;
            int AreaHeight = ModelParameters.geometryParameters.AreaHeight;
            int AreaLength = ModelParameters.geometryParameters.AreaLength; 
            modelType = ModelParameters.geometryParameters.gridType;
            Debug.Log($"Grid type is {modelType.ToString()}");
            float InitNutrient = (float)ModelParameters.nutrientParameters.InitialDensity;
            if (ModelParameters.aHLParameters.UseAHL)
                model = new Model3DWithAHL(AreaWidth, AreaHeight, AreaLength, InitNutrient, modelType, TimeStep, MaxThreadCount);
            else
                model = new Model3DnoAHL(AreaWidth, AreaHeight, AreaLength, InitNutrient, modelType, TimeStep, MaxThreadCount);
            ModelParameters.ShowInDebug();
            model.SetDifK(ModelParameters.nutrientParameters.NutrientDiffusion);
            model.Setμmax(ModelParameters.bacteriaParameters.mUmax);
            model.SetKs(ModelParameters.bacteriaParameters.kS);
            model.SetInocCount(ModelParameters.bacteriaParameters.InitialInoculationCount);
            model.SetYxs(ModelParameters.bacteriaParameters.Yxs);
            model.SetConcToDivide(ModelParameters.bacteriaParameters.ConcToDivide);

            //model.ShowModelParamsList();
            model.InitInoculate();

            if (DinamycUpdate)
            {
                InitView((model as Model3D));
                //view3D.InitOneLayerMode(AreaWidth, AreaLength,  modelType, model.GetPos);
            }
            CellsCount = model.CellCount;
            CellCountToRefreshTheDraw = (int)(PercentageOfUpdateAmount * AreaHeight * AreaWidth * AreaLength);
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
        private IEnumerator RunSimul(Model model)
        {

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
         
                if (false && iterationId % (MaxIterCount / 10) == 0)
                {
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    //Vector2 frc = model.GetFractalDimension();
                    //string fracDim = frc.x.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    //DoThePicture(PictureName + $"-FracDim-{fracDim}_NR_{model.AverageNutrientRemain}");
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
                    InitView((model as Model3D));
                Redraw(model);
                audioMan.PlayExplosion();
                // WriteSimulationDynamics(model);
            }
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
        public void BackToMenu()
        {
            SceneManager.LoadScene(0);
        }
    }
}
