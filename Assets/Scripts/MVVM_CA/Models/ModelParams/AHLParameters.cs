using System;

namespace Assets.Scripts.MVVM_CA.Models.ModelParams
{
    [Serializable]
    public class AHLParameters : BacteriaParameters
    {
        public bool UseAHL = true;
        public double AHLdiffusion = 0.2;
        public double alpha = 0.01;
        public double betta = 0.05;
        public double degradationK = 0.79;
        public double powerK = 2.5;
        public double AHLthreshold = 0.2;

    }
}
