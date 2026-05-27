using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.MVVM_CA.Models._2D;
using CellularAutomaton;

namespace Assets.Scripts.MVVM_CA.Analytics
{
    public static class ModelAnalyzer
    {
        public static bool Enabled { get; set; } = true;

        // если хочешь считать fractal dimension реже (дорого)

        private static bool headerWritten = false;
        private static string del = "\t";


        public static void TryAnalyze(Model2D model, long iterationId, float simulationTime)
        {
            if (!Enabled) return;

            // --- базовые (для всех Model2D) ---
            int biomassCount = model.BiomassCount;
            double biomassVolume = model.Biomass2DVolume();

            // Height profile metrics
            double meanHeight, maxHeight;
            ComputeHeights(model, out meanHeight, out maxHeight);

            // Roughness (std of front y)
            double roughness = ComputeRoughness(model);

            // Fractal dim (реже)
            double fracDim = double.NaN;
            // GetFractalDimension() у тебя возвращает Vector2 (x=dim, y=?)
            var fd = model.GetFractalDimension();
            fracDim = fd.x;

            // --- five-field метрики (только если это FiveStarModel) ---
            double meanN = double.NaN, meanU = double.NaN, meanE = double.NaN, meanL = double.NaN;
            double qsAreaFrac = double.NaN;

            if (model is FiveStarModel m5)
            {
                ComputeFieldMeans(m5, out meanN, out meanU, out meanE, out meanL);
                qsAreaFrac = ComputeQSAreaFraction(m5);
            }

            // --- запись ---
            WriteHeaderIfNeeded();


            string line =
                $"{iterationId}{del}{simulationTime:0.###}{del}" +
                $"{biomassCount}{del}{biomassVolume:0.######}{del}" +
                $"{meanHeight:0.###}{del} {maxHeight:0.###}{del}" +
                $"{FormatNaN(fracDim)}{del}" +
                $"{FormatNaN(meanN)}{del}{FormatNaN(meanU)}{del}{FormatNaN(meanE)}{del}{FormatNaN(meanL)}{del}" +
                $"{FormatNaN(qsAreaFrac)}";

            // используем твой логгер
            MyLogger.WriteLog(line);
        }

        private static void WriteHeaderIfNeeded()
        {
            if (headerWritten) return;
            headerWritten = true;

            string header =
                $"iter{del}simTime{del}" +
                $"biomassCount{del}biomassVolume{del}" +
                $"meanHeight{del}maxHeight{del}roughness{del}" +
                $"fractalDim{del}" +
                $"meanN{del}meanAHL{del}meanEPS{del}meanLactonase{del}" +
                $"qsAreaFrac";

            MyLogger.WriteLog(header);
        }

        private static string FormatNaN(double v)
        {
            return double.IsNaN(v) ? "" : v.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void ComputeHeights(Model2D model, out double meanHeight, out double maxHeight)
        {
            // meanHeight: средний y среди всех клеток биомассы
            // maxHeight: максимальный y среди всех клеток биомассы
            // Быстро и без LINQ на больших списках
            var cells = model.BiomassCells2D;
            if (cells == null || cells.Count == 0)
            {
                meanHeight = 0;
                maxHeight = 0;
                return;
            }

            long sumY = 0;
            int maxY = 0;
            for (int k = 0; k < cells.Count; k++)
            {
                int y = cells[k].y;
                sumY += y;
                if (y > maxY) maxY = y;
            }

            meanHeight = (double)sumY / cells.Count;
            maxHeight = maxY;
        }

        private static double ComputeRoughness(Model2D model)
        {
            // roughness = std(y) по фронтовым клеткам
            // фронт у тебя формируется в GetFractalDimension(), но мы можем вычислить фронт быстро:
            // фронт = клетки с хотя бы одним пустым соседом.
            // Если фронт уже есть в model.frontCells2D (обновляется у тебя), используем его.

            var front = model.frontCells2D;
            if (front == null || front.Count == 0) return 0;

            double mean = 0;
            for (int i = 0; i < front.Count; i++) mean += front[i].y;
            mean /= front.Count;

            double var = 0;
            for (int i = 0; i < front.Count; i++)
            {
                double dy = front[i].y - mean;
                var += dy * dy;
            }
            var /= front.Count;

            return Math.Sqrt(var);
        }

        private static void ComputeFieldMeans(FiveStarModel m5,
            out double meanN, out double meanU, out double meanE, out double meanL)
        {
            // nutr-grid поля: Substrate2D, Ahl2D, Eps2D, Lactonas2D
            var N = m5.Substrate2D;
            var U = m5.Ahl2D;
            var E = m5.Eps2D;
            var L = m5.Lactonas2D;

            int w = N.GetLength(0);
            int h = N.GetLength(1);

            double sumN = 0, sumU = 0, sumE = 0, sumL = 0;
            long count = (long)w * h;

            // Parallel reduction
            object locker = new object();
            Parallel.For(0, w, i =>
            {
                double localN = 0, localU = 0, localE = 0, localL = 0;
                for (int j = 0; j < h; j++)
                {
                    localN += N[i, j];
                    localU += U[i, j];
                    localE += E[i, j];
                    localL += L[i, j];
                }
                lock (locker)
                {
                    sumN += localN;
                    sumU += localU;
                    sumE += localE;
                    sumL += localL;
                }
            });

            meanN = sumN / count;
            meanU = sumU / count;
            meanE = sumE / count;
            meanL = sumL / count;
        }

        private static double ComputeQSAreaFraction(FiveStarModel m5)
        {
            // доля nutr-grid ячеек, где AHL превышает порог
            var U = m5.Ahl2D;
            int w = U.GetLength(0);
            int h = U.GetLength(1);

            double th = m5.AHLthreshold;
            long count = (long)w * h;

            long above = 0;
            object locker = new object();

            Parallel.For(0, w, i =>
            {
                long local = 0;
                for (int j = 0; j < h; j++)
                    if (U[i, j] > th) local++;

                lock (locker) above += local;
            });

            return (double)above / count;
        }
    }
}
