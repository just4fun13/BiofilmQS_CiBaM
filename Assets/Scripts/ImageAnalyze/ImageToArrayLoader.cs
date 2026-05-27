using Assets.Scripts.ImageAnalyze;
using Assets.Scripts.MVVM_CA;
using Assets.Scripts.MVVM_CA.Models._2D;
using Assets.Scripts.NewGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

namespace CellularAutomaton
{
    public class ImageToArrayLoader : MonoBehaviour
    {
        [SerializeField] private Texture2D[] initBacImages;
        [SerializeField] private Texture2D[] deadBacImages;
        private Texture2D initBacImage;
        [SerializeField] private int LoadGridSize = 12;
        [SerializeField] private double LoadThreshold = 0.005f;

        [SerializeField] private static DrawLineTool drawLineTool;
        [SerializeField] private View3D view3d;
        [SerializeField] private Base3D cubeBase;


        [SerializeField] private bool AnalyazeResults = false;
        [SerializeField] private bool DrawAndCalcImages = false;

        private static double[,] valsWithoutThreshold;
        private static double[,] valsWithoutThresholdDead;


        private static double[] oridinPerc = { 0.730151,0.150508,0.059305,0.036180,0.014629,
            0.005262,0.001964,0.000689,0.000511,0.000801};

        public static double[,,] valsAll;
        public static Vector3[,,] valsAllVector;

        private double oldTh = 0f;

        public static double[] LayerConc =
        {

        /* 0.2666, 0.1819,    0.1283, 0.0958,
         0.0740, 0.0591,    0.0482, 0.0350,
         0.0273, 0.0206,    0.0147, 0.0114,
         0.0086, 0.0067,    0.0052, 0.0042,    0.0031,
         0.0024, 0.0018,    0.0014, 0.0010,
         0.0007, 0.0005,    0.0004, 0.0002,
         0.0001, 0.0001,    0.0001, 0.0001,
         0.0000, 0.0000,    0.0000, 0.0000,         0.0000,*/


            0.03253, 0.05421, 0.07917, 0.08963, 0.09024, 0.08628, 0.07783, 0.06915, 0.06242, 0.05543,
            0.04928, 0.04467, 0.03990, 0.03457, 0.03070, 0.02741, 0.02353, 0.02021, 0.01751, 0.01532
        };




        private void StartNo()
        {
            drawLineTool = GameObject.FindObjectOfType<DrawLineTool>();
            drawLineTool.DrawLine(1, LayerConc);

            if (AnalyazeResults)
                AnalyzeImages();
            //drawLineTool.DrawLine(LayerConc);
            //TestLayersArray();

        }
        public List<Vector2Int> BottomLayerToDraw(int AreaWidth)
        {
            LoadGridSize = GetGridSizeFromAreaWidth(AreaWidth);
            initBacImage = initBacImages[0];
            Texture2D tex = initBacImages[0];
            int sideOffsetToPreventArts = 2;
            int w = tex.width / LoadGridSize + 1;
            int l = tex.height / LoadGridSize + 1;
            List<Vector2Int> valsAll = new List<Vector2Int>();

            valsWithoutThreshold = ImageToArrayUtils.ColFromImage(initBacImage, LoadGridSize);
            int totalInocCount = 0;
            for (int j = sideOffsetToPreventArts; j < w- sideOffsetToPreventArts; j++)
                for (int k = sideOffsetToPreventArts; k < l- sideOffsetToPreventArts; k++)
                {
                    if (valsWithoutThreshold[j, k] > 0)
                        valsAll.Add(new Vector2Int(j, k));
                }
            for (int i = 0; i < valsAll.Count-1; i++)
                for (int j = i+1; j < valsAll.Count-1; j++)
                {
                    if (valsWithoutThreshold[valsAll[i].x, valsAll[i].y] < valsWithoutThreshold[valsAll[j].x, valsAll[j].y])
                    {
                        Vector2Int vTemp = valsAll[i];
                        valsAll[i] = valsAll[j];
                        valsAll[j] = vTemp;
                    }
                }
            Debug.Log($"Total inoculation count = {valsAll.Count}");
            return valsAll;
        }

        private int GetGridSizeFromAreaWidth(int AreaWidth)
        {
            return (int)Mathf.Pow(2, (int)Mathf.Log(1024f / AreaWidth, 2));
        }

        private static void TestLayersArray()
        {
            for (int i = 0; i < 100; i++)
            {
                int[] res = GetLayersWidth(i, LayerConc.Length);
                Debug.Log($"i = {i} -> {string.Join(' ', res)}");  
            }

        }

