using System;

public class ModelScaler
{
    private double cellSizeInMeters; // Размер клетки в метрах
    private double maxTimeStep;     // Максимально допустимый временной шаг

    // Конструктор
    public ModelScaler(double cellSizeInMeters)
    {
        this.cellSizeInMeters = cellSizeInMeters;
    }

    // Пересчет коэффициента диффузии
    public double ScaleDiffusionCoefficient(double originalDiffusionCoefficient)
    {
        // Диффузия зависит от квадрата размера клетки (метры²/секунду)
        return originalDiffusionCoefficient / Math.Pow(cellSizeInMeters, 2);
    }

    // Пересчет скорости роста бактерий
    public double ScaleGrowthRate(double originalGrowthRate)
    {
        // Скорость роста не зависит от размера клетки, но может быть нормализована по времени
        return originalGrowthRate;
    }

    // Пересчет скорости потребления питательных веществ
    public double ScaleConsumptionRate(double originalConsumptionRate)
    {
        // Потребление пересчитывается в зависимости от объема ячейки (пропорционально кубу размера клетки)
        return originalConsumptionRate * Math.Pow(cellSizeInMeters, 3);
    }

    // Вычисление максимально допустимого временного шага
    private double CalculateTimeStep(double originalDiffusionCoefficient)
    {
        // Условие Куранта–Фридрихса–Леви для диффузии:
        // Δt ≤ (Δx^2) / (2 * D), где D — максимальный коэффициент диффузии
        double scaledDiffusionCoefficient = ScaleDiffusionCoefficient(originalDiffusionCoefficient);

        double deltaTime = 0.5*1d/scaledDiffusionCoefficient;
        maxTimeStep = deltaTime;
        return deltaTime; // Возвращаем вычисленный временной шаг
    }

    // Получение максимально допустимого временного шага
    public double GetMaxTimeStep()
    {
        return maxTimeStep;
    }

    // Пример использования: пересчет всех параметров
    public BacterialModelParameters ScaleParameters(BacterialModelParameters originalParams)
    {
        return new BacterialModelParameters(
            CalculateTimeStep(originalParams.DiffusionCoefficientNutrient),
            ScaleDiffusionCoefficient(originalParams.DiffusionCoefficientNutrient),
            ScaleDiffusionCoefficient(originalParams.DiffusionCoefficientAHL),
            ScaleGrowthRate(originalParams.MaxGrowthRate),
            originalParams.HalfSaturationConstant, // Не зависит от размера клетки
            ScaleConsumptionRate(originalParams.NutrientConsumptionRate),
            ScaleConsumptionRate(originalParams.AHLDegradationRate)
        );
    }
}