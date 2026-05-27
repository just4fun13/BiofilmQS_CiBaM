using System.Collections.Generic;
using UnityEngine;

public class BacterialModelParameters
{
    // Параметры диффузии (в см²/с)
    public double DiffusionCoefficientNutrient { get; set; } // Коэффициент диффузии питательных веществ
    public double DiffusionCoefficientAHL { get; set; }      // Коэффициент диффузии AHL

    // Параметры роста бактерий
    public double MaxGrowthRate { get; set; }               // Максимальная скорость роста бактерий (в 1/ч)
    public double HalfSaturationConstant { get; set; }      // Константа Михаэлиса (в мг/л)

    // Параметры потребления
    public double NutrientConsumptionRate { get; set; }     // Скорость потребления питательных веществ (в мг/(клетка·ч))
    public double AHLDegradationRate { get; set; }          // Скорость разложения AHL (в 1/ч)

    // Единицы измерения
    public string DiffusionCoefficientUnit => "cm²/s";
    public string GrowthRateUnit => "1/h";
    public string NutrientConsumptionUnit => "mg/(cell·h)";
    public string AHLDegradationUnit => "1/h";
    private Dictionary<string, double> diffusionCoefficients = new Dictionary<string, double>(); 

    // Конструктор для инициализации параметров
    public BacterialModelParameters(
        double timeStepsInSeconds,
        double diffusionCoefficientNutrient,
        double diffusionCoefficientAHL,
        double maxGrowthRate,
        double halfSaturationConstant,
        double nutrientConsumptionRate,
        double ahlDegradationRate)
    {
        DiffusionCoefficientNutrient = diffusionCoefficientNutrient;
        DiffusionCoefficientAHL = diffusionCoefficientAHL;
        MaxGrowthRate = maxGrowthRate;
        HalfSaturationConstant = halfSaturationConstant;
        NutrientConsumptionRate = nutrientConsumptionRate;
        AHLDegradationRate = ahlDegradationRate;
    }

    // Метод для загрузки предустановленных значений диффузии
    private void LoadDefaultDiffusionCoefficients()
    {
        // Пример значений коэффициентов диффузии (в см²/с)
        diffusionCoefficients["Glucose"] = 6e-6;       // Глюкоза
        diffusionCoefficients["Oxygen"] = 2e-5;        // Кислород
        diffusionCoefficients["AminoAcids"] = 1e-6;    // Аминокислоты
        diffusionCoefficients["Lactose"] = 5e-6;       // Лактоза
        diffusionCoefficients["Nitrate"] = 8e-6;       // Нитраты
    }

    // Вывод параметров для отладки
    public void PrintParameters()
    {
        Debug.Log($"Diffusion Coefficient (Nutrient): {DiffusionCoefficientNutrient} {DiffusionCoefficientUnit}");
        Debug.Log($"Diffusion Coefficient (AHL): {DiffusionCoefficientAHL} {DiffusionCoefficientUnit}");
        Debug.Log($"Max Growth Rate: {MaxGrowthRate} {GrowthRateUnit}");
        Debug.Log($"Half Saturation Constant: {HalfSaturationConstant} mg/L");
        Debug.Log($"Nutrient Consumption Rate: {NutrientConsumptionRate} {NutrientConsumptionUnit}");
        Debug.Log($"AHL Degradation Rate: {AHLDegradationRate} {AHLDegradationUnit}");
    }
}