using Assets.Scripts.DiffusionModels;
using Assets.Scripts.NewGeneration;
using UnityEngine;

[System.Serializable]
public class RectangularBiologicalTask : IBoundaryCondition
{

    private double Cinit = 1;

    public void Apply(double[,] c, CellGeometry geometry)
    {
        int height = c.GetLength(1);
        int i = 0;
        // left border is onflow
        for (int j = 0; j < height; j++)
            c[i, j] = Cinit;
    }
}
