using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CellularAutomaton
{
    public class BoxCountingMachine3D
    {
        private static float minSize = 1.3f;
        private static float maxSize = 40.3f;
        private static float deltaSize = 0.9f;


        private static float minSizeArea = 1f;
        private static float maxSizeArea = 8f;
        private static float deltaSizeArea = 0.3f;

        private static Vector3 minBounds, maxBounds;

        private static bool needDrawArea = false;
        private static bool needDrawLen = false;

        private static float maxDistance = 1.1f;

        const double alpha = 0.05;

        private static int GetIdOfC(double c)
        {
            //Debug.Log($"GetIdOfC {c} -> {(int)Math.Floor(c / alpha)}");
            return (int)Math.Floor(c / alpha);
        }


        public static float[] ModelStats3D(double[,,] C3D, double minConcTH = 0.2f)
        {
            int AreaWidth = C3D.GetLength(0);
            int AreaHeight = C3D.GetLength(1);
            int AreaLength = C3D.GetLength(2);
            int[,] busyCount = new int[AreaWidth, AreaLength];
            int[,] maxH = new int[AreaWidth, AreaLength];
            int totalBusyCount = 0;
            double totalSum = 0;

            double maxC = -1000;
            double minC = 1000;
            int[] counts = new int[150];

            for (int i = 0; i < AreaWidth; i++)
                for (int k = 0; k < AreaLength; k++)
                    for (int j = 0; j < AreaHeight; j++)
                    {
                        if (C3D[i, j, k] < minConcTH)
                            continue;

                        if (j + 1 > maxH[i, k])
                            maxH[i, k] = j + 1;

                        busyCount[i, k]++;
                        totalBusyCount++;
                        if (C3D[i, j, k] > maxC)
                            maxC = C3D[i, j, k];
                        if (C3D[i, j, k] < minC)
                            minC = C3D[i, j, k];
                        counts[GetIdOfC(C3D[i, j, k])]++;

                        totalSum += C3D[i, j, k];
                    }



            float avHeighOfall = 0;
            float avHeightWithPores = 0;
            int busyCountLayers = 0;

            string countsString = "Counts";
            for (int i = 0; i < counts.Length; i++)
                if (counts[i] > 0)
                    countsString += $"[{alpha*i}-{alpha*(i+1)}] - {(counts[i]*1f/ totalBusyCount).ToString("0.0000")}    ";

            Debug.Log($"C {minC.ToString("0.0000")}-{maxC.ToString("0.0000")} : {countsString}");

            for (int i = 0; i < AreaWidth; i++)
                for (int k = 0; k < AreaLength; k++)
                {
                    if (maxH[i, k] == 0)
                        continue;
                    busyCountLayers++;
                    avHeighOfall += maxH[i, k];
                    avHeightWithPores += busyCount[i, k];
                }
            avHeighOfall = avHeighOfall * 1f / busyCountLayers;
            avHeightWithPores = avHeightWithPores * 1f / busyCountLayers;

            float avConc = (float)totalSum * 1f / totalBusyCount;
            float avCoun = totalBusyCount * 1f / (AreaWidth * AreaLength);

            int bottomCount = 0;

            for (int i = 0; i < AreaWidth; i++)
                for (int k = 0; k < AreaLength; k++)
                    if (C3D[i, 0, k] > minConcTH)
                        bottomCount++;

            float bottomFill = bottomCount * 1f / (AreaWidth * AreaLength);

            return new float[5] { avHeighOfall, avHeightWithPores, avConc, avCoun, bottomFill };
        }

        public static float GetFractalDimension(Vector3 minBounds, Vector3 maxBounds, List<Vector3> pnts)
        {
            BoxCountingMachine3D.minBounds = minBounds;
            BoxCountingMachine3D.maxBounds = maxBounds;
            return GetFractalDimension(pnts, false);
        }

        private static float GetFractalDimension(List<Vector3> stack, bool area)
        {
            List<Vector2> pnts = new List<Vector2>();
            float start, end, step; bool needDraw;
            if (area)
            {
                start = minSizeArea;
                end = maxSizeArea;
                step = deltaSizeArea;
                needDraw = needDrawArea;
            }
            else
            {
                start = minSize;
                end = maxSize;
                step = deltaSize;
                needDraw = needDrawLen;
            }

            for (float s = start; s <= end; s += step)
            {
                Vector2 pnt = CalcLengthBoxCount(s, stack, needDraw);
                pnts.Add(pnt);
            }
            Vector2 ln = LineSqrMin(pnts);

            float er = 0;
            foreach (Vector2 pnt in pnts)
                er += Mathf.Abs(ln.y + ln.x * pnt.x - pnt.y);
            //Debug.Log($"ERROR = {er / pnts.Count}");
            return LineSqrMin(pnts).x;
        }
        private static void DrawStackDebug(List<Vector2> stack, Color color, float d)
        {
            foreach (Vector2 p in stack)
            {
                Debug.DrawLine(p + d * new Vector2(-1, -1), p + d * new Vector2(-1, +1), color, 100);
                Debug.DrawLine(p + d * new Vector2(-1, +1), p + d * new Vector2(+1, +1), color, 100);
                Debug.DrawLine(p + d * new Vector2(+1, +1), p + d * new Vector2(+1, -1), color, 100);
                Debug.DrawLine(p + d * new Vector2(+1, -1), p + d * new Vector2(-1, -1), color, 100);
            }
        }
        private static Vector2 CalcLengthBoxCount(float size, List<Vector3> pnts, bool needDraw)
        {
            int a = 0;
            Vector3Int arraySize = V3ToV3Int((maxBounds - minBounds) / size + 3*Vector3Int.one);
            bool[,,] filledBoxes = new bool[arraySize.x, arraySize.y, arraySize.z];
            foreach (Vector3 p in pnts)
            {
                Vector3 pos = p - minBounds;
                int xBox = (int)(pos.x / size);
                int yBox = (int)(pos.y / size);
                int zBox = (int)(pos.z / size);
               //                 Debug.Log($"{p} -> {pos} -> {xBox}, {yBox}, {zBox}");
                if (!filledBoxes[xBox, yBox, zBox])
                {
                    filledBoxes[xBox, yBox, zBox] = true;
                    a++;
                }
            }
            return new Vector2(Mathf.Log(1f / size), Mathf.Log(a));
        }
        private static Vector2 LineSqrMin(List<Vector2> pnts)
        {
            //x^t * x
            float[,] xtx = new float[2, 2];
            for (int i = 0; i < pnts.Count; i++)
            {
                xtx[0, 1] += pnts[i].x;
                xtx[0, 0] += pnts[i].x * pnts[i].x;
            }
            xtx[1, 0] = xtx[0, 1];
            xtx[1, 1] = pnts.Count;

            //inverse
            float[,] xtxInv = new float[2, 2];
            float d = 1 / (xtx[0, 0] * xtx[1, 1] - xtx[1, 0] * xtx[0, 1]);
            xtxInv[0, 0] = xtx[1, 1] * d;
            xtxInv[0, 1] = -xtx[0, 1] * d;
            xtxInv[1, 0] = -xtx[1, 0] * d;
            xtxInv[1, 1] = xtx[0, 0] * d;

            //times x^t
            float[,] xtxInvxt = new float[2, pnts.Count];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < pnts.Count; j++)
                {
                    xtxInvxt[i, j] = xtxInv[i, 0] * pnts[j].x + xtxInv[i, 1];
                }
            }

            //times y
            float[] theta = new float[2];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < pnts.Count; j++)
                {
                    theta[i] += xtxInvxt[i, j] * pnts[j].y;
                }
            }

            return new Vector2(theta[0], theta[1]);
        }

        private static Vector3Int V3ToV3Int(Vector3 v) => new Vector3Int((int)v.x, (int)v.y, (int)v.z);
    }
}
