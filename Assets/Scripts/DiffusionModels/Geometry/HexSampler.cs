using Assets.Scripts.NewGeneration;
using System;
using System.Linq;
using UnityEngine;

public static class HexSampler
{
    /// "Билинейная" интерполяция на shifted-hex (odd-r offset),
    /// где строки сдвинуты по X на geom.offset.x * scale (в твоём случае -0.5*scale).
    public static double SampleBilinearShiftedHex(double[,] C, CellGeometry geom, Vector2 pos,
                                                  int width, int height,
                                                  float offsetXUnits = -0.5f)
    {
        if (!geom.NeedShift)
            throw new InvalidOperationException("SampleBilinearShiftedHex requires NeedShift=true (shifted hex).");

        // Шаги по "решётке индексов" в мировых координатах
        // (в твоей геометрии BaseV[0]=(1,0), BaseV[1]=(0,1), scale задаёт физический шаг)
        Vector2 p00 = geom.GetPosition(Vector2Int.zero);
        Vector2 p10 = geom.GetPosition(new Vector2Int(1, 0));
        Vector2 p01 = geom.GetPosition(new Vector2Int(0, 1));

        double hx = p10.x - p00.x; // шаг по x между центрами в одной строке
        double hy = p01.y - p00.y; // шаг по y между строками

        // Защита
        if (Math.Abs(hx) < 1e-20 || Math.Abs(hy) < 1e-20) return 0.0;

        // --- 1) Дробный индекс по Y ---
        double gy = (pos.y - p00.y) / hy;
        int j0 = (int)Math.Floor(gy);
        double ty = gy - j0;

        // Строки для интерполяции
        int j1 = j0 + 1;

        // clamp по Y, чтобы j0 и j1 существовали
        if (j0 < 0) { j0 = 0; j1 = 1; ty = 0; }
        if (j0 > height - 2) { j0 = height - 2; j1 = height - 1; ty = 1; }

        // --- 2) В каждой строке: 1D интерполяция по X с учётом сдвига строки ---
        double cRow0 = SampleRowLinear(C, pos.x, p00.x, hx, j0, width, offsetXUnits, geom.scale);
        double cRow1 = SampleRowLinear(C, pos.x, p00.x, hx, j1, width, offsetXUnits, geom.scale);

        // --- 3) Межстрочная интерполяция ---
        return cRow0 * (1.0 - ty) + cRow1 * ty;
    }

    /// Линейная интерполяция по X внутри строки j, учитывая сдвиг строки.
    private static double SampleRowLinear(double[,] C,
                                          double xWorld,
                                          double xWorldAtI0,
                                          double hx,
                                          int j,
                                          int width,
                                          float offsetXUnits,
                                          float scale)
    {
        // offsetXUnits = -0.5 для твоей сетки, переводим в мир:
        double shift = ((j & 1) != 0) ? (offsetXUnits * scale) : 0.0;

        // Дробный индекс gx в данной строке
        double gx = (xWorld - (xWorldAtI0 + shift)) / hx;
        int i0 = (int)Math.Floor(gx);
        double tx = gx - i0;

        // clamp чтобы i0 и i0+1 существовали
        if (i0 < 0) { i0 = 0; tx = 0; }
        if (i0 > width - 2) { i0 = width - 2; tx = 1; }

        double c0 = C[i0, j];
        double c1 = C[i0 + 1, j];
        return c0 * (1.0 - tx) + c1 * tx;
    }



