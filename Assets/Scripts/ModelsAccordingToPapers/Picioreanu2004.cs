using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.ModelsAccordingToPapers
{
    public class Picioreanu2004 : MonoBehaviour
    {

            // Diffusion coefficients (in m^2/s)
            public const double Ds = 4e-10; // substrate diffusion coefficient
            public const double Db = 1e-10; // biomass diffusion coefficient
            public const double De = 1e-11; // EPS diffusion coefficient

            // Yield coefficients (in g/g)
            public const double Yxs = 0.5; // biomass yield coefficient on substrate
            public const double Yxe = 0.1; // EPS yield coefficient on substrate
            public const double Yys = 0.2; // product yield coefficient on substrate
            public const double Yyb = 0.5; // product yield coefficient on biomass

            // Reaction rates (in g/m^3/h)
            public const double m = 3.8e-17; // specific growth rate
            public const double ks = 1e-4; // Monod constant for substrate uptake
            public const double ke = 1e-4; // Monod constant for EPS uptake
            public const double qp = 3.4e-15; // specific production rate of product

            // Other constants
            public const double rho = 1000; // density of water (in kg/m^3)
            public const double alpha = 0.5; // fraction of biomass converted to EPS


    }
}
