
using System;

namespace Assets.Scripts.MVVM_CA.Models.ModelParams
{
    [Serializable]
    public class BacteriaParameters
    {
        public int InitialInoculationCount = 10;
        // [s^-1]
        public double mUmax = 0.67e-5; // 6.94e-5
        // [kgCn / m^3]
        public double kS = 3.5e-4;
        // kgCb/kgCn    
        public double Yxs = 0.045;
        public double ConcToDivide = 1;
        public double SpreadProbability = 0.05;
    }
}
