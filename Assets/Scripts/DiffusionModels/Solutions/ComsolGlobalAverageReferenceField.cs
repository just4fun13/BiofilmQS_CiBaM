using Assets.Scripts.NewGeneration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// COMSOL global reference: time -> average concentration over domain.
/// File format (tab-separated):  t<TAB>avgC
/// Optionally can have header line.
/// </summary>
public class ComsolGlobalAverageReferenceField : IReferenceField
{
    public string Name => name;
    public double MaxTime => maxTime;

    private readonly string fullPath;
    private readonly string name;
    private readonly double maxTime;

    // Stored curve
    private readonly List<double> times = new List<double>();
    private readonly List<double> avgValues = new List<double>();

    // If you still want to keep "points to compare" for compatibility,
    // we return empty list (global signal).
    private readonly List<Vector2Int> pointsToCompare = new List<Vector2Int>();
    public List<Vector2Int> PointsToCompare => pointsToCompare;

    List<Vector2> IReferenceField.PointsToCompare => throw new NotImplementedException();

    private CellGeometry base2D;
    private int gridWidth;
    private int gridHeight;

    public ComsolGlobalAverageReferenceField(string name, string fullPath, double maxTime)
    {
        this.name = name;
        this.fullPath = fullPath;
        this.maxTime = maxTime;

        LoadRawData();
    }

    private void LoadRawData()
    {
        times.Clear();
        avgValues.Clear();

        using (var reader = new StreamReader(fullPath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // Support: tab as requested, plus fallback to ';' if needed.
                string[] parts = line.Split(',');
                if (parts.Length < 2)
                    parts = line.Split(';');

                if (parts.Length < 2)
                    continue;

                if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double t))
                    continue;
                if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                    continue;

                times.Add(t);
                avgValues.Add(v);
            }
        }
        Debug.Log($"ComsolGlobalAverageReferenceField[{name}]: loaded {times.Count} rows from {fullPath}");
    }

    public void BindGrid(CellGeometry base2D, int width, int height)
    {
        this.base2D = base2D;
        this.gridWidth = width;
        this.gridHeight = height;
        // Nothing to remap (global signal)
    }

    /// <summary>
    /// Returns COMSOL avgC(t) for any cell (global value).
    /// </summary>
    public bool TryGetValue(Vector2Int cell, double time, out double value)
    {
        value = 0.0;
        return false;
    }

    /// <summary>
    /// Gets reference average value at time t using linear interpolation.
    /// </summary>


    public double GetDelta(double time, double[,] C)
    {
        double sum = 0;
        int count = 0;
        for (int i = 0; i < C.GetLength(0); i++)
        {
            for (int j = 0; j < C.GetLength(1); j++)
            {
                sum += C[i, j];
                count++;
            }
        }
        sum = sum * 1d / count;
        int index = (int)time;
        if (index < 0 || index >= avgValues.Count)
        {
            Debug.LogError($"Unexpected index value in GetDelta {index}, while array size is {avgValues.Count}");
            return -100.0;
        }
        return Math.Abs(avgValues[index]-sum);
    }

    public bool TryGetValue(Vector2 point, double time, out double value)
    {
        throw new NotImplementedException();
    }

    public Vector2Int GetId(Vector2 points)
    {
        throw new NotImplementedException();
    }
}
