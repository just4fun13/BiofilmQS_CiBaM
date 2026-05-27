using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MVVM_CA.Analytics
{
    public sealed class CurveComparer
    {
        private readonly Vector2[][] expAhl;
        private readonly Vector2[][] expBiomass;
        private readonly string[] expNames;

        private readonly int expCount;

        private int[] nextAhlIndex;
        private int[] nextBiomassIndex;

        private List<double>[] modelAhlValues;
        private List<double>[] modelBiomassValues;

        private bool hasPrevFrame = false;

        private double prevTimeH;
        private double prevAhl;
        private double prevBiomass;

        public int BestExperimentIndex { get; private set; } = -1;
        public string BestExperimentName { get; private set; } = "";
        public double BestTotalError { get; private set; } = double.PositiveInfinity;
        public double BestAhlError { get; private set; } = double.PositiveInfinity;
        public double BestBiomassError { get; private set; } = double.PositiveInfinity;

        public double[] LastAhlErrors { get; private set; }
        public double[] LastBiomassErrors { get; private set; }
        public double[] LastTotalErrors { get; private set; }

        public CurveComparer( Vector2[][] experimentAhl, Vector2[][] experimentBiomass, string[] experimentNames = null)
        {
            expAhl = experimentAhl;
            expBiomass = experimentBiomass;

            expCount = expAhl.Length;

            expNames = experimentNames ?? new string[expCount];

            for (int i = 0; i < expCount; i++)
            {
                if (string.IsNullOrWhiteSpace(expNames[i]))
                    expNames[i] = $"Experiment {i}";
            }

            nextAhlIndex = new int[expCount];
            nextBiomassIndex = new int[expCount];

            modelAhlValues = new List<double>[expCount];
            modelBiomassValues = new List<double>[expCount];

            LastAhlErrors = new double[expCount];
            LastBiomassErrors = new double[expCount];
            LastTotalErrors = new double[expCount];

            for (int i = 0; i < expCount; i++)
            {
                modelAhlValues[i] = new List<double>();
                modelBiomassValues[i] = new List<double>();
            }

            Reset();
        }

        public void Reset()
        {
            hasPrevFrame = false;

            BestExperimentIndex = -1;
            BestExperimentName = "";
            BestTotalError = double.PositiveInfinity;
            BestAhlError = double.PositiveInfinity;
            BestBiomassError = double.PositiveInfinity;

            for (int i = 0; i < expCount; i++)
            {
                nextAhlIndex[i] = 0;
                nextBiomassIndex[i] = 0;

                modelAhlValues[i].Clear();
                modelBiomassValues[i].Clear();

                LastAhlErrors[i] = double.PositiveInfinity;
                LastBiomassErrors[i] = double.PositiveInfinity;
                LastTotalErrors[i] = double.PositiveInfinity;
            }
        }

        public bool AddFrameSeconds(double timeSeconds, double modelAhl, double modelBiomass)
        {
            return AddFrame(timeSeconds / 3600.0, modelAhl, modelBiomass);
        }

        public bool AddFrame(double timeH, double modelAhl, double modelBiomass)
        {
            if (hasPrevFrame && timeH < prevTimeH)
                throw new ArgumentException("Time must be non-decreasing.");

            for (int exp = 0; exp < expCount; exp++)
            {
                WriteNextPoint(
                    expAhl[exp],
                    modelAhlValues[exp],
                    ref nextAhlIndex[exp],
                    timeH,
                    modelAhl
                );

                WriteNextPoint(
                    expBiomass[exp],
                    modelBiomassValues[exp],
                    ref nextBiomassIndex[exp],
                    timeH,
                    modelBiomass
                );
            }

            prevTimeH = timeH;
            prevAhl = modelAhl;
            prevBiomass = modelBiomass;
            hasPrevFrame = true;
            return IsFinished();
        }

        private static bool WriteNextPoint( Vector2[] expCurve,  List<double> modelValues, ref int nextIndex,
            double currentTimeH, double currentValue
        )
        {
            // Уже всё записано
            if (nextIndex >= expCurve.Length)
                return false;

            // Берём только следующую незаполненную точку
            double targetTimeH = expCurve[nextIndex].x;

            if (currentTimeH >= targetTimeH)
            {
                modelValues.Add(currentValue);
                nextIndex++;
                return true;
            }

            return false;
        }

        public bool IsFinished()
        {
            for (int i = 0; i < expCount; i++)
            {
                if (nextAhlIndex[i] < expAhl[i].Length)
                    return false;

                if (nextBiomassIndex[i] < expBiomass[i].Length)
                    return false;
            }

            return true;
        }

        public int GetBestExperiment()
        {
            BestExperimentIndex = -1;
            BestExperimentName = "";
            BestTotalError = double.PositiveInfinity;
            BestAhlError = double.PositiveInfinity;
            BestBiomassError = double.PositiveInfinity;

            for (int exp = 0; exp < 1; exp++)
            {
                double ahlError = CalcNormalizedPointError(
                    expAhl[exp],
                    modelAhlValues[exp]
                );

                double biomassError = CalcNormalizedPointError(
                    expBiomass[exp],
                    modelBiomassValues[exp]
                );

                double total = ahlError + biomassError;

                LastAhlErrors[exp] = ahlError;
                LastBiomassErrors[exp] = biomassError;
                LastTotalErrors[exp] = total;

                if (total < BestTotalError)
                {
                    BestTotalError = total;
                    BestAhlError = ahlError;
                    BestBiomassError = biomassError;
                    BestExperimentIndex = exp;
                    BestExperimentName = expNames[exp];
                }
            }

            return BestExperimentIndex;
        }

        private static double CalcNormalizedPointError( Vector2[] expCurve, List<double> modelValues)
        {
            int n = Math.Min(expCurve.Length, modelValues.Count);

            if (n == 0)
                return double.PositiveInfinity;

            double fine = 0;
            if (modelValues.Count < expCurve.Length)
                fine = (expCurve.Length - modelValues.Count) * 100;

            double expMax = MaxAbs(expCurve);
            double modelMax = MaxAbs(modelValues);

            if (expMax < 1e-12)
                expMax = 1.0;

            if (modelMax < 1e-12)
                modelMax = 1.0;

            double sum = 0.0;

            for (int i = 0; i < n; i++)
            {
                double expNorm = expCurve[i].y / expMax;
                double modelNorm = modelValues[i] / modelMax;

                sum += Math.Abs(modelNorm - expNorm);
            }

            return sum / n + fine;
        }

        private static double MaxAbs(Vector2[] curve)
        {
            double max = 0.0;

            for (int i = 0; i < curve.Length; i++)
            {
                double v = Math.Abs(curve[i].y);
                if (v > max)
                    max = v;
            }

            return max;
        }

        private static double MaxAbs(List<double> values)
        {
            double max = 0.0;

            for (int i = 0; i < values.Count; i++)
            {
                double v = Math.Abs(values[i]);
                if (v > max)
                    max = v;
            }

            return max;
        }

        public string GetReport()
        {
            string s = "Experiment\tAHL_error\tBiomass_error\tTotal_error\n";

            for (int i = 0; i < expCount; i++)
            {
                s += $"{expNames[i]}\t" +
                     $"{LastAhlErrors[i]:F6}\t" +
                     $"{LastBiomassErrors[i]:F6}\t" +
                     $"{LastTotalErrors[i]:F6}\n";
            }

            s += $"BEST\t{BestExperimentName}\t{BestTotalError:F6}";

            return s;
        }
        public string GetDebugState(int exp = 0)
        {
            string nextAhl = nextAhlIndex[exp] < expAhl[exp].Length
                ? expAhl[exp][nextAhlIndex[exp]].x.ToString("F6")
                : "done";

            string nextBio = nextBiomassIndex[exp] < expBiomass[exp].Length
                ? expBiomass[exp][nextBiomassIndex[exp]].x.ToString("F6")
                : "done";

            return $"AHL {modelAhlValues[exp].Count}/{expAhl[exp].Length}, " +
                   $"BIO {modelBiomassValues[exp].Count}/{expBiomass[exp].Length}, " +
                   $"nextAhlTime={nextAhl}, nextBioTime={nextBio}";
        }
    }
}