        public static void ShowModelErr(Model3D model)
        {
            int h = Math.Min(model.C3D.GetLength(1), LayerConc.Length);
            int w = model.C3D.GetLength(0);
            int l = model.C3D.GetLength(2);
            double lSum = 0;
            double[] lc = new double[h];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                    for (int k = 0; k < l; k++)
                    {

                        lc[i] += model.C3D[j, i, k];
                    }
 //               lc[i] *= 1d / w * l;
                lSum += lc[i];
            }
            string s = "ModelERr  : ";
            for (int i = 0; i < h; i++)
            {
                double normedV = lc[i] * 1d / lSum;
                s += $"[{normedV.ToString("0.0000")}/{LayerConc[i]}],  ";
            }
            Debug.Log(s);
        }

        private static int[] GetLayersWidth(int LayersCountO, int MaxAllCount)
        {
            int LayersCount = LayersCountO + 1;
            int[] res = new int[MaxAllCount];

            int bas = LayersCount / MaxAllCount;
            for (int i = 0; i < MaxAllCount; i++)
                res[i] = bas;

            for (int i = 0; i < LayersCount % MaxAllCount; i++)
                res[i]++;

            return res;
        }

        private static int GetLayerFromH(int h, int[] wxs)
        {
            int i = 0;
            while (i < wxs.Length)
            {
                if (h < wxs[i])
                    return i;
                i++;
                h -= wxs[i-1];
            }
            Debug.LogError($"Layer H was not ok ");
            return -1;
        }

        private static int MaxHeight(List<Vector2Int> listV2)
        {
            int maxh = -1;
            foreach (Vector2Int vi in listV2)
                if (vi.y > maxh)
                    maxh = vi.y;
            return maxh;
        }
        private static int MaxHeight3D(List<Vector3Int> listV3)
        {
            int maxh = -1;
            foreach (Vector3Int vi in listV3)
                if (vi.y > maxh)
                    maxh = vi.y;
            return maxh;
        }

        public static double ShowModelError2D(Model2D model)
        {
            int h = LayerConc.Length;
            int w = model.Bacteria2D.GetLength(0);

            double lSum = 0;
            double[] lc = new double[h];

            // max H of array numbers
            int maxH = MaxHeight(model.BiomassCells2D);//.Aggregate((v1, v2) => v1.y > v2.y ? v1 : v2).y;
            //ebug.Log($"Max H = {maxH}");
            if (maxH == -1)
            {
                Debug.Log($"{string.Join(' ', model.BiomassCells2D)}");
                return 100;
            }


            int[] wxs = GetLayersWidth(maxH, LayerConc.Length);
            string deb = "vi - ";
            foreach (Vector2Int vi in model.BiomassCells2D)
            {
                int j = vi.x;
                int i = vi.y;
                int lcIndex = GetLayerFromH(i, wxs);
                deb += $"[{j},{i}]->({lcIndex}),   ";
                lc[lcIndex] += model.Bacteria2D[j, i];
                lSum += model.Bacteria2D[j, i];
                //               lc[i] *= 1d / w * l;
            }
            double res = 0;

            //Debug.Log($"{deb}");
            string s = "ModelERr  : ";
            for (int i = 0; i < h; i++)
            {
                double normedV = lc[i] * 1d / lSum;
                s += $"MaxH = {maxH} <>  [{normedV.ToString("0.0000")}/{LayerConc[i]}],  ";
                lc[i] = normedV;
                res += Math.Abs(lc[i] - LayerConc[i]);
            }
            //Debug.Log(s + $"{Environment.NewLine}AV UHL = {model.averageUHL} + {model.BiomassCells2D.Count} ");
            drawLineTool.DrawLine(2,lc);
            //drawLineTool.AddToLine3(model.U2D[32, 0]);
            return res;
        }

