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
using UnityEngine.Rendering;
using UnityEngine.Windows;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace CellularAutomaton
{

    public class BiofilmGrowth : MonoBehaviour
    {

        const int MAXSIZE = 10000;

        [SerializeField] private bool EndlessCount = false;
        [SerializeField] private GameObject TrianglePrefab;
        [SerializeField] private GameObject DiamondPrefab;
        [SerializeField] private GameObject SquarePrefab;
        [SerializeField] private GameObject HexagonePrefab;

        [SerializeField][Range(0f, 0.5f)] private float initAreaSideOffset = 0.2f;
        [SerializeField][Range(1, 10)] private int initAreaHeight = 2;
        [SerializeField] private int AreaWidth = 1000;
        [SerializeField] private int AreaHeight = 1000;
        [SerializeField] private GridType gridType;

        [SerializeField][Range(0.01f, 0.5f)] private float divisionProbabilityInit = 0.05f;
        [SerializeField] private int cellMaxDivisionCount = 50;
        [SerializeField] private int maxTryCount = 10;
        [SerializeField][Range(1f, 50f)] private float valueToDecreaseProb = 2.5f;

        private float divisionProbabilty;
        private float diamondBottom = 0f;
        private float MinX = 1000, MaxX = -1000, MinY = 1000, MaxY = -1000;
        private Vector2 MinBounds => new Vector2(MinX, MinY);
        private Vector2 MaxBounds => new Vector2(MaxX, MaxY);
        private List<Vector2> areaPoints = new List<Vector2>();
        private List<Vector2> frontPoints = new List<Vector2>();
        
        private int CellCount = 0;
        private bool[,] Cells = new bool[MAXSIZE, MAXSIZE];
        private bool[,] indicArea = new bool[MAXSIZE, MAXSIZE];
        private Dictionary<Vector2Int, CellInfo> CellInfos = new Dictionary<Vector2Int, CellInfo>();
        private Dictionary<Vector2Int, CellInfo> newCells = new Dictionary<Vector2Int, CellInfo>();

        private Dictionary<GridType, Vector2[]> GridBaseDic;
        private Dictionary<GridType, Vector2Int[]> GridNbrsDic;
        private Dictionary<GridType, GameObject> PrefabsDic;

        private int minSizeBoxCounting = 2;
        private int maxSizeBoxCounting = 20;

        private void TryBounds(Vector2 v)
        {
            if (v.x < MinX) MinX = v.x;
            if (v.x > MaxX) MaxX = v.x;
            if (v.y < MinY) MinY = v.y;
            if (v.y > MaxY) MaxY = v.y;
        }

        private const float sqrt3d2 = 0.86602540378443864676372317075294f;

        private Vector2[] TriangleBase =
        {
            new Vector2( .5f, 0),
            new Vector2(0, sqrt3d2),
        };
        private Vector2[] DiamondBase =
        {
            new Vector2( -.5f, sqrt3d2),
            new Vector2( .5f,  sqrt3d2),
        };
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

        private bool IsOdd(Vector2Int p) => (p.x + p.y) % 2 == 1;


        private Vector2Int[] DiamondNbrs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int(-1, 0),
            new Vector2Int( 0,-1),
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
            new Vector2Int( 0,-1),
            new Vector2Int( 1,-1),
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int(-1, 1),
        };
        private Vector2Int[] TrianNbrs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
        };
        private void Awake()
        {
            GridBaseDic = new Dictionary<GridType, Vector2[]>
            {
                {GridType.Triangle, TriangleBase   },
                {GridType.Diamond,  DiamondBase },
                {GridType.Square,   SquareBase  },
                {GridType.Hexagone, HexagonBase },
            };
            GridNbrsDic = new Dictionary<GridType, Vector2Int[]>
            {
                {GridType.Triangle, TrianNbrs   },
                {GridType.Diamond,  DiamondNbrs },
                {GridType.Square,   SquareNbrs  },
                {GridType.Hexagone, HexagonNbrs },
            };
            PrefabsDic = new Dictionary<GridType, GameObject>
            {
                {GridType.Triangle, TrianglePrefab },
                {GridType.Diamond,  DiamondPrefab },
                {GridType.Square,   SquarePrefab },
                {GridType.Hexagone, HexagonePrefab },
            };
/*            if (EndlessCount)
            {
                float[] ps = ParameterHead.Instance.GetTheParams();
                AreaWidth = (int)ps[0];
                AreaHeight = (int)ps[1];
                valueToDecreaseProb = ps[2];
                maxTryCount = (int)ps[3];
                cellMaxDivisionCount = (int)ps[4];
                gridType = (GridType)((int)ps[5]);
            }
            InitArea();
*/        
        }

        private void ReadParams()
        {
            float[] ps = ParameterHead.Instance.ReadParameters();
            actualBase = GridBaseDic[gridType];
            divisionProbabilty = NormalizeProbAccordingToGridType();
            AreaWidth = (int)ps[0];
            AreaHeight = (int)ps[0];
            valueToDecreaseProb = ps[1];
            maxTryCount = (int)ps[2];
            cellMaxDivisionCount = (int)ps[3];
            gridType = (GridType)((int)ps[4]);
        }



        public static Vector2 GetGlobalPos(Vector2Int coord)
        {
            Vector2 v = coord.x * actualBase[0] + coord.y * actualBase[1];
            return v;
        }

        private float NormalizeProbAccordingToGridType()
        {
            float m = valueToDecreaseProb;
            switch (gridType)
            {
                case GridType.Triangle:
                    return divisionProbabilityInit / (0.5f + 2f / m + 0.5f / m / m);
                case GridType.Diamond:
                    return divisionProbabilityInit / (2f + 2f / m);
                case GridType.Square:
                    return divisionProbabilityInit / (1 + 2f / m + 1f / m / m);
                case GridType.Hexagone:
                    return divisionProbabilityInit / (2f + 2 / m + 2 / (m * m));
            }
            Debug.LogError("Unexpected grid tpe in normalization probabilty function");
            return 0f;
        }


        private void GenerateDiamondArea()
        {
            int xMin = (int)(initAreaSideOffset * AreaWidth);
            int xMax = (int)((1f - initAreaSideOffset) * AreaWidth);
            diamondBottom = GetPos(new Vector2Int(xMin, AreaWidth - xMin)).y;
            Debug.Log($"DiamondBottom : {diamondBottom}");
            for (int i = xMin; i < xMax; i++)
                for (int j = 0; j < initAreaHeight; j++)
                {
                    Vector2Int pos = new Vector2Int(i, j + AreaWidth - i);
                    CellInfos.Add(pos, NewCell(pos));
                }


        }

        private void GenerateArea()
        {
            int xMin = (int)(initAreaSideOffset * AreaWidth);
            int xMax = (int)((1f - initAreaSideOffset) * AreaWidth);
            for (int i = xMin; i < xMax; i++)
                for (int j = 0; j < initAreaHeight; j++)
                {
                    CellInfos.Add(new Vector2Int(i, j), NewCell(new Vector2Int(i, j)));
                }
        }


        public void InitArea()
        {
            ReadParams();
            if (gridType == GridType.Diamond)
                GenerateDiamondArea();
            else
                GenerateArea();
            foreach (var cellInfo in CellInfos)
            {
                List<Vector2Int>[] nbrs = CalcNbrsCount(cellInfo.Value.myPos);

                cellInfo.Value.highNbrs = nbrs[0];
                cellInfo.Value.midNbrs = nbrs[1];
                cellInfo.Value.lowNbrs = nbrs[2];
                //                Debug.Log($"newbee {cellInfo.Key} ->  {NbrsString(cellInfo.Value)}");
            }
                        StartCoroutine(GrowthProcess());
        }
        private string NbrsString(CellInfo cellInfo)
        {
            string s = $"{GetPos(cellInfo.myPos)}  -> H:";
            foreach (Vector2Int v in cellInfo.highNbrs)
                s += $"{v.ToString()},";
            s += " ; M:";
            foreach (Vector2Int v in cellInfo.midNbrs)
                s += $"{v.ToString()},";
            s += " ; L:";
            foreach (Vector2Int v in cellInfo.lowNbrs)
                s += $"{v.ToString()},";
            return s;
        }
        private bool IsLegal(Vector2Int v) => v.x >= 0 && v.y >= 0 && v.x < AreaWidth && v.y < AreaHeight;
        private List<Vector2Int>[] CalcNbrsCount(Vector2Int pos)
        {
            List<Vector2Int>[] nbrs = new List<Vector2Int>[3];
            nbrs[0] = new List<Vector2Int>();
            nbrs[1] = new List<Vector2Int>();
            nbrs[2] = new List<Vector2Int>();
            foreach (Vector2Int v in GridNbrsDic[gridType])
            {
                Vector2Int sum = v + pos;
                if (gridType == GridType.Triangle && !IsOdd(pos) && v == Vector2Int.up)
                    sum = pos - v;
                if (IsLegal(sum) && !Cells[sum.x, sum.y])
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
        private CellInfo NewCell(Vector2Int cellIndex)
        {
            Vector2 cellPos = GetPos(cellIndex);
            TryBounds(cellPos);
            AddPointToStack(cellIndex, areaPoints);
            CellCount++;
            Cells[cellIndex.x, cellIndex.y] = true;
            GameObject newObj;
            if (gridType == GridType.Triangle && IsOdd(cellIndex)) // reversed triangle
                newObj = Instantiate(PrefabsDic[gridType], GetPos(cellIndex) + (Vector2) transform.position, Quaternion.Euler(0, 0, 180), transform);
            else
                newObj = Instantiate(PrefabsDic[gridType], GetPos(cellIndex) + (Vector2)transform.position, Quaternion.identity, transform);
            CellInfo newCellInfo = new CellInfo(cellIndex);
            newCellInfo.myObj = newObj;
            newCellInfo.mySpr = newObj.GetComponent<SpriteRenderer>();
            return newCellInfo;
        }
        private Vector2Int GetDirectionToGrow(CellInfo info)
        {
            float p = divisionProbabilty;
            if (info.highNbrs.Count > 0 && Random.value <= p)
            {
                if (info.highNbrs.Count == 1)
                    return info.highNbrs[0];
                else
                {
                    if (Random.value > .5f)
                        return info.highNbrs[0];
                    else
                        return info.highNbrs[1];
                }
            }
            p /= valueToDecreaseProb;
            if (info.midNbrs.Count > 0 && Random.value <= p)
            {
                if (info.midNbrs.Count == 1)
                    return info.midNbrs[0];
                else
                {
                    if (Random.value > .5f)
                        return info.midNbrs[0];
                    else
                        return info.midNbrs[1];
                }
            }
            p /= valueToDecreaseProb;
            if (info.lowNbrs.Count > 0 && Random.value <= p)
            {
                if (info.lowNbrs.Count == 1)
                    return info.lowNbrs[0];
                else
                {
                    if (Random.value > .5f)
                        return info.lowNbrs[0];
                    else
                        return info.lowNbrs[1];
                }
            }
            return -Vector2Int.zero;
        }
        private IEnumerator GrowthProcess()
        {
            int waiter = 10;
            do
            {
                newCells.Clear();
                foreach (var cellInfo in CellInfos)
                {
                    if (cellInfo.Value.divisionCount == cellMaxDivisionCount || cellInfo.Value.tryCount == maxTryCount) continue;
                    Vector2Int dir = GetDirectionToGrow(cellInfo.Value);
                    cellInfo.Value.tryCount++;
                    if (dir == Vector2Int.zero) continue;
                    if (gridType == GridType.Diamond && GetPos(dir).y < diamondBottom) continue;
                    CellInfo newBuddy = NewCell(dir);
                    cellInfo.Value.divisionCount++;
                    newCells.Add(newBuddy.myPos, newBuddy);
                    newBuddy.divisionCount = cellInfo.Value.divisionCount;
                    ProcessNewBuddy(newBuddy);
                    //                    yield return new WaitForSeconds(0.02f);
                }
                foreach (var v in newCells)
                    CellInfos.Add(v.Key, v.Value);
                yield return new WaitForEndOfFrame();
                if (newCells.Count == 0) waiter--;
                CameraConfig.SetCameraBounds(new Vector4(MinX, MinY, MaxX, MaxY));
            }
            while (newCells.Count > 0 && waiter > 0);
            GetTheBiofilmFront();
        }
        private void ProcessNewBuddy(CellInfo buddy)
        {
            // Debug.Log($"proc: now cell {buddy.myPos} - {buddy.myPos.x * GridBaseDic[gridType][0] + buddy.myPos.y * GridBaseDic[gridType][1]} is busy");
            Vector2Int pos = buddy.myPos;
            foreach (Vector2Int v in GridNbrsDic[gridType])
            {
                Vector2Int sum = v + pos;
                if (gridType == GridType.Triangle && !IsOdd(pos) && v == Vector2Int.up)
                    sum = pos - v;
                if (!IsLegal(sum)) continue;

                Vector2 myPos = GetPos(buddy.myPos); ;
                Vector2 newPos = GetPos(sum);
                if (!Cells[sum.x, sum.y])
                {
                    if (newPos.y > myPos.y)
                    {
                        buddy.highNbrs.Add(sum);
                    }
                    else
                    {
                        if (newPos.y == myPos.y)
                            buddy.midNbrs.Add(sum);
                        else
                            buddy.lowNbrs.Add(sum);
                    }
                }
                else
                {
                    //Debug.Log($"{sum} {pos}");
                    if (newPos.y > myPos.y)
                    {
                        //Debug.Log($"need to delete it from {sum} cell as low");
                        if (CellInfos.ContainsKey(sum))
                            CellInfos[sum].lowNbrs.Remove(pos);
                        else
                            newCells[sum].lowNbrs.Remove(pos);
                    }
                    else
                    {
                        if (newPos.y == myPos.y)
                        {
                            //Debug.Log($"need to delete it from {sum} cell as mid");
                            if (CellInfos.ContainsKey(sum))
                                CellInfos[sum].midNbrs.Remove(pos);
                            else
                                newCells[sum].midNbrs.Remove(pos);
                        }
                        else
                        {
                            //Debug.Log($"need to delete it from {sum} cell as high");
                            if (CellInfos.ContainsKey(sum))
                                CellInfos[sum].highNbrs.Remove(pos);
                            else
                                newCells[sum].highNbrs.Remove(pos);
                        }
                    }
                }
            }
            //            Debug.Log($"ok -> {NbrsString(buddy)}");
        }
        private void AddPointToStack(Vector2Int pointId, List<Vector2> stack)
        {
            Vector2 center = GetPos(pointId);
            if (center.y <= MinY) return;
            stack.Add(center);
            return;
            foreach (Vector2Int nbr in GridNbrsDic[gridType])
            {
                Vector2 pos = (GetPos(pointId + nbr) + center) / 2f;
                stack.Add(pos);
            }
        }
        private void ProcCell(Vector2Int cellId)
        {
            indicArea[cellId.x, cellId.y] = true;
            if (Cells[cellId.x, cellId.y])
            {
                AddPointToStack(cellId, frontPoints);
                biofilmFront.Push(cellId);
            }
            else
            {
                foreach (Vector2Int v in NbrsToExplore())
                {
                    Vector2Int v_p = v + cellId;
                    if (IsLegal(v_p) && !indicArea[v_p.x, v_p.y])
                        orderToProcess.Push(v + cellId);
                }
            }
        }
        private void HighlightFrontCells(Vector2Int pos)
        {
            CellInfos[pos].mySpr.color = Color.red;
        }
   
        
        Stack<Vector2Int> orderToProcess = new Stack<Vector2Int>();
        Stack<Vector2Int> biofilmFront = new Stack<Vector2Int>();
        private Vector2Int[] NbrsToExplore()
        {
            if (gridType != GridType.Triangle)
                return GridNbrsDic[gridType];
            else
                return GridNbrsDic[GridType.Square];
        }
        private void GetTheBiofilmFront()
        {
            /*
            Vector2Int startPoint = new Vector2Int(AreaWidth - 1, AreaHeight - 1);
            orderToProcess.Push(startPoint);

            while (orderToProcess.Count > 0)
            {
                //Debug.Log($"behind: [{cellsBehind}], orderListCount: [{orderToProcess.Count}], frontCount: [{biofilmFront.Count}]");
                Vector2Int v = orderToProcess.Pop();
                //Debug.DrawLine(GetPos(v), GetPos(v) + Vector2.up, Color.red, 100);
                ProcCell(v);
            }
            */
            foreach (var coord in CellInfos)
                if (!coord.Value.HasntNbr)
                {
                    Vector2 pos = GetPos(coord.Key);
                    biofilmFront.Push(coord.Key);
                    //HighlightFrontCells(coord.Key);
                    frontPoints.Add(pos);
                }

            //            Debug.Log($"Done for  . . . {watch.ElapsedMilliseconds}, biofilmLen : {biofilmFront.Count}");
            WriteResult();
        }
        private void WriteResult()
        {
            Debug.Log($"BOUNDS : {MinX},{MinY}, {MaxX}, {MaxY}");

            float dt = 0f;// BoxCountingMachine.GetFractalDimension(new Vector4(MinX, MinY, MaxX, MaxY), frontPoints);
            int w = AreaWidth - (int)(AreaWidth * 2 * initAreaSideOffset);
            string res = $"{w}\t{gridType.ToString()}\t{valueToDecreaseProb}\t{maxTryCount}\t{dt}";
            Debug.Log(res);
            MyLogger.WriteLog(res);
            MyLogger.Screenshot($"{gridType.ToString()}_{valueToDecreaseProb}_{maxTryCount}_{cellMaxDivisionCount}");
            if (EndlessCount && !ParameterHead.Instance.IsDone())
                SceneMan.Instance.ReloadScene();
            else
                Application.Quit();
        }

        public void Reset()
        {
            SceneMan.Instance.ReloadScene();
        }
    }
}
