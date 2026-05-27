using System;
using UnityEngine;
using static Assets.Scripts.MVVM_CA.SimExpert2D;

namespace Assets.Scripts.MVVM_CA
{
    public enum RealGridScale
    {
        Scale1e4 = 10_000,
        Scale1e5 = 100_000,
        Scale1e6 = 1_000_000
    }

    public sealed class RealScaleConfig
    {
        public RealGridScale Scale = RealGridScale.Scale1e5;

        // 1 nm base unit:
        // 1:1e4 -> 10 um
        // 1:1e5 -> 100 um
        // 1:1e6 -> 1 mm
        public double BaseLengthM = 1e-9;

        public double RealWidthM = 0.01;   // 1 cm
        public double RealHeightM = 0.01;  // 1 cm

        // Твой DeltaTime в модели биологии лучше хранить в часах.
        public double TimeStepSeconds = 60.0;

        public ModelType GridType = ModelType.SimpleSquare;

        // Физические диффузии, m^2/s
        public double NutrientDiffusionM2s = 2e-9;    // условно O2 / малые молекулы в воде
        public double AhlDiffusionM2s = 5e-10;         // AHL грубо медленнее O2
        public double LactonaseDiffusionM2s = 1e-11;   // белок/фермент грубо сильно медленнее

        // Нормировки
        public double InitialSubstrate = 1.0;
        public double AhlScaleMolL = 1e-7;             // 100 nM -> 1 model AHL unit
        public double NRefCellsPerL = 5e11;            // характерная плотность клеток

        // CA controls
        public int InoculationCount = 3;
        public double SpreadProbMax = 0.0;
        public double LifetimeCost = 0.0;
        public double Yxs = 1.0;

        // Ограничение устойчивости для твоей схемы:
        // u += K * (avg - u) * dt
        // желательно K * dt <= ~0.8
        public double MaxKdt = 0.80;
    }

    public sealed class ScaledModelPreset
    {
        public int AreaWidth;
        public int AreaHeight;

        public double DxM;
        public double DxUm;
        public double DxMm;

        public double DeltaTimeHours;
        public double DeltaTimeSeconds;

        public int DiffusionSubsteps;

        public float[] MainPars;
        public float[] AhlPars;
        public float[] LactonasePars;

        public string DebugSummary()
        {
            return
                $"dx = {DxUm:F3} um = {DxMm:F6} mm\n" +
                $"grid = {AreaWidth} x {AreaHeight}\n" +
                $"dt = {DeltaTimeSeconds:F3} s = {DeltaTimeHours:F6} h\n" +
                $"diffusion substeps = {DiffusionSubsteps}\n" +
                $"Main: Dnutr={MainPars[0]}, S0={MainPars[1]}, mumax={MainPars[2]}, Ks={MainPars[5]}\n" +
                $"AHL: Dahl={AhlPars[0]}, alpha={AhlPars[1]}, beta={AhlPars[2]}, degr/sub={AhlPars[3]}, th={AhlPars[5]}, scaler={AhlPars[6]}\n" +
                $"Lac: Dlac={LactonasePars[0]}, prod={LactonasePars[2]}, degr/sub={LactonasePars[3]}, k_q={LactonasePars[7]}";
        }

        public void ApplyTo(Model model)
        {
            model.SetMainPars(MainPars);
            model.SetAhlPars(AhlPars);
            model.SetLactonasePars(LactonasePars);
            model.SetDiffusionSubsteps(DiffusionSubsteps);
        }
    }

    public static class RealScaleModelParams
    {
        // Значения CC2/rCC2 из статьи, переведённые в удобный базовый набор.
        private const double MuMax_h = 0.66;
        private const double Ks_Model = 0.38;

        private const double AlphaA_mol_cell_h = 2.3e-19;
        private const double BetaA_mol_cell_h = 2.3e-18;
        private const double GammaA_h = 0.05;
        private const double AhlHillN = 2.3;

        private const double Dilution_h = 0.1;

        private const double AlphaL = 1.1e-8;
        private const double GammaL_h = 0.005;
        private const double LacHillN = 2.5;
        private const double Ke_Lac_Ahl_h = 7.86;
        // Это уже нормированный максимум AHL-деградации лактонaзой:
        // примерно KE * L_scale для CC2.

