using Assets.Scripts.MVVM_CA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CellularAutomaton
{
    public class BoxCountingMachine
    {
        private static float minSize = 1.05f;
        private static float maxSize = 12.3f;
        private static float deltaSize = 0.3f;
/*        private static float minSize = 1.3f;
        private static float maxSize = 40.3f;
        private static float deltaSize = 0.9f;
*/

        private static float minSizeArea = 3f;
        private static float maxSizeArea = 50f;
        private static float deltaSizeArea = 7.0f;

        private static int AreaWidth = 0;
        private static int AreaHeight = 0;
        private static Vector2 minV;

        private static bool needDrawArea = false;
        private static bool needDrawLen = false;

        private static float maxDistance = 1.1f;

        public static List<Vector2> SqrDraw = new List<Vector2> ();
        public static bool Done = false;

        public static List<SqrsData> SQRS = new List<SqrsData>();

        public class SqrsData
        {
            public float size;
            public List<Vector2> points = new List<Vector2>();
            public Vector2 GraphPoint;
        }

        private static List<Vector2> DetalizeLine(List<Vector2> lentPnts)
        {
            //Debug.Log($"Start detalize : {lentPnts.Count}");
            //DrawStackDebug(lentPnts, Color.blue, 0.4f);
            int initSize = lentPnts.Count;
            List<Vector2> extendedPnts = new List<Vector2>();
            Vector2[] pnts = lentPnts.ToArray();
            for (int i = 0; i < pnts.Length-1; i++)
                for (int j = i+1; j < pnts.Length; j++)
                {
                    Vector2 p1 = pnts[i]; Vector2 p2 = pnts[j];
                    if (p1 == p2 || (p1 - p2).sqrMagnitude > maxDistance * maxDistance)
                        continue;
                    else
                        extendedPnts.Add((p1 + p2) / 2f);
                }

            foreach (Vector2 p in extendedPnts)
                lentPnts.Add(p);
            //DrawStackDebug(lentPnts, Color.green, 0.2f);
            //Debug.Log($"End detalize : {lentPnts.Count}");
            return extendedPnts;
        }

        public static Vector2 GetFractalDimension(Vector4 areaBound, List<Vector2> lenPnts)
        {
            AreaWidth  = (int) (areaBound.z - areaBound.x) + 1; 
            AreaHeight = (int) (areaBound.w - areaBound.y) + 1;
            minV = new Vector2(areaBound.x, areaBound.y);
            //lenPnts = DetalizeLine(lenPnts);
            return GetFractalDimension(lenPnts, false);
        }


        private static Vector2 GetFractalDimension(List<Vector2> stack, bool area)
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

            for (float s = start; s <= end; s+= step)
            {
                SqrsData sqrsData = new SqrsData();
                Vector2 pnt = CalcLengthBoxCount(s, stack, needDraw);
                sqrsData.size = s;
                sqrsData.points.AddRange(SqrDraw);
                sqrsData.GraphPoint = pnt;
                SQRS.Add(sqrsData);
                pnts.Add(pnt);
            }
            Vector2 ln = LineSqrMin(pnts);

            float er = 0;
            foreach (Vector2 pnt in pnts)
                er += Mathf.Abs(ln.y + ln.x * pnt.x - pnt.y);
            //Debug.Log($"ERROR = {er /pnts.Count}");
            return LineSqrMin(pnts);
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

        private static Vector2 CalcLengthBoxCount(float size, List<Vector2> pnts, bool needDraw)
        {
            int a = 0;
            int mltpl = 1;
            if (size < 1) mltpl = (int)((1 / size) + 1); 
            bool[,] filledBoxes = new bool[5000*mltpl, 5000*mltpl];
            SqrDraw.Clear();
            foreach (Vector2 p in pnts)
            {
                Vector2 pos = p - minV;
                int xBox = (int)(pos.x / size);
                int yBox = (int)(pos.y / size);
//                Debug.Log($"{p} -> {pos} -> {xBox}, {yBox}");
                if (!filledBoxes[xBox, yBox])
                {
                    filledBoxes[xBox, yBox] = true;
                    SqrDraw.Add(new Vector2(xBox, yBox));
                    if (needDraw)
                    {
                        float tm = 12.2f;
                        Debug.DrawLine(minV + new Vector2(xBox,     yBox) * size,     minV + new Vector2(xBox + 1, yBox) * size,     Color.black, tm);
                        Debug.DrawLine(minV + new Vector2(xBox + 1, yBox) * size,     minV + new Vector2(xBox + 1, yBox + 1) * size, Color.black, tm);
                        Debug.DrawLine(minV + new Vector2(xBox + 1, yBox + 1) * size, minV + new Vector2(xBox, yBox + 1) * size,     Color.black, tm);
                        Debug.DrawLine(minV + new Vector2(xBox,     yBox + 1) * size, minV + new Vector2(xBox, yBox) * size,         Color.black, tm);
                    }
                    a++;
                }
            }

            return new Vector2(Mathf.Log(1f / size), Mathf.Log(a));
        }


        public static Vector2 LineSqrMin(List<Vector2> pnts)
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

    }
}
