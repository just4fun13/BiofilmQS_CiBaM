
using Assets.Scripts.MVVM_CA;
using CellularAutomaton;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class DiffusionModelByADI : FlowDiffusionModel
    {
        private readonly DiffusionSolver diffusionSolver;


        public DiffusionModelByADI(int w, int h, CellGeometry b)
            //: base(w, h, b, n)
        {
            diffusionSolver = new DiffusionSolver();
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
            AreaWidthCells = w;
            AreaHeightCells = h;

            GeometryBase = b;
            Cn = new double[AreaWidthCells, AreaHeightCells];
            SetInitNutrient();
        }

        public DiffusionModelByADI(double AreaW, double AreaH, CellGeometry bas, int n, double deltaTimeIm, double D, double U, double Mu) 
            : base ( AreaW,  AreaH,  bas,  n,  D,  U,  Mu)

            //: base (AreaW, AreaH, bas, n)
        {
            diffusionSolver = new DiffusionSolver();
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
            GeometryBase = bas;
            double x = bas.BaseV[0].x;
            double y = bas.BaseV[1].y;
            double k_m = AreaW/x * y / AreaH;
            AreaHeightCells = (int)(n / Math.Sqrt( k_m));
            AreaWidthCells =  (int) (AreaHeightCells * ( k_m) );
            N = (int) Mathf.Sqrt(AreaWidthCells * AreaHeightCells);
            scale = Math.Max(AreaW / (AreaWidthCells - 1), AreaH / (AreaHeightCells - 1));
            GeometryBase.SetScale(scale);
            if (deltaTimeIm > 0)
            {
                deltaTime = deltaTimeIm;
            }
            else
            {
                double stabilityFactor = 1;// 0.125;
                double max_deltaTime = stabilityFactor * (h * h) / DiffusionKoef;
                //deltaTime = 1d / N / N ;// AreaWidthCells / AreaWidthCells;// GeometryBase.MinSqrWeight ;
                deltaTime = Math.Max(max_deltaTime, 1d / N / N);
                // bring delta time to be fit to 10
                double newDeltaTime = AdjustDeltaTimeToDiv10(deltaTime);
                deltaTime = newDeltaTime;
            }

            Vector2 del = bas.GetPosition(Vector2Int.zero) - bas.GetPosition(bas.GetNbrs(Vector2Int.zero)[0]);
            h = del.magnitude;
            Debug.Log($"Inited {bas.gridType.ToString()} : {AreaWidthCells} / {AreaHeightCells} => {AreaWidthCells * AreaHeightCells}, MaxDim = {MaxDim}" +
                $", h = {h}, deltaT = {deltaTime}, k_m = {k_m}");
            Cn = new double[AreaWidthCells, AreaHeightCells];
            SetInitNutrient();
            BoundaryCondition?.Apply(Cn, GeometryBase);
            //SetTopNutrient();
            diffusionSolver.Init(Cn, h, deltaTime, DiffusionKoef, maxThreadCount, ConsumptionRate);
            Debug.Log($"Inited improved diffusion model with h = {h}, dt = {deltaTime}, Dn = {DiffusionKoef}");
        }



        protected override void DiffuseSubstrate()
        {
            //DiffusionSolver.DoStep();
            //ShowCNsum();
            Cn = diffusionSolver.u;
        }

        public override void DoGrowthStep()
        {

            iter++;
            BoundaryCondition?.Apply(Cn, GeometryBase);
            double[,] uStar = new double[AreaWidthCells,AreaHeightCells];

            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    var pos = new Vector2Int(i, j);

                    double adv = ConvectionMine(pos); //
                    uStar[i, j] = Cn[i, j] - deltaTime * adv;
                }
            }
            );
            diffusionSolver.RefreshAndDoStep(uStar);
            DiffuseSubstrate();
        }
    }
}
