using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.Rendering;
using UnityEngine.Windows;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace CellularAutomaton
{

    public class Tris2D : MonoBehaviour
    {

        public enum CellState { empty, busyCanDiv, busyCanNot };


        [SerializeField] private GameObject SquarePrefab;
        [SerializeField] private GameObject HexagonePrefab;

        [SerializeField][Range(0f, 0.5f)] private double initAreaSideOffset = 0.2f;
        [SerializeField] private int AreaWidth = 1000;
        [SerializeField] private int AreaHeight = 1000;
        [SerializeField] private GridType gridType;


        private float MinX = 1000, MaxX = -1000, MinY = 1000, MaxY = -1000;
        private Vector2 MinBounds => new Vector2(MinX, MinY);
        private Vector2 MaxBounds => new Vector2(MaxX, MaxY);
        private Stack<Vector2> areaPoints = new Stack<Vector2>();
        private Stack<Vector2> frontPoints = new Stack<Vector2>();
        private List<Vector2Int> legalPoints = new List<Vector2Int>();

        private float AreaMinX, AreaMaxX;

        private CellState[,] Cells ;
        private double[,] S_Oxygen      ;
        private double[,] S_Nitrogen     ;
        private double[,] S_Carbon     ;
        private double[,] C        ;

        [SerializeField] private double InitOxygen = 0.5;
        [SerializeField] private double InitNitrogen = 0.5;
        [SerializeField] private double InitCarbon = 0.5;
        private double D_Oxygen =  0.333f  ;
        private double D_Carbon = 0.067   ;
        private double D_Nitrogen = 0.0195;
        private double μmax = 0.28;
        private double K_Carbon = 0.00224;
        private double K_Nitrogen = 0.00158;
        private double K_Oxygen = 0.000126;


        private double μNitrogen = 0.32f;
        private double μCarbon =   0.47f;
        private double μOxygen =  2f;

        private GameObject[,] CellsObjs;

        private int CellCount = 0;
        private Dictionary<GridType, Vector2[]> GridBaseDic;
        private Dictionary<GridType, Vector2Int[]> GridNbrsDic;
        private Dictionary<GridType, GameObject> PrefabsDic;

        [SerializeField] private float ConcToDivide = 1.0f;
        [SerializeField] private float LifetimeCost = 0.001f;
        [SerializeField] private Color biofilmCellColor;
        private List<Vector2Int> BiomassCells = new List<Vector2Int>();
        private List<Vector2Int> NewBiomassCells = new List<Vector2Int>();
        long rep = 0;
        [SerializeField] private double K = 0.01;
        private double averageSubstrateOxygen = 0f;
        private double averageSubstrateNitrogen = 0f;
        private double averageSubstrateCarbon = 0f;
        private double averageConsumeOxygen  = 0f;
        private double averageConsumeCarbon = 0f;
        private double averageConsumeNitrogen = 0f;
        private double subsurfaceConsume_O2 = 0;
        private double surfaceConsume_O2 = 0;
        private double initBiomass = 0.3f;
        long iterationId = 0;
        ThreadLocal<System.Random> rng;
        ParallelOptions parallelOptions;
        private int startTimeSecs;

        private int[] surfaceTop;

        private Vector2[] SquareBase =
        {
            new Vector2( 1f, 0f),
            new Vector2( 0f, 1f),
        };
        private Vector2[] HexagonBase =
        {
            new Vector2( 1f,    0f),
            new Vector2( .5f, .75f),
        };
        private static Vector2[] actualBase;
        private Vector2Int[] SquareNbrs =
        {
            new Vector2Int( 0, 1),
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0,-1),
        };
        private Vector2Int[] HexagonNbrs =
        {
            new Vector2Int( 0,-1),
            new Vector2Int( 1,-1),
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int(-1, 1),
        };

        private void Start()
        {
            if (gridType == GridType.Hexagone)
            {
                AreaWidth  = (int) (AreaWidth  * 90f / 68f);
                AreaHeight = (int) (AreaHeight * 90f / 68f);
            }

            startTimeSecs = NowTime();
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 24
            };

            surfaceTop = new int[AreaWidth];
            for (int i = 0; i < AreaWidth; i++)
                surfaceTop[i] = -1;
            Cells    = new CellState[AreaWidth, AreaHeight];
           S_Oxygen      = new double[AreaWidth, AreaHeight];
           S_Nitrogen     = new double[AreaWidth, AreaHeight];
           S_Carbon     = new double[AreaWidth, AreaHeight];
           C        = new double[AreaWidth, AreaHeight];
           CellsObjs= new GameObject[AreaWidth, AreaHeight];

            rng = new ThreadLocal<System.Random>(() => new System.Random());

            GridBaseDic = new Dictionary<GridType, Vector2[]>
            {
                {GridType.Square,   SquareBase  },
                {GridType.Hexagone, HexagonBase },
            };
            GridNbrsDic = new Dictionary<GridType, Vector2Int[]>
            {
                {GridType.Square,   SquareNbrs  },
                {GridType.Hexagone, HexagonNbrs },
            };
            PrefabsDic = new Dictionary<GridType, GameObject>
            {
                {GridType.Square,   SquarePrefab },
                {GridType.Hexagone, HexagonePrefab },
            };

            AreaMinX = Mathf.Max(GetPos(Vector2Int.zero).x, GetPos(new Vector2Int(0, AreaHeight - 1)).x);
            AreaMaxX = Mathf.Min(GetPos(new Vector2Int(AreaWidth - 1, 0)).x, GetPos(new Vector2Int(AreaWidth - 1, AreaHeight - 1)).x);
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                    {
                        Cells[i, j] = CellState.empty;
                        if (IsLegal(new Vector2Int(i, j)))
                            legalPoints.Add(new Vector2Int(i, j));
                    }

            Debug.Log($"X : {AreaMinX} - {AreaMaxX}");
            InitCellsObjs();
            SetInitSubstrate();
            GenerateArea();
            RedrawVisual();
            StartCoroutine(GrowthProcess());
        }

        private void InitCellsObjs()
        {
            CellsObjs = new GameObject[AreaWidth, AreaHeight];
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    if (IsLegal(new Vector2Int(i, j)))
                    CellsObjs[i, j] = Instantiate(PrefabsDic[gridType], GetPos(new Vector2Int(i, j)), Quaternion.identity, transform);
                }
        }

        private void SetInitSubstrate()
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    if (!IsLegal(new Vector2Int(i, j)))
                        continue;
                    S_Nitrogen[i, j] = InitNitrogen;
                    S_Carbon[i, j] = InitCarbon;
                    S_Oxygen[i, j]  = InitOxygen;
                }

        }

        private void GenerateArea()
        {
            int xMin = (int)(initAreaSideOffset * AreaWidth);
            int xMax = (int)((1f - initAreaSideOffset) * AreaWidth);
            int n = 200;
            int d = (xMax - xMin) / n;


            while (n > 0)
            {
                n--;
                int k = Random.Range(0, AreaWidth);
                if (Cells[k, 0] == CellState.empty && IsLegal(new Vector2Int(k, 0)))
                {
                    NewCell(new Vector2Int(k, 0));
                    C[k, 0] = initBiomass; 
                }
            }

            BiomassCells.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
        }

        private void DiffuseSubstrate()
        {
            double[,] sums = new double[AreaWidth, 3];
            averageSubstrateNitrogen = 0;
            averageSubstrateOxygen  = 0;
            averageSubstrateCarbon = 0;
            double[,] sNew = new double[AreaWidth, AreaHeight];
            object diffuseLocker = new object();
            // using this for correct parallel calc
           Parallel.ForEach(legalPoints, parallelOptions, (v) =>
            {
                int i = v.x, j = v.y;
                double newOxygen = S_Oxygen[i, j] + D_Oxygen * AverageSubstrateAroundSum(new Vector2Int(i, j), S_Oxygen);
                //S_Carbon[i, j] = S_Carbon[i, j] + D_Carbon * AverageSubstrateAroundSum(new Vector2Int(i, j), S_Carbon);
                //S_Nitrogen[i, j] = S_Nitrogen[i, j] + D_Nitrogen * AverageSubstrateAroundSum(new Vector2Int(i, j), S_Nitrogen);
                lock (diffuseLocker)
                {
                    sNew[i, j] = newOxygen;
                    sums[i, 1] += newOxygen;
                }
                //sums[i,0] += S_Carbon[i, j];
                //sums[i,2] += S_Nitrogen[i, j];
            }
        );

            for (int i = 0; i < AreaWidth; i++)
            {
                for (int j = 0; j < AreaHeight; j++)
                {
                    S_Oxygen[i, j] = sNew[i, j];
                }
                //averageSubstrateNitrogen += sums[i,0];
                averageSubstrateOxygen  += sums[i,1];
                //averageSubstrateCarbon += sums[i,2];
            }

            //averageSubstrateNitrogen /= legalPoints.Count;
            averageSubstrateOxygen  /=  legalPoints.Count;
            //averageSubstrateCarbon /= legalPoints.Count;
        }

        private double AverageSubstrateAroundSum(Vector2Int pos, double[,] S)
        {
            double av = 0;
            int k = 0;
            foreach (Vector2Int nbr in GridNbrsDic[gridType])
            {
                Vector2Int v = pos + nbr;
                if (IsLegal(v))
                {
                    k++;
                    av += S[v.x, v.y] - S[pos.x, pos.y];
                }
            }
            av = av * 1d / k;
            return av;
        }

        private void RedrawVisual()
        {
            Color col = biofilmCellColor;
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    if (!IsLegal(new Vector2Int(i, j)))
                        continue;
                    if (Cells[i, j] != CellState.empty)
                    {
                        col = biofilmCellColor;
                        col.a = 1f;
//                        col.a = (float)C[i, j];
                    }
                    else
                    {
                        col = new Color(0.3f, 1f, 0.3f);
                        col.a = Mathf.Round((float) S_Oxygen[i, j] * 10) / 10.0f/(float)InitOxygen  ;
                    }

                    CellsObjs[i, j].GetComponent<SpriteRenderer>().color = col;
                }
            rep = 0;
        }

        private void TryBounds(Vector2 v)
        {
            if (v.x < MinX) MinX = v.x;
            if (v.x > MaxX) MaxX = v.x;
            if (v.y < MinY) MinY = v.y;
            if (v.y > MaxY) MaxY = v.y;
        }

        private bool IsLegal(Vector2Int v) => v.x >= 0 && v.y >= 0 && v.x < AreaWidth && v.y < AreaHeight && GetPos(v).x >= AreaMinX && GetPos(v).x <= AreaMaxX;

        private List<Vector2Int>[] CalcNbrsCount(Vector2Int pos)
        {
            List<Vector2Int>[] nbrs = new List<Vector2Int>[3];
            nbrs[0] = new List<Vector2Int>();
            nbrs[1] = new List<Vector2Int>();
            nbrs[2] = new List<Vector2Int>();
            foreach (Vector2Int v in GridNbrsDic[gridType])
            {
                Vector2Int sum = v + pos;
                if (IsLegal(sum) && Cells[sum.x, sum.y] == CellState.empty)
                {
                    Vector2 myPos = GetPos(pos);
                    Vector2 newPos = GetPos(sum);
                    if (myPos.y < newPos.y)
                    {
                        nbrs[0].Add(sum);
                    }
                    else
                    {
                        if (myPos.y == newPos.y)
                            nbrs[1].Add(sum);
                        else
                            nbrs[2].Add(sum);
                    }
                }
            }
            return nbrs;
        }
        private Vector2 GetPos(Vector2Int coord)
        {
            Vector2 v = coord.x * GridBaseDic[gridType][0] + coord.y * GridBaseDic[gridType][1];
            return v;
        }
        private void NewCell(Vector2Int cellIndex)
        {
            if (cellIndex.y > surfaceTop[cellIndex.x])
                surfaceTop[cellIndex.x] = cellIndex.y;
            Vector2 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            NewBiomassCells.Add(cellIndex);
            CellCount++;
            Cells[cellIndex.x, cellIndex.y] = CellState.busyCanDiv;
        }

        private void ConsumeSubstrate(Vector2Int pos)
        {
            int i = pos.x; int j = pos.y;
            double J_Nitrogen = 2;// Math.Min(μNitrogen * S_Nitrogen[i, j]  / (K_Nitrogen + S_Nitrogen[i, j]) * S_Oxygen[i, j] / (K_Oxygen + S_Oxygen[i, j]), S_Nitrogen[i, j]);
            double J_Carbon = 1;  //Math.Min(μCarbon * S_Carbon[i, j]  / (K_Carbon + S_Carbon[i, j]) * S_Oxygen[i, j] / (K_Oxygen + S_Oxygen[i, j]), S_Carbon[i, j]);
            double J_Oxygen =  Math.Min(μOxygen   * S_Oxygen [i, j] / (K_Oxygen +  S_Oxygen[i, j ]),  S_Oxygen[i, j]);
            if (surfaceTop[pos.x] == pos.y) // is on surface top
                surfaceConsume_O2 += J_Oxygen;
            else
                subsurfaceConsume_O2 += J_Oxygen;
            averageConsumeOxygen += J_Oxygen   ;
            averageConsumeCarbon += J_Carbon ;
            averageConsumeNitrogen += J_Nitrogen ;
            //double μ = μmax * S_Nitrogen[i, j] / (K_Nitrogen + S_Nitrogen[i, j]) * μCarbon * S_Carbon[i, j] / (K_Carbon + S_Carbon[i, j]) * S_Oxygen[i, j] / (K_Oxygen + S_Oxygen[i, j]);
            double μ = μmax * J_Oxygen;
            C[i, j] += μ * C[i, j];
            //S_Carbon[i, j] -= J_Carbon;
           // S_Nitrogen[i, j] -= J_Nitrogen;
            S_Oxygen[i, j] -=  J_Oxygen ;
        }

        private void Divide(Vector2Int pos)
        {
            List<Vector2Int> dirs = new List<Vector2Int>();
            foreach (Vector2Int nbr in GridNbrsDic[gridType])
            {
                Vector2Int p = pos + nbr;
                if (IsLegal(p) && Cells[p.x, p.y] == CellState.empty)
                    dirs.Add(p);
            }
            lock (locker)
            {
                if (dirs.Count > 0)
            {
                    Vector2Int dir = dirs[rng.Value.Next(dirs.Count)];
                    C[pos.x, pos.y] /= 2f;// C[pos.x, pos.y] - ConcToDivide;
                    C[dir.x, dir.y] = C[pos.x, pos.y];// ConcToDivide;
                    NewCell(dir);
            }
            else
            {
                    if (pos.y >= AreaHeight - 1)
                        return;
                Cells[pos.x, pos.y] = CellState.busyCanNot;
                                C[pos.x, pos.y] /= 2f;
                                C[pos.x, pos.y + 1] += C[pos.x, pos.y];
                /**/
            }
                }
        }

        object locker = new object();
        [SerializeField] private int difCOunt = 10;

        private IEnumerator GrowthProcess()
        {
            int refFreq = 4;
            //        var watch = new Stopwatch();
            do
            {
                iterationId++;
                NewBiomassCells.Clear();
                //Parallel.ForEach(BiomassCells, (cellPos) =>
                averageConsumeOxygen = 0f;
                averageConsumeNitrogen = 0f;
                averageConsumeCarbon = 0f;
                surfaceConsume_O2 = 0;
                subsurfaceConsume_O2 = 0;
                //foreach (Vector2Int cellPos in BiomassCells)

                // Create a ParallelOptions object with a MaxDegreeOfParallelism of 4
                DiffuseSubstrate();
                DiffuseSubstrate();
                DiffuseSubstrate();
                DiffuseSubstrate();

                foreach (Vector2Int cellPos in BiomassCells)
                {
                    if (Cells[cellPos.x, cellPos.y] == CellState.busyCanDiv)
                        ConsumeSubstrate(cellPos);
                    C[cellPos.x, cellPos.y] -= LifetimeCost;
                    if (C[cellPos.x, cellPos.y] > ConcToDivide)
                        Divide(cellPos);
                }
                averageConsumeCarbon /= BiomassCells.Count;
                averageConsumeOxygen  /= BiomassCells.Count;
                averageConsumeNitrogen /= BiomassCells.Count;
                BiomassCells.AddRange(NewBiomassCells);
                MyLogger.WriteLog($"{iterationId};{averageSubstrateOxygen}");
                yield return new WaitForEndOfFrame();
                if (iterationId % refFreq != 0)
                    continue;
                foreach (Vector2Int vx in NewBiomassCells)
                    CellsObjs[vx.x, vx.y].GetComponent<SpriteRenderer>().color = biofilmCellColor;
                RedrawVisual();
                yield return new WaitForEndOfFrame();
                Screenshot($"{AreaWidth}x{AreaHeight}_id_{iterationId}_G_{μmax / D_Oxygen / InitOxygen}");
                yield return new WaitForEndOfFrame();
            }
            //            while (Math.Min(averageSubstrateOxygen, Math.Min(averageSubstrateNitrogen, averageSubstrateCarbon))  > 0.1f);
            while (averageSubstrateOxygen > 0.1f) ;
            foreach (Vector2Int vx in NewBiomassCells)
                CellsObjs[vx.x, vx.y].GetComponent<SpriteRenderer>().color = biofilmCellColor;
            MyLogger.WriteLog($"{iterationId},{CellCount},");
            RedrawVisual();

            yield return new WaitForEndOfFrame();
            Screenshot($"{AreaWidth}x{AreaHeight}_id_{iterationId}_G_{μmax / D_Oxygen / InitOxygen}");
            yield return new WaitForEndOfFrame();
            PlayTheSound();
            Debug.Log("Process it done");
        }

        private void OnGUI()
        {
            GUI.TextArea(new Rect(0, 0, 600, 150),
                $"O2 = {averageConsumeOxygen.ToString("0.000")} -   {averageSubstrateOxygen.ToString("0.000")}{Environment.NewLine}" +
                $"SURFACE = {surfaceConsume_O2.ToString("0.000")} {Environment.NewLine}" +
                $"SUB = {subsurfaceConsume_O2.ToString("0.000")} {Environment.NewLine}" +
                $"{iterationId}" +
                $"{Environment.NewLine}Time = {NowTime() - startTimeSecs} secs");
        }

        private static string imagesLoc = Environment.CurrentDirectory + $@"\Log\Pictures\";
        private void PlayTheSound()
        {
            FindObjectOfType<AudioSource>().Play();
        }
        private void Screenshot(string name)
        {
            string path = imagesLoc + name + "_" + startTimeSecs + ".jpg";
            ScreenCapture.CaptureScreenshot(path);
        }

        private int NowTime()
        {
            var nowTime = DateTime.Now;
            return 3600 * nowTime.Hour + 60 * nowTime.Minute + nowTime.Second;
        }


    }
}
