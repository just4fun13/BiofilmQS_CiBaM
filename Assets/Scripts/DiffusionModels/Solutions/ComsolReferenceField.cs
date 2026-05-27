using Assets.Scripts.NewGeneration;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using UnityEngine;

public class ComsolReferenceField : IReferenceField
{
    public string Name => name;
    public double MaxTime => maxTime;
    
    public List<Vector2> PointsToCompare => pointsOfArray;

    private readonly string fullPath;

    private string name;
    private double maxTime;
    private double timeStep;

    int multix = 1000000;

    // "сырые" данные: позиция -> массив значений по времени
    private Dictionary<Vector2Int, float[]> rawData = new Dictionary<Vector2Int, float[]>();

    // скорректированные под сетку: ячейка -> массив по времени
    private Dictionary<Vector2Int, float[]> gridData = new Dictionary<Vector2Int, float[]>();
    private Dictionary<Vector2, Vector2Int> mapToRealPoints = new Dictionary<Vector2, Vector2Int>();

    public List<Vector2> pointsOfArray = new List<Vector2>();

    private CellGeometry base2D;
    private int gridWidth;
    private int gridHeight;

    float minToAvoid = 0;// 0.0012f;

    public ComsolReferenceField(string name, string fullPath, double maxTime)
    {
        this.name = name;
        this.fullPath = fullPath;
        this.maxTime = maxTime;
        
        LoadRawData();
    }

    private void LoadRawData()
    {
        rawData.Clear();
        pointsOfArray.Clear();
        using (var reader = new System.IO.StreamReader(fullPath))
        {
            int tLen = 0;

            List<float> xPoints = new List<float>();
            List<float> yPoints = new List<float>();


            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(';');
                if (parts.Length < 3) continue;
                float xVal = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                float yVal = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                float x = 0.001f * xVal;
                float y = 0.001f * yVal;
                if (!xPoints.Contains(xVal)) xPoints.Add(xVal);
                if (!yPoints.Contains(yVal)) yPoints.Add(yVal);



                if (x < minToAvoid)
                    continue;

                pointsOfArray.Add(new Vector2(x, y));

                tLen = parts.Length - 2;
                float[] u = new float[tLen];

                for (int i = 0; i < tLen; i++)
                    u[i] = float.Parse(parts[i + 2], System.Globalization.CultureInfo.InvariantCulture);

                rawData.Add(new Vector2Int(Mathf.RoundToInt(multix * x), Mathf.RoundToInt(y* multix)), u);
            }

            xPoints.Sort();
            yPoints.Sort();

            foreach (Vector2 pnt in pointsOfArray)
            {
                Vector2 point = pnt * 1000;
                //mapToRealPoints.Add(point, new Vector2Int(xPoints.IndexOf(point.x), yPoints.IndexOf(point.y)));
            }


            if (tLen > 1)
                timeStep = 10;// maxTime / (tLen - 1);
            else
                timeStep = maxTime;
        }



        Debug.Log($"ComsolReferenceField[{name}]: loaded {rawData.Count} points, dt={timeStep}");
    }

    public void BindGrid(CellGeometry base2D, int width, int height)
    {
        this.base2D = base2D;
        this.gridWidth = width;
        this.gridHeight = height;

        RemapToGrid(base2D);
    }

    /// <summary>
    /// Коррекция данных под новую сетку
    /// (аналог твоего UpdateComsolRelation / GetClosestId).
    /// </summary>
    private void RemapToGrid(CellGeometry base2D)
    {
        gridData.Clear();
        foreach (var pair in rawData)
        {
            Vector2Int key = pair.Key;
            Vector2 comsolPos = new Vector2(key.x / (float)multix, key.y / (float)multix);
            float[] uArr = pair.Value;
            Vector2Int vi = base2D.GetIdOfPoint(comsolPos);

            if (!gridData.ContainsKey(vi))
            {
                gridData.Add(vi, uArr);
            }
            else
            {
                //Debug.LogError($"Doubling closest points for id {vi}");
                continue;
            }
//            Debug.Log($"Points {comsolPos:F5} -> {vi}");
        }

        Debug.Log($"ComsolReferenceField[{name}]: mapped {gridData.Count} cells.");
    }


    private Vector2Int FindClosestCell(Vector2 pos)
    {
        float closestDist = float.MaxValue;
        Vector2Int closest = new Vector2Int(-1, -1);

        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                Vector2 cellPos = base2D.GetPosition(new Vector2Int(i, j));
                float dist = (cellPos - pos).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = new Vector2Int(i, j);
                }
            }
        }
        Debug.Log($"V={pos}->{closest.x},{closest.y}");

        if (closest.x < 0)
            Debug.LogError($"ComsolReferenceField[{name}]: couldn't find closest cell for pos={pos}");

        return closest;
    }

    public bool TryGetValue(Vector2Int cell, double time, out double value)
    {
        value = 0;

        if (!gridData.TryGetValue(cell, out var uArr))
            return false;

        double t = time / timeStep;
        int i0 = (int)Math.Floor(t);
        int i1 = Math.Min(i0 + 1, uArr.Length - 1);
        double w = t - i0; // [0..1]
        value = (1.0 - w) * uArr[i0] + w * uArr[i1];
        return true;
    }
    public bool TryGetValue(Vector2 point, double time, out double value)
    {
        value = 0;
        Vector2Int kk = new Vector2Int(Mathf.RoundToInt(multix * point.x), Mathf.RoundToInt(multix * point.y));
        if (!rawData.TryGetValue(kk, out var uArr))
            return false;

        double t = time / timeStep;
        int i0 = (int)Math.Floor(t);
        int i1 = Math.Min(i0 + 1, uArr.Length - 1);
        double w = t - i0; // [0..1]
        value = (1.0 - w) * uArr[i0] + w * uArr[i1];
        return true;
    }

    public double GetDelta(double time, double[,] C)
    {
        throw new NotImplementedException();
    }

    public Vector2Int GetId(Vector2 points)
    {
        return mapToRealPoints[points];
    }
}
