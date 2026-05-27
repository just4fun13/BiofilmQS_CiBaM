using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
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

    public class BiofilmGrowthNewAge : MonoBehaviour
    {

        public enum CellState { empty, busyCanDiv, busyCanNot};


        const int MAXSIZE = 10000;

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

        private CellState[,] Cells = new CellState[MAXSIZE, MAXSIZE];
        private double[,] S = new double[MAXSIZE, MAXSIZE];
        private double[,] C = new double[MAXSIZE, MAXSIZE];
        private double[,] P = new double[MAXSIZE, MAXSIZE];
        private GameObject[,] CellsObjs;
        private bool[,] indicArea = new bool[MAXSIZE, MAXSIZE];

        private int CellCount = 0;
        private Dictionary<GridType, Vector2[]> GridBaseDic;
        private Dictionary<GridType, Vector2Int[]> GridNbrsDic;
        private Dictionary<GridType, GameObject> PrefabsDic;

        [SerializeField] private float InitSubstrateCount = 0.5f;
        [SerializeField] private float SubstrateDiffusionParameter = 0.0004f;
        [SerializeField] private float ConcToDivide = 1.0f;
        [SerializeField] private float LifetimeCost = 0.001f;
        [SerializeField] private float DivisionProbability = 0.05f;
        [SerializeField] private Color biofilmCellColor;
        private List<Vector2Int> BiomassCells = new List<Vector2Int>();
        private List<Vector2Int> NewBiomassCells = new List<Vector2Int>();
        long rep = 0;
        [SerializeField] private double μmax = 0.0002f;
        [SerializeField] private double K = 0.01;
        private double averageSubstrate = 0f;
        private double averageConsume = 0f;
        int RedrawTime = 1;
        long iterationId = 0;
        ThreadLocal<System.Random> rng;


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
        private Vector2[] HexBase =
        {
            new Vector2( 1f,    0f),
            new Vector2( 0f, .75f),
        };
        private Vector2Int[] SquareNbrs =
        {
            new Vector2Int( 0, 1),
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0,-1),
        };
        private Vector2Int[] HexagonNbrs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0,-1),
            new Vector2Int( 0, 1),
            new Vector2Int( 1,-1),
            new Vector2Int( 1, 1),
        };
        private Vector2Int[] HexagonNegNbrs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( -1,-1),
            new Vector2Int( -1, 1),
            new Vector2Int( 0,-1),
            new Vector2Int( 0, 1),
        };
        private void Awake()
        {
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
            InitCellsObjs();
            SetInitSubstrate();
            InitArea();
        }

        private void SetInitSubstrate()
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                    S[i, j] = InitSubstrateCount;
        }


        private Vector2Int[] GetNbrs(int y)
        {
            if (gridType != GridType.Hexagone || y % 2 != 0)
                return GridNbrsDic[gridType];
            return HexagonNegNbrs;

        }

        private void DiffuseSubstrate()
        {
             double[] sums = new double[AreaWidth];
            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 24
            };

            Parallel.For(0, AreaWidth, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeight; j++)
                {
                    sums[i] += S[i, j];
                    S[i, j] = (S[i, j] + SubstrateDiffusionParameter * (AverageSubstrateAround(new Vector2Int(i, j)) - S[i, j]));
                }
            });

            foreach (double d in sums)
                averageSubstrate += d;


            averageSubstrate /= AreaWidth * AreaHeight; 
