using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CellularAutomaton
{

    public class Growth3Dcubes : MonoBehaviour
    {


        const int MAXSIZE = 500;
        private const float sqrt2 = 1.4142135623730950488016887242097f;



        [SerializeField] private GameObject CubePrefab;
        [SerializeField] private GameObject TruncatedOctahedronPrefab;

        [SerializeField][Range(0f, 0.5f)] private double initAreaSideOffset = 0.2f;
        [SerializeField] private int AreaWidth = MAXSIZE;
        [SerializeField] private int AreaHeight = MAXSIZE;
        [SerializeField] private int AreaLength = MAXSIZE;


        private float MinX = 1000, MaxX = -1000, MinY = 1000, MaxY = -1000;
        private Vector3 MinBounds => new Vector3(MinX, MinY);
        private Vector3 MaxBounds => new Vector3(MaxX, MaxY);
        private Stack<Vector3> areaPoints = new Stack<Vector3>();
        private Stack<Vector3> frontPoints = new Stack<Vector3>();

        [SerializeField] private GridType gridType;


        private bool[,,] Cells = new bool[MAXSIZE, MAXSIZE, MAXSIZE];
        private double[,,] S = new double[MAXSIZE, MAXSIZE, MAXSIZE];
        private double[,,] C = new double[MAXSIZE, MAXSIZE, MAXSIZE];
        private double[,,] P = new double[MAXSIZE, MAXSIZE, MAXSIZE];
        private bool[,,] indicArea = new bool[MAXSIZE, MAXSIZE, MAXSIZE];

        private int CellCount = 0;
        private Dictionary<GridType, Vector3[]> GridBaseDic;
        private Dictionary<GridType, Vector3Int[]> GridNbrsDic;
        private Dictionary<GridType, GameObject> PrefabsDic;

        [SerializeField] private float InitSubstrateCount = 0.5f;
        [SerializeField] private float SubstrateDiffusionParameter = 0.0004f;
        [SerializeField] private float ConcToDivide = 1.0f;
        [SerializeField] private float LifetimeCost = 0.001f;
        [SerializeField] private float DivisionProbability = 0.05f;
        [SerializeField] private Color biofilmCellColor;
        private List<Vector3Int> BiomassCells = new List<Vector3Int>();
        private List<Vector3Int> NewBiomassCells = new List<Vector3Int>();
        [SerializeField] private double μmax = 0.0002f;
        [SerializeField] private double K = 0.01;
        private double averageSubstrate = 0f;
        private double averageConsume = 0f;

        private void SetInitSubstrate()
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; k < AreaLength; k++)
                        S[i, j, k] = InitSubstrateCount;
        }

        private void DiffuseSubstrate()
        {
            double[] sums = new double[AreaWidth];
            Parallel.For(0, AreaWidth, i =>
            {
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; k < AreaLength; k++)
                    {
                        sums[i] += S[i, j, k];
                        S[i, j, k] = (S[i, j, k] + SubstrateDiffusionParameter * (AverageSubstrateAround(new Vector3Int(i, j, k)) - S[i, j, k]));
                }
            });

            foreach (double d in sums)
                averageSubstrate += d;


            averageSubstrate /= AreaWidth * AreaHeight;

            //            Debug.Log($"SubSub = {subSum}");
        }



        private void Update()
        {
            DiffuseSubstrate();
        }

        private double AverageSubstrateAround(Vector3Int pos)
        {
            double av = 0;
            int k = 0;
            foreach (Vector3Int nbr in GridNbrsDic[gridType])
            {
                Vector3Int v = pos + nbr;
                if (IsLegal(v))
                {
                    k++;
                    av += S[v.x, v.y, v.z];
                }
            }
            av = av * 1d / k;
            return av;
        }

        private void InitCellsObjs()
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; j < AreaLength; k++)
                    {
                        Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(i, j, k)), Quaternion.identity, transform);
                    }
        }

        private void TryBounds(Vector2 v)
        {
            if (v.x < MinX) MinX = v.x;
            if (v.x > MaxX) MaxX = v.x;
            if (v.y < MinY) MinY = v.y;
            if (v.y > MaxY) MaxY = v.y;
        }


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

        private Vector3Int[] TruncOctNbrs =
        {
            new Vector3Int(0 - 1, 0 - 1, 0 + 2),
            new Vector3Int(0 - 1, 0 - 1, 0 + 1),
            new Vector3Int(0 - 1, 0,     0 + 1),
            new Vector3Int(0,     0 - 1, 0 + 1),
            new Vector3Int(0,     0,     0 + 1),
            new Vector3Int(0 + 1, 0,     0    ),
            new Vector3Int(0 - 1, 0,     0    ),
            new Vector3Int(0,     0 + 1, 0    ),
            new Vector3Int(0,     0 - 1, 0    ),
            new Vector3Int(0,     0,     0 - 1),
            new Vector3Int(0,     0 + 1, 0 - 1),
            new Vector3Int(0 + 1, 0,     0 - 1),
            new Vector3Int(0 + 1, 0 + 1, 0 - 1),
            new Vector3Int(0 + 1, 0 + 1, 0 - 2),
        };

        private Vector3Int[] CubeNbrs =
{
            new Vector3Int(  1,  0,  0),
            new Vector3Int(  0,  1,  0),
            new Vector3Int(  0,  0,  1),
            new Vector3Int( -1,  0,  0),
            new Vector3Int(  0, -1,  0),
            new Vector3Int(  0,  0, -1),
        };


        private Vector3[] CubeBase =
        {
            new Vector3( 1f, 0f, 0f),
            new Vector3( 0f, 0f, 1f),
            new Vector3( 0f, 1f, 0f),
        };

        private Vector3[] TruncOctBase =
        {
            new Vector3(sqrt2, 0f,  sqrt2),
            new Vector3(sqrt2, 0f, -sqrt2),
            new Vector3(sqrt2, 1f, 0f),
        };


        private void Awake()
        {
            GridBaseDic = new Dictionary<GridType, Vector3[]>
            {
                {GridType.Cube,     CubeBase    },
                {GridType.TruncatedOctahedron, TruncOctBase }
            };
            GridNbrsDic = new Dictionary<GridType, Vector3Int[]>
            {
                {GridType.Cube,   CubeNbrs  },
                {GridType.TruncatedOctahedron, TruncOctNbrs },
            };
            PrefabsDic = new Dictionary<GridType, GameObject>
            {
                {GridType.Cube,   CubePrefab },
                {GridType.TruncatedOctahedron, TruncatedOctahedronPrefab },
            };
            InitCellsObjs();
            SetInitSubstrate();
            InitArea();
        }

        private void GenerateArea()
        {
            int xMin = (int)(initAreaSideOffset * AreaWidth);
            int xMax = (int)((1f - initAreaSideOffset) * AreaWidth);
            int n = 10;
            int d = (xMax - xMin) / n;
            for (int i = xMin; i < xMax; i++)
                for (int j = xMin; j < xMax; j++)
                {
                    if (i % d == 0 && j % d == 0)
                        NewCell(new Vector3Int(i, j, 0));
                }
            BiomassCells.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
        }


        public void InitArea()
        {
            GenerateArea();
            StartCoroutine(GrowthProcess());
        }

        private bool IsLegal(Vector3Int v) => v.x >= 0 && v.y >= 0 && v.z >= 0 && v.x < AreaWidth && v.y < AreaHeight && v.z < AreaLength;

        private List<Vector3Int>[] CalcNbrsCount(Vector3Int pos)
        {
            List<Vector3Int>[] nbrs = new List<Vector3Int>[3];
            nbrs[0] = new List<Vector3Int>();
            nbrs[1] = new List<Vector3Int>();
            nbrs[2] = new List<Vector3Int>();
            foreach (Vector3Int v in GridNbrsDic[gridType])
            {
                Vector3Int sum = v + pos;
                if (IsLegal(sum) && !Cells[sum.x, sum.y, sum.z])
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
        private Vector3 GetPos(Vector3Int coord)
        {
            Vector3 v = coord.x * GridBaseDic[gridType][0] + coord.y * GridBaseDic[gridType][1] + coord.z * GridBaseDic[gridType][2];
            return v;
        }
        private void NewCell(Vector3Int cellIndex)
        {
            Vector3 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            NewBiomassCells.Add(cellIndex);
            CellCount++;
            Cells[cellIndex.x, cellIndex.y, cellIndex.z] = true;
            P[cellIndex.x, cellIndex.y, cellIndex.z] = 0.3;
            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(cellIndex.x, cellIndex.y, cellIndex.z)), Quaternion.identity, transform);
        }

        private void ConsumeSubstrate(Vector3Int pos)
        {
            int i = pos.x; int j = pos.y; int k = pos.z;
            double v = Math.Min(μmax * S[i, j, k] / (K + S[i, j, k]), S[i, j, k]);
            averageConsume += v;
            C[i, j, k] += v;
            S[i, j, k] -= v;
        }

        private void Divide(Vector3Int pos)
        {
            List<Vector3Int> dirs = new List<Vector3Int>();
            foreach (Vector3Int nbr in GridNbrsDic[gridType])
            {
                Vector3Int p = pos + nbr;
                if (IsLegal(p) && !Cells[p.x, p.y, p.z])
                    dirs.Add(p);
            }
            if (dirs.Count > 0)
            {
                Vector3Int dir = dirs[Random.Range(0, dirs.Count)];
                C[pos.x, pos.y, pos.z] /= 2f;// C[pos.x, pos.y] - ConcToDivide;
                C[dir.x, dir.y, dir.z] = C[pos.x, pos.y, pos.z];// ConcToDivide;
                NewCell(dir);
            }
            else
            {
                /*                C[pos.x, pos.y] /= 2f;
                                C[pos.x, pos.y + 1] += C[pos.x, pos.y];
                */
            }
        }
        int step = 0;
        private IEnumerator GrowthProcess()
        {
            do
            {
                NewBiomassCells.Clear();
                //Parallel.ForEach(BiomassCells, (cellPos) =>
                averageConsume = 0f;
                foreach (Vector3Int cellPos in BiomassCells)
                {
                    ConsumeSubstrate(cellPos);
                    C[cellPos.x, cellPos.y, cellPos.z] -= LifetimeCost;
                    if (C[cellPos.x, cellPos.y, cellPos.z] > ConcToDivide && Random.value <= DivisionProbability)
                        Divide(cellPos);
                }
                averageConsume /= BiomassCells.Count;
                //);
                BiomassCells.AddRange(NewBiomassCells);
                yield return new WaitForEndOfFrame();
                MyLogger.WriteLog($"{step},{CellCount},");
                step++;
            }
            while (averageSubstrate > 0.1f);
        }


        private void OnGUI()
        {
            GUI.TextArea(new Rect(0, 0, 300, 100), $"{averageSubstrate}{Environment.NewLine}{averageConsume}");
        }

    }
}
