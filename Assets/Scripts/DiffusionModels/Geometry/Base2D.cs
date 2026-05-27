using CellularAutomaton;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class Base2D : MonoBehaviour
    {
        [Header("Grid geometry")]
        public GridType gridType;
        // Сдвиг для нечётных строк при NeedShift = true
        // (по умолчанию горизонтальный сдвиг на полклетки влево)
        [SerializeField] private Vector2 offset = Vector2.left * 0.5f;

        // Базисные векторы (по умолчанию ортонормальные)
        public Vector2[] BaseV = new Vector2[2]
        {
            new Vector2(1, 0),
            new Vector2(0, 1),
        };

        [Header("Hex / Rect")]
        public bool NeedShift = false; // false = прямоугольная; true = "гекс" со сдвигом строк

        [Header("Neighbours")]
        public GameObject elementPrefab;
        public Vector2Int[] neighbors = new Vector2Int[4]; // для прямоугольной / чётной строки
        public Vector2Int[] shiftN = new Vector2Int[6]; // для нечётной строки при NeedShift

        public double MinSqrWeight { get; private set; } = 10d;

        public double[] HorizontalImpact;

        
        public Dictionary<Vector2Int, double> DirSqrWeight { get; private set; } =
            new Dictionary<Vector2Int, double>();

        [Header("Scale")]
        public float scale = 1f;


        [Header("Optional: for extended square")]
        public Vector2Int[] faces4 = new Vector2Int[4];     // (±1,0),(0,±1)
        public Vector2Int[] stencil8 = new Vector2Int[8];   // faces + diagonals (если нужно)

        // --- cached geometry for even/odd rows (важно для shifted hex) ---
        private bool geomBuilt = false;
        private FaceGeom geomEven;
        private FaceGeom geomOdd;

        private struct FaceGeom
        {
            public double CellArea; // м^2 (в 2D “площадь”)
            public Dictionary<Vector2Int, double> FaceLen;   // м
            public Dictionary<Vector2Int, double> CenterDist;// м
        }

        /// <summary>Faces only: 4 for square, 6 for hex. НЕ диагонали.</summary>
        public Vector2Int[] GetFaces(Vector2Int pos)
        {
            if (!NeedShift)
            {
                // если у тебя в neighbors сейчас 8, то используй faces4
                if (faces4 != null && faces4.Length == 4) return faces4;
                return neighbors; // если neighbors уже 4
            }
            // hex: neighbors/shiftN должны быть 6
            return GetNbrs(pos);
        }

        /// <summary>Extended stencil (можно 8) — только для градиентов/лимитеров.</summary>
        public Vector2Int[] GetStencil(Vector2Int pos)
        {
            if (stencil8 != null && stencil8.Length > 0) return stencil8;
            return GetFaces(pos);
        }

        public double GetCellArea(Vector2Int pos)
        {
            EnsureGeomBuilt();
            return (NeedShift && (pos.y % 2 != 0)) ? geomOdd.CellArea : geomEven.CellArea;
        }

        public double GetFaceLength(Vector2Int pos, Vector2Int dir)
        {
            EnsureGeomBuilt();
            var g = (NeedShift && (pos.y % 2 != 0)) ? geomOdd : geomEven;
            return g.FaceLen[dir];
        }

        public double GetCenterDistance(Vector2Int pos, Vector2Int dir)
        {
            EnsureGeomBuilt();
            var g = (NeedShift && (pos.y % 2 != 0)) ? geomOdd : geomEven;
            return g.CenterDist[dir];
        }

        private void EnsureGeomBuilt()
        {
            if (geomBuilt) return;

            // берём “внутренние” точки, чтобы соседи точно существовали
            Vector2Int pe = new Vector2Int(5, 6); // even y
            Vector2Int po = new Vector2Int(5, 7); // odd  y

            geomEven = BuildFaceGeom(pe);
            geomOdd = NeedShift ? BuildFaceGeom(po) : geomEven;

            geomBuilt = true;

            Debug.Log($"[{gridType.ToString()}] CellArea even={geomEven.CellArea:F6}, odd={geomOdd.CellArea:F3}");
        }

        private FaceGeom BuildFaceGeom(Vector2Int p)
        {
            var faces = GetFaces(p);

            // собрать midpoints для каждой face-direction
            Vector2 x0 = GetPosition(p);

            var mids = new List<(Vector2Int dir, Vector2 mid, float ang)>(faces.Length);
            var dist = new Dictionary<Vector2Int, double>(faces.Length);

            foreach (var dir in faces)
            {
                Vector2 x1 = GetPosition(p + dir);
                Vector2 e = x1 - x0;
                Vector2 mid = x0 + 0.5f * e;
                float ang = Mathf.Atan2(mid.y - x0.y, mid.x - x0.x);
                mids.Add((dir, mid, ang));
                dist[dir] = e.magnitude;
            }

            // сортировка по углу вокруг x0
            mids.Sort((a, b) => a.ang.CompareTo(b.ang));

            // площадь многоугольника midpoints (shoelace)
            double area = 0.0;
            for (int i = 0; i < mids.Count; i++)
            {
                Vector2 a = mids[i].mid;
                Vector2 b = mids[(i + 1) % mids.Count].mid;
                area += (double)a.x * b.y - (double)a.y * b.x;
            }
            area = Math.Abs(area) * 0.5;

            // длина “грани” напротив dir: это ребро между соседними midpoints
            // Ребро между mid_i и mid_{i+1} соответствует face между двумя направлениями,
            // но для FVM нам нужна длина грани, перпендикулярной к dir.
            //
            // Для регулярных сеток можно безопасно принять:
            // FaceLen(dir) = длина ребра многоугольника, "связанного" с dir:
            // берём ребро, где mid(dir) является одной из вершин (среднее из двух смежных рёбер).
            var faceLen = new Dictionary<Vector2Int, double>(faces.Length);

            for (int i = 0; i < mids.Count; i++)
            {
                var cur = mids[i];
                Vector2 prev = mids[(i - 1 + mids.Count) % mids.Count].mid;
                Vector2 next = mids[(i + 1) % mids.Count].mid;

                double l1 = (cur.mid - prev).magnitude;
                double l2 = (next - cur.mid).magnitude;

                faceLen[cur.dir] = 0.5 * (l1 + l2);
            }

            return new FaceGeom
            {
                CellArea = area,
                FaceLen = faceLen,
                CenterDist = dist
            };
        }




        public void SetScale(double s)
        {
            scale = (float)s;

            DirSqrWeight.Clear();
            MinSqrWeight = double.MaxValue;

            // Достаём всевозможные направления соседей из пары точек (1,1) и (2,2)
            Vector2Int pos = new Vector2Int(0, 0);
            for (int i = 0; i < 2; i++)
            {
                pos += Vector2Int.one;
                foreach (Vector2Int nbr in GetNbrs(pos))
                {
                    if (DirSqrWeight.ContainsKey(nbr))
                        continue;

                    double sqrWeight =
                        (GetPosition(pos + nbr) - GetPosition(pos)).sqrMagnitude;

                    if (sqrWeight < MinSqrWeight)
                        MinSqrWeight = sqrWeight;

                    DirSqrWeight.Add(nbr, sqrWeight);
                }
            }
        }

        public double GetSqrWeight(Vector2Int nbr) => DirSqrWeight[nbr];

        /// <summary>
        /// Возвращает список направлений соседей для данной ячейки.
        /// </summary>
        public Vector2Int[] GetNbrs(Vector2Int pos)
        {
            if (!NeedShift)
                return neighbors;

            // Гекс-сетка: чётные строки — neighbors, нечётные — shiftN
            if (pos.y % 2 == 0)
                return neighbors;
            else
                return shiftN;
        }

        /// <summary>
        /// Прямое преобразование: индекс -> мировые координаты.
        /// </summary>
        public Vector2 GetPosition(Vector2Int id)
        {
            Vector2 p = BaseV[0] * id.x + BaseV[1] * id.y;

            if (NeedShift && (id.y % 2 != 0))
                p += offset;

            return scale * p;
        }

        /// <summary>
        /// Обратное преобразование: мировые координаты -> индекс ближайшей ячейки.
        /// Для прямоугольной сетки — ближайший узел.
        /// Для "гекса" — с учётом горизонтального сдвига нечётных строк.
        /// </summary>
        public Vector2Int GetIdOfPoint(Vector2 pos)
        {
            int x, y;

            if (!NeedShift)
            {
                // Обычная регулярная решётка:
                // pos = scale * (i * e1 + j * e2), e1=(1,0), e2=(0,1)
                // i ~ x/scale, j ~ y/scale
                float ix = pos.x / (scale * BaseV[0].x);
                float iy = pos.y / (scale * BaseV[1].y);

                x = Mathf.RoundToInt(ix);
                y = Mathf.RoundToInt(iy);
            }
            else
            {
                // "Гекс": чётные строки без сдвига, нечётные — со сдвигом offset.

                // сначала оцениваем индекс по вертикали
                float iy = pos.y / (scale * BaseV[1].y);
                y = Mathf.RoundToInt(iy);

                float xNorm = pos.x / scale;

                if (y % 2 == 0)
                {
                    // чётная строка: pos.x ≈ scale * (i * BaseV[0].x)
                    float ix = xNorm / BaseV[0].x;
                    x = Mathf.RoundToInt(ix);
                }
                else
                {
                    // нечётная строка: pos.x ≈ scale * (i * BaseV[0].x + offset.x)
                    float ix = (xNorm - offset.x) / BaseV[0].x;
                    x = Mathf.RoundToInt(ix);
                }
            }

            return new Vector2Int(x, y);
        }

        // ============================================================
        //                ТЕСТОВЫЕ МЕТОДЫ ДЛЯ ПРОВЕРКИ
        // ============================================================

        /// <summary>
        /// Удобный метод: задать scale так, чтобы область [0,areaW]x[0,areaH]
        /// была покрыта равномерной сеткой из (cellsX x cellsY) узлов,
        /// с крайними узлами на границе (0 и areaW/areaH).
        /// </summary>
        public void SetScaleForArea(float areaWidth, float areaHeight, int cellsX, int cellsY)
        {
            // чтобы узлы легли точно в [0,Area], нужен шаг Area/(N-1)
            float sx = areaWidth / Mathf.Max(1, cellsX - 1);
            float sy = areaHeight / Mathf.Max(1, cellsY - 1);

            // берём минимальный — сетка впишется в область
            float s = Mathf.Min(sx, sy);
            SetScale(s);
        }

        /// <summary>
        /// Контекстное меню в инспекторе: тестирует прямое и обратное
        /// преобразование координат для разных N = 10,20,50,51,100,101.
        /// Работает для той конфигурации Base2D, что сейчас на объекте
        /// (NeedShift/offset/BaseV).
        /// </summary>
        [ContextMenu("Test Base2D Mapping")]
        public void TestBase2DMapping()
        {
            int[] Ns = { 10, 20, 50, 51, 100, 101 };
            float areaW = 1f;
            float areaH = 1f;

            Debug.Log($"[Base2D] === TEST MAPPING START (NeedShift={NeedShift}) ===");

            foreach (int n in Ns)
            {
                // Здесь мы предполагаем квадратную сетку n x n.
                SetScaleForArea(areaW, areaH, n, n);

                int maxI = n - 1;
                int maxJ = n - 1;

                int step = Mathf.Max(1, n / 10); // не все узлы, а шагами, чтобы не заспамить лог

                bool anyError = false;

                for (int i = 0; i <= maxI; i += step)
                {
                    for (int j = 0; j <= maxJ; j += step)
                    {
                        Vector2Int id = new Vector2Int(i, j);
                        Vector2 pos = GetPosition(id);
                        Vector2Int id2 = GetIdOfPoint(pos);

                        if (id2 != id)
                        {
                            anyError = true;
                            Debug.LogWarning(
                                $"[Base2D] N={n}: id={id} -> pos={pos} -> id2={id2} (MISMATCH)");
                        }
                    }
                }

                if (!anyError)
                    Debug.Log($"[Base2D] N={n}: mapping OK for sampled points.");
            }

            // Дополнительная проверка конкретных координат (например, центр области)
            Vector2[] samplePoints =
            {
                new Vector2(0.5f, 0.5f),
                new Vector2(0.25f, 0.25f),
                new Vector2(0.75f, 0.75f),
                new Vector2(0.1f, 0.9f),
            };

            foreach (int n in Ns)
            {
                SetScaleForArea(areaW, areaH, n, n);
                Debug.Log($"[Base2D] N={n}: sampling arbitrary world points:");

                foreach (var p in samplePoints)
                {
                    Vector2Int id = GetIdOfPoint(p);
                    Vector2 posBack = GetPosition(id);
                    Debug.Log($"   p={p} -> id={id} -> posBack={posBack}");
                }
            }

            Debug.Log($"[Base2D] === TEST MAPPING END ===");
        }
    }
}
