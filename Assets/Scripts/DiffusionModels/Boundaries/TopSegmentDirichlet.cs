using Assets.Scripts.DiffusionModels;
using Assets.Scripts.NewGeneration;
using System;

[Serializable]
public class TopSegmentDirichlet : IBoundaryCondition
{
    public double value = 10.0;
    public float xMinFraction = 0.4f;
    public float xMaxFraction = 0.6f;

    public void Apply(double[,] c, CellGeometry base2D)
    {
        int width = c.GetLength(0);
        int height = c.GetLength(1);
        int j = height - 1;
        int iMin = (int)(xMinFraction * width);
        int iMax = (int)(xMaxFraction * width);

        for (int i = iMin; i <= iMax; i++)
            c[i, j] = value;
    }
}