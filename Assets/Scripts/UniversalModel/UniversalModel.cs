
using Assets.Scripts.MVVM_CA;
using CellularAutomaton;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class UniversalModel
    {
        private double AreaWidthReal = 1;
        private double AreaHeightReal = 1;

        public int AreaWidthCells { get; private set; } = 100;
        public int AreaHeightCells { get; private set; } = 100;

        public Base2D GeometryBase;
        object locker = new object();
        object lockerNutr = new object();
        object lockerRemoveFront = new object();

        public double[,] S2D { get; private set; } // Substrate
        public double[,] C2D { get; private set; } // Bacteria
        public double[,] A2D { get; private set; } // AHL

        public double[,] S3D { get; private set; } // Substrate
        public double[,] C3D { get; private set; } // Bacteria
        public double[,] A3D { get; private set; } // AHL

        protected double μmax = (1.52 * 0.00001 / 0.045 + 0.00003);
        protected double K = 3.5 * 0.0001;
        private double NutrientDiffusionKoef = 0.1;
        protected float AhlDiffusionKoef = 0.764f;
        protected long rep = 0;
        protected float ConcToDivide = 1.0f;
        protected float LifetimeCost = 0.000f;
        protected float InitSubstrateCount = 0.5f;
        protected double Alpha = 0.01;
        protected double Betta = 0.99;
        protected double AHLpowerK = 2.5;
        protected double AHLdegr = 0.93;
        protected double Uth = 0.5;
        protected double UthPow = 1;// 0.17677669529663688110021109052621;
        protected double DeltaTime = 0.0001;
        protected double LowNutrientThreshold = 0.0;
        protected double HighNutrientThreshold = 0.0;
        protected double AHLthreshold = 0.0;
        public double AverageNutrientRemain = 0.0;
        public double maxUhl { get; protected set; } = 0f;


        protected System.Random rng = new System.Random();

        protected int maxThreadCount = 24;
        private int N = 0;
        public double deltaTime = 1d;
        private double timeScale = 4d;
        public double scale { get; private set; } = 1;
        private double MaxDim => AreaWidthCells * AreaHeightCells;
        ParallelOptions parallelOptions;
        float h = 1f;

        public double GetConsume(double Cb, double Cs)
        {
            return Math.Min(μmax * Cb * Cs / (K + Cs) * DeltaTime, Cs);
        }

        public UniversalModel(int w, int h, Base2D b)
        {
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
            AreaWidthCells = w;
            AreaHeightCells = h;

            GeometryBase = b;
            S2D = new double[AreaWidthCells, AreaHeightCells];
            SetInitNutrient();
        }
        public void SetTimeScaleK(double newK)
        {
            timeScale = newK;
        }

        public UniversalModel(double AreaW, double AreaH, Base2D bas, int n)
        {
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
            GeometryBase = bas;
            double x = bas.BaseV[0].x;
            double y = bas.BaseV[1].y;
            double k_m = AreaW / x * y / AreaH;
            AreaHeightCells = (int)(n / Math.Sqrt(k_m));
            AreaWidthCells = (int)(AreaHeightCells * (k_m));
            N = (int)Mathf.Sqrt(AreaWidthCells * AreaHeightCells);
            scale = Math.Max(AreaW / (AreaWidthCells - 1), AreaH / (AreaHeightCells - 1));
            GeometryBase.SetScale(scale);
            deltaTime = 1d / N / N;// AreaWidthCells / AreaWidthCells;// GeometryBase.MinSqrWeight ;
            Vector2 del = bas.GetPosition(Vector2Int.zero) - bas.GetPosition(bas.GetNbrs(Vector2Int.zero)[0]);
            h = del.magnitude;
            Debug.Log($"Inited {bas.name} : {AreaWidthCells} / {AreaHeightCells} => {AreaWidthCells * AreaHeightCells}, MaxDim = {MaxDim}" +
                $", h = {h}, deltaT = {deltaTime}, k_m = {k_m}");
            S2D = new double[AreaWidthCells, AreaHeightCells];
            SetInitNutrient();
            SetTopNutrient();
        }
        private void SetInitNutrient()
        {
            for (int i = 0; i < AreaWidthCells; i++)
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    S2D[i, j] = 0;
                }
        }
        private void SetTopNutrient()
        {
            for (int i = (int)(0.4 * AreaWidthCells); i <= (int)(0.6 * AreaWidthCells); i++)
                S2D[i, AreaHeightCells - 1] = 10;
        }
        private bool InBound(Vector2Int pos) => pos.x >= 0 && pos.x < AreaWidthCells && pos.y >= 0 && pos.y < AreaHeightCells;
        private void DiffuseSubstrate()
        {
            double[,] Snew = new double[AreaWidthCells, AreaHeightCells];

            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    Snew[i, j] = S2D[i, j] + deltaTime * NutrientDiffusionKoef * (BalancedAverageSubstrate(new Vector2Int(i, j)));
                    //            cNew[i, j] = Cn[i, j] + DiffusionKoef * NodeBalance(new Vector2Int(i, j)) ;
                }
            });
            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    S2D[i, j] = Snew[i, j];
                }
            });

        }
        private void DiffuseAHL()
        {
            double[,] Anew = new double[AreaWidthCells, AreaHeightCells];

            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    Anew[i, j] = A2D[i, j] + deltaTime * NutrientDiffusionKoef * (BalancedAverageAhl(new Vector2Int(i, j)));
                    //            cNew[i, j] = Cn[i, j] + DiffusionKoef * NodeBalance(new Vector2Int(i, j)) ;
                }
            });
            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    A2D[i, j] = Anew[i, j];
                }
            });
        }
        private double BalancedAverageAhl(Vector2Int pos)
        {
            double av = 0;
            int k = 0;
            Vector2Int[] nbrs = GeometryBase.GetNbrs(pos);
            foreach (Vector2Int nbr in nbrs)
            {
                Vector2Int v = pos + nbr;
                if (InBound(v))
                {
                    k++;
                    av += (A2D[v.x, v.y] - A2D[pos.x, pos.y]) / GeometryBase.GetSqrWeight(nbr);
                }
            }
            av = av * timeScale / k;
            return av;
        }
        private double BalancedAverageSubstrate(Vector2Int pos)
        {
            double av = 0;
            int k = 0;
            Vector2Int[] nbrs = GeometryBase.GetNbrs(pos);
            foreach (Vector2Int nbr in nbrs)
            {
                Vector2Int v = pos + nbr;
                if (InBound(v))
                {
                    k++;
                    av += (S2D[v.x, v.y] - S2D[pos.x, pos.y]) / GeometryBase.GetSqrWeight(nbr);
                }
            }
            av = av * timeScale / k;
            return av;
        }
        private void ConsumeSubstrate(Vector2Int pos)
        {
            double v = GetConsume(C2D[pos.x, pos.y], S2D[pos.x, pos.y]);
            C2D[pos.x, pos.y] += v;
            lock (lockerNutr)
            {
              //  A2D[pos.x, pos.y] += GetAHLprod(C2D[pos.x, pos.y], A2D[pos.x, pos.y]);
                S2D[pos.x, pos.y] -= v;
            }
            //OnAHL[pos.x, pos.y] = (A2D[pos.x, pos.y] >= AHLthreshold * maxUhl) ? 1 : 0;
        }
        public void DoGrowthStep()
        {
            SetTopNutrient();
            DiffuseSubstrate();
        }
        public double GetValueInPoint(Vector2 pos)
        {
            int x = (int)(pos.x / scale / GeometryBase.BaseV[0].x);
            int y = (int)(pos.y / scale / GeometryBase.BaseV[1].y);
            Vector2Int posInt = new Vector2Int(x, y);
            return S2D[posInt.x, posInt.y];
        }
/*        public double GetValueInPoint(Vector3 pos)
        {
            int x = (int)(pos.x / scale / GeometryBase.BaseV[0].x);
            int y = (int)(pos.y / scale / GeometryBase.BaseV[1].y);
            int z = (int)(pos.z / scale / GeometryBase.BaseV[2].z);
            Vector3Int posInt = new Vector2Int(x, y, z);
            return S[posInt.x, posInt.y, posInt.z];
        }
*/
    
    }
}
