using UnityEngine;

public class ImageToArrayUtils 
{
    public static bool[,] ArrayFromImage(Texture2D tex)
    {
        bool[,] arr = new bool[tex.width, tex.height];
        for (int i = 0; i < tex.width; i++)
            for (int j = 0; j < tex.height; j++)
            {
                Color pixelColor = tex.GetPixel(i, j);
                arr[i, j] = (pixelColor.r != 1 || pixelColor.g != 1 || pixelColor.b != 1);
            }
        return arr;
    }

    public static double[,] GreenFromImage(Texture2D tex, int gridSize)
    {
        int w = tex.width / gridSize + 1;
        int h = tex.height / gridSize + 1;
        double[,] arr = new double[w, h];
        for (int i = 0; i < tex.width; i++)
            for (int j = 0; j < tex.height; j++)
            {
                arr[i / gridSize, j / gridSize] += tex.GetPixel(i, j).g ;
            }
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                arr[i, j] = arr[i, j] * 1f / (gridSize * gridSize);
        return arr;
    }
    public static double[,] RedFromImage(Texture2D tex, int gridSize)
    {
        int w = tex.width / gridSize + 1;
        int h = tex.height / gridSize + 1;
        double[,] arr = new double[w, h];
        for (int i = 0; i < tex.width; i++)
            for (int j = 0; j < tex.height; j++)
            {
                arr[i / gridSize, j / gridSize] +=  tex.GetPixel(i, j).r ;
            }
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                arr[i, j] = arr[i, j] * 1f / (gridSize * gridSize);
        return arr;
    }
    public static double[,] ColFromImage(Texture2D tex, int gridSize)
    {
        int w = tex.width / gridSize + 1;
        int h = tex.height / gridSize + 1;
        double[,] arr = new double[w, h];
        for (int i = 0; i < tex.width; i++)
            for (int j = 0; j < tex.height; j++)
            {
                arr[i / gridSize, j / gridSize] += tex.GetPixel(i, j).g + tex.GetPixel(i, j).r + tex.GetPixel(i, j).b;
            }
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                arr[i, j] = arr[i, j] * 1f / (gridSize * gridSize);
        return arr;
    }
    public static Color[,] ColorsFromImage(Texture2D tex, int gridSize, float Th)
    {
        double[,] greenVals = ColFromImage(tex, gridSize);
        Color[,] cols = new Color[greenVals.GetLength(0), greenVals.GetLength(1)];
        for (int i = 0; i < cols.GetLength(0); i++)
            for (int j = 0; j < cols.GetLength(1); j++)
            {
                if (greenVals[i, j] > Th)
                    cols[i, j] = Color.Lerp(Color.black, Color.green, (float)greenVals[i, j]);
                else
                    cols[i, j] = Color.black;
            }
        return cols;
    }

}