        public static double ShowModelError3D(Model3D model)
        {
            int h = LayerConc.Length;
            int w = model.C3D.GetLength(0);
            int l = model.C3D.GetLength(2);

            double lSum = 0;
            double[] lc = new double[h];

            // max H of array numbers
            int maxH = MaxHeight3D(model.BiomassCells3D);//.Aggregate((v1, v2) => v1.y > v2.y ? v1 : v2).y;
            //ebug.Log($"Max H = {maxH}");
            if (maxH == -1)
            {
                Debug.Log($"{string.Join(' ', model.BiomassCells3D)}");
                return 100;
            }


            int[] wxs = GetLayersWidth(maxH, LayerConc.Length);
            string deb = "vi - ";
            foreach (Vector3Int vi in model.BiomassCells3D)
            {
                int j = vi.x;
                int i = vi.y;
                int k = vi.z;
                int lcIndex = GetLayerFromH(i, wxs);
                deb += $"[{j},{i}]->({lcIndex}),   ";
                lc[lcIndex] += model.C3D[j, i, k];
                lSum += model.C3D[j, i, k];
                //               lc[i] *= 1d / w * l;
            }
            //Debug.Log($"{deb}");
            double res = 0;
            string s = "ModelERr  : ";
            for (int i = 0; i < h; i++)
            {
                double normedV = lc[i] * 1d / lSum;
                s += $"MaxH = {maxH} <>  [{normedV.ToString("0.0000")}/{LayerConc[i]}],  ";
                lc[i] = normedV;
                res += Math.Abs(lc[i] - LayerConc[i]);
            }
            //Debug.Log(s);
            drawLineTool.DrawLine(2, lc);
            //drawLineTool.AddToLine3(model.U2D[32, 0]);
            return res;
        }


        private void AnalyzeImages()
        {
            initBacImage = initBacImages[0];
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            Texture2D tex = initBacImages[0];
            int h = initBacImages.Length;
            int w = tex.width / LoadGridSize ;
            int l = tex.height / LoadGridSize ;
            valsAll = new double[w, h, l];
            valsAllVector = new Vector3[w, h, l];

            List<Vector3> valList = new List<Vector3>();
            string s = "";
            //DisplayTexture(newTex);
            double[] layeredConcentration = new double[initBacImages.Length];
            double[] liveConc = new double[initBacImages.Length];
            double[] deadConc = new double[initBacImages.Length];
            double lSum = 0;
            for (int i = 0; i < initBacImages.Length; i++)
            {
                initBacImage = initBacImages[i];
                valsWithoutThreshold = ImageToArrayUtils.GreenFromImage(initBacImage, LoadGridSize);
                valsWithoutThresholdDead = ImageToArrayUtils.RedFromImage(deadBacImages[i], LoadGridSize);
                int kMin = (int) (l * 0.03);
                for (int j = 0; j < w; j++)
                    for (int k = kMin; k < l; k++)
                    {
                        valsAll[j, i, k] = valsWithoutThreshold[j, k] + valsWithoutThresholdDead[j, k];
                        valsAllVector[j, i, k] = new Vector3( 
                            (float) valsWithoutThresholdDead[j, k], 
                            (float) valsWithoutThreshold[j, k], 
                            (float) (valsWithoutThreshold[j, k] + valsWithoutThresholdDead[j, k]) );
                        if (valsAll[j, i, k] > LoadThreshold)
                            valList.Add(new Vector3(j, i, k));
                        layeredConcentration[i] += valsAll[j, i, k];
                        liveConc[i] += valsWithoutThreshold[j, k];
                        deadConc[i] += valsWithoutThresholdDead[j, k];
                    }
                layeredConcentration[i] *= 1d / (w * (l - kMin));
                liveConc[i] *= 1d / (w * (l - kMin));
                deadConc[i] *= 1d / (w * (l - kMin));
                lSum += layeredConcentration[i];

            }
            for (int i = 0; i < initBacImages.Length; i++)
                s += $"{(layeredConcentration[i]*1d/lSum).ToString("0.00000")}, ";

            timer.Stop();
            Debug.Log(s);
            cubeBase.SetScale(1);
            float[] imageAnalData = BoxCountingMachine3D.ModelStats3D(valsAll, LoadThreshold);

            if (DrawAndCalcImages)
                DrawResults(w, h, l);

            Debug.Log($"Readed all images in arr[{w},{h},{l}] for {timer.ElapsedMilliseconds/1000} seconds, Fd = " +
                $" {BoxCountingMachine3D.GetFractalDimension(Vector3.zero, new Vector3(w, h, l), valList)}, " +
                $" Hmax = {imageAnalData[0]}, Hfull = {imageAnalData[1]}, Cav = {imageAnalData[2]}, CountDivBot = {imageAnalData[3]}, " +
                $" BottomFill = {imageAnalData[4]}");
            drawLineTool.DrawLine(1,layeredConcentration);
            drawLineTool.DrawLine(2,liveConc);
            drawLineTool.DrawLine(3,deadConc);
        }

