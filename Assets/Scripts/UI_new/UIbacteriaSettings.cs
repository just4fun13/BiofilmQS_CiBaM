using Assets.Scripts.MVVM_CA.Models.ModelParams;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI_new
{
    public class UIbacteriaSettings :  UIcontent
    {

        [SerializeField] private TMP_Text muMaxText;
        [SerializeField] private TMP_Text initInocText;
        [SerializeField] private TMP_Text kS_text;
        [SerializeField] private TMP_Text Yxs_text;
        [SerializeField] private TMP_Text SpreadProb_text;

        [SerializeField] private Image BacImage;

        [SerializeField] private Sprite[] bacteriaSprites;



        private double maxMuMax = 3.0; //mg / l
        private double minMuMax = 0.1;
        private double muMax = 0.4;

        private double maxKs = 0.5; //mg / l
        private double minKs = 0.01;
        private double Ks = 0.05;
        private double deltaKS = 0.01;


        private double deltaYxs = 0.001;
        private double maxYxs = 0.1; //mg / l
        private double minYxs = 0.01;
        private double    Yxs = 0.045;

        private int MinInitInocCount = 1;
        private int MaxInitInocCount = 100;
        private int InitInocCount = 10;

        private int MaxSpreadProb = 100;
        private int MinSpreadProb = 1;
        private int SpreadProb = 5;

        private double delta = 0.1;


        private void Start()
        {
            if (PlayerPrefs.HasKey("muMax"))
                muMax = GetFloatVal("muMax", muMaxText);
            if (PlayerPrefs.HasKey("Ks"))
                Ks = GetFloatVal("Ks", kS_text);
            if (PlayerPrefs.HasKey("Yxs"))
                Yxs = GetFloatVal("Yxs", Yxs_text);
            if (PlayerPrefs.HasKey("InitInocCount"))
                InitInocCount = (int)GetIntVal("InitInocCount", initInocText);
            if (PlayerPrefs.HasKey("SpreadProb"))
                SpreadProb = (int)GetIntVal("SpreadProb", SpreadProb_text);
        }

        private float GetFloatVal(string key, TMP_Text tmp, string format = "0.###")
        {
            float t = PlayerPrefs.GetFloat(key);
            tmp.text = t.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            return t;
        }

        private int GetIntVal(string key, TMP_Text tmp)
        {
            int t = PlayerPrefs.GetInt(key);
            tmp.text = t.ToString();
            return t;
        }

        public void IncMuMax()
        {
            muMax = Math.Clamp(muMax + delta, minMuMax, maxMuMax);
            ModelParameters.bacteriaParameters.mUmax = muMax ;
            muMaxText.text = $"{muMax.ToString("0.0")} ч⁻¹";
            ModelParameters.ShowInDebug();
        }

        public void DecMuMax()
        {
            muMax = Math.Clamp(muMax - delta, minMuMax, maxMuMax);
            ModelParameters.bacteriaParameters.mUmax = muMax;
            muMaxText.text = $"{muMax.ToString("0.0")} ч⁻¹";
            ModelParameters.ShowInDebug();
        }

        public void IncKs()
        {
            Ks = Math.Clamp(Ks + deltaKS, minKs, maxKs);
            ModelParameters.bacteriaParameters.kS = Ks;
            kS_text.text = $"{Ks.ToString("0.00")} мг/л";
            ModelParameters.ShowInDebug();
        }
        public void DecKs()
        {
            Ks = Math.Clamp(Ks - deltaKS, minKs, maxKs);
            ModelParameters.bacteriaParameters.kS = Ks;
            kS_text.text = $"{Ks.ToString("0.00")} мг/л";
            ModelParameters.ShowInDebug();
        }

        public void IncInocCount()
        {
            InitInocCount = Math.Clamp(InitInocCount + 1, MinInitInocCount, MaxInitInocCount);
            ModelParameters.bacteriaParameters.InitialInoculationCount = InitInocCount;
            initInocText.text = $"{InitInocCount}";
            ModelParameters.ShowInDebug();
        }
        public void DecInocCount()
        {
            InitInocCount = Math.Clamp(InitInocCount - 1, MinInitInocCount, MaxInitInocCount);
            ModelParameters.bacteriaParameters.InitialInoculationCount = InitInocCount;
            initInocText.text = $"{InitInocCount}";
            ModelParameters.ShowInDebug();
        }
        public void IncSpreadProb()
        {
            SpreadProb = Math.Clamp(SpreadProb + 1, MinSpreadProb, MaxSpreadProb);
            ModelParameters.bacteriaParameters.SpreadProbability = SpreadProb * 1d/100;
            SpreadProb_text.text = $"{SpreadProb} %";
            ModelParameters.ShowInDebug();
        }
        public void DecSpreadProb()
        {
            SpreadProb = Math.Clamp(SpreadProb - 1, MinSpreadProb, MaxSpreadProb);
            ModelParameters.bacteriaParameters.SpreadProbability = SpreadProb * 1d/100;
            SpreadProb_text.text = $"{SpreadProb} %";
            ModelParameters.ShowInDebug();
        }
        public void IncYxs()
        {
            Yxs = Math.Clamp(Yxs + deltaYxs, minYxs, maxYxs);
            ModelParameters.bacteriaParameters.Yxs = Yxs;
            Yxs_text.text = $"{Yxs.ToString("0.000")} г/г";
            ModelParameters.ShowInDebug();
        }
        public void DecYxs()
        {
            Yxs = Math.Clamp(Yxs - deltaYxs, minYxs, maxYxs);
            ModelParameters.bacteriaParameters.Yxs = Yxs;
            Yxs_text.text = $"{Yxs.ToString("0.000")} г/г";
            ModelParameters.ShowInDebug();
        }

        public override void ReadAll()
        {
            Debug.Log($"ReadAll for {gameObject.name} my own");

            ModelParameters.bacteriaParameters.mUmax = muMax;
            ModelParameters.bacteriaParameters.kS = Ks;
            ModelParameters.bacteriaParameters.InitialInoculationCount = InitInocCount;
            ModelParameters.bacteriaParameters.Yxs = Yxs;
            ModelParameters.bacteriaParameters.SpreadProbability = SpreadProb * 1d / 100;
            PlayerPrefs.SetFloat("muMax", (float)muMax);
            PlayerPrefs.SetFloat("Ks", (float)Ks);
            PlayerPrefs.SetFloat("Yxs", (float)Yxs);
            PlayerPrefs.SetInt("InitInocCount", InitInocCount);
            PlayerPrefs.SetInt("SpreadProb", SpreadProb);
            PlayerPrefs.Save();
        }

    }
}
