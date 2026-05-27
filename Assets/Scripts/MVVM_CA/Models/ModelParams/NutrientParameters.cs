

using System;

namespace Assets.Scripts.MVVM_CA.Models.ModelParams
{
    [Serializable]
    public class NutrientParameters
    {
        // mg / l
        public double InitialDensity = 1;
        // [m2 / s]
        public double NutrientDiffusion = 0.6; //1.6e-9;
    }
}
