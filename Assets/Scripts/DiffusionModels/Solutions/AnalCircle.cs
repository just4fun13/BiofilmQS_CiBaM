using Assets.Scripts.NewGeneration;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class AnalCircle : IReferenceField
{
    public string Name => "Analytic_Circle_Accum_To_10";
    public double MaxTime => double.PositiveInfinity;
    private List<Vector2Int> pointsToCompare = new List<Vector2Int>();
    public List<Vector2Int> PointsToCompare => pointsToCompare;

    List<Vector2> IReferenceField.PointsToCompare => throw new NotImplementedException();

    public double DiffusionKoef = 0.1;   // D из модели
    public double ReactionK = 1.0;       // k из модели
    public int NeighborCount = 6;        // для гекса
    public double TimeScaleK = 4.0;      // то, что ты ставишь в model.SetTimeScaleK

    private CellGeometry base2D;
    private Vector2 center;
    private double radius;

    private const double alpha1 = 2.4048255576957727686; // первый нуль J0

    public void BindGrid(CellGeometry base2D, int width, int height)
    {
        this.base2D = base2D;

        Vector2 leftDown = base2D.GetPosition(Vector2Int.zero);
        Vector2 rightTop = base2D.GetPosition(new Vector2Int(width - 1, height - 1));
        int h = 10;
        for (int i = h; i < width - h; i += h)
            for (int j = h; j < height - h; j += h)
                pointsToCompare.Add(new Vector2Int(i, j));
        center = 0.5f * (leftDown + rightTop);
        float rx = (rightTop.x - leftDown.x) * 0.5f;
        float ry = (rightTop.y - leftDown.y) * 0.5f;
        radius = Math.Min(rx, ry);
    }

    public bool TryGetValue(Vector2Int cell, double time, out double value)
    {
        if (base2D == null || radius <= 0)
        {
            value = 0;
            return false;
        }

        Vector2 pos = base2D.GetPosition(cell);
        double r = (pos - center).magnitude;
        double rNorm = r / radius; // 0..1

        if (rNorm > 1.0)
        {
            value = 10.0; // вне круга считаем граничное значение
            return true;
        }

        // Бессель
        double j0 = BesselJ0(alpha1 * rNorm);

        // Эффективный коэффициент диффузии
        double D_eff = DiffusionKoef * (TimeScaleK / NeighborCount);

        // λ = D_eff * (α1^2 / R^2) + k
        double lambda = D_eff * (alpha1 * alpha1) / (radius * radius) + ReactionK;

        // u(r,t) = 10 - 10 J0(α1 r/R) e^{-λ t}
        value = 10.0 - 10.0 * j0 * Math.Exp(-lambda * time);
        return true;
    }

    private double BesselJ0(double z)
    {
        double zz = z * z / 4.0;
        double term = 1.0;
        double sum = 1.0;

        for (int k = 1; k <= 20; k++)
        {
            term *= -zz / (k * k);
            sum += term;
        }

        return sum;
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
