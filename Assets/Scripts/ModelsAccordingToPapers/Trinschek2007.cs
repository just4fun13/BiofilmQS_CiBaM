using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;

namespace CellularAutomaton
{

    public class Trinschek2007 : MonoBehaviour
    {

        public enum CopyMethod { Instantiate, MeshFilter, None };
        public enum CellState { empty, busyCanDiv, busyNoDiv };
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private Mesh mesh;
        private MeshFilter meshFilter;
        private long iterationId = 0;

        [SerializeField] private bool ClearLog = false;
        [SerializeField] private bool DrawResult = false;
        [SerializeField] private bool NeedPrint = false;
        [SerializeField] private bool ShowGizmo = true;


        [SerializeField] private CopyMethod copyMethod = CopyMethod.Instantiate;
        [SerializeField] private GridType gridType;
        [SerializeField] private GameObject CubePrefab;
        [SerializeField] private GameObject ExtendCubePrefab;
        [SerializeField] private GameObject TruncatedOctahedronPrefab;
        [SerializeField] private int AreaWidth  ;
        [SerializeField] private int AreaHeight ;
        [SerializeField] private int AreaLength ;

        private int[,] surfaceTop;
        private float MinX = 1000, MaxX = -1000, MinY = 1000, MaxY = -1000, MinZ = 1000, MaxZ = -1000;
        private float AreaMinX, AreaMaxX, AreaMinY, AreaMaxY;
        
        private Vector3 MinBounds => new Vector3(MinX, MinY, MinZ);
        private Vector3 MaxBounds => new Vector3(MaxX, MaxY, MaxZ);

        private Queue<GameObject> objectsPool = new Queue<GameObject>();

        private int CellCount = 0;
        private Dictionary<GridType, Vector3[]> GridBaseDic;
        private Dictionary<GridType, Vector3Int[]> GridNbrsDic;
        private Dictionary<GridType, GameObject> PrefabsDic;

        private Dictionary<Vector3Int, GameObject> BacteriaObjDic = new Dictionary<Vector3Int, GameObject>();

        [SerializeField] private double ConcToDivide = 1.0f;
        [SerializeField] private double ConcToShare = 0.7f;
        [SerializeField] private double LifetimeCost = 0.001f;
        private List<Vector3Int> BiomassCells = new List<Vector3Int>();
        private List<Vector3Int> NewBiomassCells = new List<Vector3Int>();
        private List<Vector3Int> CellsToRedraw = new List<Vector3Int>();
        [SerializeField] private double InitOxygen = 0.5;
        [SerializeField] private double InitCarbon = 0.5;
        [SerializeField] private double InitNitrogen = 0.5;
        [SerializeField] private double FinishThreshold = 0.1;

        private double K_Oxygen =   0.01;
        private double K_Carbon =   0.01;
        private double K_Nitrogen = 0.01;

        private double D_Oxygen =   0.22;
        private double D_Carbon =   0.67;
        private double D_Nitrogen = 0.195;


        private double μOxygen = 0.40;
        private double μCarbon = 0.47;
        private double μNitrogen = 0.32;

        private double μmax = 0.194;
        private int    DiffusionCount = 10;
        private double averageSubstrateOxygen = 0f;
        private double oxygenSurfaceConsume = 0f;
        private double averageSubstrateNitrogen = 0f;
        private double averageSubstrateCarbon = 0f;
        private double averageConsumeOxygen = 0f;
        private double averageConsumeNitrogen = 0f;
        private double averageConsumeCarbon = 0f;
        private double initBiomass = 0.3f;

        ParallelOptions parallelOptions;
        ThreadLocal<System.Random> rng;
        object locker = new object();
        object locker2 = new object();

