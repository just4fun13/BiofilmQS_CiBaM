using Assets.Scripts.NewGeneration;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AnalSqr : IReferenceField
{
    public string Name => "Analytic1_sin_sin";
    public double MaxTime => double.PositiveInfinity;
    private List<Vector2Int> pointsToCompare = new List<Vector2Int>();
    public List<Vector2> PointsToCompare => null;// pointsToCompare;


    public double DiffusionKoef = 0.1;

    private CellGeometry base2D;
    private double Lx;
    private double Ly;

    public void BindGrid(CellGeometry base2D, int width, int height)
    {
        this.base2D = base2D;
        int h = 10;
        for (int i = h; i < width - h; i += h)
            for (int j = h; j < height - h; j += h)
                pointsToCompare.Add(new Vector2Int(i, j));

        // Берём крайние точки сетки и считаем "длину" по x и y
        // тут можно варьировать, но идея такая:
        Vector2 pRight = base2D.GetPosition(new Vector2Int(width - 1, 0));
        Vector2 pTop = base2D.GetPosition(new Vector2Int(0, height - 1));

        Lx = pRight.x;  // предполагаем, что слева x≈0, справа x≈Lx
        Ly = pTop.y;    // снизу y≈0, сверху y≈Ly
    }

    public bool TryGetValue(Vector2Int cell, double time, out double value)
    {
        if (base2D == null || Lx <= 0 || Ly <= 0)
        {
            value = 0;
            return false;
        }

        Vector2 pos = base2D.GetPosition(cell);

        double xNorm = pos.x / Lx;   // нормировка в [0,1]
        double yNorm = pos.y / Ly;

        // немного подстрахуемся от численных артефактов
        if (xNorm < 0 || xNorm > 1 || yNorm < 0 || yNorm > 1)
        {
            value = 0;
            return false;
        }

        double factor = Math.Exp(-2.0 * Math.PI * Math.PI * DiffusionKoef * time);
        value = factor * Math.Sin(Math.PI * xNorm) * Math.Sin(Math.PI * yNorm);
        return true;
    }

    public double GetDelta(double time, double[,] C)
    {
        throw new NotImplementedException();
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