//            Debug.Log($"SubSub = {subSum}");
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
                        col.a = (float)C[i,j];
                        //                        col.a = (float)C[i, j];
                    }
                    else
                    {
                        col = new Color(0.3f, 1f, 0.3f);
                        col.a = Mathf.Round((float)S[i, j] * 10) / 10.0f / (float)InitSubstrateCount;
                    }

                    CellsObjs[i, j].GetComponent<SpriteRenderer>().color = col;
                }
            rep = 0;
        }

        private double AverageSubstrateAround(Vector2Int pos)
        {
            double av = 0;
            int k = 0;
            Vector2Int[] nbrs = GetNbrs(pos.y);
            foreach (Vector2Int nbr in nbrs)
            {
                Vector2Int v = pos + nbr;
                if (IsLegal(v))
                {
                    k++;
                    av += S[v.x, v.y];
                }
            }
            av = av * 1d / k;
            return av;
        }

        private void InitCellsObjs()
        {
            CellsObjs = new GameObject[AreaWidth, AreaHeight];
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    CellsObjs[i, j] = Instantiate(PrefabsDic[gridType], GetPos(new Vector2Int(i, j)), Quaternion.identity, transform);
                }
        }

        private void TryBounds(Vector2 v)
        {
            if (v.x < MinX) MinX = v.x;
            if (v.x > MaxX) MaxX = v.x;
            if (v.y < MinY) MinY = v.y;
            if (v.y > MaxY) MaxY = v.y;
        }



        private void GenerateArea()
        {
            /*            int xMin = (int)(initAreaSideOffset * AreaWidth);
                        int xMax = (int)((1f - initAreaSideOffset) * AreaWidth);
                        int n = 60;  
                        int d = (xMax - xMin) / n;
                        while (n > 0)
                        {
                            n--;
                            int k = Random.Range(0, AreaWidth);
                            if (Cells[k, 0] == CellState.empty)
                            NewCell(new Vector2Int(k, 0));
                        }

                        BiomassCells.AddRange(NewBiomassCells);
                        NewBiomassCells.Clear();
            */
            int n = 5;
            int h = (int) (AreaWidth )/ (n);
            for (int i = h; i < AreaWidth; i+= h)
                NewCell(new Vector2Int(i, 0));
            BiomassCells.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
        }


        public void InitArea()
        {
            GenerateArea();
            StartCoroutine(GrowthProcess());
        }

        private bool IsLegal(Vector2Int v) => v.x >= 0 && v.y >= 0 && v.x < AreaWidth && v.y < AreaHeight;

        private List<Vector2Int>[] CalcNbrsCount(Vector2Int pos)
        {
            List<Vector2Int>[] nbrs = new List<Vector2Int>[3];
            nbrs[0] = new List<Vector2Int>();
            nbrs[1] = new List<Vector2Int>();
            nbrs[2] = new List<Vector2Int>();
            Vector2Int[] CellNbrs = GetNbrs(pos.y); 
            foreach (Vector2Int v in CellNbrs)
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
            if (gridType != GridType.Hexagone)
            {
                Vector2 v = coord.x * GridBaseDic[gridType][0] + coord.y * GridBaseDic[gridType][1];
                return v;
            }
            Vector2 v2 = coord.x * HexBase[0] + coord.y * HexBase[1];
            if (coord.y % 2 != 0)
                v2 += 0.5f * Vector2.right;
            return v2;
        }
        private void NewCell(Vector2Int cellIndex)
        {
            Vector2 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            NewBiomassCells.Add(cellIndex);
            CellCount++;
            Cells[cellIndex.x, cellIndex.y] = CellState.busyCanDiv;
            P[cellIndex.x, cellIndex.y] = 0.3;
        }

        private void ConsumeSubstrate(Vector2Int pos)
        {
            int i = pos.x; int j = pos.y;
            double v = Math.Min(μmax *S[i, j] / (K + S[i, j]), S[i, j]);
            averageConsume += v;
            C[i, j] += v;
            S[i, j] -= v;
        }

        private void Divide(Vector2Int pos)
        {
            List<Vector2Int> dirs = new List<Vector2Int>();
            Vector2Int[] CellNbrs = GetNbrs(pos.y);
            foreach (Vector2Int nbr in CellNbrs)
            {
                Vector2Int p = pos + nbr;
                if (IsLegal(p) && Cells[p.x, p.y] == CellState.empty)
                    dirs.Add(p);
            }
            if (dirs.Count > 0)
            {
                lock (locker)
                {
                    Vector2Int dir = dirs[rng.Value.Next(dirs.Count)];
                    C[pos.x, pos.y] /= 2f;// C[pos.x, pos.y] - ConcToDivide;
                    C[dir.x, dir.y] = C[pos.x, pos.y];// ConcToDivide;
                    NewCell(dir);
                }
            }
            else
            {
                Cells[pos.x, pos.y] = CellState.busyCanNot;
/*                C[pos.x, pos.y] /= 2f;
                C[pos.x, pos.y + 1] += C[pos.x, pos.y];
*/            }
        }

        object locker = new object();
        [SerializeField] private int difCOunt = 10;

        private IEnumerator GrowthProcess()
        {
            int refFreq = 10;
    //        var watch = new Stopwatch();
            do
            {
                iterationId++;
                NewBiomassCells.Clear();
                //Parallel.ForEach(BiomassCells, (cellPos) =>
                averageConsume = 0f;
                //foreach (Vector2Int cellPos in BiomassCells)

                // Create a ParallelOptions object with a MaxDegreeOfParallelism of 4
                ParallelOptions parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 24
                };
                for (int i = 0; i < difCOunt; i++)
                    DiffuseSubstrate();
                Parallel.ForEach(BiomassCells, parallelOptions, cellPos =>
                {
                    if (Cells[cellPos.x, cellPos.y] == CellState.busyCanDiv)
                        ConsumeSubstrate(cellPos);
                    C[cellPos.x, cellPos.y] -= LifetimeCost;
                    if (C[cellPos.x, cellPos.y] > ConcToDivide && rng.Value.Next(100) <= DivisionProbability * 100)
                    Divide(cellPos);
                } 
                );
                averageConsume /= BiomassCells.Count;
                BiomassCells.AddRange(NewBiomassCells);
                if (iterationId % refFreq != 0)
                    continue;
                foreach (Vector2Int vx in NewBiomassCells)
                    CellsObjs[vx.x, vx.y].GetComponent<SpriteRenderer>().color = biofilmCellColor;
                MyLogger.WriteLog($"{iterationId},{CellCount},");
                RedrawVisual();
                yield return new WaitForEndOfFrame();
            }
            while (averageSubstrate > 0.1f);
        }

        long oldIter = 0;
        private void OnGUI()
        {
            GUI.TextArea(new Rect(0, 0, 300, 100), $"{averageSubstrate}{Environment.NewLine}{averageConsume}{Environment.NewLine}{iterationId}{Environment.NewLine}{Time.time}{Environment.NewLine}{(iterationId-oldIter)*1f*Time.deltaTime}");
            oldIter = iterationId;
        }

    }
}