        private List<Vector3Int> legalPoints = new List<Vector3Int>();
        private Vector3Int[] TruncOctNbrs =
        {
            new Vector3Int(-1, 2,-1),
            new Vector3Int( 1,-2, 1),
            new Vector3Int( 0, 1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(-1, 1,-1),
            new Vector3Int( 0, 1,-1),
            new Vector3Int( 1, 0, 0),
            new Vector3Int( 0, 0, 1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 0,-1),
            new Vector3Int( 1,-1, 1),
            new Vector3Int( 0,-1, 1),
            new Vector3Int( 0,-1, 0),
            new Vector3Int( 1,-1, 0),
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
        private Vector3Int[] CubeExtendNbrs =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),

            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(-1, 0, -1),
            new Vector3Int(0, 1, 1),
            new Vector3Int(0, -1, -1),

            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, 0, 1),
            new Vector3Int(1, 0, -1),
            new Vector3Int(0, -1, 1),
            new Vector3Int(0, 1, -1),
        };
        private Vector3[] CubeBase =
        {
            new Vector3( 1f, 0f, 0f),
            new Vector3( 0f, 1f, 0f),
            new Vector3( 0f, 0f, 1f),
        };
        private Vector3[] TruncOctBase =
        {
            new Vector3(2f, 0f,  0f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 0, 2f),
        };
        private Vector3[] CubeExtendBase =
        {
            new Vector3( 1f, 0f, 0f),
            new Vector3( 0f, 1f, 0f),
            new Vector3( 0f, 0f, 1f),
        };


        private CellState[,,] Cells;
        private double[,,] SOxygen     ;
        private double[,,] SCarbon    ;
        private double[,,] SNitrogen    ;
        private double[,,] C;
        private int startTimeSecs;

        private void Start()
        {

            if (ClearLog)
                MyLogger.ClearLog();
            surfaceTop = new int[AreaWidth, AreaLength];
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0;j < AreaLength; j++)
                    surfaceTop[i,j] = -1;

             Cells = new CellState[AreaWidth, AreaHeight, AreaLength];
               SOxygen =    new double[AreaWidth, AreaHeight, AreaLength];
              SCarbon =    new double[AreaWidth, AreaHeight, AreaLength];
              SNitrogen =    new double[AreaWidth, AreaHeight, AreaLength];
                 C =    new double[AreaWidth, AreaHeight, AreaLength];

            Vector3 initVector = SimControl.Instance.GetVals();  // (C, N, O);
            InitCarbon = initVector.x;
            InitNitrogen = initVector.y;
            InitOxygen = initVector.z;

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
                {GridType.ExtendCube, CubeExtendBase  },
                {GridType.TruncatedOctahedron, TruncOctBase }
            };
            GridNbrsDic = new Dictionary<GridType, Vector3Int[]>
            {
                {GridType.Cube,   CubeNbrs  },
                {GridType.ExtendCube,   CubeExtendNbrs  },
                {GridType.TruncatedOctahedron, TruncOctNbrs },
            };
            PrefabsDic = new Dictionary<GridType, GameObject>
            {
                {GridType.Cube,   CubePrefab },
                {GridType.ExtendCube,   ExtendCubePrefab },
                {GridType.TruncatedOctahedron, TruncatedOctahedronPrefab },
            };

            AreaMinX = MaxF(
                GetPos(Vector3Int.zero).x, 
                GetPos(new Vector3Int(0, AreaHeight - 1, AreaLength - 1)).x,
                GetPos(new Vector3Int(0, 0, AreaLength - 1)).x, 
                GetPos(new Vector3Int(0, AreaHeight - 1, 0)).x );
            AreaMaxX = MinF(
                GetPos(new Vector3Int(AreaWidth-1, AreaHeight - 1, AreaLength - 1)).x, 
                GetPos(new Vector3Int(AreaWidth - 1, 0, 0)).x,
                GetPos(new Vector3Int(AreaWidth - 1, 0, AreaLength - 1)).x, 
                GetPos(new Vector3Int(AreaWidth - 1, AreaHeight - 1, 0)).x);

            AreaMinY = AreaMinX;
            /*                MaxF(GetPos(Vector3Int.zero).z,
                                        GetPos(new Vector3Int(0,             0, AreaLength - 1)).z,
                                        GetPos(new Vector3Int(AreaWidth - 1, 0, AreaLength - 1)).z,
                                        GetPos(new Vector3Int(AreaWidth - 1, 0, 0)             ).z);
            */
            AreaMaxY = AreaMaxX;
