

using System;

namespace Assets.Scripts.MVVM_CA.Models.ModelParams
{
    public class MainParameters
    {
        public bool DynamicUpdate = true;
        public bool WriteLog = true;
        public int DoScreens = -1;
        public double TimeStep = 1;//5e-2;// 0.05;
        public double MaxTimeInHours = 10;
    }
}
