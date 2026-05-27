using System;
using System.Collections.Generic;
using UnityEngine;

public static class GenDist
{
    static System.Random rand = new System.Random();

    public static List<Vector2Int> GetPoints(int AreaWidth, int pointsCount)
    {
        int N = pointsCount; // количество точек
        int iterations = 1000; // количество итераций
        double step = 0.002; // шаг смещения
        double boxSize = 1.0; // размер области

        Vector2[] points = InitializeRandomPoints(N);
        RepulsionLayout(points, boxSize, step, iterations);


        List<Vector2Int> result = new List<Vector2Int>();
        for (int i = 0; i < points.Length; i++)
        {
            int x = (int) (points[i].x * AreaWidth);
            int y = (int) (points[i].y * AreaWidth);
            Vector2Int vi = new Vector2Int(x, y);
            result.Add(vi);
        }

        return result;
    }

    static Vector2[] InitializeRandomPoints(int N)
    {
        Vector2[] points = new Vector2[N];
        for (int i = 0; i < N; i++)
        {
            points[i] = new Vector2(
                (float)(0.05 + 0.9 * rand.NextDouble()),
                (float)(0.05 + 0.9 * rand.NextDouble())
            );
        }
        return points;
    }

    static void RepulsionLayout(Vector2[] points, double boxSize, double step, int iterations)
    {
        int N = points.Length;

        for (int iter = 0; iter < iterations; iter++)
        {
            Vector2[] forces = new Vector2[N];

            // Силы отталкивания между точками
            for (int i = 0; i < N; i++)
            {
                for (int j = i + 1; j < N; j++)
                {
                    double dx = points[i].x - points[j].x;
                    double dy = points[i].y - points[j].y;
                    double distSq = dx * dx + dy * dy;
                    if (distSq < 1e-6) distSq = 1e-6;

                    double f = 1.0 / distSq;
                    forces[i].x += (float)(f * dx);
                    forces[i].y += (float)(f * dy);
                    forces[j].x -= (float)(f * dx);
                    forces[j].y -= (float)(f * dy);
                }
            }

            // Силы отталкивания от стенок
            for (int i = 0; i < N; i++)
            {
                float x = points[i].x;
                float y = points[i].y;

                forces[i].x += (float)(1.0 / x); // слева
                forces[i].x -= (float)(1.0 / (boxSize - x)); // справа

                forces[i].y += (float)(1.0 / y); // сверху
                forces[i].y -= (float)(1.0 / (boxSize - y)); // снизу
            }

            // Обновление позиций
            for (int i = 0; i < N; i++)
            {
                points[i].x += (float)(step * forces[i].x);
                points[i].y += (float)(step * forces[i].y);

                // Ограничение: точки внутри области
                points[i].x = Math.Max(0.01f, Math.Min(0.99f, points[i].x));
                points[i].y = Math.Max(0.01f, Math.Min(0.99f, points[i].y));
            }
        }
    }
}