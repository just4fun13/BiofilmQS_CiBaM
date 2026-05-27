
using Assets.Scripts.DiffusionModels;
using Assets.Scripts.MVVM_CA;
using CellularAutomaton;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class DiffusionModel 
    {

        public int AreaWidthCells { get; protected set; } = 100;
        public int AreaHeightCells { get; protected set; } = 100;

        public CellGeometry GeometryBase;
        public double[,] Cn { get; protected set; }
        public double BotCn => Cn[0, 0];
        public double CellArea { get; protected set; } = 0;
        
        protected System.Random rng = new System.Random();



        public class DiffusionModelParameters
        {
            public double AreaWidthInMeters { get; protected set; }
            public double AreaHeightInMeters { get; protected set; }
            public double DiffusionKoefSqrMeterPerSec { get; protected set; }
            public double FlowVelocityMetersInSec { get; protected set; }
            public double ConsumptionRate { get; protected set; }
            public DiffusionModelParameters(double AreaW, double AreaH, double DiffusionK = 0.1, double FlowVel = 1, double ConsRate = 1) 
            { 
                AreaHeightInMeters = AreaW;
                AreaHeightInMeters = AreaH;
                DiffusionKoefSqrMeterPerSec = DiffusionK;
                FlowVelocityMetersInSec = FlowVel;
                ConsumptionRate = ConsRate;
            }
        }


        protected DiffusionModelParameters parameters;
        private double middleNutrientLevel = 0;
        public int maxThreadCount { get; protected set; } = 24;
        protected double DiffusionKoef => parameters.DiffusionKoefSqrMeterPerSec;
        protected double FlowVelocity => parameters.FlowVelocityMetersInSec;
        protected double ConsumptionRate => parameters.ConsumptionRate;
        protected int maxIter = 1000;
        protected int iter = 0;
        protected int N = 0;
        public double deltaTime = 1d;
        protected double timeScale = 4d;
        public double scale { get; protected set; } = 1;
        public IBoundaryCondition BoundaryCondition { get; set; }
        protected double MaxDim => AreaWidthCells * AreaHeightCells;
        protected ParallelOptions parallelOptions;
        protected float h = 1f;
        public void SetTimeScaleK(double newK)
        {
            timeScale = newK;
        }
        public DiffusionModel() { }
        public DiffusionModel(double AreaW, double AreaH, CellGeometry bas, int n, double D, double U, double Mu)
        {
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
            parameters = new DiffusionModelParameters(AreaW, AreaH, D, U, Mu);
            GeometryBase = bas;
            iter = 0;
            double x = bas.BaseV[0].x;
            double y = bas.BaseV[1].y;
            double k_m = AreaW/x * y / AreaH;
            if (bas.gridType != GridType.Hexagone)
            {
                AreaHeightCells = (int)(n / Math.Sqrt(k_m));
                AreaWidthCells = (int)(n * n / AreaHeightCells );
                N = (int)Mathf.Sqrt(AreaWidthCells * AreaHeightCells);
                //scale = Math.Max(AreaW / (AreaWidthCells - 1), AreaH / (AreaHeightCells - 1));
                scale = Math.Min(AreaW / (AreaWidthCells ), AreaH / (AreaHeightCells ));
            }
            else
            {
                //double k = n / Math.Pow(2, 1d / 2d);
                //AreaHeightCells = (int) Math.Round(2*k);//
                AreaHeightCells = (int)(n / Math.Sqrt(k_m));
                //AreaWidthCells = (int)Math.Round(k);
                AreaWidthCells = (int)(AreaHeightCells * (k_m));
                N = (int)Mathf.Sqrt(AreaWidthCells * AreaHeightCells);
                scale = Math.Max(AreaW / (AreaWidthCells -1), AreaH / (AreaHeightCells -1));
            }

            GeometryBase.SetScale(scale);
            Vector2 del = bas.GetPosition(Vector2Int.zero) - bas.GetPosition(bas.GetNbrs(Vector2Int.zero)[0]);
            h = del.magnitude;
            double stabilityFactor = 0.125;
            double max_deltaTime = stabilityFactor * (h * h)/DiffusionKoef;
            //deltaTime = 1d / N / N ;// AreaWidthCells / AreaWidthCells;// GeometryBase.MinSqrWeight ;
            deltaTime = max_deltaTime;
            // bring delta time to be fit to 10
            double newDeltaTime = AdjustDeltaTimeToDiv10(deltaTime);
            string TimeChange = $"TT=[{deltaTime:F4}->{newDeltaTime:F4}]";
            deltaTime = newDeltaTime;

            Debug.Log($"Inited {bas.gridType.ToString()} with scale {scale} : {AreaWidthCells} / {AreaHeightCells} => {AreaWidthCells * AreaHeightCells}, MaxDim = {MaxDim}" +
                $", h = {h}, {TimeChange}, k_m = {k_m}, params : D={DiffusionKoef}, U={FlowVelocity}, Mu={ConsumptionRate}");
            Cn = new double[AreaWidthCells, AreaHeightCells];
            SetInitNutrient();
            BoundaryCondition?.Apply(Cn, bas);
        }

        protected double AdjustDeltaTimeToDiv10(double deltaTimeInitial)
        {
            if (deltaTimeInitial <= 0)
                throw new ArgumentException("deltaTimeInitial must be positive");

            // порядок величины
            double log10 = -Math.Log10(deltaTimeInitial);
            int order = (int)Math.Ceiling(log10);

            // если 5*10^exp слишком большое — пробуем 1*10^exp
            if (deltaTimeInitial * Math.Pow(10, order) > 5)
                return 5 * Math.Pow(10, -order);
            else
                return Math.Pow(10, - order);
        }

        public void SetMaxTreadCount(int mxtr)
        {
            maxThreadCount = mxtr;
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
        }
        public void SetMaxIter(int _maxIter)
        {
            maxIter = _maxIter;
        }
        protected void SetInitNutrient()
        {
            for (int i = 0; i < AreaWidthCells; i++)
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    Cn[i, j] = 0;
                }
        }
        protected bool InBoundX(int x) => x >= 0 && x < AreaWidthCells;
        protected bool InBoundY(int y) => y >= 0 && y < AreaHeightCells;
        protected bool InBound(Vector2Int pos) => pos.x >= 0 && pos.x < AreaWidthCells && pos.y >= 0 && pos.y < AreaHeightCells;
        //protected bool InBound(Vector2Int pos) => (pos - middlePoint).magnitude < Rad; 
        public bool IsDone => iter >= maxIter;

        private double GetDim(Vector2Int v)
        {
            double ans = 1;
            if (v.x == 0)
                ans =  AreaHeightCells * AreaHeightCells;
            if (v.y == 0)
                ans = AreaWidthCells * AreaWidthCells;
            return ans;
        }
        protected virtual void DiffuseSubstrate()
        {
            //SetTopNutrient();
            BoundaryCondition?.Apply(Cn, GeometryBase);

            double[,] cNew = new double[AreaWidthCells, AreaHeightCells];

            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    cNew[i, j] = Cn[i, j] + deltaTime * DiffusionKoef * (BalancedAverage(new Vector2Int(i, j)));
                    //            cNew[i, j] = Cn[i, j] + DiffusionKoef * NodeBalance(new Vector2Int(i, j)) ;
                }
            });
            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    Cn[i, j] = cNew[i, j];
                }
            });
        }

        protected double BalancedAverage(Vector2Int pos)
        {
            double av = 0;
            int k = 0;
            Vector2Int[] nbrs = GeometryBase.GetNbrs(pos);

            foreach (Vector2Int nbr in nbrs)
            {
                 // считаем направление, даже если оно "упирается" в границу
                Vector2Int v = pos + nbr;
                double w = GeometryBase.GetSqrWeight(nbr);



                if (InBound(v))
                {
                    av += (Cn[v.x, v.y] - Cn[pos.x, pos.y]) / w;
                    k++;
                }
                else
                {
                    // Нейман: dU/dn = 0 → (U_ghost - U_center) = 0 → вклад = 0
                    // Ничего не добавляем, просто оставляем av как есть.
                }
            }

            if (k == 0)
                return 0; // на всякий случай

            av = av * timeScale / k;
            return av;
        }

        public virtual void DoGrowthStep()
        {
            iter++;
            DiffuseSubstrate();
        }
    }
}
