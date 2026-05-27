using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


namespace CellularAutomaton
{
    public class Tester : MonoBehaviour
    {
        [SerializeField] private GameObject CircleObj;
        [SerializeField] private TMP_Text outputText;

        private void Awake()
        {

            ExampleUsage();
            //StartCoroutine(TestStep());

        }

        void ExampleUsage()
        {
            // Исходные параметры модели
            var originalParams = new BacterialModelParameters(
                timeStepsInSeconds: 1,
                diffusionCoefficientNutrient: 1.6e-9, // 6e-10 м²/с
                diffusionCoefficientAHL: 5e-11,     // 5e-11 м²/с
                maxGrowthRate: 1.0,                // 1.0 1/ч
                halfSaturationConstant: 0.5,       // 0.5 мг/л
                nutrientConsumptionRate: 0.1,      // 0.1 мг/(клетка·ч)
                ahlDegradationRate: 0.05           // 0.05 1/ч
            );

            // Создаем экземпляр ModelScaler
            double cellSizeInMeters = 1e-6; // 1 микрометр
            var scaler = new ModelScaler(cellSizeInMeters);

            // Пересчитываем параметры
            var scaledParams = scaler.ScaleParameters(originalParams);

            // Выводим результаты
            scaledParams.PrintParameters();
            Debug.Log($"Max Time Step: {scaler.GetMaxTimeStep()} seconds");
        }

        private IEnumerator TestStep()
        {
            float a, b, c;
            int i, j, k;
            k = 0;
            c = 0;
            b = 11 * 11 * 5;
            for (i = 0; i <= 10; i++)
            for (j = 0; j <= 10; j++)
            for (a = 0.1f; a <= 0.5f; a += 0.1f)
            {
                        yield return new WaitForSeconds(0.03f);
                        k++;
                        c = k / b * 100;
                        outputText.text = $"i = {i}{Environment.NewLine}j = {j} {Environment.NewLine} a = {a}{Environment.NewLine} % = {c}";
            }


        }


        private void AddPointsSquare(Vector2Int cellId)
        {
            Vector2 offset = cellId;
            AddSphere(offset + 0.5f * Vector2.one);
            AddSphere(offset - 0.5f * Vector2.one);
            AddSphere(offset + 0.5f * new Vector2(1, -1));
            AddSphere(offset + 0.5f * new Vector2(-1, 1));
        }

        private void AddPointsHexagon(Vector2Int cellId)
        {
            Vector2 offset = cellId;
            AddSphere(offset +  new Vector2(0, 0.5f));
            AddSphere(offset +  new Vector2(0, -0.5f));
            AddSphere(offset +  new Vector2(0.5f, 0.25f));
            AddSphere(offset +  new Vector2(-0.5f, 0.25f));
            AddSphere(offset +  new Vector2(0.5f, -0.25f));
            AddSphere(offset +  new Vector2(-0.5f, -0.25f));
        }


        private void AddSphere(Vector2 pos)
        {
            Instantiate(CircleObj, pos, Quaternion.identity);
        }


    }
}