        public static ScaledModelPreset Build(RealScaleConfig cfg)
        {
            double dx = cfg.BaseLengthM * (int)cfg.Scale;

            int width = Math.Max(1, (int)Math.Ceiling(cfg.RealWidthM / dx));
            int height = Math.Max(1, (int)Math.Ceiling(cfg.RealHeightM / dx));

            double dtS = cfg.TimeStepSeconds;
            double dtH = dtS / 3600.0;

            int nbrCount = NeighborCount(cfg.GridType);

            // Твоя схема:
            // u += K * (avg - u) * dt
            //
            // Для физического D:
            // K ~= neighborCount * D_phys / dx^2
            //
            // D_phys в m^2/s, а dt у модели в часах,
            // значит K должен быть в h^-1:
            double kNutr_h = ToModelDiffusionK(cfg.NutrientDiffusionM2s, dx, nbrCount);
            double kAhl_h = ToModelDiffusionK(cfg.AhlDiffusionM2s, dx, nbrCount);
            double kLac_h = ToModelDiffusionK(cfg.LactonaseDiffusionM2s, dx, nbrCount);

            double maxK = Math.Max(kNutr_h, Math.Max(kAhl_h, kLac_h));
            int substeps = Math.Max(1, (int)Math.Ceiling(maxK * dtH / cfg.MaxKdt));

            double dtSubH = dtH / substeps;

            // AHL хранится в Ahl2D как нормированная концентрация:
            // Ahl2D = A_real_mol_L / AhlScaleMolL
            double ahlAlpha_model_h = AlphaA_mol_cell_h * cfg.NRefCellsPerL / cfg.AhlScaleMolL;
            double ahlBeta_model_h = BetaA_mol_cell_h * cfg.NRefCellsPerL / cfg.AhlScaleMolL;

            // AHLthreshold = 1 означает:
            // QS включается около AhlScaleMolL, например около 100 nM.
            double ahlThresholdNorm = 1.0;
            double ahlScaler = 1.0;

            // AHLdegr применяется каждый diffusion substep.
            double ahlDegrPerSubstep = Math.Exp(-GammaA_h * dtSubH);

            // Лактонaза нормированная 0..1.
            // Если h=1, она стремится к 1 с характерной скоростью GammaL + Dilution.
            double lacResponse_h = GammaL_h + Dilution_h;
            double lacDegrPerSubstep = Math.Exp(-lacResponse_h * dtSubH);

            float[] mainPars =
            {
                (float)kNutr_h,                  // pars[0] DiffusionKoef
                (float)cfg.InitialSubstrate,     // pars[1] InitSubstrateCount
                (float)MuMax_h,                  // pars[2] μmax
                (float)cfg.InoculationCount,     // pars[3] InoculationCount
                (float)cfg.SpreadProbMax,        // pars[4] SpreadProbMax
                (float)(Ks_Model * cfg.InitialSubstrate), // pars[5] Ks
                (float)cfg.Yxs,                  // pars[6] Yxs
                (float)cfg.LifetimeCost          // pars[7] LifetimeCost
            };

            float[] ahlPars =
            {
                (float)kAhl_h,                   // ahlPars[0] AhlDiffusionKoef
                (float)ahlAlpha_model_h,         // ahlPars[1] AhlAlpha
                (float)ahlBeta_model_h,          // ahlPars[2] AhlBetta
                (float)ahlDegrPerSubstep,        // ahlPars[3] AHLdegr
                (float)AhlHillN,                 // ahlPars[4] AHLpowerK
                (float)ahlThresholdNorm,         // ahlPars[5] AHLthreshold
                (float)ahlScaler                 // ahlPars[6] AHLscaler
            };

            float[] lacPars =
            {
                (float)kLac_h,                   // lacPars[0] LacDiffusionKoef
                1.0f,                            // lacPars[1] LacKq
                (float)lacResponse_h,            // lacPars[2] LacProd
                (float)lacDegrPerSubstep,        // lacPars[3] LacDegr
                (float)LacHillN,                 // lacPars[4] LacPowerK
                1.0f,                            // lacPars[5] LacThreshold
                1.0f,                            // lacPars[6] LacMax
                (float)Ke_Lac_Ahl_h              // lacPars[7] Lac_k_q
            };

            return new ScaledModelPreset
            {
                AreaWidth = width,
                AreaHeight = height,

                DxM = dx,
                DxUm = dx * 1e6,
                DxMm = dx * 1e3,

                DeltaTimeHours = dtH,
                DeltaTimeSeconds = dtS,

                DiffusionSubsteps = substeps,

                MainPars = mainPars,
                AhlPars = ahlPars,
                LactonasePars = lacPars
            };
        }

        private static double ToModelDiffusionK(double dPhysM2s, double dxM, int neighborCount)
        {
            return neighborCount * dPhysM2s * 3600.0 / (dxM * dxM);
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
    }
}