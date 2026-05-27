using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CellularAutomaton
{
    public class SimControl : GenericSingletonClass<SimControl>
    {
        [SerializeField] private bool RestartTheScene = false;

        private float startC = 0.125f;
        private float startO = 3.000f;
        private float endC = 3.0f;
        private float endO = 3.0f;
        private float delta = 0.125f;
        private float C = 0.25f, N = 50f, O = 0.25f;
        int totalSteps, step;
        protected override void Awake()
        {
            base.Awake();
            C = startC - delta; 
            N = 50f;
            O = startO- delta;
            totalSteps = (int) (((endC - startC) / delta + 1) * ( (endO - startO) / delta + 1)) ;
            step = 0;
            // Создаем новый объект CultureInfo с нужными настройками
            CultureInfo culture = new CultureInfo("en-US");
            culture.NumberFormat.NumberDecimalSeparator = ".";

            // Устанавливаем культуру по умолчанию
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public Vector3 GetVals()
        {
            C += delta;
            if (C > endC)
            {
                C = startC;
                O += delta;
                if (O > endO)
                {
                   PlayTheSound();
                   Debug.Log("Full sycle done ");
                   O = startO;
                   N = 0.25f;
                }
            }
            step++;
            return new Vector3(C, N, O);
        }
        private void PlayTheSound()
        {
            FindObjectOfType<AudioSource>().Play();
        }

        public void ImFinished()
        {
            Debug.Log($"step : {C}/{endC} {O}/{endO} ->  {step}/{totalSteps}");
            if (RestartTheScene) 
                SceneMan.Instance.ReloadScene();
        }


    }
}
