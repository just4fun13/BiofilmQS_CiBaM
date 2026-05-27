using System;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Debug = UnityEngine.Debug;
using Assets.Scripts.MVVM_CA.Models._2D;
using Assets.Scripts.ImageAnalyze;
using Unity.VisualScripting;

namespace Assets.Scripts.MVVM_CA.Utils
{
    public static class XavierResultsComparer
    {
        // =========================
        // EXP DATA (Xavier 2005)
        // =========================

        // Exp1: time rows (4) / height columns (15) : filled space fraction (0..1)
        private static double[,] HeightTime = new double[,]
        {
            { 0.24, 0.08, 0.02, 0.01, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00},
            { 0.58, 0.31, 0.12, 0.05, 0.03, 0.01, 0.01, 0.01, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00},
            { 0.72, 0.49, 0.25, 0.11, 0.05, 0.03, 0.02, 0.01, 0.01, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00},
            { 0.98, 0.91, 0.72, 0.47, 0.27, 0.16, 0.09, 0.05, 0.03, 0.02, 0.01, 0.01, 0.01, 0.01, 0.01},
        };

        // Exp1 plot2 (time / value)
        private static Vector2[] exp1_plot2_BiomassPerSurfaceArea = new Vector2[4]
        {
            new Vector2(   13, 0.42f),
            new Vector2(   16, 1.28f),
            new Vector2(18.5f, 2.24f),
            new Vector2(   22, 6.43f),
        };

        private static Vector2[] exp1_plot2_SubstratumCoverage = new Vector2[4]
        {
            new Vector2(   13, 25.74f),
            new Vector2(   16, 62.71f),
            new Vector2(18.5f, 74.82f),
            new Vector2(   22, 98.41f),
        };

        private static Vector2[] exp1_plot2_MeanBiofilmThickness = new Vector2[4]
        {
            new Vector2(   13, 1.98f),
            new Vector2(   16, 3.04f),
            new Vector2(18.5f, 3.87f),
            new Vector2(   22, 6.72f),
        };

        private static Vector2[] exp1_plot2_SurfaceRoughness = new Vector2[4]
        {
            new Vector2(   13, 0.63f),
            new Vector2(   16, 0.88f),
            new Vector2(18.5f, 0.91f),
            new Vector2(   22, 0.66f),
        };

        // Exp2 plot1: (time / distance-from-substratum / filled space fraction)
        private static Vector3[] exp2_plot1_DistanceFromSubstratum = new Vector3[40]
        {
            new Vector3(24,  2, 0.96f), new Vector3(24,  6, 0.61f), new Vector3(24, 10, 0.12f), new Vector3(24, 14, 0.02f),
            new Vector3(24, 18, 0.01f), new Vector3(24, 22, 0f), new Vector3(24, 26, 0f), new Vector3(24, 30, 0f),

            new Vector3(28,  2, 1f), new Vector3(28,  6, 0.95f), new Vector3(28, 10, 0.49f), new Vector3(28, 14, 0.10f),
            new Vector3(28, 18, 0.03f), new Vector3(28, 22, 0.02f), new Vector3(28, 26, 0.01f), new Vector3(28, 30, 0.00f),

            new Vector3(32,  2, 1f), new Vector3(32,  6, 0.99f), new Vector3(32, 10, 0.73f), new Vector3(32, 14, 0.27f),
            new Vector3(32, 18, 0.08f), new Vector3(32, 22, 0.02f), new Vector3(32, 26, 0.01f), new Vector3(32, 30, 0.01f),

            new Vector3(36,  2, 1f), new Vector3(36,  6, 0.99f), new Vector3(36, 10, 0.97f), new Vector3(36, 14, 0.80f),
            new Vector3(36, 18, 0.44f), new Vector3(36, 22, 0.15f), new Vector3(36, 26, 0.05f), new Vector3(36, 30, 0.02f),

            new Vector3(40,  2, 1f), new Vector3(40,  6, 1f), new Vector3(40, 10, 1f), new Vector3(40, 14, 0.94f),
            new Vector3(40, 18, 0.74f), new Vector3(40, 22, 0.43f), new Vector3(40, 26, 0.18f), new Vector3(40, 30, 0.07f),
        };

        // Exp2 plot2 (time / value)
        private static Vector2[] exp2_plot2_BiomassPerSurfaceArea = new Vector2[5]
        {
            new Vector2(24, 0.01f), new Vector2(28, 0.01f), new Vector2(32, 0.01f), new Vector2(36, 0.02f), new Vector2(40, 0.02f),
        };

