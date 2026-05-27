using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static Assets.Scripts.MVVM_CA.SimExpert2D;

namespace Assets.Scripts.MVVM_CA
{
    public abstract class Model
    {
        public int AreaWidth { get; protected set; } = 1000;
        public int AreaHeight { get; protected set; } = 1000;
        protected int AreaLength = 1000;
        protected int NutrientAreaWidth = 1000;
        protected int NutrientAreaHeight = 1000;
        protected int NutrientAreaLength = 1000;
        public ModelType gridType { get; protected set; }
        protected int difCOunt = 10;
        public int NutrGridSimpl { get; protected set; } = 1;
        protected int maxThreadCount = 24;
        protected float MinX = 1000, MaxX = -1000, MinY = 1000, MaxY = -1000, MinZ = 1000, MaxZ = -1000;
        public int CellCount { get; protected set; } = 0;
        public Vector2 YBounds => new Vector2(MinY, MaxY);
        public double[,] U2Dnorm { get; protected set; }
        public double[,] OnAHL { get; protected set; }

        // gammaS from Buddrus, U * L / (cell * h)
        protected double GammaS = 1.3e-12;

        // how many cells/L correspond to B = 1 in the CA model
        protected double NrefCellsPerL = 5e11;

        // Buddrus substrate exponent ns
        protected double SubstratePower = 1.3;

        public double GammaSValue => GammaS;
        public double NrefCellsPerLValue => NrefCellsPerL;
        public double SubstratePowerValue => SubstratePower;

        public double GammaSEffectivePerSecond => GammaS * NrefCellsPerL / 3600.0;

        public double μmax { get; protected set; } = (1.52 * 0.00001 / 0.045 + 0.00003);
        public double DiffusionKoef { get; protected set; } = 600;//0.0264f;
        public double SpreadProbMax { get; protected set; } = 0.0;
        public double SpreadProb { get; protected set; } = 0.0;

        public double ConsumeDescreaseVal { get; protected set; } = 0.0;
        public int InoculationCount { get; protected set; } = 3;
        public double Ks { get; protected set; } = 5e-5;
        public double Yxs { get; protected set; } = 1;
        public float AhlDiffusionKoef { get; protected set; } = 0.764f;
        protected long rep = 0;
        protected double ConcToDivide = 1f;//0.001f;
        protected double LifetimeCost = 0.000f;
        public double InitSubstrateCount { get; protected set; } = 0.5f;
        public double randomDIrectionDIvideProbability { get; protected set; } = 1.0;


        // AHL PARAMETERS
        public double AhlAlpha { get; protected set; } = 0.01;
        public double AhlBetta { get; protected set; } = 0.99;
        public double AHLpowerK { get; protected set; } = 2.5;
        public double AHLdegrPerHour { get; protected set; } = 0.93;
        public double AHLscaler { get; protected set; } = 1.0;
        public double AHLthreshold { get; protected set; } = 0.0;
        // LACTONASE PARAMTERES
        protected double LacDiffusionKoef = 0.01;
        protected double LacKq = 0.99;
        protected double LacProd = 2.5;
        protected double LacDegr = 0.93;
        protected double LacPowerK = 1.0;
        protected double LacThreshold = 1.0;
        protected double LacMax = 1.0;
        protected double Lac_k_q = 1.0;

        // EPS PARAMETERS
        protected double EpsAlpha = 0.01;
        protected double EpsBetta = 0.99;
        protected double EpsDifReducion = 2.5;
        protected double EpsThreshold = 0.93;
        protected double EpsDegr = 1.0;
        protected double EpsMax = 1.0;



        //        protected double Uth = 0.5;
        protected ThreadLocal<System.Random> rng;
        public int RandomSeed { get; private set; } = 12345;
        private int randomStreamCounter = 0;


        public void InitRandomSeed(int seed)
        {
            RandomSeed = seed;
            randomStreamCounter = 0;

            rng?.Dispose();

            rng = new ThreadLocal<System.Random>(() =>
            {
                int streamId = Interlocked.Increment(ref randomStreamCounter);
                int threadSeed = MixSeed(RandomSeed, streamId);
                return new System.Random(threadSeed);
            });

            Debug.Log($"Model random seed = {RandomSeed}");
        }

