


using Assets.Scripts.MVVM_CA.Analytics;
using CellularAutomaton;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.ImageAnalyze
{
    public class DrawLineTool : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] lineRenderers;

        [SerializeField] private float ScaleY = 0.1f;

        public static DrawLineTool instance;

        private void Awake()
        {
            //DrawLine(ImageToArrayLoader.LayerConc, lineRenderers[0]);
            if (instance != null)
            {
                Debug.LogError($"There are too many DrawLineTool scripts on the scene!");
                Destroy(this);
            }
            else
            {
                instance = this;
            }
        }

        private const float zOffset = 1000;
        private const float sideStep = 0.1f;

        public void DrawLine(int i, double[] data) => DrawLine(data, lineRenderers[i]);
        public void AddPointToLine(int i, float point) => AddPointToLine(point, lineRenderers[i]);
        public static void AddLineZero(float val)
        {
            instance.AddPointToLine(0, val);
        }
        private void DrawLine(double[] data, LineRenderer lr)
        {
            lr.positionCount = data.Length;
            for (int i = 0; i < data.Length; i++)
            {
                float x = i * 1f / 10f;
                lr.SetPosition(i, new Vector3(x, (float)data[i] * ScaleY, zOffset));
            }
        }
        private void AddPointToLine(float v, LineRenderer lr)
        {
            lr.positionCount++;
            lr.SetPosition(lr.positionCount - 1, new Vector3( sideStep*(lr.positionCount - 1), v, zOffset));
        }
        public void ShowExp(double[,] data)
        {
            double[] d1 = new double[data.GetLength(1)];
            double[] d2 = new double[data.GetLength(1)];
            double[] d3 = new double[data.GetLength(1)];
            double[] d4 = new double[data.GetLength(1)];

            for (int i = 0; i < d1.Length; i++)
            {
                d1[i] = data[0, i];
                d2[i] = data[1, i];
                d3[i] = data[2, i];
                d4[i] = data[3, i];
            }

            DrawLine(0,d1);
            DrawLine(1,d2);
            DrawLine(2,d3); 
            DrawLine(3,d4);
        }
        public void ShowDif()
        {
            int totalCompParts = 4;
            string er = "GraphDif : ";
            for (int j = 0; j < totalCompParts; j++)
            {
                //if (lineRenderers[j].positionCount != lineRenderers[j + totalCompParts].positionCount)
                //    continue;
                float dif = 0;
                int iMax = lineRenderers[totalCompParts + j].positionCount;
                for (int i = 0; i < iMax; i++)
                {
                    dif += Mathf.Abs(lineRenderers[j].GetPosition(i).y - lineRenderers[j + totalCompParts].GetPosition(i).y);
                }
                dif = dif * 1f / iMax;
                er += $"[{j}:{dif.ToString("0.000")}],   ";
            }
            Debug.Log(er);
        }
        public static void DrawLine(List<Vector2> points, LineRenderer lr, float scaleX, float scaleY)
        {
            lr.positionCount = points.Count;
            for (int i =0; i<points.Count; i++)
            {
                lr.SetPosition(i, new Vector3(points[i].x/scaleX * 2, points[i].y/scaleY, 99));
            }
        }

        public static void DrawLines34(List<Vector2> biomassLine, List<Vector2> ahlLine, float uTH)
        {
            DrawLine(biomassLine, instance.lineRenderers[2], 50, MaxY(biomassLine));
//            DrawLine(ahlLine, instance.lineRenderers[3], 50, MaxY(ahlLine));
            DrawLine(ahlLine, instance.lineRenderers[3], 50, 1);

            //DrawLine(HorLine(uTH / MaxY(ahlLine)), instance.lineRenderers[4], 50, MaxY(ahlLine));
            DrawLine(HorLine(uTH) , instance.lineRenderers[4], 50, 1 );
        }

        private static List<Vector2> HorLine(float v)
        {
            List<Vector2> list = new List<Vector2>();
            list.Add(new Vector2(0, v));
            list.Add(new Vector2(50, v));
            return list;
        }


        private static float MaxY(List<Vector2> list)
        {
            if (list == null || list.Count == 0)
            {
                Debug.LogError("List is empty");
                return -1;
            }
            float maxY = list[0].y;
            foreach (Vector2 v in list)
                if (v.y > maxY)
                    maxY = v.y;
            return maxY;
        }
       

    }
}
