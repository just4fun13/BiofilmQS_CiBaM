using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CellularAutomaton
{

    public class Growth3D : MonoBehaviour
    {

        public enum CopyMethod { Instantiate, MeshFilter};

        public enum CellState { empty, busyCanDiv, busyNoDiv};
        

        const int MAXSIZE = 300;
        private const float sqrt2 = 1.4142135623730950488016887242097f;
        long iterationId = 0;

        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private Mesh mesh;
        private MeshFilter meshFilter;


        [SerializeField] private CopyMethod copyMethod = CopyMethod.Instantiate;

        [SerializeField] private GridType gridType;

        [SerializeField] private GameObject CubePrefab;
        [SerializeField] private GameObject TruncatedOctahedronPrefab;

        [SerializeField][Range(0f, 0.5f)] private double initAreaSideOffset = 0.2f;
        [SerializeField] private int AreaWidth = MAXSIZE;
        [SerializeField] private int AreaHeight = MAXSIZE;
        [SerializeField] private int AreaLength = MAXSIZE;


        private float MinX = 1000, MaxX = -1000, MinY = 1000, MaxY = -1000, MinZ = 1000, MaxZ = -1000;
        private Vector3 MinBounds => new Vector3(MinX, MinY, MinZ);
        private Vector3 MaxBounds => new Vector3(MaxX, MaxY, MaxZ);
        private Stack<Vector3> areaPoints = new Stack<Vector3>();
        private Stack<Vector3> frontPoints = new Stack<Vector3>();

        private Queue<GameObject> objectsPool = new Queue<GameObject>();

        private CellState[,,] Cells = new CellState[MAXSIZE, MAXSIZE, MAXSIZE];
        private double[,,] S = new double[MAXSIZE, MAXSIZE, MAXSIZE];
        private double[,,] C = new double[MAXSIZE, MAXSIZE, MAXSIZE];
        private bool[,,] indicArea = new bool[MAXSIZE, MAXSIZE, MAXSIZE];

        private int CellCount = 0;
        private Dictionary<GridType, Vector3[]> GridBaseDic;
        private Dictionary<GridType, Vector3Int[]> GridNbrsDic;
        private Dictionary<GridType, GameObject> PrefabsDic;

        private Dictionary<Vector3Int, GameObject> BacteriaObjDic = new Dictionary<Vector3Int, GameObject>();

        [SerializeField] private float InitSubstrateCount = 0.5f;
        [SerializeField] private float SubstrateDiffusionParameter = 0.0004f;
        [SerializeField] private float ConcToDivide = 1.0f;
        [SerializeField] private float LifetimeCost = 0.001f;
        private List<Vector3Int> BiomassCells = new List<Vector3Int>();
        private List<Vector3Int> NewBiomassCells = new List<Vector3Int>();
        [SerializeField] private double μmax = 0.0002f;
        [SerializeField] private double K = 0.01;
        [SerializeField] private int DiffusionCount = 10;
        private double averageSubstrate = 0f;
        private double averageConsume = 0f;

        ParallelOptions parallelOptions;
        ThreadLocal<System.Random> rng;
        object locker = new object();
        object locker2 = new object();

        private void Awake()
        {
            rng = new ThreadLocal<System.Random>(() => new System.Random());
            mesh = new Mesh();
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 24
            };
            meshFilter = GetComponent<MeshFilter>();
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

            for (int i = 0; i < MAXSIZE; i++)
                for (int j = 0; j < MAXSIZE; j++)
                    for (int k = 0; k < MAXSIZE; k++)
                    {
                        Cells[i, j, k] = CellState.empty;
                    }
                {

                }
            { }

            SetInitSubstrate();
            InoculateBacteriaCells();
            StartCoroutine(GrowthProcess());
        }

        private void DiffuseSubstrate()
        {
            double[] sums = new double[AreaWidth];
            Parallel.For(0, AreaWidth, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; k < AreaLength; k++)
                    {
                        sums[i] += S[i, j, k];
                        S[i, j, k] = S[i, j, k] + SubstrateDiffusionParameter * AverageSubstrateAroundSum(new Vector3Int(i, j, k) ) ;
                    }
            });

            foreach (double d in sums)
                averageSubstrate += d;


            averageSubstrate /= AreaWidth * AreaHeight * AreaLength;

            //            Debug.Log($"SubSub = {subSum}");
        }


        private double AverageSubstrateAroundSum(Vector3Int pos)
        {
            double av = 0;
            int k = 0;
            foreach (Vector3Int nbr in GridNbrsDic[gridType])
            {
                Vector3Int v = pos + nbr;
                if (IsLegal(v))
                {
                    k++;
                    av += S[v.x, v.y, v.z] ;
                }
            }
            av -= S[pos.x, pos.y, pos.z] * k;
            av = av * 1d ;
            return av;
        }

        private void TryBounds(Vector3 v)
        {
            if (v.x < MinX) MinX = v.x;
            if (v.x > MaxX) MaxX = v.x;
            if (v.y < MinY) MinY = v.y;
            if (v.y > MaxY) MaxY = v.y;
            if (v.y < MinZ) MinZ = v.z;
            if (v.y > MaxZ) MaxZ = v.z;
        }

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


        private void SetInitSubstrate()
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; k < AreaLength; k++)
                        S[i, j, k] = InitSubstrateCount;
        }


        private void InoculateBacteriaCells()
        {
            int xMin = (int)(initAreaSideOffset * AreaWidth);
            int xMax = (int)((1f - initAreaSideOffset) * AreaWidth);
            int n = 15;
            int d = (xMax - xMin) / n;
            while (n > 0)
            {
                n--;
                int i = Random.Range(0, AreaWidth);
                int j = Random.Range(0, AreaHeight);
                if (Cells[i, j, 0] == CellState.empty)
                {
                    NewCell(new Vector3Int(i, j, 0), Vector3.zero);
                }
            }
            foreach (Vector3Int cellIndex in NewBiomassCells)
            {
                if (copyMethod == CopyMethod.Instantiate)
                {
                    GameObject newObj = Instantiate(PrefabsDic[gridType], GetPos(cellIndex), Quaternion.identity, transform);
                    BacteriaObjDic.Add(cellIndex, newObj);
                }
                else
                    AddVertAndTris(GetPos(cellIndex));
            }
            if (copyMethod == CopyMethod.MeshFilter)
                RedrawTheMesh();
            Debug.Log($"Init phase done, total cels at the start: {CellCount}");
            BiomassCells.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
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
                if (IsLegal(sum) && Cells[sum.x, sum.y, sum.z] == CellState.empty)
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
        private void NewCell(Vector3Int cellIndex, Vector3 direction)
        {
            Vector3 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            NewBiomassCells.Add(cellIndex);
            CellCount++;
            Cells[cellIndex.x, cellIndex.y, cellIndex.z] = CellState.busyCanDiv;
        }

        private void AddVertAndTris(Vector3 newCellPos)
        {
            int k = vertices.Count;
            Vector3[] possibleNewVert = new Vector3[8];
            possibleNewVert[0] = newCellPos + new Vector3(-0.5f, -0.5f, -0.5f);
            possibleNewVert[1] = newCellPos + new Vector3(-0.5f, -0.5f,  0.5f);
            possibleNewVert[2] = newCellPos + new Vector3( 0.5f, -0.5f,  0.5f);
            possibleNewVert[3] = newCellPos + new Vector3( 0.5f, -0.5f, -0.5f);
            possibleNewVert[4] = newCellPos + new Vector3(-0.5f,  0.5f, -0.5f);
            possibleNewVert[5] = newCellPos + new Vector3(-0.5f,  0.5f,  0.5f);
            possibleNewVert[6] = newCellPos + new Vector3( 0.5f,  0.5f,  0.5f);
            possibleNewVert[7] = newCellPos + new Vector3( 0.5f,  0.5f, -0.5f);
            int[] p = new int[8];
            bool unique = true;
            List<Vector3> newVert = new List<Vector3>();
            string debugV = "", debugT = "";
            for (int i = 0; i < 8; i++)
            {
                if (vertices.Contains(possibleNewVert[i]))
                {
                    p[i] = vertices.IndexOf(possibleNewVert[i]);
                    unique = false;
                }
                else
                {
                    p[i] = k + newVert.Count;
                    newVert.Add(possibleNewVert[i]);
                }
                debugV += $"{p[i]}, ";
            }
            Debug.Log($"Adding new cube at pos {newCellPos} - unique {unique}");

            // Define the triangles of the cube
            int[] newTris = new int[36];
            newTris[0 ] =  p[4];  newTris[ 1] = p[0]; newTris[2] =  p[5];
            newTris[3 ] =  p[5];  newTris[ 4] = p[0]; newTris[5] =  p[1];
            newTris[6 ] =  p[5];  newTris[ 7] = p[1]; newTris[8] =  p[6];
            newTris[9 ] =  p[6];  newTris[10] = p[1]; newTris[11] = p[2];
            newTris[12] =  p[6];  newTris[13] = p[2]; newTris[14] = p[7];
            newTris[15] =  p[7];  newTris[16] = p[2]; newTris[17] = p[3];
            newTris[18] =  p[7];  newTris[19] = p[3]; newTris[20] = p[4];
            newTris[21] =  p[4];  newTris[22] = p[3]; newTris[23] = p[0];
            newTris[24] =  p[7];  newTris[25] = p[4]; newTris[26] = p[6];
            newTris[27] =  p[6];  newTris[28] = p[4]; newTris[29] = p[5];
            newTris[30] =  p[0];  newTris[31] = p[3]; newTris[32] = p[1];
            newTris[33] =  p[1];  newTris[34] = p[3]; newTris[35] = p[2];

            for (int i = 0; i < 36; i+= 3)
            debugT += $"({newTris[i]},{newTris[i+1]},{newTris[i+2]} ";

            vertices.AddRange(newVert);
            triangles.AddRange(newTris);
            Debug.Log(debugT);
            Debug.Log(debugV);
        }

        private void ConsumeSubstrate(Vector3Int pos)
        {
            int i = pos.x; int j = pos.y; int k = pos.z;
            double v = Math.Min(μmax * S[i, j, k] / (K + S[i, j, k]), S[i, j, k]);
            averageConsume += v;
            C[i, j, k] += v;
            S[i, j, k] -= v;
        }

        private void Divide(Vector3Int index)
        {
            List<Vector3Int> dirs = new List<Vector3Int>();
            bool hasBorderNbrs = false;            
            foreach (Vector3Int nbr in GridNbrsDic[gridType])
            {
                Vector3Int p = index + nbr;
                if (IsLegal(p))
                {
                    if (Cells[p.x, p.y, p.z] == CellState.empty)
                        dirs.Add(p);
                }
                else
                    hasBorderNbrs= true;
            }
            if (dirs.Count > 0)
            {
                lock (locker)
                {
                    Vector3Int dir = dirs[rng.Value.Next(dirs.Count)];
                    C[index.x, index.y, index.z] /= 2f;// C[pos.x, pos.y] - ConcToDivide;
                    C[dir.x, dir.y, dir.z] = C[index.x, index.y, index.z];// ConcToDivide;
                    NewCell(dir, dir-index);
                }
            }
            else
            {
                if (hasBorderNbrs)
                    return;
                lock (locker2)
                {
                    Cells[index.x, index.y, index.z] = CellState.busyNoDiv;
                    if (BacteriaObjDic.ContainsKey(index))
                    {
                        objectsPool.Enqueue(BacteriaObjDic[index]);
                        BacteriaObjDic.Remove(index);
                    }
                }

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
                iterationId++;
                for (int i = 0; i < DiffusionCount; i++)
                    DiffuseSubstrate();

                NewBiomassCells.Clear();
                averageConsume = 0f;
                Parallel.ForEach(BiomassCells, parallelOptions, (cellPos) =>
                //foreach (Vector3Int cellPos in BiomassCells)
                {
                    //if (Cells[cellPos.x, cellPos.y, cellPos.z] == CellState.busyCanDiv)
                        ConsumeSubstrate(cellPos);
                    C[cellPos.x, cellPos.y, cellPos.z] -= LifetimeCost;
                    if (C[cellPos.x, cellPos.y, cellPos.z] > ConcToDivide )
                        Divide(cellPos);
                }
                );
                averageConsume /= BiomassCells.Count;
                BiomassCells.AddRange(NewBiomassCells);
                MyLogger.WriteLog($"{step},{CellCount},");

                if (copyMethod == CopyMethod.Instantiate)
                {
                    foreach (Vector3Int cellIndex in NewBiomassCells)
                    {
                            GameObject newObj;
                            if (objectsPool.TryDequeue(out newObj) )
                            {
                                 if (newObj != null && newObj.transform != null)
                                    newObj.transform.position = GetPos(cellIndex);
                            }
                            else
                            {
                                newObj = Instantiate(PrefabsDic[gridType], GetPos(cellIndex), Quaternion.identity, transform);
                            }
                            if (!BacteriaObjDic.ContainsKey(cellIndex))
                                BacteriaObjDic.Add(cellIndex, newObj);
                    }
                }
                else
                {
                    foreach (Vector3Int cellIndex in NewBiomassCells)
                        AddVertAndTris(GetPos(cellIndex));
                    RedrawTheMesh();
                }
                yield return new WaitForEndOfFrame();
            }
            while (averageSubstrate > 0.1f);
        }


        private void OnGUI()
        {
            GUI.TextArea(new Rect(0, 0, 300, 100), $"{averageSubstrate}{Environment.NewLine}{averageConsume}");
        }

        private void RedrawTheMesh()
        {
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            meshFilter.mesh = mesh;
            mesh.RecalculateNormals();
        }

    }
}
