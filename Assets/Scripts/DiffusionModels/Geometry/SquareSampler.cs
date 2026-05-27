using Assets.Scripts.NewGeneration;
using System;
using UnityEngine;

public static class SquareSampler
{
    /// Возвращает дробные индексы сетки: gx = i + tx, gy = j + ty
    /// где i,j - целые индексы, tx,ty in [0,1]
    public static void WorldToFracIndex(CellGeometry geom, Vector2 pos,
                                        out int i0, out int j0,
                                        out double tx, out double ty,
                                        int width, int height)
    {
        // (0,0) в мире
        Vector2 p00 = geom.GetPosition(Vector2Int.zero);
        Vector2 p10 = geom.GetPosition(new Vector2Int(1, 0));
        Vector2 p01 = geom.GetPosition(new Vector2Int(0, 1));

        double hx = p10.x - p00.x;
        double hy = p01.y - p00.y;

        // дробные координаты
        double gx = (pos.x - p00.x) / hx;
        double gy = (pos.y - p00.y) / hy;

        i0 = (int)Math.Floor(gx);
        j0 = (int)Math.Floor(gy);

        tx = gx - i0;
        ty = gy - j0;

        // clamp чтобы i0+1/j0+1 существовали
        if (i0 < 0) { i0 = 0; tx = 0; }
        if (j0 < 0) { j0 = 0; ty = 0; }

        if (i0 > width - 2) { i0 = width - 2; tx = 1; }
        if (j0 > height - 2) { j0 = height - 2; ty = 1; }
    }

    public static double SampleBilinear(double[,] C, CellGeometry geom, Vector2 pos,
                                    int width, int height)
    {
        if (geom.NeedShift)
            throw new InvalidOperationException("SampleBilinear is for square grid (NeedShift=false) only.");

        SquareSampler.WorldToFracIndex(geom, pos, out int i0, out int j0,
                                       out double tx, out double ty, width, height);

        double c00 = C[i0, j0];
        double c10 = C[i0 + 1, j0];
        double c01 = C[i0, j0 + 1];
        double c11 = C[i0 + 1, j0 + 1];

        double c0 = c00 * (1 - tx) + c10 * tx;
        double c1 = c01 * (1 - tx) + c11 * tx;
        return c0 * (1 - ty) + c1 * ty;
    }
}