using CellularAutomaton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.MVVM_CA.Models.ModelParams
{
    public static class ModelParametersReadWriter
    {

        private static Dictionary<string, double> parameters = new Dictionary<string, double>()
        {
            { "234", 23 },



        };

        private class Parameter
        {
            string name;
            double value;
            public Parameter(string name, double value)
            {
                this.name = name;
                this.value = value;
            }
        }

        public static void WriteAllParameters()
        {

        }



        public static void ReadModelParameters()
        {


        }


    }
}