    // Барицентрическая интерполяция в треугольнике (A,B,C) для значения f(A)=fa...
    private static double Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c,
                                      double fa, double fb, double fc)
    {
        // вычисляем барицентрические координаты
        double detT = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Math.Abs(detT) < 1e-30) return fa; // вырожденный случай

        double wA = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / detT;
        double wB = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / detT;
        double wC = 1.0 - wA - wB;

        return wA * fa + wB * fb + wC * fc;
    }

    // Проверка "точка внутри треугольника" через барицентрические (с допуском)
    private static bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c, double eps = 1e-12)
    {
        double detT = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Math.Abs(detT) < 1e-30) return false;

        double wA = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / detT;
        double wB = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / detT;
        double wC = 1.0 - wA - wB;

        return wA >= -eps && wB >= -eps && wC >= -eps;
    }

    /// Интерполяция на shifted-hex (pointy/flat не важно), используя реальную геометрию GetPosition().
    /// Метод: локальный ромб (2 треугольника) + P1.
    public static double SampleP1_Rhombus(double[,] C, CellGeometry geom, Vector2 pos, int width, int height)
    {
        if (!geom.NeedShift)
            throw new InvalidOperationException("SampleP1_Rhombus is for hex (NeedShift=true).");

        // 1) Берём ближайшую ячейку как опорную
        Vector2Int p0 = geom.GetIdOfPoint(pos);

        // clamp чтобы соседи существовали
        p0.x = Mathf.Clamp(p0.x, 0, width - 2);   // потому что будем брать p0.x+1
        p0.y = Mathf.Clamp(p0.y, 0, height - 2);  // потому что будем брать верхний ряд

        // 2) Определяем 4 вершины ромба:
        // A = (i,j)
        // B = (i+1,j)
        // верхняя пара и нижняя пара зависят от чётности строки
        int i = p0.x;
        int j = p0.y;

        // базовые точки
        Vector2Int A = new Vector2Int(i, j);
        Vector2Int B = new Vector2Int(i + 1, j);

        // В shifted-hex (смещение нечётных рядов) "верхние" соседи для A/B разные.
        // Для even row (j%2==0) обычно верхние диагонали: (i, j+1) и (i+1, j+1)
        // Для odd  row (j%2==1) обычно верхние диагонали: (i-1, j+1) и (i, j+1)
        // НО у тебя направления могут быть инвертированы — поэтому лучше определять через GetPosition:
        // Мы сделаем так: выберем два кандидата сверху и два снизу и возьмём те, что реально выше по y.

        // кандидаты "сверху" относительно A
        Vector2Int Au1 = new Vector2Int(i, j + 1);
        Vector2Int Au2 = new Vector2Int(i - 1, j + 1);
        // кандидаты "сверху" относительно B
        Vector2Int Bu1 = new Vector2Int(i + 1, j + 1);
        Vector2Int Bu2 = new Vector2Int(i, j + 1);

        // кандидаты "снизу"
        Vector2Int Ad1 = new Vector2Int(i, j - 1);
        Vector2Int Ad2 = new Vector2Int(i - 1, j - 1);
        Vector2Int Bd1 = new Vector2Int(i + 1, j - 1);
        Vector2Int Bd2 = new Vector2Int(i, j - 1);

        // helper: безопасный clamp индексов
        Vector2Int ClampId(Vector2Int v) =>
            new Vector2Int(Mathf.Clamp(v.x, 0, width - 1), Mathf.Clamp(v.y, 0, height - 1));

        Au1 = ClampId(Au1); Au2 = ClampId(Au2);
        Bu1 = ClampId(Bu1); Bu2 = ClampId(Bu2);
        Ad1 = ClampId(Ad1); Ad2 = ClampId(Ad2);
        Bd1 = ClampId(Bd1); Bd2 = ClampId(Bd2);

        Vector2 pA = geom.GetPosition(A);
        Vector2 pB = geom.GetPosition(B);

        // выбрать верхнюю точку для A: та, у которой y больше pA.y
        Vector2Int Aup = (geom.GetPosition(Au1).y > geom.GetPosition(Au2).y) ? Au1 : Au2;
        Vector2Int Bup = (geom.GetPosition(Bu1).y > geom.GetPosition(Bu2).y) ? Bu1 : Bu2;

        // выбрать нижнюю: меньший y
        Vector2Int Adn = (geom.GetPosition(Ad1).y < geom.GetPosition(Ad2).y) ? Ad1 : Ad2;
        Vector2Int Bdn = (geom.GetPosition(Bd1).y < geom.GetPosition(Bd2).y) ? Bd1 : Bd2;

        // 3) Строим два треугольника ромба:
        // верхний: (A, B, Bup) и (A, Aup, Bup) — зависит от геометрии
        // нижний: (A, B, Bdn) и (A, Adn, Bdn)
        // Мы просто проверим, в какой треугольник попала точка, и интерполируем.

        // Сначала попробуем верхнюю половину
        Vector2 pAup = geom.GetPosition(Aup);
        Vector2 pBup = geom.GetPosition(Bup);

        double cA = C[A.x, A.y];
        double cB = C[B.x, B.y];
        double cAup = C[Aup.x, Aup.y];
        double cBup = C[Bup.x, Bup.y];

        if (PointInTri(pos, pA, pB, pBup))
            return Barycentric(pos, pA, pB, pBup, cA, cB, cBup);

        if (PointInTri(pos, pA, pAup, pBup))
            return Barycentric(pos, pA, pAup, pBup, cA, cAup, cBup);

        // Если не попали сверху — пробуем низ
        Vector2 pAdn = geom.GetPosition(Adn);
        Vector2 pBdn = geom.GetPosition(Bdn);

        double cAdn = C[Adn.x, Adn.y];
        double cBdn = C[Bdn.x, Bdn.y];

        if (PointInTri(pos, pA, pB, pBdn))
            return Barycentric(pos, pA, pB, pBdn, cA, cB, cBdn);

        if (PointInTri(pos, pA, pAdn, pBdn))
            return Barycentric(pos, pA, pAdn, pBdn, cA, cAdn, cBdn);

        // fallback: ближайшая ячейка
        return C[p0.x, p0.y];
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///



    public static Vector2Int[] GetCyclicNeighbors(Vector2Int A, CellGeometry geom,
                                              Vector2Int[] offsetsEven,
                                              Vector2Int[] offsetsOdd,
                                              bool clockwise = true)
    {
        var offsets = ((A.y & 1) == 0) ? offsetsEven : offsetsOdd;

        Vector2 pA = geom.GetPosition(A);

        // Сортируем по углу вокруг A
        var ordered = offsets
            .Select(o =>
            {
                Vector2 p = geom.GetPosition(A + o);
                float ang = Mathf.Atan2(p.y - pA.y, p.x - pA.x); // [-pi..pi]
                return (o, ang);
            })
            .OrderBy(t => t.ang)
            .Select(t => t.o)
            .ToArray();

        if (clockwise)
            Array.Reverse(ordered);

        return ordered;
    }

    static bool InRange(Vector2Int id, int w, int h)
    => id.x >= 0 && id.x < w && id.y >= 0 && id.y < h;

    public static double SampleP1_ByNeighborFan(double[,] C, CellGeometry geom, Vector2 pos,
                                            int width, int height    )
    {
        Vector2Int[] offsetsEven = geom.neighbors;

        Vector2Int[] offsetsOdd = geom.shiftN;

       Vector2Int A = geom.GetIdOfPoint(pos);
        if (A.x < 0) A.x = 0; if (A.x >= width) A.x = width - 1;
        if (A.y < 0) A.y = 0; if (A.y >= height) A.y = height - 1;

        Vector2 pA = geom.GetPosition(A);
        double fA = C[A.x, A.y];

        // получаем соседей вокруг A в правильном циклическом порядке
        var nbrs = GetCyclicNeighbors(A, geom, offsetsEven, offsetsOdd, clockwise: false);

        // идём по парам соседей (B,C) = (nbr[k], nbr[k+1]) по кругу
        for (int k = 0; k < nbrs.Length; k++)
        {
            Vector2Int B = A + nbrs[k];
            Vector2Int Cc = A + nbrs[(k + 1) % nbrs.Length];

            if (!InRange(B, width, height) || !InRange(Cc, width, height))
                continue;

            Vector2 pB = geom.GetPosition(B);
            Vector2 pC = geom.GetPosition(Cc);

            if (PointInTri(pos, pA, pB, pC))
            {
                double fB = C[B.x, B.y];
                double fC = C[Cc.x, Cc.y];
                return Barycentric(pos, pA, pB, pC, fA, fB, fC);
            }
        }

        return fA;
    }


}