        private static int MixSeed(int seed, int streamId)
        {
            unchecked
            {
                uint x = (uint)seed;
                x ^= (uint)(streamId * 0x9E3779B9);
                x ^= x >> 16;
                x *= 0x85EBCA6B;
                x ^= x >> 13;
                x *= 0xC2B2AE35;
                x ^= x >> 16;

                return (int)(x & 0x7FFFFFFF);
            }
        }


        protected double DeltaTime = 0.0001;
        public double AverageNutrientRemain = 0.0;
        public double AverageAhl = 0.0;


        protected double AntiDieTh = 0.1;
        protected double EpsRed = 0.8;

        protected double MaxCellBiomassConcentration = 50;


        // WASHOUT PARAMETERS
        public enum WashoutMode
        {
            None,
            LiquidOnly,
            ChemostatLike,
            DepthDependent
        }
        public DiffusionSolver.DiffusionMode DiffusionSolverMode { get; protected set; } = DiffusionSolver.DiffusionMode.ADI;
        public WashoutMode CurrentWashoutMode { get; protected set; } = WashoutMode.DepthDependent;
        // Вымывание жидкой фазы: питание/AHL.
        // Единицы: h^-1.
        public double LiquidDilutionRatePerHour { get; protected set; } = 0.0;
        // Концентрация питания во входящей среде.
        public double InflowSubstrate { get; protected set; } = 1.0;
        // Вымывание биомассы на фронте, h^-1.
        public double FrontBiomassWashoutRatePerHour { get; protected set; } = 0.0;
        // Вымывание биомассы в глубине, h^-1.
        // Обычно можно поставить 0.
        public double DeepBiomassWashoutRatePerHour { get; protected set; } = 0.0;
        // На какой глубине вымывание выходит на DeepBiomassWashoutRatePerHour.
        public int WashoutDepthCells { get; protected set; } = 2;
        // Как часто пересчитывать глубину.
        // 1 = каждый шаг, 10 = раз в 10 шагов.
        public int WashoutDepthUpdateEverySteps { get; protected set; } = 1;
        public double BiomassRemoveThreshold { get; protected set; } = 1e-6;
        public void SetDepthDependentWashout(double deepBiomassWashoutRatePerHour, int washoutDepthCells, int updateEverySteps = 1)
        {
            CurrentWashoutMode = WashoutMode.DepthDependent;


            DeepBiomassWashoutRatePerHour = 0;//  Math.Max(0.0, deepBiomassWashoutRatePerHour);

            //WashoutDepthCells = Math.Max(1, washoutDepthCells);
            //WashoutDepthUpdateEverySteps = Math.Max(1, updateEverySteps);
        }



        protected int diffusionSubsteps = 1;
        public int DiffusionSubsteps => diffusionSubsteps;

        public void SetDiffusionSubsteps(int n)
        {
            diffusionSubsteps = Math.Max(1, n);
        }

        public void SetNutrGridSimp(int n) => NutrGridSimpl = n;

