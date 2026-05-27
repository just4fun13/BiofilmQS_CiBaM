using Assets.Scripts.MVVM_CA.Models._2D;
using Assets.Scripts.NewGeneration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.MVVM_CA
{
    public class View2D : MonoBehaviour
    {

        [SerializeField] private GameObject HexPrefab;
        [SerializeField] private GameObject SquarePrefab;
        [SerializeField] private GameObject DiamondPrefab;
        [SerializeField] private GameObject ArrowPrefab;
        [SerializeField] private Color BacMaxColor;
        [SerializeField] private Color NutMaxColor;
        [SerializeField] private Color AhlMaxColor;
        [SerializeField] private Color ClrSelColor;
        [SerializeField] private float NutrientLevelCount = 5f;

        [SerializeField] private Color FrontCellsColor = Color.magenta;

        [SerializeField] private GameObject CircleObj;


        public bool ShowNutrient = true, ShowLactonas = false, ShowBacteria = true, ShowAHL = true, ShowEps = true;


        private bool InitedWithGO = false;
        private bool InitedWithTex = false;
        
        private CellGeometry geometryBase;
        private Texture2D texture;
        private Sprite sprite;
        private SpriteRenderer spriteRenderer;

        private int V(bool b) => b ? 1 : 0;
        private Vector3Int vx2d => new Vector3Int(V(ShowNutrient), V(ShowBacteria), V(ShowAHL));

        private GameObject prefab;
        private Func<Vector2Int, Vector2> GetPos;
        SpriteRenderer[,] visualSprs;
        private int AreaWidth, AreaHeight;
        private GameObject tempObject;

        private double InitNutrVal = 1f;

        public static Color GetRainbowColor(double value) => Rainbow.GetRainbowColor(value);


        public void ShowNutr(bool b) =>   ShowNutrient = b;
        public void ShowBac(bool b) => ShowBacteria = b;
        public void ShowAhl(bool b) => ShowAHL = b;
        public void ShowLactonase(bool b) => ShowLactonas = b;
        public void ShowEPS(bool b) => ShowEps = b;


        public void SetGlobalPosition(Vector2 pos)
        {
            tempObject.transform.position = pos;    
        }

        public void Init(ModelType model2DType, Func<Vector2Int, Vector2> GetPosAction, int W, int H, double initNut, bool UseTexMode = false)
        {
            InitNutrVal = initNut;
            if (tempObject != null)
                Destroy(tempObject);
            AreaWidth = W;
            AreaHeight = H;
            GetPos = GetPosAction;
            tempObject = new GameObject();

            if (UseTexMode)
            {
                if (model2DType == ModelType.Hexagon)
                    Debug.LogError($"Hex grid isn't best for texture mode, may cause some unexpected behaviours.");
                InitedWithTex = true;
                InitTexture();
            }
            else
            {
                InitedWithGO = true;
                InitWithGameObjects(model2DType);
            }

        }


        public void InitWithGameObjects(ModelType model2DType)
        {
            visualSprs = new SpriteRenderer[AreaWidth, AreaHeight];
            tempObject.transform.position = transform.position;
            switch (model2DType)
            {
                case ModelType.SimpleSquare:
                    prefab = SquarePrefab;
                    break;
                case ModelType.ExtendedSquare:
                    prefab = SquarePrefab;
                    break;
                case ModelType.Diamond:
                    prefab = DiamondPrefab;
                    break;
                case ModelType.Hexagon:
                    prefab = HexPrefab;
                    break;
            }
            int simLayer = LayerMask.NameToLayer("Water");
            for (int i = 0; i < AreaWidth; i++) 
                for (int j = 0; j < AreaHeight; j++)
                {
                    GameObject newObject = Instantiate(prefab, GetPos(new Vector2Int(i, j)), Quaternion.identity, tempObject.transform );
                    newObject.layer = simLayer;
                    visualSprs[i, j] = newObject.GetComponent<SpriteRenderer>();
                    visualSprs[i, j].color = Color.white;
                }
        }

        private void InitTexture()
        {
            texture = new Texture2D(AreaWidth, AreaHeight);

            Color[] colorArray = new Color[AreaWidth * AreaHeight];
            for (int i = 0; i < colorArray.Length; i++)
            {
                colorArray[i] = Color.white;
            }

            texture.SetPixels(colorArray);
            texture.Apply();
            
            // Создаём спрайт из текстуры
            sprite = Sprite.Create(texture, new Rect(0, 0, AreaWidth, AreaHeight), new Vector2(0.5f, 0.5f));
            tempObject.transform.localScale = Vector3.one * 100;
            tempObject.transform.position = Vector2.right * AreaWidth / 2 + Vector2.up * AreaHeight/2;
            // Получаем или добавляем компонент SpriteRenderer
            spriteRenderer = tempObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;

            // Устанавливаем позицию объекта
            transform.position = new Vector3(0, 0, 0); // Можно задать любую позицию
        }

        public void InitWithOffset(ModelType model2DType, Func<Vector2Int, Vector2> GetPosAction, int W, int H, double initNut, float sideOffsetValue)
        {
            InitNutrVal = initNut;
            if (tempObject != null)
                Destroy(tempObject);
            AreaWidth = W;
            AreaHeight = H;
            visualSprs = new SpriteRenderer[AreaWidth, AreaHeight];
            GetPos = GetPosAction;
            tempObject = new GameObject();
            tempObject.transform.position = transform.position;
            switch (model2DType)
            {
                case ModelType.SimpleSquare:
                    prefab = SquarePrefab;
                    break;
                case ModelType.ExtendedSquare:
                    prefab = SquarePrefab;
                    break;
                case ModelType.Diamond:
                    prefab = DiamondPrefab;
                    break;
                case ModelType.Hexagon:
                    prefab = HexPrefab;
                    break;
            }
            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                {
                    GameObject newObject = Instantiate(prefab, GetPos(new Vector2Int(i, j)), Quaternion.identity, tempObject.transform);
                    visualSprs[i, j] = newObject.GetComponent<SpriteRenderer>();
                    visualSprs[i, j].color = Color.white;
                }
            tempObject.transform.position = Vector3.right * sideOffsetValue;
        }

        public void InitForDIffusionGameObjects(int W, int H, CellGeometry base2D)
        {
            if (tempObject != null)
                Destroy(tempObject);
            AreaWidth = W;
            AreaHeight = H;
            visualSprs = new SpriteRenderer[AreaWidth, AreaHeight];
            tempObject = new GameObject();
            geometryBase = base2D;
            switch (base2D.gridType)
            {
                case CellularAutomaton.GridType.Hexagone:
                    prefab = HexPrefab;
                    break;
                default:
                    prefab = SquarePrefab;
                    break;
            }
            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                {
                    GameObject newObject = Instantiate(prefab, base2D.GetPosition(new Vector2Int(i, j)), Quaternion.identity, tempObject.transform);
                    newObject.name = $"[{i},{j}]";
                    visualSprs[i, j] = newObject.GetComponent<SpriteRenderer>();
                    visualSprs[i, j].color = Color.white;
                }
            tempObject.transform.position = transform.position;
        }

        public void SetScale(float scale)
        {
            foreach (SpriteRenderer renderer in visualSprs)
                renderer.transform.localScale = Vector3.one * scale;
        }


        public void UpdateVisual(Model2D model)
        {
            if (model is Model2DWithAHL)
                UpdateVisualAHL(model);
            else
                UpdateVisualNoAhl(model);
        }

        public void UpdateVisualAHL(Model2D model)
        {
            double[,] Bac = model.Bacteria2D;
            double[,] Nutrient = model.Substrate2D;
            double[,] AHL = model.Ahl2D;
            float ahlScaler = 10f;

            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    if (false && Bac[i, j] >= 1)
                    {
                        if (InitedWithGO)
                            visualSprs[i, j].color = Color.cyan;
                        else
                            texture.SetPixel(i, j, Color.cyan);
                        continue;
                    }
                    Color col, colB;
                    colB = Color.Lerp(ClrSelColor, BacMaxColor, (float)(Bac[i, j]));

                    Color colN = Color.Lerp(ClrSelColor, NutMaxColor, (float)(Nutrient[i, j]/InitNutrVal));
                    Color colA = Color.Lerp(Color.black, AhlMaxColor, (float)Math.Log(1 + AHL[i, j]) );
                    if (ShowNutrient)
                        col = colN;
                    else
                        col = ClrSelColor;
                    if ((float)(Bac[i, j]) > 0.0001 && ShowBacteria)
                        col = colB;
                    col.r = Mathf.Min(col.r + vx2d.z * colA.r, 1);
                    col.g = Mathf.Min(col.g + vx2d.z * colA.g, 1);
                    col.b = Mathf.Min(col.b + vx2d.z * colA.b, 1);
                    col.a = 1;
                    if (InitedWithGO)
                        visualSprs[i, j].color = col;
                    else
                        texture.SetPixel(i, j, col);
                }
            if (InitedWithTex)
                texture.Apply();
        }

        public void UpdateVisualAHL_Slice(Model3D model)
        {
            int w = model.U3D.GetLength(0);
            int h = model.U3D.GetLength(1);
            int l = model.U3D.GetLength(2);



            double[,] Nutrient = new double[w, h];
            double[,] Bac = new double[w, h];
            double[,] AHL = new double[w, h];
            double Uth = model.AHLthreshold;

            int lx = l / 2;
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    Nutrient[i, j] = model.S3D[i, j, lx];
                    Bac[i, j] = model.C3D[i, j, lx];
                    AHL[i, j] = model.U3D[i, j, lx];
                }

            //UpdateVisualAHL((Model2D)model);
        }

        public void UpdateVisualNoAhl(Model2D model)
        {
            double[,] Bac = model.Bacteria2D;
            double[,] Nutrient = model.Substrate2D;

            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    if (false && Bac[i, j] >= 1)
                    {
                        if (InitedWithGO)
                            visualSprs[i, j].color = Color.cyan;
                        else
                            texture.SetPixel(i, j, Color.cyan);
                        continue;
                    }
                    Color col = Color.white;
                    Color colB = Color.Lerp(ClrSelColor, BacMaxColor, (float)(Bac[i, j]));
                    Color colN = Color.Lerp(ClrSelColor, NutMaxColor, (float)(Nutrient[i / model.NutrGridSimpl, j / model.NutrGridSimpl] / InitNutrVal));
                    if (ShowNutrient)
                        col = colN;
                    else
                        col = ClrSelColor;
                    if ((float)(Bac[i, j]) > 0.0001 && ShowBacteria)
                        col = colB;
                    col.a = 1;
                    if (InitedWithGO)
                        visualSprs[i, j].color = col;
                    else
                        texture.SetPixel(i, j, col);
                }
            if (InitedWithTex)
                texture.Apply();
        }

        public async void ShowDirs(List<Vector4> dirs)
        {
            foreach (Vector4 d in dirs)
            {
                GameObject nObj = Instantiate(ArrowPrefab, new Vector2(d.x, d.y),
                    Quaternion.Euler(0f, 0f, -90f + Mathf.Rad2Deg* Mathf.Atan2(d.w-d.y, d.z - d.x )) );
                DestroyLater( nObj );
            }
        }

        private async void DestroyLater(GameObject go)
        {
            for (int i = 0; i < 40; i++)
            {
                await Task.Yield();
            }
            Destroy(go);
        }

        public void HighlightFrontCells(List<Vector2Int> FrontCells)
        {
            foreach (Vector2Int vi in FrontCells)
                visualSprs[vi.x, vi.y].color = FrontCellsColor;
        }

        public void DrawPoints(List<Vector2> points)
        {
            foreach (Vector2 pos in points)
            Instantiate(CircleObj, pos, Quaternion.identity, transform);
        }

        public void DrawCells(CellState[,] cells)
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    if (cells[i, j] == CellState.empty)
                    {
                        Color col = Color.white;
                        col.a = 0.1f;
                        visualSprs[i, j].color = col;
                    }
                    else
                        if (cells[i, j] == CellState.busyCanDiv)
                        visualSprs[i, j].color = Color.green;
                    else
                        visualSprs[i, j].color = Color.yellow;
                }
        }

        public void DrawDiffusion(double[,] cells)
        {
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    visualSprs[i, j].color = GetRainbowColor(cells[i, j]*10d);
                }
        }

        public void DrawDelta(double[,] cells)
        {
            double mid = 0.5f;// maxDelta / 2d;
            Color col;
            for (int i = 0; i < AreaWidth; i++)
                for (int j = 0; j < AreaHeight; j++)
                {
                    if (cells[i, j] > mid)
                    {
                        col = Color.Lerp(Color.green, Color.red, (float)((cells[i, j] - mid) / mid));
                        col.a = 1;
                    }
                    //new Color((float)cells[i, j], 0, 0);
                    else
                    {
                        col = Color.Lerp(Color.blue, Color.green, (float)((cells[i, j]) / mid));
                        col.a = Mathf.Lerp(0.3f, 1f, (float)((cells[i, j]) / mid));
                    }
                    visualSprs[i, j].color = col;
                }
        }

        public void DrawPoint(Vector2 pos)
        {
            GameObject ob = Instantiate(CircleObj, pos, Quaternion.identity, transform);
            ob.GetComponent<SpriteRenderer>().color = Color.black;
            ob.transform.localScale = 0.10f * Vector3.one;
        }

        public void FitToWorldRect(float targetWidth, float targetHeight)
        {
            if (tempObject == null) return;

            // Текущий world-размер сетки ДО масштабирования.
            // Для SimpleSquare GetPosition обычно (i,j) => шаг = 1.
            // Тогда ширина = (W-1), высота = (H-1).
            float currentWidth = Mathf.Max(1, AreaWidth - 1);
            float currentHeight = Mathf.Max(1, AreaHeight - 1);

            float sx = targetWidth / currentWidth;
            float sy = targetHeight / currentHeight;

            // Масштабируем РОДИТЕЛЯ: это масштабирует и позиции, и спрайты.
            tempObject.transform.localScale = new Vector3(sx, sy, 1f);

            // Чтобы (0,0) совпадал: можно выставить позицию родителя туда же,
            // где у тебя стоит viewConcentr. Обычно достаточно:
            tempObject.transform.position = transform.position;
        }

        public void UpdateVisualFive(Model2D model)
        {
            int W = model.AreaWidth;
            int H = model.AreaHeight;

            var C = model.Bacteria2D;    // biomass (cell grid)
            var S = model.Substrate2D;   // nutrient (nutr grid)
            var U = model.Ahl2D;         // AHL (nutr grid)
            var E = model.Eps2D;         // EPS (nutr grid)
            var L = model.Lactonas2D;    // Lactonase (nutr grid)

            // --- 1) Нормировка питания относительно начального уровня ---
            // S0 = InitSubstrateCount * NutrGridSimpl^2 (как ты задаёшь в SetInitSubstrate)
            double S0 = model.InitSubstrateCount * model.NutrGridSimpl * model.NutrGridSimpl;
            if (S0 <= 1e-12) S0 = 1.0;

            // --- 2) Найдём max для остальных (можно оставить как есть) ---
            double maxC = 0, maxU = 0, maxE = 0, maxL = 0;

            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                {
                    if (C[i, j] > maxC) maxC = C[i, j];

                    int ni = i / model.NutrGridSimpl;
                    int nj = j / model.NutrGridSimpl;

                    if (ni >= 0 && nj >= 0 && ni < S.GetLength(0) && nj < S.GetLength(1))
                    {
                        if (U[ni, nj] > maxU) maxU = U[ni, nj];
                        if (E[ni, nj] > maxE) maxE = E[ni, nj];
                        if (L[ni, nj] > maxL) maxL = L[ni, nj];
                    }
                }

            if (maxC <= 0) maxC = 1;
            if (maxU <= 0) maxU = 1;
            if (maxE <= 0) maxE = 1;
            if (maxL <= 0) maxL = 1;

            // --- 3) Рендер ---
            for (int i = 0; i < W; i++)
                for (int j = 0; j < H; j++)
                {
                    int ni = i / model.NutrGridSimpl;
                    int nj = j / model.NutrGridSimpl;

                    float nutr = 0f, ahl = 0f, eps = 0f, lact = 0f;

                    if (ni >= 0 && nj >= 0 && ni < S.GetLength(0) && nj < S.GetLength(1))
                    {
                        // Nutrient нормируем по начальному
                        nutr = (float)(S[ni, nj] / S0);
                        nutr = Mathf.Clamp01(nutr);

                        // Остальные по max в кадре (как у тебя)
                        ahl = (float)(U[ni, nj] / maxU);
                        eps = (float)(E[ni, nj] / maxE);
                        lact = (float)(L[ni, nj] / maxL);

                        ahl = Mathf.Clamp01(ahl);
                        eps = Mathf.Clamp01(eps);
                        lact = Mathf.Clamp01(lact);
                    }

                    // Бактерия по биомассе
                    float bac = (float)(C[i, j] / maxC);
                    bac = Mathf.Clamp01(bac);

                    bool hasBac = C[i, j] > 0; // если хочешь порог — поставь > 1e-6

                    // --- Цветовые каналы по ТЗ ---
                    // Питание: СИНИЙ
                    // Бактерия: ЗЕЛЁНЫЙ
                    // AHL: КРАСНЫЙ
                    // Lactonase: ещё один цвет (сделаем фиолетовый через B+R)
                    // EPS: подсветка бактерий в жёлтый/золотистый (green -> yellow)

                    float r = 0f, g = 0f, b = 0f;

                    // 1) Питание и бактерии взаимоисключающие:
                    if (!hasBac && ShowNutrient)
                    {
                        // нет бактерии -> показываем питание синим
                        b += nutr;
                    }
                    else
                    {
                        // есть бактерия -> питание НЕ показываем, показываем бактерию зелёным
                        if (ShowBacteria)
                            g +=  bac;

                        // EPS подсветка бактерий: зелёный -> жёлтый/золотистый
                        // делаем добавку в R пропорционально EPS (и немного в G, чтобы было "золото")
                        // eps=0 => зелёный; eps=1 => жёлтый/золотистый
                        if (ShowEps)
                            b += eps;
                        if (ShowLactonas)
                            r += lact;
                        if (ShowAHL)
                            r += ahl;
                    }

                    // clamp
                    r = Mathf.Clamp01(r);
                    g = Mathf.Clamp01(g);
                    b = Mathf.Clamp01(b);

                    // Альфа: можно использовать EPS как "плотность" (или просто 1)
                    float a = 1f;

                    Color col = new Color(r, g, b, a);

                    if (InitedWithGO)
                        visualSprs[i, j].color = col;
                    else
                        texture.SetPixel(i, j, col);
                }

            if (!InitedWithGO)
                Apply();
        }


        void SetPixel(int x, int y, Color c)
        {
            texture.SetPixel(x, y, c);
        }

        void Apply()
        {
            texture.Apply();
        }
    }
}
