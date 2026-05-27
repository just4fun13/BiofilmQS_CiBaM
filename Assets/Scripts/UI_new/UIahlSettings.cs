using Assets.Scripts.MVVM_CA.Models.ModelParams;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI_new
{
    public class UIahlSettings : UIcontent
    {

        [SerializeField] private TMP_Text AHLdiffusionText;
        [SerializeField] private TMP_Text AlphaText;
        [SerializeField] private TMP_Text BettaText;
        [SerializeField] private TMP_Text AHLpowerKtext;
        [SerializeField] private TMP_Text AHLthText;
        [SerializeField] private TMP_Text AHLdegrText;

        public bool UseAHL { get; private set; } = true;



        private double delta_3 = 0.001;
        private double delta_2 = 0.01;
        private double delta_1 = 0.1;


        private double maxDif = 3.0; //mg / l
        private double minDif = 0.1;
        private double Dif = 0.278;

        private double maxBetta = 0.5; //mg / l
        private double minBetta = 0.01;
        private double betta = 0.05;
        
        private double MinAlpha = 0.00;
        private double MaxAlpha = 1;
        private double alpha = 0.01;

        private double maxPowerK = 5.0; //mg / l
        private double minPowerK = 1.0;
        private double powerK = 2.5;

        private double MaxUth = 5;
        private double MinUth = 0;
        private double Uth = 1;

        private double MaxUdegr = 0.05;  // h^-1
        private double MinUdegr = 0.01;
        private double Udegr = 0.05;


        public void SetUseAHL(bool notUseAHL)
        {
            UseAHL = !notUseAHL;
            ModelParameters.aHLParameters.UseAHL = UseAHL;
        }


        public void IncAHLdif()
        {
            Dif = Math.Clamp(Dif + delta_3, minDif, maxDif);
            ModelParameters.aHLParameters.AHLdiffusion = Dif;
            AHLdiffusionText.text = $"{Dif.ToString("0.000")}";
            ModelParameters.ShowInDebug();
        }

        public void DecAHLdif()
        {
            Dif = Math.Clamp(Dif - delta_3, minDif, maxDif);
            ModelParameters.aHLParameters.AHLdiffusion = Dif;
            AHLdiffusionText.text = $"{Dif.ToString("0.000")}";
            ModelParameters.ShowInDebug();
        }

        public void IncBetta()
        {
            betta = Math.Clamp(betta + delta_2, minBetta, maxBetta);
            ModelParameters.aHLParameters.betta = betta;
            BettaText.text = $"{betta.ToString("0.00")}";
            ModelParameters.ShowInDebug();
        }
        public void DecBetta()
        {
            betta = Math.Clamp(betta - delta_2, minBetta, maxBetta);
            ModelParameters.aHLParameters.betta = betta;
            BettaText.text = $"{betta.ToString("0.00")}";
            ModelParameters.ShowInDebug();
        }

        public void IncAlpha()
        {
            alpha = Math.Clamp(alpha + delta_2, MinAlpha, MaxAlpha);
            ModelParameters.aHLParameters.alpha = alpha;
            AlphaText.text = $"{alpha.ToString("0.00")}";
            ModelParameters.ShowInDebug();
        }
        public void DecAlpha()
        {
            alpha = Math.Clamp(alpha - delta_2, MinAlpha, MaxAlpha);
            ModelParameters.aHLParameters.alpha = alpha;
            AlphaText.text = $"{alpha.ToString("0.00")}";
            ModelParameters.ShowInDebug();
        }
        public void IncUth()
        {
            Uth = Math.Clamp(Uth + delta_1, MinUth, MaxUth);
            ModelParameters.aHLParameters.AHLthreshold = Uth;
            AHLthText.text = $"{Uth.ToString("0.0")}";
            ModelParameters.ShowInDebug();
        }
        public void DecUth()
        {
            Uth = Math.Clamp(Uth - delta_1, MinUth, MaxUth);
            ModelParameters.aHLParameters.AHLthreshold = Uth;
            AHLthText.text = $"{Uth.ToString("0.0")}";
            ModelParameters.ShowInDebug();
        }
        public void IncUdegr()
        {
            Udegr = Math.Clamp(Udegr + delta_2, MinUdegr, MaxUdegr);
            ModelParameters.aHLParameters.degradationK = Udegr;
            AHLdegrText.text = $"{Udegr.ToString("0.00")}";
            ModelParameters.ShowInDebug();
        }
        public void DecUdegr()
        {
            Udegr = Math.Clamp(Udegr - delta_2, MinUdegr, MaxUdegr);
            ModelParameters.aHLParameters.degradationK = Udegr;
            AHLdegrText.text = $"{Udegr.ToString("0.00")}";
            ModelParameters.ShowInDebug();
        }

        public void IncPowerK()
        {
            powerK = Math.Clamp(powerK + delta_1, minPowerK, maxPowerK);
            ModelParameters.aHLParameters.powerK = powerK;
            AHLpowerKtext.text = $"{powerK.ToString("0.0")}";
            ModelParameters.ShowInDebug();
        }
        public void DecPowerK()
        {
            powerK = Math.Clamp(powerK - delta_1, minPowerK, maxPowerK);
            ModelParameters.aHLParameters.powerK = powerK;
            AHLpowerKtext.text = $"{powerK.ToString("0.0")}";
            ModelParameters.ShowInDebug();
        }
        public override void ReadAll()
        {
            ModelParameters.aHLParameters.AHLdiffusion = Dif;
            ModelParameters.aHLParameters.AHLthreshold = Uth;
            ModelParameters.aHLParameters.alpha = alpha;
            ModelParameters.aHLParameters.betta = betta;
            ModelParameters.aHLParameters.powerK = powerK;
            ModelParameters.aHLParameters.degradationK = Udegr;

        }
    }
}