        private static Vector2[] exp2_plot2_SubstratumCoverage = new Vector2[5]
        {
            new Vector2(24,  95.86f), new Vector2(28, 100.00f), new Vector2(32, 100.00f), new Vector2(36, 100.00f), new Vector2(40, 100.00f),
        };

        private static Vector2[] exp2_plot2_MeanBiofilmThickness = new Vector2[5]
        {
            new Vector2(24,  5.24f), new Vector2(28,  8.39f), new Vector2(32, 10.42f), new Vector2(36, 15.79f), new Vector2(40, 19.76f),
        };

        private static Vector2[] exp2_plot2_SurfaceRoughness = new Vector2[5]
        {
            new Vector2(24, 0.60f), new Vector2(28, 0.44f), new Vector2(32, 0.38f), new Vector2(36, 0.30f), new Vector2(40, 0.29f),
        };

        private static int[] HeightsExp1 = new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29 };
        private static int[] HeightsExp2 = new int[] { 2, 6, 10, 14, 18, 22, 26, 30 };

        private static float[] timeStampsExp1 = new float[] { 13, 16, 18.5f, 22 };
        private static float[] timeStampsExp2 = new float[] { 24, 28, 32, 36, 40 };

        // =========================
        // PUBLIC: drawing
        // =========================

        public static void ShowExp()
        {
            DrawLineTool.instance.ShowExp(HeightTime);
        }

        public static void CalcAndShotDiffExp1(Model model, float timeStampSeconds)
        {
            float timeStampHours = timeStampSeconds / 1800f; // your convention

            double[] dat;
            if (model is Model2D model2d)
            {
                dat = CalcFilledFraction3D(model2d.Bacteria2D, HeightsExp1);
            }
            else
            {
                Model3D model3d = (Model3D)model;
                dat = CalcFilledFraction(model3d.C3D, HeightsExp1);
            }

            DrawLineTool.instance.DrawLine(GetLineIndex(timeStampHours), dat);
        }

        // Keep this as-is (only drawing helper)
        private static int GetLineIndex(float hVal)
        {
            for (int i = 0; i < timeStampsExp1.Length; i++)
            {
                if (hVal <= timeStampsExp2[i])
                    return i + 4;
            }
            return 7;
        }

        // =========================
        // FITNESS / LOSS API
        // =========================

        public enum XavierExperimentId { Exp1 = 1, Exp2 = 2 }

        public struct FitnessOptions
        {
            /// <summary>
            /// If AutoHeightScale == true, HeightScale is ignored and computed from current biofilm max height.
            /// If AutoHeightScale == false, HeightScale is used as-is.
            /// </summary>
            public bool AutoHeightScale;

            /// <summary>Manual scale: y_model ~= y_xavier * HeightScale</summary>
            public double HeightScale;

            /// <summary>Cb > BiomassThreshold means "occupied"</summary>
            public double BiomassThreshold;

            /// <summary>Increase weights near front (where exp fraction ~ 0.5)</summary>
            public bool UseFrontWeights;

            /// <summary>Front weight multiplier (e.g., 3.0)</summary>
            public double FrontWeight;

            public double Eps;
        }

        public static ( double lossProfile,   double lossRough,       double lossBiomass,  
                        double lossCoverage,  double lossThickness,   double lossTotal )
         CalcLoss(   Model2D model,  XavierExperimentId expId,  int timeIndex,  FitnessOptions opt,
             double wProfile,   double wRough, double wBiomass = 0.2,
             double wCoverage = 0.1,  double wThickness = 0.2 )
        {
            double[,] Cb = model.Bacteria2D;

            // =========================
            // 1. PROFILE LOSS (как было)
            // =========================
            double lossProfile = CalcProfileLossInternal(model, expId, timeIndex, opt);

            // =========================
            // 2. ROUGHNESS LOSS (как было)
            // =========================
            double modelRough = CalcSurfaceRoughness(Cb);
            double expRough = GetExperimentalRoughness(expId, timeIndex);
            double lossRough =
                SafeRelSq(modelRough, expRough, opt.Eps);

            // =========================
            // 3. BIOMASS PER SURFACE
            // =========================
            double modelBio = CalcBiomassPerSurfaceArea(Cb);
            double expBio = GetExperimentalBiomass(expId, timeIndex);
            double lossBiomass =
                SafeRelSq(modelBio, expBio, opt.Eps);

            // =========================
            // 4. SUBSTRATUM COVERAGE
            // =========================
            double modelCov = CalcSubstratumCoverage(Cb);
            double expCov = GetExperimentalCoverage(expId, timeIndex);
            double lossCoverage =
                SafeRelSq(modelCov, expCov, opt.Eps);

            // =========================
            // 5. MEAN THICKNESS
            // =========================
            double modelTh = CalcMeanBiofilmThickness(Cb);
            double expTh = GetExperimentalThickness(expId, timeIndex);
            double lossThickness =
                SafeRelSq(modelTh, expTh, opt.Eps);

            // =========================
            // TOTAL
            // =========================
            double lossTotal =
                  wProfile * lossProfile
                + wRough * lossRough
                + wBiomass * lossBiomass
                + wCoverage * lossCoverage
                + wThickness * lossThickness;

            return (
                lossProfile,
                lossRough,
                lossBiomass,
                lossCoverage,
                lossThickness,
                lossTotal
            );
        }