/*                MinF(
                GetPos(new Vector3Int(AreaWidth - 1, AreaHeight - 1, AreaLength - 1)).z,
                GetPos(new Vector3Int(            0, AreaHeight - 1, AreaLength - 1)).z,
                GetPos(new Vector3Int(AreaWidth - 1, AreaHeight - 1,              0)).z,
                GetPos(new Vector3Int(            0, AreaHeight - 1,              0)).z);
*/
//            Debug.Log($"X:{AreaMinX} - {AreaMaxX}, Y:{AreaMinY} - {AreaMaxY}");

            //TestDraw();

            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; k < AreaLength; k++)
                    {
                        Cells[i, j, k] = CellState.empty;
                        if (IsLegal(new Vector3Int(i, j, k)))
                            legalPoints.Add(new Vector3Int(i, j, k));
                    }
            SetInitSubstrate();
//            Logger.WriteLog($"Init:C={InitCarbon},N={InitNitrogen},O={InitOxygen}");
//            Logger.WriteLog($"Size:{AreaWidth} x {AreaHeight} x {AreaLength}");
            InoculateBacteriaCells();
//            StartCoroutine(PrintResultAndClear());
            StartCoroutine(GrowthProcess());
            startTimeSecs = NowTime();
        }
        private int NowTime()
        {
            var nowTime = DateTime.Now;
            return 3600 * nowTime.Hour + 60 * nowTime.Minute + nowTime.Second;
        }
        private void TestDraw()
        {


            Debug.Log($"{GetPos(new Vector3Int(0, 0, 0))}," +
                      $"{GetPos(new Vector3Int(AreaWidth - 1, 0, 0))}," +
                      $"{GetPos(new Vector3Int(0, AreaHeight - 1, 0))}," +
                      $"{GetPos(new Vector3Int(AreaWidth - 1, AreaHeight - 1, 0))}," +
                      $"{GetPos(new Vector3Int(0, 0, AreaLength - 1))}," +
                      $"{GetPos(new Vector3Int(0, AreaHeight - 1, AreaLength - 1))}," +
                      $"{GetPos(new Vector3Int(AreaWidth - 1, 0, AreaLength - 1))}," +
                      $"{GetPos(new Vector3Int(AreaWidth - 1, AreaHeight - 1, AreaLength - 1))},");

            /*            for (int i = 0; i < AreaWidth; i++) 
                        for (int j = 0; j < AreaHeight; j++)
                        {
                                Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(i, j, 0)), Quaternion.identity, transform);
                                Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(i, j, AreaWidth)), Quaternion.identity, transform);
                                Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(0, i, j)), Quaternion.identity, transform);
                                Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(AreaWidth, i, j)), Quaternion.identity, transform);
                                Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(i, 0, j)), Quaternion.identity, transform);
                                Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(i, AreaWidth, j)), Quaternion.identity, transform);
                            }
            */

            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(0, 0, 0)), Quaternion.identity, transform);
            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(0, 0, AreaLength - 1)), Quaternion.identity, transform);
            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(0, AreaHeight - 1, 0)), Quaternion.identity, transform);
            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(0, AreaHeight - 1, AreaLength - 1)), Quaternion.identity, transform);
            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(AreaWidth - 1, 0, 0)), Quaternion.identity, transform);
            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(AreaWidth - 1, 0, AreaLength - 1)), Quaternion.identity, transform);
            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(AreaWidth - 1, AreaHeight - 1, 0)), Quaternion.identity, transform);
            Instantiate(PrefabsDic[gridType], GetPos(new Vector3Int(AreaWidth - 1, AreaHeight - 1, AreaLength - 1)), Quaternion.identity, transform);


        }
        private float MaxF(float x, float y, float z, float w)
        {
            return Mathf.Max(Mathf.Max(x,y), Mathf.Max(z,w));
        }
        private float MinF(float x, float y,float z, float w)
        {
            return Mathf.Min(Mathf.Min(x, y), Mathf.Min(z,w));
        }
        private void SetInitSubstrate()
        {
            int legCount = 0;
            foreach (Vector3Int v in legalPoints)
            {
                int i = v.x;
                int j = v.y;
                int k = v.z;
                        legCount++;
                        SCarbon[i, j, k] = InitCarbon;
                        SNitrogen[i, j, k] = InitNitrogen;
                        SOxygen [i, j, k] = InitOxygen;
            }
//            Debug.Log($"total legal count : {legCount}");
        }
        private void InoculateBacteriaCells()
        {
            int N = 100;

            int n = (int) Mathf.Sqrt(N) + 1;
            int h = (int) (AreaMaxX - AreaMinX)/ n / 2;

            int x0 = (int) AreaMinX / 2  ;

            while (N > 0) 
            {
                int i = (int)UnityEngine.Random.Range(x0, AreaMaxX - x0);
                int j = (int)UnityEngine.Random.Range(x0, AreaMaxX - x0);
                Vector3Int indexOfNew = new Vector3Int(i, 0, j);//(x0 + i * h, 0, x0 + j * h);
                if (!IsLegal(indexOfNew) || Cells[i, 0, j] != CellState.empty)
                    continue;
                N--;
               

                C[indexOfNew.x, 0, indexOfNew.z] = initBiomass;
                NewCell(indexOfNew);
            }

            /*            for (int i = 1; i < n; i++)
                            for (int j = 1; j < n; j++)
                            {
                                Vector3Int indexOfNew = new Vector3Int(x0 + i * h, 0, x0 + j * h);
                                C[indexOfNew.x, 0, indexOfNew.z] = initBiomass;
                                NewCell(indexOfNew);

                            }
            */
            if (copyMethod != CopyMethod.None)
            foreach (Vector3Int cellIndex in NewBiomassCells)
            {

                if (copyMethod == CopyMethod.Instantiate)
                {
                    GameObject newObj = Instantiate(PrefabsDic[gridType], GetPos(cellIndex), Quaternion.identity, transform);
                    BacteriaObjDic.Add(cellIndex, newObj);
                        newObj.GetComponent<MeshRenderer>().material.color = Color.green;
                    }
                    else
                    AddVertAndTris(GetPos(cellIndex));
            }
            if (copyMethod == CopyMethod.MeshFilter)
                RedrawTheMesh();
            //Debug.Log($"Init phase done, total cels at the start: {CellCount}");
            BiomassCells.AddRange(NewBiomassCells);
            NewBiomassCells.Clear();
        }
        private void DiffuseSubstrate()
        {
            double[,] sums = new double[AreaWidth, 3];
            averageSubstrateNitrogen = 0;
            averageSubstrateOxygen = 0;
            averageSubstrateCarbon = 0;
            double[,,,] sNew = new double[AreaWidth, AreaHeight, AreaLength, 3];
            object diffuseLocker = new object();

            Parallel.ForEach(legalPoints, parallelOptions, (v) =>
            //foreach (Vector3Int v in legalPoints)
            {
                int i = v.x, j = v.y, k = v.z;
                double newOx = SOxygen[i, j, k] + D_Oxygen * AverageSubstrateAroundSum(v, SOxygen);
                double newCar = SCarbon[i, j, k] + D_Carbon * AverageSubstrateAroundSum(v, SCarbon);
                double newNit = SNitrogen[i, j, k] + D_Nitrogen * AverageSubstrateAroundSum(v, SNitrogen);
                lock (diffuseLocker)
                {
                    sNew[i, j, k, 0] = newNit;
                    sNew[i, j, k, 1] = newOx;
                    sNew[i, j, k, 2] = newCar;
                    sums[i, 0] += SNitrogen[i, j, k];
                    sums[i, 1] += SOxygen[i, j, k];
                    sums[i, 2] += SCarbon[i, j, k];
                }
            }
            );

            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                    for (int k = 0; k < AreaLength; k++)
                    {
                        SNitrogen[i, j, k] = sNew[i, j, k, 0];
                        SCarbon[i, j, k]   = sNew[i, j, k, 1];
                        SOxygen[i, j, k] = sNew[i, j, k, 2];
                    }



            for (int i = 0; i < AreaWidth; i++)
            {
                averageSubstrateNitrogen += sums[i, 0];
                averageSubstrateOxygen += sums[i, 1];
                averageSubstrateCarbon += sums[i, 2];
            }

            averageSubstrateNitrogen /= legalPoints.Count;
            averageSubstrateOxygen   /= legalPoints.Count;
            averageSubstrateCarbon   /= legalPoints.Count;
            //            Debug.Log($"SubSub = {subSum}");
        }
        private double AverageSubstrateAroundSum(Vector3Int pos, double[,,] S)
        {
            double av_Nutr = 0;
            int k_Nutr = 0;
            foreach (Vector3Int nbr in GridNbrsDic[gridType])
            {
                Vector3Int v = pos + nbr;
                if (IsLegal(v))
                {
                    if (true || Cells[v.x, v.y, v.z] == CellState.empty) 
                    {
                        k_Nutr++;
                        av_Nutr += S[v.x, v.y, v.z];
                    }
                }
            }
            av_Nutr -= S[pos.x, pos.y, pos.z] * k_Nutr;
            av_Nutr = av_Nutr * 1d/k_Nutr;
            return av_Nutr;
        }
        private void TryBounds(Vector3 v)
        {
            if (v.x < MinX) MinX = v.x;
            if (v.x > MaxX) MaxX = v.x;
            if (v.y < MinY) MinY = v.y;
            if (v.y > MaxY) MaxY = v.y;
            if (v.z < MinZ) MinZ = v.z;
            if (v.z > MaxZ) MaxZ = v.z;
        }
        private bool IsLegal(Vector3Int v)
        {
            bool b = v.x >= 0 && v.y >= 0 && v.z >= 0 && v.x < AreaWidth && v.y < AreaHeight && v.z < AreaLength;
            Vector3 pos = GetPos(v);
            if (pos.x <= AreaMaxX && pos.x >= AreaMinX && pos.z >= AreaMinY && pos.z <= AreaMaxY)
                return b;
            else
                return false;
        }
        private bool HasFreeSpaceAround(Vector3Int pos)
        {
            int co = 0;
            foreach (Vector3Int v in GridNbrsDic[gridType])
            {
                Vector3Int sum = v + pos;
                if (!IsLegal(sum) || Cells[sum.x, sum.y, sum.z] == CellState.empty)
                     return true;
            }
            if (co > 3) return true;
            return false;
        }
        private Vector3 GetPos(Vector3Int coord)
        {
            Vector3 v = coord.x * GridBaseDic[gridType][0] + coord.y * GridBaseDic[gridType][1] + coord.z * GridBaseDic[gridType][2];
            return v;
        }
        private void NewCell(Vector3Int cellIndex)
        {
            if (cellIndex.y > surfaceTop[cellIndex.x, cellIndex.z])
                surfaceTop[cellIndex.x, cellIndex.z] = cellIndex.y; 
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
            possibleNewVert[1] = newCellPos + new Vector3(-0.5f, -0.5f, 0.5f);
            possibleNewVert[2] = newCellPos + new Vector3(0.5f, -0.5f, 0.5f);
            possibleNewVert[3] = newCellPos + new Vector3(0.5f, -0.5f, -0.5f);
            possibleNewVert[4] = newCellPos + new Vector3(-0.5f, 0.5f, -0.5f);
            possibleNewVert[5] = newCellPos + new Vector3(-0.5f, 0.5f, 0.5f);
            possibleNewVert[6] = newCellPos + new Vector3(0.5f, 0.5f, 0.5f);
            possibleNewVert[7] = newCellPos + new Vector3(0.5f, 0.5f, -0.5f);
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
            newTris[0] = p[4]; newTris[1] = p[0]; newTris[2] = p[5];
            newTris[3] = p[5]; newTris[4] = p[0]; newTris[5] = p[1];
            newTris[6] = p[5]; newTris[7] = p[1]; newTris[8] = p[6];
            newTris[9] = p[6]; newTris[10] = p[1]; newTris[11] = p[2];
            newTris[12] = p[6]; newTris[13] = p[2]; newTris[14] = p[7];
            newTris[15] = p[7]; newTris[16] = p[2]; newTris[17] = p[3];
            newTris[18] = p[7]; newTris[19] = p[3]; newTris[20] = p[4];
            newTris[21] = p[4]; newTris[22] = p[3]; newTris[23] = p[0];
            newTris[24] = p[7]; newTris[25] = p[4]; newTris[26] = p[6];
            newTris[27] = p[6]; newTris[28] = p[4]; newTris[29] = p[5];
            newTris[30] = p[0]; newTris[31] = p[3]; newTris[32] = p[1];
            newTris[33] = p[1]; newTris[34] = p[3]; newTris[35] = p[2];

            for (int i = 0; i < 36; i += 3)
                debugT += $"({newTris[i]},{newTris[i + 1]},{newTris[i + 2]} ";

            vertices.AddRange(newVert);
            triangles.AddRange(newTris);
            Debug.Log(debugT);
            Debug.Log(debugV);
        }
        private void ConsumeSubstrate(Vector3Int pos)
        {
            int i = pos.x; int j = pos.y; int k = pos.z;
            double J_Carbon = Math.Min(μCarbon * SCarbon[i, j, k] / (K_Carbon + SCarbon[i, j, k]) * SOxygen[i, j, k] / (K_Oxygen + SOxygen[i, j, k]), SCarbon[i, j, k]);
            double J_Nitrogen = Math.Min(μNitrogen * SNitrogen[i, j, k] / (K_Nitrogen + SNitrogen[i, j, k]) * SOxygen[i, j, k] / (K_Oxygen + SOxygen[i, j, k]), SNitrogen[i, j, k]);
            double J_Oxygen  = Math.Min(μOxygen  * SOxygen [i, j, k] / (K_Oxygen +  SOxygen [i, j, k]),  SOxygen [i, j, k]);
            if (HasFreeSpaceAround(pos) )//(surfaceTop[pos.x, pos.z] == pos.y) // if on the top
                oxygenSurfaceConsume += J_Oxygen;
            else
                averageConsumeOxygen += J_Oxygen;
            averageConsumeNitrogen += J_Nitrogen;
            averageConsumeCarbon += J_Carbon;
            double μ = μmax * SCarbon[i, j, k] / (K_Carbon + SCarbon[i, j, k]) * μNitrogen * SNitrogen[i, j, k] / (K_Nitrogen + SNitrogen[i, j, k]) * SOxygen[i, j, k] / (K_Oxygen + SOxygen[i, j, k]);
            C[i, j, k] += μ * C[i, j, k] ;
            SNitrogen[i, j, k] -= J_Nitrogen;
            SCarbon[i, j, k] -= J_Carbon;
            SOxygen[i, j, k] -= J_Oxygen;
        }
        private void ShareSubstrate(Vector3Int index)
        {
            List<Vector3Int> dirs = new List<Vector3Int>();
            double shareAmount = (C[index.x, index.y, index.z] - ConcToShare) * 1f / GridNbrsDic[gridType].Length;
            foreach (Vector3Int nbr in GridNbrsDic[gridType])
            {
                Vector3Int p = index + nbr;
                if (IsLegal(p) && C[p.x, p.y, p.z] > C[index.x, index.y, index.z])
                {
                    C[p.x, p.y, p.z] += shareAmount;
                    C[index.x, index.y, index.z] -= shareAmount;
                }
            }
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
                    hasBorderNbrs = true;
            }
            if (dirs.Count > 0)
            {
                lock (locker)
                {
                    Vector3Int dir = dirs[rng.Value.Next(dirs.Count)];
                    C[index.x, index.y, index.z] /= 2f;// C[pos.x, pos.y] - ConcToDivide;
                    C[dir.x, dir.y, dir.z] = C[index.x, index.y, index.z];// ConcToDivide;
                    NewCell(dir);
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
            }
        }
        private IEnumerator GrowthProcess()
        {
            int minValOfNew = 50;
            int stopEncounter = 0;
            int stopStop = 50;
            do
            {
                iterationId++;
                //for (int i = 0; i < DiffusionCount; i++)
                DiffuseSubstrate();

                NewBiomassCells.Clear();
                averageConsumeOxygen = 0;
                averageConsumeCarbon = 0;
                averageConsumeNitrogen = 0;
                oxygenSurfaceConsume = 0;
                Parallel.ForEach(BiomassCells, parallelOptions, (cellPos) =>
                //foreach (Vector3Int cellPos in BiomassCells)
                {
                    if (Cells[cellPos.x, cellPos.y, cellPos.z] == CellState.busyCanDiv)
                        ConsumeSubstrate(cellPos);
                    else
                    {
                        if (C[cellPos.x, cellPos.y, cellPos.z] > ConcToShare)
                            ShareSubstrate(cellPos);
                    }
                    C[cellPos.x, cellPos.y, cellPos.z] -= LifetimeCost;
                    if (C[cellPos.x, cellPos.y, cellPos.z] > ConcToDivide)
                        Divide(cellPos);
                }
                );
                //Logger.WriteLog($"{iterationId};{Math.Log(CellCount)};{CellCount};{averageConsumeOxygen};{oxygenSurfaceConsume}");
                averageConsumeNitrogen /= BiomassCells.Count;
                averageConsumeOxygen /= BiomassCells.Count;
                averageConsumeCarbon /= BiomassCells.Count;
                BiomassCells.AddRange(NewBiomassCells);
                CellsToRedraw.AddRange(NewBiomassCells);
                if (iterationId % 10 != 0)
                    continue;
                //if (iterationId % 100 == 0)
                //    StartCoroutine(PrintResultAndClear());
                if (copyMethod != CopyMethod.None)
                {
                    if (copyMethod == CopyMethod.Instantiate)
                    {
                        foreach (Vector3Int cellIndex in CellsToRedraw)
                        {
                            GameObject newObj;
                            if (objectsPool.TryDequeue(out newObj))
                            {
                                if (newObj != null && newObj.transform != null)
                                    newObj.transform.position = GetPos(cellIndex);
                            }
                            else
                            {
                                newObj = Instantiate(PrefabsDic[gridType], GetPos(cellIndex), Quaternion.identity, transform);
                            }
                            Vector3 pos = GetPos(cellIndex);
                            if (iterationId > 50)
                                newObj.GetComponent<MeshRenderer>().material.color = ColFromH(pos.y);
                            else
                                newObj.GetComponent<MeshRenderer>().material.color = Color.green;
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
                }
                if (NewBiomassCells.Count == 0)
                    stopEncounter++;
                else
                    stopEncounter = 0;
                if (stopEncounter == 50)
                    break;
                yield return new WaitForEndOfFrame();
            }
            while (Math.Min(Math.Min(averageSubstrateCarbon, averageSubstrateNitrogen), averageSubstrateOxygen) > FinishThreshold);
            if (copyMethod == CopyMethod.None && DrawResult)
            StartCoroutine(PrintResultAndClear());
            AnalyseTheResult();
        }

        private IEnumerator PrintResultAndClear()
        {
            Debug.Log("Print results and clear");
            GameObject tempObject = new GameObject();
            Transform tempTransform = tempObject.transform;
            foreach (Vector3Int cellIndex in BiomassCells)
            {
                //if (HasFreeSpaceAround(cellIndex))
                {
                    Vector3 pos = GetPos(cellIndex);
                    GameObject newObj = Instantiate(PrefabsDic[gridType], pos, Quaternion.identity, tempTransform);
                    if (iterationId > 50)
                    newObj.GetComponent<MeshRenderer>().material.color = ColFromH(pos.y);
                    else
                        newObj.GetComponent<MeshRenderer>().material.color = Color.green;

                }
            }
            yield return new WaitForEndOfFrame();
            NeedPrint = false;
//            Screenshot($"{AreaWidth}x{AreaHeight}x{AreaLength}_C{100 * InitCarbon}_N{100 * InitNitrogen}_O{100 * InitOxygen}");
            Screenshot($"{CellCount}_{iterationId}");
            yield return new WaitForEndOfFrame();
            Debug.Log("Printed");
            Destroy(tempObject);
        }

        private Color ColFromH(float h)
        {
            float div = (MaxY - MinY) / 2f;
            if (h > MinY + div )
                return Color.Lerp(Color.yellow, Color.red, (h - MinY - div)*1f/div);
            else
                return Color.Lerp(Color.green, Color.yellow, (h - MinY)/div );
        }


        private IEnumerator WaitForPrint()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                if (NeedPrint)
                {
                    yield return new WaitForEndOfFrame();
                    NeedPrint = false;
                    Screenshot($"{AreaWidth}x{AreaHeight}x{AreaLength}_C{100 * InitCarbon}_N{100 * InitNitrogen}_O{100 * InitOxygen}");
                    yield return new WaitForEndOfFrame();
                    Debug.Log("Printed");
                }
            }
        }

        private async void AnalyseTheResult()
        {
            List<Vector3> data = new List<Vector3>();
            foreach (Vector3Int v in BiomassCells)
            {
                if (HasFreeSpaceAround(v))
                    data.Add(GetPos(v));
            }
            float FradDim = BoxCountingMachine3D.GetFractalDimension(MinBounds, MaxBounds, data);
            MyLogger.WriteLog($"{AreaWidth};{AreaHeight};{AreaLength};{InitCarbon.ToString("0.00")};{InitNitrogen.ToString("0.00")};{InitOxygen.ToString("0.00")};{FradDim.ToString("0.00000")}");

//            Debug.Log($"Done for {NowTime() - startTimeSecs} seconds, {AreaWidth};{AreaHeight};{AreaLength};{InitCarbon.ToString("0.0")};{InitNitrogen.ToString("0.0")};{InitOxygen.ToString("0.0")};{FradDim}" +
//                $"{Environment.NewLine} Iter={iterationId}, Ccount = {CellCount} ");
/*            if (DrawResult)
            {
                await Task.Delay(1000);
                Screenshot($"{AreaWidth}x{AreaHeight}x{AreaLength}_C{100 * InitCarbon}_N{100 * InitNitrogen}_O{100 * InitOxygen}");
                await Task.Delay(1000);
            }
*/            StartCoroutine(WaitForPrint());
            SimControl.Instance.ImFinished();
        }
        private void OnGUI()
        {
            if (ShowGizmo)
            GUI.TextArea(new Rect(0, 0, 600, 150),
                $"Oxygen = {averageConsumeOxygen.ToString("0.000")} -   {averageSubstrateOxygen.ToString("0.000")}{Environment.NewLine}" +
                $"Carbon = {averageConsumeCarbon.ToString("0.000")} - {averageSubstrateCarbon.ToString("0.000")}{Environment.NewLine}" +
                $"Nitrogen = {averageConsumeNitrogen.ToString("0.000")} - {averageSubstrateNitrogen.ToString("0.000")}" +
                $"{Environment.NewLine}id = {iterationId}" +
                $"{Environment.NewLine}CellCount = {CellCount} {(CellCount*1f/legalPoints.Count).ToString("#0.0%")}" +
                $"{Environment.NewLine}Time = {NowTime() - startTimeSecs} secs");
        }
        private void RedrawTheMesh()
        {
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            meshFilter.mesh = mesh;
            mesh.RecalculateNormals();
        }


        private static string imagesLoc = Environment.CurrentDirectory + $@"\Log\Pictures\";

        private void Screenshot(string name)
        {
            string path = imagesLoc + name + "_" + startTimeSecs  +  ".jpg";
            ScreenCapture.CaptureScreenshot(path);
        }
    
        public async void DoThePicture()
        {
            await Task.Delay(1000);
            Screenshot($"{AreaWidth}x{AreaHeight}x{AreaLength}_C{100 * InitCarbon}_N{100 * InitNitrogen}_O{100 * InitOxygen}");
            await Task.Delay(1000);
        }

    }
}
