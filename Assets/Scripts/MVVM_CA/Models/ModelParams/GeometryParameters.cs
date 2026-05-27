using CellularAutomaton;
using System;

namespace Assets.Scripts.MVVM_CA.Models.ModelParams
{
    [Serializable]
    public class GeometryParameters
    {
        public int modelScale = 5;
        public ModelType gridType = ModelType.SimpleSquare;
        public int AreaWidth  = 30;
        public int AreaHeight = 30;
        public int AreaLength = 30;

    }
}