        public static (double rough, double coverage, double thickness)
        CalcPars(Model2D model)
        {
            double[,] Cb = model.Bacteria2D;

            double modelRough = CalcSurfaceRoughness(Cb);
            double modelCov = CalcSubstratumCoverage(Cb);
            double modelTh = CalcMeanBiofilmThickness(Cb);

            return (modelRough, modelCov, modelTh );
        }

        private static double SafeRelSq(double model, double exp, double eps)
        {
            if (Math.Abs(exp) < eps)
                return model * model; // fallback

            double r = (model - exp) / exp;
            return r * r;
        }
        private static double AutoHeightScaleFromMaxHeight(double[,] Cb, XavierExperimentId expId, double bMin, double eps)
        {
            int simMaxY = CalcMaxBiofilmHeightY(Cb, bMin); // in model cells (0..H-1)
            int expMaxH = (expId == XavierExperimentId.Exp1) ? MaxOf(HeightsExp1) : MaxOf(HeightsExp2);

            if (expMaxH <= 0) return 1.0;

            // If film is empty, avoid zero scale
            if (simMaxY <= 0) return 1.0;

            return simMaxY / Math.Max(expMaxH, eps);
        }
        private static int CalcMaxBiofilmHeightY(double[,] Cb, double bMin)
        {
            int w = Cb.GetLength(0);
            int h = Cb.GetLength(1);
            int maxY = -1;

            for (int x = 0; x < w; x++)
            {
                for (int y = h - 1; y >= 0; y--)
                {
                    if (Cb[x, y] > bMin)
                    {
                        if (y > maxY) maxY = y;
                        break;
                    }
                }
            }
            return Math.Max(0, maxY);
        }
        private static int MaxOf(int[] arr)
        {
            int m = int.MinValue;
            for (int i = 0; i < arr.Length; i++) if (arr[i] > m) m = arr[i];
            return m;
        }
        private static int ClampInt(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
        public static int GetTimeIndex(float tHours, XavierExperimentId expId)
        {
            int timeIndex;
            if (expId == XavierExperimentId.Exp1)
                timeIndex = Array.FindIndex(timeStampsExp1, x => Math.Abs(x - tHours) < 1e-6);
            else
                timeIndex = Array.FindIndex(timeStampsExp2, x => Math.Abs(x - tHours) < 1e-6);
            return timeIndex;
        }

        // =========================
        // PROFILE LOSS internals
        // =========================


        private static double[] GetExpProfileExp1(int timeIndex)
        {
            int nH = HeightTime.GetLength(1);
            double[] p = new double[nH];
            for (int j = 0; j < nH; j++)
                p[j] = HeightTime[timeIndex, j];
            return p;
        }
        private static double[] GetExpProfileExp2(int timeIndex, out int[] heights)
        {
            heights = HeightsExp2;
            double[] p = new double[heights.Length];

            for (int idx = 0; idx < exp2_plot1_DistanceFromSubstratum.Length; idx++)
            {
                var v = exp2_plot1_DistanceFromSubstratum[idx];
                //if (Math.Abs(v.x - timeStampHours) < 1e-6)
                {
                    int hIndex = Array.IndexOf(heights, (int)v.y);
                    if (hIndex >= 0) p[hIndex] = v.z;
                }
            }
            return p;
        }
        private static double[] CalcFilledFractionScaled(double[,] Cb, int[] hs, double heightScale, double bMin)
        {
            double[] ans = new double[hs.Length];
            int width = Cb.GetLength(0);
            int height = Cb.GetLength(1);

            for (int j = 0; j < hs.Length; j++)
            {
                int yModel = (int)Math.Round(hs[j] * heightScale);
                yModel = ClampInt(yModel, 0, height - 1);

                int filled = 0;
                for (int i = 0; i < width; i++)
                    if (Cb[i, yModel] > bMin) filled++;

                ans[j] = filled * 1.0 / width;
            }
            return ans;
        }
        private static double WeightedMSE(double[] sim, double[] exp, double[] w, double eps)
        {
            int n = Math.Min(sim.Length, exp.Length);
            double num = 0.0, den = 0.0;

            for (int i = 0; i < n; i++)
            {
                double wi = w[i];
                double d = sim[i] - exp[i];
                num += wi * d * d;
                den += wi;
            }
            return num / (den + eps);
        }
        private static int EstimateFrontIndex(double[] frac)
        {
            int best = 0;
            double bestD = double.MaxValue;
            for (int i = 0; i < frac.Length; i++)
            {
                double d = Math.Abs(frac[i] - 0.5);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
        private static double[] BuildWeights(double[] expFrac, FitnessOptions opt)
        {
            double[] w = new double[expFrac.Length];
            for (int i = 0; i < w.Length; i++) w[i] = 1.0;

            if (!opt.UseFrontWeights) return w;

            int f = EstimateFrontIndex(expFrac);
            for (int i = 0; i < w.Length; i++)
            {
                int dist = Math.Abs(i - f);
                // gaussian-like weight around front
                w[i] *= 1.0 + (opt.FrontWeight - 1.0) * Math.Exp(-0.5 * dist * dist);
            }
            return w;
        }
        // =========================
        // ROUGHNESS
        // =========================
        private static bool TryGetExpRoughness(XavierExperimentId expId, float tHours, out double r)
        {
            r = 0.0;
            if (expId == XavierExperimentId.Exp1)
            {
                for (int i = 0; i < exp1_plot2_SurfaceRoughness.Length; i++)
                    if (Math.Abs(exp1_plot2_SurfaceRoughness[i].x - tHours) < 1e-6)
                    { r = exp1_plot2_SurfaceRoughness[i].y; return true; }
                return false;
            }
            else
            {
                for (int i = 0; i < exp2_plot2_SurfaceRoughness.Length; i++)
                    if (Math.Abs(exp2_plot2_SurfaceRoughness[i].x - tHours) < 1e-6)
                    { r = exp2_plot2_SurfaceRoughness[i].y; return true; }
                return false;
            }
        }

        // =========================
        // ORIGINAL helper fns (kept)
        // =========================

        public static void AddToArrayAndCompareExp1(float Time, double[,] Cb, int expId = 1)
        {
            double[] fillFrac;
            int timeId;

            if (expId == 1)
            {
                fillFrac = CalcFilledFraction3D(Cb, HeightsExp1);
                timeId = Array.FindIndex(timeStampsExp1, x => x == Time);
            }
            else
            {
                fillFrac = CalcFilledFraction3D(Cb, HeightsExp2);
                timeId = Array.FindIndex(timeStampsExp2, x => x == Time);
            }

            if (timeId == -1)
            {
                Debug.LogError($"Not found such a time step {Time} for {expId} experiment in XavierResultsComparer");
                return;
            }

            // you can extend it to compare fillFrac with exp arrays if needed
        }

        private static double[] CalcFilledFraction3D(double[,] Cb, int[] hs)
        {
            double[] ans = new double[hs.Length];
            int width = Cb.GetLength(0);
            for (int j = 0; j < hs.Length; j++)
            {
                int y = hs[j];
                y = ClampInt(y, 0, Cb.GetLength(1) - 1);

                int filled = 0;
                for (int i = 0; i < width; i++)
                {
                    if (Cb[i, y] > 0)
                        filled++;
                }
                ans[j] = filled * 1.0 / width;
            }
            return ans;
        }
        private static double[] CalcFilledFraction(double[,,] Cb, int[] hs)
        {
            double[] ans = new double[hs.Length];
            int width = Cb.GetLength(0);
            int length = Cb.GetLength(2);

            for (int j = 0; j < hs.Length; j++)
            {
                int y = hs[j];
                y = ClampInt(y, 0, Cb.GetLength(1) - 1);

                int filled = 0;
                for (int i = 0; i < width; i++)
                    for (int k = 0; k < length; k++)
                        if (Cb[i, y, k] > 0) filled++;

                ans[j] = filled * 1.0 / (width * length);
            }
            return ans;
        }
        private static double BiomassPerSurface(double[,] Cb)
        {
            double sum = 0;
            for (int x = 0; x < Cb.GetLength(0); x++)
                for (int y = 0; y < Cb.GetLength(1); y++)
                    sum += Cb[x, y];

            return sum / Cb.GetLength(0);
        }
        private static double CalcSubstratumCoverage(double[,] Cb)
        {
            double sum = 0;
            for (int x = 0; x < Cb.GetLength(0); x++)
                if (Cb[x, 0] > 0)
                    sum++;

            return sum*100d/Cb.GetLength(0);
        }
        private static double CalcBiomassPerSurfaceArea(double[,] Cb)
        {
            int w = Cb.GetLength(0);
            int h = Cb.GetLength(1);

            double sum = 0.0;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (sum > 0)
                        sum ++;

            return sum * 1d/ w;
        }
        private static double CalcMeanBiofilmThickness(double[,] Cb)
        {
            int w = Cb.GetLength(0);
            int h = Cb.GetLength(1);

            double sumHeight = 0.0;

            for (int x = 0; x < w; x++)
            {
                int height = 0;
                for (int y = h - 1; y >= 0; y--)
                {
                    if (Cb[x, y] > 0)
                    {
                        height = y + 1;
                        break;
                    }
                }
                sumHeight += height;
            }

            return sumHeight / w;
        }
        private static double CalcSurfaceRoughness(double[,] Cb)
        {
            int w = Cb.GetLength(0);
            int h = Cb.GetLength(1);

            double[] heights = new double[w];

            for (int x = 0; x < w; x++)
            {
                int yFront = -1;
                for (int y = h - 1; y >= 0; y--)
                {
                    if (Cb[x, y] > 0)
                    {
                        yFront = y;
                        break;
                    }
                }
                heights[x] = (yFront >= 0) ? yFront : 0;
            }

            double mean = 0.0;
            for (int x = 0; x < w; x++) 
                mean += heights[x];
            mean /= Math.Max(1, w);

            double var = 0.0;
            for (int x = 0; x < w; x++)
            {
                double d = heights[x] - mean;
                var += d * d;
            }
            var /= Math.Max(1, w);
            if (mean > 0)
                return Math.Sqrt(var)/mean;
            return Math.Sqrt(var);
        }
        private static double CalcProfileLossInternal(Model2D model2d, XavierExperimentId expId, int timeIndex, FitnessOptions opt)
        {
            double heightScale = opt.HeightScale;
            if (expId == XavierExperimentId.Exp1)
            {

                double[] expFrac = GetExpProfileExp1(timeIndex);
                double[] simFrac = CalcFilledFractionScaled(model2d.Bacteria2D, HeightsExp1, heightScale, opt.BiomassThreshold);

                double[] w = BuildWeights(expFrac, opt);
                return WeightedMSE(simFrac, expFrac, w, opt.Eps);
            }
            else
            {

                int[] hs;
                double[] expFrac = GetExpProfileExp2(timeIndex, out hs);
                double[] simFrac = CalcFilledFractionScaled(model2d.Bacteria2D, hs, heightScale, opt.BiomassThreshold);

                double[] w = BuildWeights(expFrac, opt);
                return WeightedMSE(simFrac, expFrac, w, opt.Eps);
            }
        }
        private static double GetExperimentalBiomass(XavierExperimentId expId, int t)
        {
            return expId == XavierExperimentId.Exp1
                ? exp1_plot2_BiomassPerSurfaceArea[t].y
                : exp2_plot2_BiomassPerSurfaceArea[t].y;
        }
        private static double GetExperimentalCoverage(XavierExperimentId expId, int t)
        {
            return expId == XavierExperimentId.Exp1
                ? exp1_plot2_SubstratumCoverage[t].y
                : exp2_plot2_SubstratumCoverage[t].y;
        }
        private static double GetExperimentalThickness(XavierExperimentId expId, int t)
        {
            return expId == XavierExperimentId.Exp1
                ? exp1_plot2_MeanBiofilmThickness[t].y
                : exp2_plot2_MeanBiofilmThickness[t].y;
        }
        private static double GetExperimentalRoughness(XavierExperimentId expId, int t)
        {
            return expId == XavierExperimentId.Exp1
                ? exp1_plot2_SurfaceRoughness[t].y
                : exp2_plot2_SurfaceRoughness[t].y;
        }

    }
}