        public void RecalcAndRedrawTheLineByCount()
        {
            Texture2D tex = initBacImages[0];
            double[] layeredConcentration = new double[initBacImages.Length];
            double[] live = new double[initBacImages.Length];
            double[] dead = new double[initBacImages.Length];
            double[] inter = new double[initBacImages.Length];

            int h = initBacImages.Length;
            int w = tex.width / LoadGridSize;
            int l = tex.height / LoadGridSize;

            for (int i = 0; i < initBacImages.Length; i++)
            {
                int kMin = (int)(l * 0.07);
                for (int j = 0; j < w; j++)
                    for (int k = kMin; k < l-kMin; k++)
                    {
                        if (valsAll[j, i, k] > LoadThreshold)
                            layeredConcentration[i] += 0.3;
                        if (valsAllVector[j, i, k].x > LoadThreshold)
                            dead[i] += 0.3;
                        if (valsAllVector[j, i, k].y > LoadThreshold)
                            live[i] += 0.3;
                        if (valsAllVector[j, i, k].y > LoadThreshold && valsAllVector[j, i, k].x > LoadThreshold)
                            inter[i] += 0.3;
                    }
                layeredConcentration[i] *= 1d / (w * (l - kMin));
                live[i] *= 1d / (w * (l - 2 * kMin));
                dead[i] *= 1d / (w * (l - 2 * kMin));
                inter[i] *= 1d / (w * (l -2 * kMin));
            }
            drawLineTool.DrawLine(0,layeredConcentration);
            drawLineTool.DrawLine(1,live);
            drawLineTool.DrawLine(2,dead);
            drawLineTool.DrawLine(3,inter);
        }

        public void RecalcAndRedrawTheLineByConc()
        {
            Texture2D tex = initBacImages[0];
            double[] layeredConcentration = new double[initBacImages.Length];
            double[] live = new double[initBacImages.Length];
            double[] dead = new double[initBacImages.Length];
            double[] inter = new double[initBacImages.Length];

            int h = initBacImages.Length;
            int w = tex.width / LoadGridSize;
            int l = tex.height / LoadGridSize;

            for (int i = 0; i < initBacImages.Length; i++)
            {
                int kMin = (int)(l * 0.03);
                for (int j = 0; j < w; j++)
                    for (int k = kMin; k < l; k++)
                    {
                        if (valsAll[j, i, k] > LoadThreshold)
                            layeredConcentration[i] += valsAll[j, i, k];
                        if (valsAllVector[j, i, k].x > LoadThreshold)
                            dead[i] += valsAllVector[j, i, k].x;
                        if (valsAllVector[j, i, k].y > LoadThreshold)
                            live[i] += valsAllVector[j, i, k].x;
                        if (valsAllVector[j, i, k].y > LoadThreshold && valsAllVector[j, i, k].x > LoadThreshold)
                            inter[i] += valsAllVector[j, i, k].x + valsAllVector[j, i, k].y;
                    }
                layeredConcentration[i] *= 1d / (w * (l - kMin));
                live[i] *= 1d / (w * (l - kMin));
                dead[i] *= 1d / (w * (l - kMin));
                inter[i] *= 1d / (w * (l - kMin));
            }
            drawLineTool.DrawLine(1,layeredConcentration);
            drawLineTool.DrawLine(2,live);
            drawLineTool.DrawLine(3,dead);
            drawLineTool.DrawLine(4,inter);
        }

        public void DrawResults(int w, int h, int l)
        {
            view3d.InitN(w, h, l, cubeBase);
            view3d.DrawExpData(valsAll, LoadThreshold);
        }

        private void UpdateNo()
        {
            if (oldTh != LoadThreshold)
            {
                oldTh = LoadThreshold;
                view3d.DrawExpData(valsAll, LoadThreshold);
            }
        }

        public Vector2Int GetSize()
        {
            return new Vector2Int( initBacImage.width  / LoadGridSize + 1,
                                   initBacImage.height / LoadGridSize + 1);
        }

        public void InitModelWithImage(Model model)
        {
            valsWithoutThreshold = ImageToArrayUtils.ColFromImage(initBacImage, LoadGridSize);
           // model.InitInoculateFromFile(valsWithoutThreshold, LoadThreshold);
        }

        public static string GetArrayDelta(Model model)
        {
            double[] normDataModel = NormalizedData(PrepareToNorm((model as Model3D).C3D));
            double[] delta = new double[normDataModel.Length];
            string s = "Delta = ";
            for (int i = 0; i < normDataModel.GetLength(0); i++)
            {
                delta[i] = Math.Abs(normDataModel[i] - oridinPerc[i]);
                s += "("+ normDataModel[i].ToString("0.0000") + "/" + oridinPerc[i].ToString("0.0000") 
             + "=" + delta[i].ToString("0.0000") + ")  " + ";";
            }
            return s;
/*            Debug.Log($"{s}");
            return delta;
*/        }

