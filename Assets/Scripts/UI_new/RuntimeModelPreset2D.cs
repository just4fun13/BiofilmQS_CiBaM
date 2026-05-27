using System;
using CellularAutomaton;
using UnityEngine;
using Assets.Scripts.MVVM_CA.Models.ModelParams;

namespace Assets.Scripts.MVVM_CA
{
    public sealed class RuntimeModelPreset2D
    {
        public int Width;
        public int Height;

        public double DxM;
        public double RealWidthM;
        public double RealHeightM;

        public double TimeStepSeconds;
        public double MaxTimeSeconds;

        public float[] MainPars;
        public float[] AhlPars;

        public string DebugSummary()
        {
            return
                $"GRID: {Width}x{Height}\n" +
                $"dx = {DxM:E3} m\n" +
                $"real size = {RealWidthM * 1000.0:F3} mm x {RealHeightM * 1000.0:F3} mm\n" +
                $"dt = {TimeStepSeconds:F3} s\n" +
                $"max time = {MaxTimeSeconds:F1} s\n" +
                $"Dnutr model = {MainPars[0]:E3} 1/s\n" +
                $"mu model = {MainPars[2]:E3} 1/s\n" +
                $"Dahl model = {AhlPars[0]:E3} 1/s\n" +
                $"AHL degr per step = {AhlPars[3]:F6}";
        }
    }

    public static class ModelParameterScaler2D
    {
        // UI показывает D как мантиссу: 2.1 означает 2.1e-9 m^2/s.
        private const double DiffusionUiMultiplier = 1e-9;

        // Если AHL в модели хранится как нормированная концентрация.
        // 1.0 model unit = 100 nM.
        private const double AhlScaleMolL = 1e-7;

        public static RuntimeModelPreset2D BuildFromUI()
        {
            int width = ModelParameters.geometryParameters.AreaWidth;
            int height = ModelParameters.geometryParameters.AreaHeight;
            int scaleExp = ModelParameters.geometryParameters.modelScale;
            ModelType gridType = ModelParameters.geometryParameters.gridType;

            double dx = DxFromScale(scaleExp);

            double nutrDiffPhys = ModelParameters.nutrientParameters.NutrientDiffusion * DiffusionUiMultiplier;
            double ahlDiffPhys = ModelParameters.aHLParameters.AHLdiffusion * DiffusionUiMultiplier;

            double muPerHour = ModelParameters.bacteriaParameters.mUmax;
            double muPerSecond = muPerHour / 3600.0;

            double dt = RecommendTimeStepSeconds(dx, nutrDiffPhys, ahlDiffPhys, muPerSecond);

            double maxTimeSeconds = ModelParameters.mainParameters.MaxTimeInHours * 3600.0;

            int nbs = NeighborCount(gridType);

            // Это коэффициент для твоей старой формы:
            // u += K * (avgNeighbours - u) * dt
            //
            // Если используешь DiffusionSolver с лапласианом,
            // туда нужно передавать K / nbs.
            double nutrModelK = nbs * nutrDiffPhys / (dx * dx);
            double ahlModelK = nbs * ahlDiffPhys / (dx * dx);

            double gammaAhlPerHour = ModelParameters.aHLParameters.degradationK;
            double gammaAhlPerSecond = gammaAhlPerHour / 3600.0;
            double ahlDegrPerStep = Math.Exp(-gammaAhlPerSecond * dt);

            double ahlAlphaPerHour = ModelParameters.aHLParameters.alpha;
            double ahlBetaPerHour = ModelParameters.aHLParameters.betta;

            double ahlAlphaPerSecond = ahlAlphaPerHour / 3600.0;
            double ahlBetaPerSecond = ahlBetaPerHour / 3600.0;

            float[] mainPars =
            {
                (float)nutrModelK,
                (float)ModelParameters.nutrientParameters.InitialDensity,
                (float)muPerSecond,
                (float)ModelParameters.bacteriaParameters.InitialInoculationCount,
                (float)ModelParameters.bacteriaParameters.SpreadProbability,
                (float)ModelParameters.bacteriaParameters.kS,
                (float)ModelParameters.bacteriaParameters.Yxs,
                0f
            };

            float[] ahlPars =
            {
                (float)ahlModelK,
                (float)ahlAlphaPerSecond,
                (float)ahlBetaPerSecond,
                (float)ahlDegrPerStep,
                (float)ModelParameters.aHLParameters.powerK,
                (float)ModelParameters.aHLParameters.AHLthreshold,
                1.0f
            };

            return new RuntimeModelPreset2D
            {
                Width = width,
                Height = height,

                DxM = dx,
                RealWidthM = width * dx,
                RealHeightM = height * dx,

                TimeStepSeconds = dt,
                MaxTimeSeconds = maxTimeSeconds,

                MainPars = mainPars,
                AhlPars = ahlPars
            };
        }

