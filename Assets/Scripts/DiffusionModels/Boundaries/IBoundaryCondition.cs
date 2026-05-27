
using Assets.Scripts.NewGeneration;

namespace Assets.Scripts.DiffusionModels
{
    public interface IBoundaryCondition
    {
        void Apply(double[,] c, CellGeometry base2D);

    }
}