        public static double Sum(double[,] dat)
        {
            double s = 0;
            foreach (double d in dat)
                s += d; 
            return s;
        }

        public static double[] NormalizedData(double[,] dat)
        {
            int[] nD = new int[10];
            foreach (double d in dat)
            {
                if (d < 0.1)
                {
                    nD[0]++;
                    continue;
                }
                if (d < 0.2)
                {
                    nD[1]++;
                    continue;
                }
                if (d < 0.3)
                {
                    nD[2]++;
                    continue;
                }
                if (d < 0.4)
                {
                    nD[3]++;
                    continue;
                }
                if (d < 0.5)
                {
                    nD[4]++;
                    continue;
                }
                if (d < 0.6)
                {
                    nD[5]++;
                    continue;
                }
                if (d < 0.7)
                {
                    nD[6]++;
                    continue;
                }
                if (d < 0.8)
                {
                    nD[7]++;
                    continue;
                }
                if (d < 0.9)
                {
                    nD[8]++;
                    continue;
                }
                nD[9]++;
            }
            double[] procents = new double[10];
            for (int i = 0; i < nD.Length; i++)
                procents[i] = nD[i]*1f/(dat.GetLength(0)*dat.GetLength(1));
            return procents;
        }
        public static double[,] PrepareToNorm(double[,,] dat)
        {
            double[,] preparedDat = new double[dat.GetLength(0), dat.GetLength(2)];
            double max = -10;
            for (int i = 0; i < dat.GetLength(0); i++)
                for (int j = 0; j < dat.GetLength(2); j++)
                {

                    for (int k = 0; k < dat.GetLength(1); k++)
                        preparedDat[i, j] += dat[i, k, j];
                    if (preparedDat[i, j] > max)
                        max = preparedDat[i, j];
                }
            for (int i = 0; i < dat.GetLength(0); i++)
                for (int j = 0; j < dat.GetLength(2); j++)
                    preparedDat[i, j] /= max;


            return preparedDat;
        }
        public static double[] NormalizedData(double[,,] dat)
        {
            int[] nD = new int[10];
            foreach (double d in dat)
            {
                if (d < 0.1)
                {
                    nD[0]++;
                    continue;
                }
                if (d < 0.2)
                {
                    nD[1]++;
                    continue;
                }
                if (d < 0.3)
                {
                    nD[2]++;
                    continue;
                }
                if (d < 0.4)
                {
                    nD[3]++;
                    continue;
                }
                if (d < 0.5)
                {
                    nD[4]++;
                    continue;
                }
                if (d < 0.6)
                {
                    nD[5]++;
                    continue;
                }
                if (d < 0.7)
                {
                    nD[6]++;
                    continue;
                }
                if (d < 0.8)
                {
                    nD[7]++;
                    continue;
                }
                if (d < 0.9)
                {
                    nD[8]++;
                    continue;
                }
                nD[9]++;
            }
            double[] procents = new double[10];
            for (int i = 0; i < nD.Length; i++)
                procents[i] = nD[i] * 1d / (dat.GetLength(0) * dat.GetLength(1) * dat.GetLength(2));
            return procents;
        }

        private Texture2D ColorArrayToTexture(Color[,] colorArray)
        {
            // Проверяем размер массива
            int width = colorArray.GetLength(0);
            int height = colorArray.GetLength(1);

            // Создаем новую текстуру
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Заполняем пиксели текстуры из массива цветов
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, colorArray[x, y]);
                }
            }

            // Применяем изменения к текстуре
            texture.Apply();
            return texture;
        }
        void DisplayTexture(Texture2D texture)
        {
            // Создаем объект Sprite из текстуры
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);

            // Создаем новый GameObject с компонентом SpriteRenderer
            GameObject spriteObject = new GameObject("GeneratedSprite");
            spriteObject.AddComponent<SpriteRenderer>().sprite = sprite;

            // Устанавливаем позицию объекта (по центру камеры)
            spriteObject.transform.position = Vector3.zero;
        }
        void SaveTextureToFile(Texture2D texture)
        {
            // Преобразуем текстуру в байты формата PNG
            byte[] bytes = texture.EncodeToPNG();

            // Определяем путь для сохранения файла
            string filePath = Application.dataPath + "/GeneratedTexture.png";

            // Сохраняем файл
            System.IO.File.WriteAllBytes(filePath, bytes);

            Debug.Log($"Текстура сохранена: {filePath}");
        }
    }
}