        public void SetSize(Vector2 vSize)
        {
            AreaWidth = (int)vSize.x;
            AreaHeight = (int)vSize.y;
        }
        public void SetSize3D(Vector3 vSize)
        {
            AreaWidth = (int)vSize.x;
            AreaHeight = (int)vSize.y;
            AreaLength = (int)vSize.z;
        }
        public void SetMainPars(float[] pars)
        {
            DiffusionKoef = pars[0];
            InitSubstrateCount = pars[1];
            μmax = pars[2];
            if (pars[3] >= 1)
                InoculationCount = (int)pars[3];
            else
                InoculationCount = (int)(AreaWidth * pars[3]);
            SpreadProbMax = pars[4];
            Ks = pars[5];
            Yxs = pars[6];
            LifetimeCost = pars[7];
        }
        public void SetAhlPars(float[] ahlPars)
        {
            AhlDiffusionKoef = ahlPars[0];
            AhlAlpha = ahlPars[1];
            AhlBetta = ahlPars[2];
            AHLdegrPerHour = ahlPars[3];
            AHLpowerK = ahlPars[4];
            AHLthreshold = ahlPars[5];
            AHLscaler = ahlPars[6];
        }
        public void SetWashPars(float[] washPars)
        {
            LiquidDilutionRatePerHour = washPars[0];
            FrontBiomassWashoutRatePerHour = washPars[1];
            InflowSubstrate = washPars[2];
        }
        public void SetLactonasePars(float[] lacPars)
        {
            LacDiffusionKoef = lacPars[0];
            LacKq = lacPars[1];
            LacProd = lacPars[2];
            LacDegr = lacPars[3];
            LacPowerK = lacPars[4];
            LacThreshold = lacPars[5];
            LacMax = lacPars[6];
            Lac_k_q = lacPars[7];
        }
        public void SetEpsPars(float[] epsPars)
        {
            EpsAlpha = epsPars[0];
            EpsBetta = epsPars[1];
            EpsDifReducion = epsPars[2];
            EpsThreshold = epsPars[3];
            EpsDegr = epsPars[4];
            EpsMax = epsPars[5];
            Debug.Log($"Eps pars = A = {EpsAlpha}, B = {EpsBetta}, th = {EpsThreshold}, max = {EpsMax}");
        }
        public void SetReduceCons(float redCons) => ConsumeDescreaseVal = redCons;
        public void SetAHLThreshold(double ahlTh) => AHLthreshold = ahlTh;
        public void SetInocCount(double inocCount)
        {
            if (inocCount>=1)
                InoculationCount   = (int) inocCount;
            else
                InoculationCount = (int) (AreaWidth* inocCount);
        }
        public void SetDifK(double difK) => DiffusionKoef = difK;
        public void SetInitNutrient(double c0) => InitSubstrateCount = c0;
        public void Setμmax(double μ) => μmax = μ;
        public void SetKs(double ks) => Ks = ks;
        public void SetYxs(double yxs) => Yxs = yxs;
        public void SetLiquidWash(double liqWash) => LiquidDilutionRatePerHour = liqWash;
        public void SetBacWash(double bacWash) => InflowSubstrate = bacWash;
        public void SetNutrInflow(double nutInfl) => FrontBiomassWashoutRatePerHour = nutInfl;
        public void SetConcToDivide(double concToDivide) => ConcToDivide = concToDivide;
        public void SetSpreadProb(float spreadProb) => SpreadProbMax = spreadProb;
        public void SetRandomAmount(double val) => randomDIrectionDIvideProbability = val;

        public void ShowModelParamsList()
        {
            Debug.Log($"Grid type : {gridType.ToString()}");
            Debug.Log("GRID PARAMS");
            Debug.Log($"Area Width : {AreaWidth}");
            Debug.Log($"Area Height : {AreaHeight}");
            Debug.Log($"Area Length : {AreaLength}");
            Debug.Log("MAIN PARAMS");
            Debug.Log($"DiffusionKoef : {DiffusionKoef}");
            Debug.Log($"InitSubstrateCount : {InitSubstrateCount}");
            Debug.Log($"μmax : {μmax}");
            Debug.Log("AHL PARAMS");
            Debug.Log($"AhlDiffusionKoef : {AhlDiffusionKoef}");
            Debug.Log($"Alpha : {AhlAlpha}");
            Debug.Log($"Betta : {AhlBetta}");
            Debug.Log($"AHLdegr : {AHLdegrPerHour}");
            Debug.Log($"AHLpowerK : {AHLpowerK}");
            Debug.Log($"InoculationCount : {InoculationCount}");
            Debug.Log($"AHLthreshold : {AHLthreshold}");
            Debug.Log($"ConsumeDescreaseVal : {ConsumeDescreaseVal}");
            Debug.Log($"SpreadProb : {SpreadProbMax}");
        }

