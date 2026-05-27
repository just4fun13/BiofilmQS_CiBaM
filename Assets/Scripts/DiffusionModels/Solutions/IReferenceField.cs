using Assets.Scripts.NewGeneration;
using System.Collections.Generic;
using UnityEngine;

public interface IReferenceField
{
    string Name { get; }        // просто для логов
    double MaxTime { get; }     // до какого времени есть данные (для Comsol/Matlab)
    List<Vector2> PointsToCompare { get; }

    // Вызывается каждый раз, когда ты создаёшь новую сетку (новый N / новый DiffusionModel)
    void BindGrid(CellGeometry base2D, int width, int height);

    // Получить значение эталона в данной ячейке (i,j) в момент времени t.
    // Возвращает false, если значения нет (вне диапазона, нет точки и т.п.).
    bool TryGetValue(Vector2Int cell, double time, out double value);
    bool TryGetValue(Vector2 point, double time, out double value);
    double GetDelta(double time, double[,] C);
    public Vector2Int GetId(Vector2 points);
}
