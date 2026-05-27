using Assets.Scripts.MVVM_CA.Models._2D;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace Assets.Scripts.MVVM_CA
{
    public class UIinput : MonoBehaviour
    {

        [SerializeField] private TMP_InputField[] sizeTexts;
        [SerializeField] private TMP_InputField[] mainParsTexts;
        [SerializeField] private TMP_InputField[] ahlTexts;
        [SerializeField] private TMP_InputField[] washTexts;

        [SerializeField] private TMP_InputField[] epsTexts;
        [SerializeField] private TMP_InputField[] lacTexts;
        [SerializeField] private TMP_Dropdown gridTypeDropdown;
        [SerializeField] private Slider timeSlider;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_InputField totalHoursText;
        [SerializeField] private TMP_InputField randomSeedText;

        
        public enum InputType { _2d, _3d}

        private InputType inputType;

        private int Scale = 4;

        public float DeltaTimeValue => timeSlider.value;

        public bool ShowLog = false;


        public float TotalHours => float.Parse(totalHoursText.text);
        private string InputData => inputType == InputType._2d ? inputsData : inputsData3d;

        private string inputsData = "inputsData";
        private string inputsData3d = "inputsData3d";
        private char delimeter = ';';
        public Vector2 GetSize => new Vector2(float.Parse(sizeTexts[0].text), float.Parse(sizeTexts[1].text));
        public Vector3Int GetSize3 => new Vector3Int(int.Parse(sizeTexts[0].text), int.Parse(sizeTexts[1].text), int.Parse(sizeTexts[2].text));

        public int GetGridType => gridTypeDropdown.value;
        public int RandomSeed => int.Parse(randomSeedText.text);


        public void ShowHideLog(bool newVal)
        {
            ShowLog = newVal;
        }

        public void SetTime(float timeValue)
        {
            if (Scale  == 4) 
                timeText.text = $"{timeValue:F2} s";
            else
                if (Scale == 5)
                    timeText.text = $"{timeValue:F4} s";
                else
                    timeText.text = $"{timeValue:F6} s";
        }

        public int UnitSizeMicron => (int) Mathf.Pow(10, 6 - Scale);

        public void SetScaleFromUnitSize(float unitSize)
        {
            if (UnitSizeMicron == 1)
            {
                Scale =  6;
                return;
            }
            if (UnitSizeMicron == 10)
            {
                Scale = 5;
                return;
            }
            Scale = 4;
        }

        public void SetScale(int newScale)
        {
            if (Scale == newScale) return;
            int dScale = newScale - Scale;
            float multip = Mathf.Pow(100, dScale);
            Debug.Log($"scale: {Scale} -> {newScale} multip = {multip}");
            MultiplyText(mainParsTexts[0], multip);
            MultiplyText(mainParsTexts[2], 1/multip);
            MultiplyText(ahlTexts[0], multip);
//            MultiplyText(ahlTexts[0], multip);
            timeSlider.minValue /= multip;
            timeSlider.maxValue /= multip;
            timeSlider.value = (timeSlider.minValue + timeSlider.maxValue)/2f;
            Scale = newScale;
            SetTime(timeSlider.value);
        }

        private void MultiplyText(TMP_InputField t, float m)
        {
            float val = float.Parse(t.text);
            t.text = $"{m*val}";
        }

        public void SetInputType(InputType input)
        {
            inputType = input;
        }
        public float[] GetMainParsInputs()
        {
            float[] d = new float[mainParsTexts.Length];
            for (int i = 0; i < mainParsTexts.Length; i++)
            {
                d[i] = float.Parse(mainParsTexts[i].text);
            }
            return d;
        }
        public float[] GetAhlParsInputs()
        {
            float[] d = new float[ahlTexts.Length];
            for (int i = 0; i < ahlTexts.Length; i++)
            {
                d[i] = float.Parse(ahlTexts[i].text);
            }
            return d;
        }
        public float[] GetEpsParsInputs()
        {
            float[] d = new float[epsTexts.Length];
            for (int i = 0; i < epsTexts.Length; i++)
            {
                d[i] = float.Parse(epsTexts[i].text);
            }
            return d;
        }
        public float[] GetLactonaseParsInputs()
        {
            float[] d = new float[lacTexts.Length];
            for (int i = 0; i < lacTexts.Length; i++)
            {
                d[i] = float.Parse(lacTexts[i].text);
            }
            return d;
        }
        public float[] GetWashParsInputs()
        {
            float[] d = new float[washTexts.Length];
            for (int i = 0; i < washTexts.Length; i++)
            {
                d[i] = float.Parse(washTexts[i].text);
            }
            return d;
        }
        public void SetInputs(float[] d)
        {
            if (PlayerPrefs.HasKey(InputData))
            {
                Debug.Log("Data was loaded");
                string stringData = PlayerPrefs.GetString(InputData);
                float[] dNew = GetDat(stringData);
                gridTypeDropdown.value = (int)(dNew[0]);
                int k = 1;
                for (int i = 0; i < sizeTexts.Length; i++)
                {
                    sizeTexts[i].text = $"{dNew[k]}";
                    k++;
                }
                for (int i = 0; i < mainParsTexts.Length; i++)
                {
                    mainParsTexts[i].text = $"{dNew[k]}";
                    k++;
                }
                for (int i = 0; i < ahlTexts.Length; i++)
                {
                    ahlTexts[i].text = $"{dNew[k]}";
                    k++;
                }
                for (int i = 0; i < epsTexts.Length; i++)
                {
                    epsTexts[i].text = $"{dNew[k]}";
                    k++;
                }
                for (int i = 0; i < lacTexts.Length; i++)
                {
                    lacTexts[i].text = $"{dNew[k]}";
                    k++;
                }
                for (int i = 0; i < washTexts.Length; i++)
                {
                    washTexts[i].text = $"{dNew[k]}";
                    k++;
                }
            }
            else
            {
                Debug.Log("Data was not found");
                gridTypeDropdown.value = 0;
                int k = 1;
                for (int i = 0; i < sizeTexts.Length; i++)
                {
                    sizeTexts[i].text = $"{d[k]}";
                    k++;
                }
                for (int i = 0; i < mainParsTexts.Length; i++)
                {
                    mainParsTexts[i].text = $"{d[k]}";
                    k++;
                }
                for (int i = 0; i < ahlTexts.Length; i++)
                {
                    ahlTexts[i].text = $"{d[k]}";
                    k++;
                }
                for (int i = 0; i < epsTexts.Length; i++)
                {
                    epsTexts[i].text = $"{d[k]}";
                    k++;
                }
                for (int i = 0; i < lacTexts.Length; i++)
                {
                    lacTexts[i].text = $"{d[k]}";
                    k++;
                }
                for (int i = 0; i < washTexts.Length; i++)
                {
                    washTexts[i].text = $"{d[k]}";
                    k++;
                }
            }
        }
        private float[] GetDat(string raw)
        {
            string[] valStr = raw.Split(delimeter);
            float[] val = new float[valStr.Length];
            for (int i = 0; i < val.Length; i++)
            {
                val[i] = float.Parse(valStr[i]);
            }
            return val;
        }
        public void SaveData()
        {
            string str = $"";
            str += $"{gridTypeDropdown.value}";
            int k = 1;
            for (int i = 0; i < sizeTexts.Length; i++)
            {
                str += $"{delimeter}{sizeTexts[i].text}";
                k++;
            }
            for (int i = 0; i < mainParsTexts.Length; i++)
            {
                str += $"{delimeter}{mainParsTexts[i].text}";
                k++;
            }
            for (int i = 0; i < ahlTexts.Length; i++)
            {
                str += $"{delimeter}{ahlTexts[i].text}";
                k++;
            }
            for (int i = 0; i < epsTexts.Length; i++)
            {
                str += $"{delimeter}{epsTexts[i].text}";
                k++;
            }
            for (int i = 0; i < lacTexts.Length; i++)
            {
                str += $"{delimeter}{lacTexts[i].text}";
                k++;
            }
            for (int i = 0; i < washTexts.Length; i++)
            {
                str += $"{delimeter}{washTexts[i].text}";
                k++;
            }
            Debug.Log($"Data was saved [{k}]=({str})");
            PlayerPrefs.SetString(InputData, str);
            PlayerPrefs.Save(); 
        }


        public void SaveCurrentConfig()
        {
            SimulationConfig config = new SimulationConfig();

            config.width  = int.Parse(sizeTexts[0].text);
            config.height = int.Parse(sizeTexts[1].text);
            config.gridType = GetGridType.ToString();

            config.unitSizeMicrom = UnitSizeMicron;
            config.timeStepSeconds = DeltaTimeValue;
            config.totalTimeHours = float.Parse(totalHoursText.text);

            config.nutrientDiffusion = float.Parse(mainParsTexts[0].text);
            config.initialNutrient   = float.Parse(mainParsTexts[1].text);
            config.muMax             = float.Parse(mainParsTexts[2].text);
            config.initialCells      = float.Parse(mainParsTexts[3].text);
            config.spreadProbability = float.Parse(mainParsTexts[4].text);
            config.ks                = float.Parse(mainParsTexts[5].text);
            config.yxs               = float.Parse(mainParsTexts[6].text);

            config.ahlDiffusion    = float.Parse(ahlTexts[0].text);
            config.ahlAlpha        = float.Parse(ahlTexts[1].text);
            config.ahlBeta         = float.Parse(ahlTexts[2].text);
            config.ahlDegradation  = float.Parse(ahlTexts[3].text);
            config.hillCoefficient = float.Parse(ahlTexts[4].text);
            config.ahlThreshold    = float.Parse(ahlTexts[5].text);
            config.ahlReference    = float.Parse(ahlTexts[6].text);

            config.liquidWashout   = float.Parse(washTexts[0].text);
            config.bacteriaWashout = float.Parse(washTexts[1].text);
            config.nutrientInflow  = float.Parse(washTexts[2].text);

            config.randomSeed = int.Parse(randomSeedText.text);

            SimulationConfigStorage.Save(config, "successful_simulation_001");
        }

        public void LoadConfig()
        {
            SimulationConfig config = SimulationConfigStorage.LoadOrDefault("successful_simulation_001");

            if (config == null)
                return;

            ApplyConfig(config);
        }

        private void ApplyConfig(SimulationConfig config)
        {
            sizeTexts[0].text = config.width.ToString();
            sizeTexts[1].text = config.height.ToString();

            SetScaleFromUnitSize(config.unitSizeMicrom);
            timeText.text =  config.timeStepSeconds.ToString();
            totalHoursText.text = config.totalTimeHours.ToString();

            mainParsTexts[0].text = config.nutrientDiffusion.ToString();
            mainParsTexts[1].text = config.initialNutrient.ToString();
            mainParsTexts[2].text = config.muMax.ToString();
            mainParsTexts[3].text = config.initialCells.ToString();
            mainParsTexts[4].text = config.spreadProbability.ToString();
            mainParsTexts[5].text = config.ks.ToString();
            mainParsTexts[6].text = config.yxs.ToString();

            ahlTexts[0].text = config.ahlDiffusion.ToString();
            ahlTexts[1].text = config.ahlAlpha.ToString();
            ahlTexts[2].text = config.ahlBeta.ToString();
            ahlTexts[3].text = config.ahlDegradation.ToString();
            ahlTexts[4].text = config.hillCoefficient.ToString();
            ahlTexts[5].text = config.ahlThreshold.ToString();
            ahlTexts[6].text = config.ahlReference.ToString();

            washTexts[0].text   = config.liquidWashout.ToString();
            washTexts[1].text = config.bacteriaWashout.ToString();
            washTexts[2].text  = config.nutrientInflow.ToString();

            randomSeedText.text = config.randomSeed.ToString();
        }
    }
}
