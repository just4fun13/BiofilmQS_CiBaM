using System;
using System.Collections;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;

namespace CellularAutomaton
{
    public class InputHandler : GenericSingletonClass<InputHandler>
    {
        private int minMaxDiv = 10;
        private int maxMaxDiv = 500;
        private int maxDivV = 50;

        private int minMaxTry = 10;
        private int maxMaxTry = 70;
        private int maxTryV = 35;

        private int minAreaW = 30;
        private int maxAreaW = 5000;
        private int AreaWV = 30;

        private float minProbDec = 1f;
        private float maxProbDec = 10f;
        private float probDecV = 2f;

        private GridType gridType = 0;

        [SerializeField] private TMP_InputField maxTry;
        [SerializeField] private TMP_InputField areaW;
        [SerializeField] private TMP_InputField maxDiv;
        [SerializeField] private TMP_InputField probDec;
        [SerializeField] private TMP_Dropdown GridType;


        private void Awake()
        {
            ReadParams();
            GridType.value = (int)gridType;
            areaW.text = AreaWV.ToString();
            maxDiv.text = maxDivV.ToString();
            maxTry.text = maxTryV.ToString();
            probDec.text = probDecV.ToString();
        }

        private void ReadParams()
        {
            float[] ps = ParameterHead.Instance.ReadParameters();
            AreaWV = (int)ps[0];
            probDecV = ps[1];
            maxTryV = (int)ps[2];
            maxDivV = (int)ps[3];
            gridType = (GridType)((int)ps[4]);
        }

        public void AreaWChange()
        {
            string newValue = areaW.text;
            int.TryParse(newValue, out AreaWV);
            Debug.Log($"read Area <{newValue}>= {AreaWV}, clamp to {minAreaW}={maxAreaW}");
            AreaWV = Mathf.Clamp(AreaWV, minAreaW, maxAreaW);
            areaW.text = AreaWV.ToString();
            RefreshParams();
        }

        public void MaxDivChange()
        {
            string newValue = maxDiv.text;
            int.TryParse(newValue, out maxDivV);
            maxDivV = Mathf.Clamp(maxDivV, minMaxDiv, maxMaxDiv);
            maxDiv.text = maxDivV.ToString();
            RefreshParams();
        }

        public void MaxTryChange()
        {
            string newValue = maxTry.text;
            int.TryParse(newValue, out maxTryV);
            maxTryV = Mathf.Clamp(maxTryV, minMaxTry, maxMaxTry);
            maxTry.text = maxTryV.ToString();
            RefreshParams();
        }

        public void DecProbChange()
        {
            string newValue = probDec.text;
            float.TryParse(newValue, out probDecV);
            probDecV = Mathf.Clamp(probDecV, minProbDec, maxProbDec);
            probDec.text = probDecV.ToString();
            RefreshParams();
        }

        public void GridTypeChanged()
        {
            gridType = (GridType)GridType.value;
            RefreshParams();
        }

        private void RefreshParams()
        {
            ParameterHead.Instance.WriteParameters(new float[] 
            {
                 AreaWV, probDecV, maxTryV, maxDivV, (float) gridType
            });

        }
}
}
