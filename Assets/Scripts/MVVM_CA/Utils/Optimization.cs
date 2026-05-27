using Assets.Scripts.MVVM_CA.Models._2D;
using CellularAutomaton;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

namespace Assets.Scripts.MVVM_CA.Utils
{
    public class Optimization
    {
        /*


        // ============================================================
        // OPTIMIZATION (autofit)
        // ============================================================

        [Header("Optimization")]
        [SerializeField] private bool OptimizeMode = false;
        [SerializeField] private XavierResultsComparer.XavierExperimentId FitExperiment = XavierResultsComparer.XavierExperimentId.Exp1;

        [SerializeField] private int OptIterations = 200;        // global random candidates
        [SerializeField] private int OptLocalRefineSteps = 80;   // local refine steps
        [SerializeField] private int OptRepeatsPerCandidate = 3; // average over seeds
        [SerializeField] private int OptMaxIter = 144000;        // safety cap per candidate
        [SerializeField] private float OptEvalEverySeconds = 3600; // 1 hour in your convention

        [Header("Fit metric weights")]
        [SerializeField] private double WProfile = 1.0;
        [SerializeField] private double WRough = 0.5;
        [SerializeField] private double BiomassThresholdForFit = 0.0;

        [Header("Fit ranges")]
        [SerializeField] private RangeF R_DiffK = new RangeF { Min = 0.005f, Max = 0.2f };
        [SerializeField] private RangeF R_MuMax = new RangeF { Min = 0.0005f, Max = 0.05f };
        [SerializeField] private RangeF R_ConcToDivide = new RangeF { Min = 1f, Max = 1f };
        [SerializeField] private RangeF R_LifetimeCost = new RangeF { Min = 0.0f, Max = 0.00f };
        [SerializeField] private RangeF R_SpreadProb = new RangeF { Min = 0.0f, Max = 0.05f };
        [SerializeField] private RangeF R_AHLthreshold = new RangeF { Min = 0.3f, Max = 20f };
        [SerializeField] private RangeF R_InoculationCount = new RangeF { Min = 1, Max = 10 };
        [SerializeField] private RangeF R_InitNutrient = new RangeF { Min = 0.001f, Max = 0.07f };


        [Serializable]
        public struct RangeF
        {
            public float Min;
            public float Max;

            public float Sample() => UnityEngine.Random.Range(Min, Max);
            public float Clamp(float v) => Mathf.Clamp(v, Min, Max);
        }

        private struct Candidate
        {
            public float DiffK;
            public float MuMax;
            public float ConcToDivide;
            public float LifetimeCost;
            public float SpreadProb;
            public float AHLthreshold;
            public float InoculationCount;
            public float InitNutrient;

            public override string ToString()
                => $"DiffK={DiffK:F5}, MuMax={MuMax:F6}, ConcDiv={ConcToDivide:F3}, Life={LifetimeCost:F5}, Spread={SpreadProb:F4}," +
                $" AHLth={AHLthreshold:F3}, Inoc={InoculationCount:F3}, AHLth={InitNutrient:F3}";
        }

        // ============================================================

        // ============================================================
        // OPTIMIZATION IMPLEMENTATION
        // ============================================================

        // ============================
        // OPTIMIZATION (GLOBAL ONLY) — for TimeStep = 1 second
        // Uses XavierResultsComparer.GetTimeIndex(simTimeSeconds, expId)
        // Counts each calc moment ONCE, skips when timeIndex == -1
        // ============================

        private IEnumerator OptimizeLoop()
        {
            Debug.Log("=== Optimization started (GLOBAL ONLY) ===");

            
            Candidate best = SampleRandomCandidate();
            double bestLossTotal = double.PositiveInfinity;

            for (int it = 0; it < OptIterations; it++)
            {
                Candidate c = SampleRandomCandidate();
                bool done = false;
                CandidateResult res = default;

                yield return StartCoroutine(RunAndScoreCandidateCoroutine(c, (r) =>
                {
                    res = r;
                    done = true;
                }));

                while (!done) yield return null;

                // печать результата по кандидату (ошибки на расчетных моментах + суммы)
                MyLogger.WriteLog($"{it + 1}\t{AreaWidth}\t{AreaHeight}\t{c:F6}\t{res}\t{res.SumProfile:F6}\t{res.SumRough:F6}\t{bestLossTotal}");
                Debug.Log($"{it + 1}\t{AreaWidth}\t{AreaHeight}\t{c:F6}\t{res.SumTotal:F4}\t{res.SumProfile:F3}\t{res.SumRough:F3}" +
                    $"\t{res.SumBiomass:F3}\t{res.SumCoverage:F3}\t{res.SumThickness:F3}");

                // обновляем best по суммарной Total
                if (res.SumTotal < bestLossTotal)
                {
                    bestLossTotal = res.SumTotal;
                    best = c;

                    // лог лучшего (редко)
                    MyLogger.WriteLog($"best\t{AreaWidth}\t{AreaHeight}\t{bestLossTotal:F6}\t{best}\t{res.SumProfile:F6}\t{res.SumRough:F6}");
                }

                // отдаём кадр
                if (it % 2 == 0) yield return null;
            }

            Debug.Log($"=== Optimization DONE === bestTotal={bestLossTotal:F6} best={best}");
            progressUI?.Show();
        }

        // ------- helpers used by OptimizeLoop -------

        private struct CandidateResult
        {
            public double[] LossProfile;
            public double[] LossRough;
            public double[] LossBiomass;
            public double[] LossCoverage;
            public double[] LossThickness;
            public double[] LossTotal;

            public double SumProfile;
            public double SumRough;
            public double SumBiomass;
            public double SumCoverage;
            public double SumThickness;
            public double SumTotal;
        }

        private IEnumerator RunAndScoreCandidateCoroutine(Candidate c, Action<CandidateResult> onDone)
        {
            var opt = GetFitensOptions();

            int timeCount = (FitExperiment == XavierResultsComparer.XavierExperimentId.Exp1) ? 4 : 5;

            CandidateResult result = new CandidateResult
            {
                LossProfile = new double[timeCount],
                LossRough = new double[timeCount],
                LossBiomass = new double[timeCount],
                LossCoverage = new double[timeCount],
                LossThickness = new double[timeCount],
                LossTotal = new double[timeCount],
                SumProfile = 0,
                SumRough = 0,
                SumBiomass = 0,
                SumCoverage = 0,
                SumThickness = 0,
                SumTotal = 0
            };

            // accumulate over repeats, then average
            double[] accP = new double[timeCount];
            double[] accR = new double[timeCount];
            double[] accB = new double[timeCount];
            double[] accC = new double[timeCount];
            double[] accTh = new double[timeCount];
            double[] accT = new double[timeCount];

            for (int rep = 0; rep < OptRepeatsPerCandidate; rep++)
            {
                UnityEngine.Random.InitState(12345 + rep + (int)Time.time);

                Model2D model = CreateModelForFit();
                ApplyCandidateRobust(model, c);

                bool[] counted = new bool[timeCount];
                int countedN = 0;

                float simTimeSeconds = 0f;
                long iterLocal = 0;

                while (iterLocal < OptMaxIter)
                {
                    model.DoGrowthStep();

                    simTimeSeconds += TimeStep; // TimeStep = 1 сек
                    iterLocal++;

                    // проверка расчетных моментов
                    int timeIndex = XavierResultsComparer.GetTimeIndex(simTimeSeconds / 3600, FitExperiment);
                    if (timeIndex >= 0 && timeIndex < timeCount && !counted[timeIndex])
                    {
                        (double lp, double lr, double lb, double lc, double lth, double lt) = XavierResultsComparer.CalcLoss(
                            model, FitExperiment, timeIndex, opt,
                            wProfile: WProfile, wRough: WRough
                        );

                        accP[timeIndex] += lp;
                        accR[timeIndex] += lr;
                        accB[timeIndex] += lb;
                        accC[timeIndex] += lc;
                        accTh[timeIndex] += lth;
                        accT[timeIndex] += lt;

                        counted[timeIndex] = true;
                        //Debug.Log($"{iterLocal}\t{c}\t{lp}\t{lr}\t{lt}");
                    }

                    if (iterLocal % OptStepsPerFrame == 0)
                    {
                        //Debug.Log($"{iterLocal}\t{timeIndex}");
                        yield return null;
                    }
                }

                // penalty for missed timepoints (optional)
                if (countedN < timeCount)
                {
                    for (int k = 0; k < timeCount; k++)
                    {
                        if (!counted[k])
                        {
                            accP[k] += 10.0;
                            accR[k] += 10.0;
                            accT[k] += 10.0;
                        }
                    }
                }

                yield return null;
            }

            double inv = 1.0 / Math.Max(1, OptRepeatsPerCandidate);
            for (int i = 0; i < timeCount; i++)
            {
                result.LossProfile[i] = accP[i] * inv;
                result.LossRough[i] = accR[i] * inv;
                result.LossBiomass[i] = accB[i] * inv;
                result.LossCoverage[i] = accC[i] * inv;
                result.LossThickness[i] = accTh[i] * inv;
                result.LossTotal[i] = accT[i] * inv;

                result.SumProfile += result.LossProfile[i];
                result.SumRough += result.LossRough[i];
                result.SumBiomass += result.LossBiomass[i];
                result.SumCoverage += result.LossCoverage[i];
                result.SumThickness += result.LossThickness[i];
                result.SumTotal += result.LossTotal[i];
            }

            onDone?.Invoke(result);
        }
        private Candidate SampleRandomCandidate()
        {
            return new Candidate
            {
                DiffK = R_DiffK.Sample(),
                MuMax = R_MuMax.Sample(),
                ConcToDivide = R_ConcToDivide.Sample(),
                LifetimeCost = R_LifetimeCost.Sample(),
                SpreadProb = R_SpreadProb.Sample(),
                AHLthreshold = R_AHLthreshold.Sample(),
                InitNutrient = R_InitNutrient.Sample(),
                InoculationCount = R_InoculationCount.Sample(),
            };
        }

        private Candidate MutateAround(Candidate best, float relStep = 0.15f)
        {
            float Mut(float v, RangeF r)
            {
                float dv = Mathf.Max(1e-9f, Mathf.Abs(v)) * relStep * UnityEngine.Random.Range(-1f, 1f);
                return r.Clamp(v + dv);
            }

            best.DiffK = Mut(best.DiffK, R_DiffK);
            best.MuMax = Mut(best.MuMax, R_MuMax);
            best.ConcToDivide = Mut(best.ConcToDivide, R_ConcToDivide);
            best.LifetimeCost = Mut(best.LifetimeCost, R_LifetimeCost);
            best.SpreadProb = Mut(best.SpreadProb, R_SpreadProb);
            best.AHLthreshold = Mut(best.AHLthreshold, R_AHLthreshold);
            best.InitNutrient = Mut(best.InitNutrient, R_InitNutrient);
            best.InoculationCount = Mut(best.InoculationCount, R_InoculationCount);
            return best;
        }

        private XavierResultsComparer.FitnessOptions GetFitensOptions()
        {
            var opt = new XavierResultsComparer.FitnessOptions
            {
                AutoHeightScale = true,
                HeightScale = 1.0,
                BiomassThreshold = BiomassThresholdForFit,
                UseFrontWeights = true,
                FrontWeight = 3.0,
                Eps = 1e-12
            };
            return opt;
        }

        private float[] GetFitTimeStampsSeconds(XavierResultsComparer.XavierExperimentId expId)
        {
            return expId == XavierResultsComparer.XavierExperimentId.Exp1
                ? new float[] { 13f * 3600f, 16f * 3600f, 18.5f * 3600f, 22f * 3600f }
                : new float[] { 24f * 3600f, 28f * 3600f, 32f * 3600f, 36f * 3600f, 40f * 1800f };
        }

        private Model2D CreateModelForFit()
        {
            // Same as PrepareModelSolo, but without view init + without viewModel
            int w = (int)uiInput.GetSize.x;
            int h = (int)uiInput.GetSize.y;
            ModelType mt = (ModelType)uiInput.GetGridType;
            AreaWidth = w;
            AreaHeight = h;
            float initN = uiInput.GetMainParsInputs()[1];

            Model2D model;
            if (!UseAHL)
                model = new Model2DnoAHL(w, h, initN, mt, TimeStep, MaxThreadCount);
            else
                model = new Model2DWithAHL(w, h, initN, mt, TimeStep, MaxThreadCount);

            SetModelParametersWithUI(model);
            model.InitInoculate();

            return model;
        }

        /// <summary>
        /// Robust parameter application via reflection (so this compiles even if some members are named slightly differently).
        /// It tries:
        /// - SetMainPars(float[])
        /// - properties/fields: DiffusionKoef, μmax, ConcToDivide, LifetimeCost, SpreadProb, AHLthreshold
        /// - methods: SetSpreadProb, SetAHLThreshold, SetConcToDivide, SetLifetimeCost (if exist)
        /// </summary>
        private void ApplyCandidateRobust(Model2D model, Candidate c)
        {
            // 1) main pars: reuse UI array but override diffusion and muMax indices used in your code
            float[] main = uiInput.GetMainParsInputs();
            if (main != null && main.Length >= 3)
            {
                main[0] = c.DiffK;
                main[2] = c.MuMax;
                model.SetMainPars(main);
            }

            // 2) apply other params (try methods first, then properties/fields)
            TryInvokeFloat(model, "SetConcToDivide", c.ConcToDivide);
            TryInvokeFloat(model, "SetLifetimeCost", c.LifetimeCost);
            TryInvokeFloat(model, "SetSpreadProb", c.SpreadProb);
            TryInvokeFloat(model, "SetAHLThreshold", c.AHLthreshold);

            TrySetMemberFloat(model, "ConcToDivide", c.ConcToDivide);
            TrySetMemberFloat(model, "LifetimeCost", c.LifetimeCost);
            TrySetMemberFloat(model, "SpreadProb", c.SpreadProb);
            TrySetMemberFloat(model, "AHLthreshold", c.AHLthreshold);
            TrySetMemberFloat(model, "InoculationCount", c.InoculationCount);
            TrySetMemberFloat(model, "InitSubstrateCount", c.InitNutrient);

            // common alternative spellings (if you used different names)
            //TrySetMemberFloat(model, "AHLthreshold", c.AHLthreshold);
            //TrySetMemberFloat(model, "AHLThreshold", c.AHLthreshold);
        }

        private static bool TryInvokeFloat(object obj, string methodName, float arg)
        {
            try
            {
                var t = obj.GetType();
                var m = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null) return false;

                var pars = m.GetParameters();
                if (pars.Length != 1) return false;

                if (pars[0].ParameterType == typeof(float))
                {
                    m.Invoke(obj, new object[] { arg });
                    return true;
                }

                if (pars[0].ParameterType == typeof(double))
                {
                    m.Invoke(obj, new object[] { (double)arg });
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySetMemberFloat(object obj, string memberName, float value)
        {
            try
            {
                var t = obj.GetType();

                // property
                var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanWrite)
                {
                    if (p.PropertyType == typeof(float)) { p.SetValue(obj, value); return true; }
                    if (p.PropertyType == typeof(double)) { p.SetValue(obj, (double)value); return true; }
                    if (p.PropertyType == typeof(int)) { p.SetValue(obj, (int)Mathf.Round(value)); return true; }
                }

                // field
                var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                {
                    if (f.FieldType == typeof(float)) { f.SetValue(obj, value); return true; }
                    if (f.FieldType == typeof(double)) { f.SetValue(obj, (double)value); return true; }
                    if (f.FieldType == typeof(int)) { f.SetValue(obj, (int)Mathf.Round(value)); return true; }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
        */
    }
}