        public float[] GetStockParams()
        {
            float[] dat = new float[10];
            dat[3]  = (float) DiffusionKoef    ;
            dat[4]  = (float) AhlDiffusionKoef ;
            dat[5]  = (float) AhlAlpha            ;
            dat[6]  = (float) AhlBetta            ;
            dat[7]  = (float) AHLdegrPerHour          ;
            dat[8]  = (float) AHLpowerK        ;
            dat[9]  = (float) InoculationCount;
            dat[10] = (float) AHLthreshold;
            dat[12] = (float) ConsumeDescreaseVal;
            dat[13] = (float) SpreadProbMax;
            dat[14] = (float) μmax;
            return dat;
        }
        public double maxAhl { get; protected set; } = 0f;
        public double averageUHL { get; protected set; } = 0f;
        protected double averageConsume = 0f;
        public virtual Vector2 GetFractalDimension()
        {
            return Vector2.zero;
        }
        public virtual Vector3 GetPos(Vector3Int coord)
        {
            return Vector3.zero;
        }
        public virtual Vector2 GetPos(Vector2Int coord)
        {
            return Vector2.zero;
        }
        public virtual void DoGrowthStep()
        {

        }
        protected double GetConsume(double B, double S)
        {
            return μmax * B * S / (Ks + S) * DeltaTime;//Math.Min(μmax * B * S / (K + S) * DeltaTime, S );
        }
        protected double GetEPSProd(double B, double E)
        {
            return B * (AhlAlpha + AhlBetta * E / (AHLthreshold + E)) * DeltaTime;
        }
        protected double BuddrusSubstrateFactor(double S)
        {
            if (S <= 0.0)
                return 0.0;

            double sPow = Math.Pow(S, SubstratePower);
            double kPow = Math.Pow(Ks, SubstratePower);

            return sPow / (kPow + sPow + 1e-12);
        }

        protected double GetBuddrusBiomassGrowth(double B, double S)
        {
            double f = BuddrusSubstrateFactor(S);
            return μmax * B * f * DeltaTime;
        }

        protected double GetBuddrusSubstrateConsumption(double B, double S)
        {
            double f = BuddrusSubstrateFactor(S);
            return GammaSEffectivePerSecond * B * f * DeltaTime;
        }
        protected string GetConsumeDebug(double B, double S) => $"{μmax} * {B} * {S} / ({Ks} + {S}) * {DeltaTime}";
        protected double GetConsumeDecrease(double consume, double U)
        {
            //Debug.Log($"{ConsumeDescreaseVal} {U} {AHLthreshold} {B} {DeltaTime} {S} =>" +
            //    $" {ConsumeDescreaseVal * (U * U / (U * U + AHLthreshold * AHLthreshold)) * B * DeltaTime}");
            return consume *  ConsumeDescreaseVal * (U * U / (U * U + AHLthreshold * AHLthreshold)); ;
        }
        protected double GetAHLprod(double B, double U)
        {
            double Upow =  Math.Pow(U, AHLpowerK);
            if (B == 0) return 0;
            return B * (AhlAlpha + AhlBetta * Upow / (AHLthreshold + Upow + 1)) * DeltaTime;
            //return (Alpha + Betta * Upow) * DeltaTime;
        }

        protected double GetAHLProdNew(double S, double B, double U)
        {
            double Upow = Math.Pow(U, AHLpowerK);
            if (B == 0) return 0;
            return B * (AhlAlpha + AhlBetta * Upow / (AHLthreshold + Upow + 1)) * (S / (Ks + S))  * DeltaTime;
        }

        protected double Hill01(double x, double th, double k)
        {
            if (x <= 0) return 0;
            double xk = Math.Pow(x, k);
            double thk = Math.Pow(th, k);
            return xk / (xk + thk + 1e-12);
        }

        protected double EPSProduction(double B, double U)
        {
            // pE = EpsAlpha + EpsBetta * Hill(U, EpsThreshold, AHLpowerK?) 
            // лучше использовать ту же степень что для EPS: можно взять AHLpowerK или сделать отдельную.
            // у тебя отдельной степени для EPS нет -> используем AHLpowerK
            double h = Hill01(U/AHLscaler, EpsThreshold, AHLpowerK);
            return B * (EpsAlpha + EpsBetta * h) * DeltaTime;
        }

        protected double LactonaseProduction(double B, double U)
        {
            double h = Hill01(U, LacThreshold, LacPowerK);
            return B * LacProd * h * DeltaTime;
        }

        public double G => AreaHeight * AreaHeight * μmax * ConcToDivide / DiffusionKoef / InitSubstrateCount;

        public virtual int BiomassCount => 0;

        public virtual void AddNutrientAtHighLevel()
        {
        }
        public virtual void InitInoculate()
        {

        }
        public virtual void InitInoculateFromFile(List<Vector2Int> vList)
        {

        }
        public virtual Vector4 ModelStats3D()
        {
            return Vector4.zero;
        }
        public virtual void AddAntiSubstance()
        { }
        public virtual double CustomFun(Vector2Int v) => 0;
        public virtual int BottomLayerCountRemain() => 0;
    }
}

