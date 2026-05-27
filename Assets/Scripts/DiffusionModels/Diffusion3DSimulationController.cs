using Assets.Scripts.MVVM_CA;
using CellularAutomaton;
using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.NewGeneration
{
    public class Diffusion3DSimulationController : MonoBehaviour
    {

        [SerializeField] private View3D viewConcentr;
        [SerializeField] private View3D deltaView;
        [SerializeField] private Base3D base3D;


        [SerializeField] private Base3D errorBase;
        private int AreaWidthCells = 100;
        private int AreaHeightCells = 100;
        private int AreaLengthCells = 100;
        [SerializeField] public int Nmin = 30;

        [SerializeField] public int Nmax = 30;

        [SerializeField] public int N = 30;
        [SerializeField] public TMP_Text iterText;
        [SerializeField] public TMP_Text modelInfoText;
        [SerializeField] private double maxTime = 10;
        [SerializeField] private float AreaWidth;
        [SerializeField] private float AreaHeight;
        [SerializeField] private float AreaLength;
        [SerializeField] private float timeScaleK = 1;
        [SerializeField] private bool WriteLog = false;
        [SerializeField] private bool DynamicUpdate = false;
        [SerializeField] private bool DrawPictureAtTheEnd = false;
        //        private string comsolUloc = Environment.CurrentDirectory + $@"\Data\ComsolData.csv";
        //        private string comsolUloc = Environment.CurrentDirectory + $@"\Data\ComsolDataNew.csv";

        private string comsolUloc = Environment.CurrentDirectory + $@"\Data\SetTopEveryFrame_GeomExtraFine_TimeStep_0_0_5.csv";
        
        private Dictionary<Vector3Int, double[]> U = new Dictionary<Vector3Int, double[]>();
//        private Dictionary<Vector3Int, Vector3Int> V3Int_model_To_V3Int_DeltaError = new Dictionary<Vector3Int, Vector3Int>();

       // private Dictionary<Vector3, Vector3Int> ComsolPos_To_C3N_Id = new Dictionary<Vector3, Vector3Int>();
        System.Diagnostics.Stopwatch SimTimeTimer = new System.Diagnostics.Stopwatch();
        int maxIterCount = 0;
        private double deltaT, simTime;
        long iter = 0;
        int iterStep = 1;
        int drawStep = 100;
        DiffusionModel3D model;
        AudioMan audioMan;
        double timeStepForDelta = 0.1d;

        bool busy = false;  


        private async void Awake()
        {
            if (Nmax == Nmin)
            {
                PrepareModelAndGo();
                ReadDataComsol();
                StartCoroutine(RunSimul());
            }
            else
                StartCoroutine(DoManyIterations());
        }
        IEnumerator DoManyIterations()
        {
            for (N = Nmin; N <= Nmax; N++)
            {
                PrepareModelAndGo();
                ReadDataComsol();
                StartCoroutine(RunSimul());
                while (busy)
                    yield return new WaitForSeconds(1);
                model = null;
                yield return new WaitForSeconds(1);
            }
        }
        private void PrepareModelAndGo()
        {
            busy = true;
            simTime = 0;
            iter = 0;
            audioMan = GameObject.FindObjectOfType<AudioMan>();
            //model = new OriginModel(AreaW, AreaH, base2D, MaxTCount, MaxDCount, DivProbability, _μ);
            //model = new DiffusionModel(AreaW, AreaH, base2D);
            model = new DiffusionModel3D(AreaWidth, AreaHeight, AreaLength, base3D, N);
            AreaWidthCells = model.AreaWidthCells;
            AreaHeightCells = model.AreaHeightCells;
            AreaLengthCells = model.AreaLengthCells;
            //            Focus2DCamera();
            if (DynamicUpdate)
            {
                viewConcentr.InitN(AreaWidthCells, AreaHeightCells, AreaLengthCells, base3D);
                viewConcentr.SetScale((float)(model.scale), isTruncOct: base3D.name == "TruncOct");
                errorBase.SetScale(0.1f);
                deltaView.InitN(10, 10, 10, errorBase);
                deltaView.SetScale(0.1f);
                deltaView.SetPosition(Vector3.right * 50);
            }

            modelInfoText.text = $"Тип: {base3D.name}{Environment.NewLine}N = {N}{Environment.NewLine}";

            deltaT = model.deltaTime;
            maxIterCount = (int)(maxTime / deltaT);
            //iterStep = 40;// maxIterCount / 5; 
            if (maxIterCount < 10000)
                drawStep = 1;
            else
                drawStep = maxIterCount / 10000;
            model.SetMaxIter(maxIterCount);
            model.SetTimeScaleK(timeScaleK);
            //loadProgress.ProgressChanged +=  ShowLoadProg;
            //model.CorrectModelInitValues(U);
        }
        private void ReadDataComsol()
        {
            U.Clear();
            // reading U
            using (StreamReader streamReader = new StreamReader(comsolUloc))
            {
                string line = streamReader.ReadLine();
                int j = 0;
                int tLen = 0;
                Dictionary<Vector3Int, int> closePointsCounter = new Dictionary<Vector3Int, int>();
                while (line != null)
                {
                    string[] parts = line.Split(';');
                    float x = float.Parse(parts[0], CultureInfo.InvariantCulture) ;
                    float y = float.Parse(parts[1], CultureInfo.InvariantCulture) ;
                    float z = float.Parse(parts[2], CultureInfo.InvariantCulture) ;
                    Vector3 comsolPos = new Vector3(x, y, z);
                    Vector3Int vi = base3D.GetIdOfPoint(new Vector3(x, y, z));
                    tLen = parts.Length - 3;
                    timeStepForDelta = maxTime * 1d / (tLen - 1)  ;
                    Vector3Int viPos = new Vector3Int(
                        (int)Math.Round((x - 0.05) / 0.1),
                        (int)Math.Round((y - 0.05) / 0.1),
                        (int)Math.Round((z - 0.05) / 0.1));
                    //V3Int_model_To_V3Int_DeltaError.Add(vi, viPos);
                    //ComsolPos_To_C3N_Id.Add(comsolPos, vi);
                    try
                    {
                        if (!U.ContainsKey(vi))
                        {
                            double[] newArr = new double[tLen];
                            for (int i = 0; i < tLen; i++)
                                newArr[i] += float.Parse(parts[i + 3], CultureInfo.InvariantCulture);
                            U.Add(vi, newArr);
                            closePointsCounter.Add(vi, 1);
                        }
                        else
                        {
                            Debug.LogError($"Doubling closest points for id {vi}");
                            for (int i = 0; i < tLen; i++)
                            {
                                U[vi][i] += float.Parse(parts[i + 3]);
                                //U[vi][i] /= 2;
                            }
                            closePointsCounter[vi]++;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"[{parts[0]},{parts[1]},{parts[2]}]->[{x},{y},{z}]-> - {e.Message}");
                    }
                    j++;
                    line = streamReader.ReadLine();
                }
                foreach (var pair in closePointsCounter)
                    if (pair.Value != 1)
                        for (int i = 0; i < tLen; i++)
                            U[pair.Key][i] = U[pair.Key][i]*1f / closePointsCounter[pair.Key];
               // Debug.Log($"Finished read comsol data with timeStepDelta = {timeStepForDelta}");
            }
        }
        private void CalcDelta(bool showInLog, bool showInDebug)
        {
            double maxD = -1000;
            double avDelta = 0;
            double minD = 1000;
            int t = (int)Math.Round(simTime / timeStepForDelta);
            double[,,] deltaAll = new double[10, 10, 10];
            foreach (var pair in U)
            {
                Vector3Int vi = pair.Key;
                double delta = Math.Abs(model.Cn[vi.x, vi.y, vi.z] - pair.Value[t]);
                //Vector3Int deltaId = V3Int_model_To_V3Int_DeltaError[vi];
                //deltaAll[deltaId.x, deltaId.y, deltaId.z] = 100*delta;
                //double delta = Math.Abs(model.Cn[vi.x, vi.y, vi.z] - pair.Value[t]);
                if (delta > maxD) maxD = delta;
                if (delta < minD) minD = delta;
                avDelta += delta;
            }
            if (DynamicUpdate)
                deltaView.DrawDiffusion(deltaAll);
            avDelta = avDelta / U.Count;
            if (showInDebug)
                Debug.Log($"Calc Delta SimTime = {simTime}  minDelta = {minD}, maxDelta = {maxD}, averageDelta = {avDelta}");


            //            (int)Math.Round((z - 0.05) / 0.1));
            if (showInLog)
            {

                MyLogger.WriteLog($"{base3D.name}\t{N}\t{simTime.ToString("#0.00")}\t{maxD}\t{avDelta}\t{SimTimeTimer.ElapsedMilliseconds}");
/*                for (double x = 0.05; x <= 0.95; x += 0.1)
                    for (double y = 0.05; y <= 0.95; y += 0.1)
                        for (double z = 0.05; z <= 0.95; z += 0.1)
                        {
                            Vector3 vpos = new Vector3((float)x, (float)y, (float)z);
                            Vector3Int vix = base3D.GetIdOfPoint(vpos);
                            MyLogger.WriteLog($"{base3D.name}\t{N}\t{simTime.ToString("#0.00")}\t{maxD}\t{avDelta}\t{Timer.ElapsedMilliseconds}" +
                                        $"\t{x.ToString("0.00")}\t{(y).ToString("0.00")}\t{(z).ToString("0.00")}\t" +
                                        $"{model.Cn[vix.x, vix.y, vix.z]}\t{U[vix][t]}");
                        }
*/            }
        }
        
        IEnumerator RunSimul()
        {
            //CalcDeltas(showInLog : true, showInDebug: false);
            yield return new WaitForEndOfFrame();
            SimTimeTimer.Reset();
            SimTimeTimer.Start();
            if ((simTime % timeStepForDelta) < deltaT)
                CalcDelta(showInLog: WriteLog, showInDebug: true);

            while (simTime <= maxTime )
            {

                model.DoGrowthStep();


                if (iter % (maxIterCount / 100) == 0) //(iter % (10) == 0)
                {
                    //view.DrawDiffusion(model.Cn);
                    if (DynamicUpdate && iter % (maxIterCount / 100 ) == 0)
                    {
                        //CalcDeltas(true, false);
                        viewConcentr.DrawDiffusion(model.Cn);
                    }
                    ShowIterTime();    
                    yield return new WaitForEndOfFrame();
                }
                simTime += deltaT;
                iter++;
                //if ((simTime % timeStepForDelta) < deltaT)
                //    CalcDelta(showInLog: WriteLog, showInDebug: true);


            }
            //CalcDeltas(true, true);
            ShowIterTime();
            if (DrawPictureAtTheEnd)
            {
                viewConcentr.DrawDiffusion(model.Cn);
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                MyLogger.Screenshot($"{base3D.name}_{N}_2");
            }
            yield return new WaitForEndOfFrame();
            audioMan.PlayExplosion();
            Debug.Log($"Simul is done = {model.BotCn}");
            busy = false;
        }
        private void ShowIterTime()
        {
            iterText.text = $"Итерация:{iter}{Environment.NewLine}Время:{simTime.ToString("00.00")}";
        }       
    }
}
