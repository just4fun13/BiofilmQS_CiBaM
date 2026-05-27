using Assets.Scripts.DiffusionModels;
using Assets.Scripts.NewGeneration;

public class AnalSqrBoundaries : IBoundaryCondition
{
    public void Apply(double[,] c, CellGeometry base2D)
    {
        int w = c.GetLength(0);
        int h = c.GetLength(1);
        // Нижняя и верхняя границы
        for (int i = 0; i < w; i++)
        {
            c[i, 0] = 0.0;      // y = 0
            c[i, h - 1] = 0.0;      // y = 1
        }

        // Левая и правая границы
        for (int j = 0; j < h; j++)
        {
            c[0, j] = 0.0;      // x = 0
            c[w - 1, j] = 0.0;      // x = 1
        }
    }
}