        public static RecoveredPhysicalParams2D ReverseFromRuntime(
            int scaleExp,
            ModelType gridType,
            double timeStepSeconds,
            float[] mainPars,
            float[] ahlPars)
        {
            double dx = DxFromScale(scaleExp);
            int nbs = NeighborCount(gridType);

            double nutrDiffPhys = mainPars[0] * dx * dx / nbs;
            double ahlDiffPhys = ahlPars[0] * dx * dx / nbs;

            double muPerHour = mainPars[2] * 3600.0;

            double gammaPerSecond = -Math.Log(Math.Clamp(ahlPars[3], 1e-12f, 1f)) / timeStepSeconds;
            double gammaPerHour = gammaPerSecond * 3600.0;

            return new RecoveredPhysicalParams2D
            {
                NutrientDiffusionM2s = nutrDiffPhys,
                AhlDiffusionM2s = ahlDiffPhys,

                NutrientDiffusionUi = nutrDiffPhys / DiffusionUiMultiplier,
                AhlDiffusionUi = ahlDiffPhys / DiffusionUiMultiplier,

                MuMaxPerHour = muPerHour,

                InitialSubstrate = mainPars[1],
                Ks = mainPars[5],
                Yxs = mainPars[6],

                AhlAlphaPerHour = ahlPars[1] * 3600.0,
                AhlBetaPerHour = ahlPars[2] * 3600.0,
                AhlGammaPerHour = gammaPerHour,
                AhlPowerK = ahlPars[4],
                AhlThreshold = ahlPars[5]
            };
        }

        public static double DxFromScale(int scaleExp)
        {
            return Math.Pow(10.0, -scaleExp);
        }

        private static int NeighborCount(ModelType gridType)
        {
            switch (gridType)
            {
                case ModelType.Hexagon:
                    return 6;

                case ModelType.ExtendedSquare:
                    return 8;

                case ModelType.SimpleSquare:
                default:
                    return 4;
            }
        }

        private static double RecommendTimeStepSeconds(
            double dx,
            double nutrDiff,
            double ahlDiff,
            double muPerSecond)
        {
            double maxDiff = Math.Max(nutrDiff, ahlDiff);

            // Characteristic diffusion time over one cell.
            double tauDiff = dx * dx / Math.Max(maxDiff, 1e-30);

            // Ограничение по росту: не больше 5% прироста за шаг.
            double dtGrowth = 0.05 / Math.Max(muPerSecond, 1e-12);

            // Для implicit схема устойчива, но для нормальной точности
            // лучше не делать dt намного больше времени диффузии через несколько ячеек.
            double dtRecommended = 5.0 * tauDiff;
            double dtMin = 0.1 * tauDiff;
            double dtMax = Math.Min(20.0 * tauDiff, dtGrowth);

            dtRecommended = Math.Clamp(dtRecommended, dtMin, dtMax);

            // Ограничим снизу и сверху удобными UI-значениями.
            dtRecommended = Math.Clamp(dtRecommended, 0.01, 300.0);

            return dtRecommended;
        }
    }

    public sealed class RecoveredPhysicalParams2D
    {
        public double NutrientDiffusionM2s;
        public double AhlDiffusionM2s;

        public double NutrientDiffusionUi;
        public double AhlDiffusionUi;

        public double MuMaxPerHour;

        public double InitialSubstrate;
        public double Ks;
        public double Yxs;

        public double AhlAlphaPerHour;
        public double AhlBetaPerHour;
        public double AhlGammaPerHour;
        public double AhlPowerK;
        public double AhlThreshold;

        public string DebugSummary()
        {
            return
                $"Recovered physical params:\n" +
                $"Dnutr = {NutrientDiffusionM2s:E3} m2/s = {NutrientDiffusionUi:F3} × 10^-9 m2/s\n" +
                $"Dahl  = {AhlDiffusionM2s:E3} m2/s = {AhlDiffusionUi:F3} × 10^-9 m2/s\n" +
                $"muMax = {MuMaxPerHour:F4} h^-1\n" +
                $"S0 = {InitialSubstrate:F4}\n" +
                $"Ks = {Ks:F4}\n" +
                $"Yxs = {Yxs:F4}\n" +
                $"alpha = {AhlAlphaPerHour:F4} h^-1\n" +
                $"beta = {AhlBetaPerHour:F4} h^-1\n" +
                $"gammaAHL = {AhlGammaPerHour:F4} h^-1\n" +
                $"powerK = {AhlPowerK:F3}\n" +
                $"threshold = {AhlThreshold:F3}";
        }
    }
}