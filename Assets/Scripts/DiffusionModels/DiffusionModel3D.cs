
using Assets.Scripts.MVVM_CA;
using CellularAutomaton;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class DiffusionModel3D
    {
        private double AreaWidthReal = 1;
        private double AreaHeightReal = 1;

        public int AreaWidthCells { get; private set; } = 100;
        public int AreaHeightCells { get; private set; } = 100;
        public int AreaLengthCells { get; private set; } = 100;

        public Base3D GeometryBase;
        public double[,,] Cn { get; private set; }
        public double BotCn => Cn[0, 0, 0];
        protected System.Random rng = new System.Random();

        private double middleNutrientLevel = 0;
        protected int maxThreadCount = 24;
        private double DiffusionKoef = 0.1;
        private int maxIter = 1000;
        private int iter = 0;
        private int N = 0;
        public double deltaTime = 1d;
        private double timeScale = 4d;
        public double scale { get; private set; } = 1;
        private double MaxDim => AreaWidthCells * AreaHeightCells * AreaLengthCells;
        private List<Vector3Int> InitPointsList = new List<Vector3Int>();

        ParallelOptions parallelOptions;
        double h = 1f;
        private double C0 = 10000;
        public DiffusionModel3D(int w, int h, Base3D b)
        {
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
            AreaWidthCells = w;
            AreaHeightCells = h;

            GeometryBase = b;
            Cn = new double[AreaWidthCells, AreaHeightCells, AreaLengthCells];
            SetInitNutrient();
        }
        public void SetTimeScaleK(double newK)
        {
            timeScale = newK;
        }

        public DiffusionModel3D(double AreaW, double AreaH, double AreaL, Base3D bas, int n)
        {
            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxThreadCount
            };
            GeometryBase = bas;
            double x = bas.BaseV[0].x;
            double y = bas.BaseV[1].y;
            double z = bas.BaseV[2].z;
            double k_m = AreaW/x * y / AreaH;
            if (bas.name != "TruncOct")
            {
                AreaHeightCells = (int)Math.Round(n / Math.Sqrt(k_m));
                AreaWidthCells = (int)Mathf.Round(Mathf.Sqrt(n * n * n / AreaHeightCells)); //(int) Math.Round(AreaHeightCells * (k_m) );
                AreaLengthCells = AreaWidthCells;
                N = (int)Math.Round(Math.Pow(AreaWidthCells * AreaHeightCells * AreaLengthCells, 1d / 3d));
                scale = Math.Max(AreaW / (AreaWidthCells), Math.Max(AreaH / (AreaHeightCells), AreaL / (AreaLengthCells)));
            }
            else
            {
                double k = n / Math.Pow(2, 1d / 3d);
                AreaHeightCells = (int) Math.Round(2*k);
                AreaWidthCells  = (int) Math.Round( k);
                AreaLengthCells = (int) Math.Round( k);
                Debug.Log($"k = {k} {AreaWidthCells} * {AreaHeightCells} * {AreaLengthCells} = {AreaHeightCells * AreaWidthCells * AreaLengthCells}");
                N = (int)Math.Round(Math.Pow(AreaWidthCells * AreaHeightCells * AreaLengthCells, 1d / 3d));
                scale = Math.Max(AreaW / (AreaWidthCells), Math.Max(AreaH / (AreaHeightCells), AreaL / (AreaLengthCells)));
            }

            GeometryBase.SetScale(scale);
            deltaTime = 1d /N /N  ;// AreaWidthCells / AreaWidthCells;//  ;
            //if (GeometryBase.NeedShift) // if is Truncated Octahedron
                deltaTime = GeometryBase.MinSqrWeight;
            Vector3 del = bas.GetPosition(Vector3Int.zero) - bas.GetPosition(bas.GetNbrs(Vector3Int.zero)[0]);
            h = del.magnitude;
            Debug.Log($"Inited {bas.name} : {AreaWidthCells} * {AreaHeightCells} * {AreaLengthCells}  =>" +
                $" {AreaWidthCells * AreaLengthCells * AreaHeightCells}, MaxDim = {MaxDim} N = {N}" +
                $", h = {h}, deltaT = {deltaTime}, k_m = {k_m}, scale = {scale}");
            Cn = new double[AreaWidthCells, AreaHeightCells, AreaLengthCells];


            double xMin = 0.3;
            double yMin = 0.999;
            double zMin = 0.4;
            double xMax = 0.7;
            double yMax = 1;
            double zMax = 0.6;
            int j = AreaHeightCells - 1;
            for (int i = 0; i < AreaWidthCells; i++)
                    for (int k = 0; k < AreaLengthCells; k++)   
                    {
                        Vector3Int posId = new Vector3Int(i, j, k);
                        Vector3 pos = bas.GetPosition(posId);
                        if ( pos.x > xMin && pos.x < xMax
                            && pos.z > zMin && pos.z < zMax)
                            InitPointsList.Add(posId);
                    }

            SetInitNutrient();
            SetTopNutrient();
        }

        public void SetMaxIter(int _maxIter)
        {
            maxIter = _maxIter;
        }

        private void SetInitNutrient()
        {
            for (int i = 0; i < AreaWidthCells; i++)
                for (int j = 0; j < AreaHeightCells; j++)
                    for (int k = 0; k < AreaLengthCells; k++)
                    {
                        Cn[i, j, k] = 0;
                }
            //Cn[0, 0, 0] = C0;
        }

        private void SetTopNutrient()
        {
            foreach (Vector3Int v in InitPointsList)
                Cn[v.x, v.y, v.z] = 10;
            return;

            if (GeometryBase.NeedShift)
            {
                SetTopNutrientTruncOct();
                return;
            }
            int xMin = (int)Math.Round(0.3 * AreaWidthCells);
            int xMax = (int)Math.Round(0.7 * AreaWidthCells);
            int zMin = (int)Math.Round(0.4 * AreaWidthCells);
            int zMax = (int)Math.Round(0.6 * AreaWidthCells);
            for (int i = xMin; i < xMax; i++)
                for (int k = zMin; k < zMax; k++)
                        Cn[i, AreaHeightCells - 1, k] = 10;
        }

        private void SetTopNutrientTruncOct()
        {
            int xMin = (int)Math.Round(0.3 * AreaWidthCells);
            int xMax = (int)Math.Round(0.7 * AreaWidthCells);
            int zMin = (int)Math.Round(0.4 * AreaWidthCells);
            int zMax = (int)Math.Round(0.6 * AreaWidthCells);
            for (int i = xMin; i <= xMax; i++)
                for (int k = zMin; k <= zMax; k++)
                {
                    Cn[i, AreaHeightCells - 1, k] = 10;
//                    Cn[i, AreaHeightCells - 2, k] = 10;
                }
        }



        private bool InBound(Vector3Int pos) => pos.x >= 0 && pos.x < AreaWidthCells 
                                             && pos.y >= 0 && pos.y < AreaHeightCells
                                             && pos.z >= 0 && pos.z < AreaLengthCells;

        private void DiffuseSubstrate()
        {
            double[,,] cNew = new double[AreaWidthCells, AreaHeightCells, AreaLengthCells];

            SetTopNutrient();
            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                    for (int k = 0; k < AreaLengthCells; k++)
                    {
                        cNew[i, j, k] = Cn[i, j, k] + deltaTime * DiffusionKoef * (BalancedAverage(new Vector3Int(i, j, k)));
                    //            cNew[i, j] = Cn[i, j] + DiffusionKoef * NodeBalance(new Vector2Int(i, j)) ;
                }
            });
            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                    for (int k = 0; k < AreaLengthCells; k++)
                    {
                        Cn[i, j, k] = cNew[i, j, k];
                }
            });


            //            Debug.Log($"SubSub = {subSum}");
        }
        private double BalancedAverage(Vector3Int pos)
        {
            double av = 0;
            int k = 0;
            Vector3Int[] nbrs = GeometryBase.GetNbrs(pos); 
            foreach (Vector3Int nbr in nbrs)
            {
                Vector3Int v = pos + nbr;
                if (InBound(v) )
                {
                    k++;
                      av += (Cn[v.x, v.y, v.z] - Cn[pos.x, pos.y, pos.z]) / GeometryBase.GetSqrWeight(nbr) ;
                }
            }
            av = av * timeScale / k ;
            return av;
        }

        public void DoGrowthStep()
        {

            iter++;
            //SetTopNutrient();
            DiffuseSubstrate();
        }

        public double CalcDeltaAnalytical(double TimeT)
        {
            double delta;
            double deltaSum = 0;
            double mult1 = C0 / 8d * Math.Pow(Math.PI * DiffusionKoef * TimeT, -1.5);
            double mult2 = -1d / (4 * DiffusionKoef * TimeT);

            for (int i = 0; i < AreaWidthCells; i++)
                for (int j = 0; j < AreaHeightCells; j++)
                    for (int k = 0; k < AreaLengthCells; k++)
                    {
                        double sumSqr = GeometryBase.GetPosition(new Vector3Int(i, j, k)).sqrMagnitude;
                        double Ccur = mult1 * Math.Exp(-(sumSqr) / mult2) * 0.000001;
                        //Debug.Log($"{i},{j},{k} -> {Cn[i, j, k]} != {Ccur}");
                        delta = Math.Abs(Cn[i, j, k] - Ccur);
                        deltaSum += delta;  
                    }
            deltaSum = deltaSum / (AreaWidthCells * AreaHeightCells * AreaLengthCells);
            return deltaSum;
        }
    }
}
