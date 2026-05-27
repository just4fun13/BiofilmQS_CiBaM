using Assets.Scripts.MVVM_CA.Analytics;
using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

namespace Assets.Scripts.ImageAnalyze
{
    public class XChartLineTool : MonoBehaviour
    {
        [SerializeField] private LineChart ahlChart;
        [SerializeField] private LineChart bioChart;
        [SerializeField] private LineChart nutChart;

        public enum ChartName { ahl, bio, nut }

        [Header("Series names")]
        private string[] ahlSerieNames =
        {
            "Exp AHL",
            "Model AHL",
            "Threshold",
        };

        private string[] bioSerieNames =
        {
            "Exp biomass",
            "Sim biomass",
        };

        private string[] nutSerieNames =
        {
            "Nutrient Remain",
        };

        [Header("X axis range")]
        [SerializeField] private double minTimeHours = 0.0;
        [SerializeField] private double maxTimeHours = 42.0;

        public static XChartLineTool instance;
        private readonly List<Vector2> biomassRaw = new();
        private readonly List<Vector2> ahlRaw = new();
        private float biomassMax = 1;
        private float ahlMax = 1e-9f;
        private float bioExpMax = 0.5f;
        private float ahlExpMax = 0.8f;
        private bool initialized = false;
        private bool bioMaxChanged = false;
        private bool ahlMaxChanged = false;


        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("There are too many XChartLineTool scripts on the scene!");
                Destroy(this);
                return;
            }

            instance = this;

            InitCharts();
        }

        private void Start()
        {
            ShowExp();
        }

        public void InitCharts()
        {
            if (initialized)
                return;

            InitSingleChart(ahlChart, ahlSerieNames, "AHL(t)");
            InitSingleChart(bioChart, bioSerieNames, "Biomass(t)");
            InitSingleChart(nutChart, nutSerieNames, "Nutrient(t)");

            initialized = true;
        }

        private void InitSingleChart(LineChart chart, string[] serieNames, string titleText)
        {
            if (chart == null)
            {
                Debug.LogError("XChartLineTool: chart is null.");
                return;
            }

            // Удаляем все старые серии и данные, которые могли быть созданы в Inspector
            chart.RemoveData();

            var title = chart.EnsureChartComponent<Title>();
            title.show = true;
            title.text = titleText;

            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = true;

            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;

            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.show = true;
            xAxis.type = Axis.AxisType.Value;
            xAxis.axisName.show = true;
            xAxis.axisName.name = "Time, h";

            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.show = true;
            yAxis.type = Axis.AxisType.Value;
            yAxis.axisName.show = false;
            yAxis.axisName.name = "a.u.";

            for (int i = 0; i < serieNames.Length; i++)
            {
                chart.AddSerie<Line>(serieNames[i]);
            }
        }

        public void ClearDataOnly()
        {
            ClearSingleChartData(ahlChart);
            ClearSingleChartData(bioChart);
            ClearSingleChartData(nutChart);
        }

        private void ClearSingleChartData(LineChart chart)
        {
            if (chart == null)
                return;

            for (int i = 0; i < chart.series.Count; i++)
            {
                chart.series[i].ClearData();
            }
        }

        public LineChart GetChart(ChartName chartName)
        {
            if (chartName == ChartName.ahl) return ahlChart;
            if (chartName == ChartName.nut) return nutChart;
            return bioChart;
        }

        public void ShowExp()
        {
            ClearDataOnly();

            DrawVector2ArrayAtSerie(ahlChart, 0, BuddrusCurves.Ahl[0]);
            DrawVector2ArrayAtSerie(bioChart, 0, BuddrusCurves.Biomass[0]);

            DrawHorizontalLine(ahlChart, 2, 1.0f, minTimeHours, maxTimeHours);
        }

        private void DrawVector2ArrayAtSerie(LineChart chart, int serieIndex, Vector2[] vArray)
        {
            if (chart == null || vArray == null)
                return;

            for (int i = 0; i < vArray.Length; i++)
            {
                chart.AddData(serieIndex, vArray[i].x, vArray[i].y);
            }
        }

        private double GetSerieMax(Serie serie)
        {
            double max = -1000;
            for (int i = 0; i < serie.dataCount; i++)
            {
                if (max < serie.GetData(i, 0))
                    max = serie.GetData(i, 0);
            }
            return max;
        }

        private void DrawHorizontalLine(LineChart chart, int serieIndex, float y, double xMin, double xMax)
        {
            if (chart == null)
                return;

            chart.AddData(serieIndex, xMin, y);
            chart.AddData(serieIndex, xMax, y);
        }

        public static void AddModelPoint(Vector3 vals, float timeHours)
        {
            if (instance == null)
            {
                Debug.LogError("XChartLineTool instance is null.");
                return;
            }

            // ВАЖНО: здесь НЕ вызываем Clear().
            // Просто добавляем новую точку.

            instance.nutChart.AddData(0, timeHours, vals.z); // Nutrient

            float biomass = vals.y;
            instance.biomassRaw.Add(new Vector2(timeHours, biomass));

            if (biomass > instance.biomassMax)
            {
                instance.biomassMax = biomass;
                instance.bioMaxChanged = true;
            }
            if (instance.bioMaxChanged)
            {
                instance.RedrawBiomassModelSerie();
            }
            else
            {
                float normalized = biomass / instance.biomassMax * instance.bioExpMax;
                instance.bioChart.AddData(1, timeHours, normalized); // Model biomass
            }



            float ahl = vals.x;
            instance.ahlRaw.Add(new Vector2(timeHours, ahl));

            if (ahl > instance.ahlMax)
            {
                instance.ahlMax = ahl;
                instance.ahlMaxChanged = true;
            }
            if (instance.ahlMaxChanged)
            {
                instance.RedrawAhlModelSerie();
            }
            else
            {
                float normalized = ahl / instance.ahlMax * instance.ahlExpMax;
                instance.ahlChart.AddData(1, timeHours, normalized); // Model biomass
            }
        }


        private void RedrawBiomassModelSerie()
        {
            int serieIndex = 1; // Model biomass

            bioChart.series[serieIndex].ClearData();

            for (int i = 0; i < biomassRaw.Count; i++)
            {
                float x = biomassRaw[i].x;
                float y = biomassRaw[i].y / biomassMax * bioExpMax;

                bioChart.AddData(serieIndex, x, y);
            }
            instance.bioMaxChanged = false;    
        }

        private void RedrawAhlModelSerie()
        {
            int serieIndex = 1; // Model biomass

            ahlChart.series[serieIndex].ClearData();

            for (int i = 0; i < ahlRaw.Count; i++)
            {
                float x = ahlRaw[i].x;
                float y = ahlRaw[i].y / ahlMax * ahlExpMax;

                ahlChart.AddData(serieIndex, x, y);
            }
            instance.ahlMaxChanged = false;
        }
        public static void ResetCharts()
        {
            if (instance == null)
                return;

            instance.ShowExp();
        }
    }
}