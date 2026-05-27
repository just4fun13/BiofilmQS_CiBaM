using Assets.Scripts.MVVM_CA.Models.ModelParams;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI_new
{
    public class UInutrientSettings : UIcontent
    {

        [SerializeField] private TMP_Text DiffisionKoefTmp;
        [SerializeField] private TMP_Text NutrDensityTmp;
        [SerializeField] private Image NutrImage;

        [SerializeField] private Sprite[] nutrientSprites;

        [SerializeField] private GameObject AreaLengthField;

 
        private double maxNutrDensity = 2; //mg / l
        private double minNutrDensity   = 0.01;
        private double nutrDensity = 1;
        private double deltaDens = 0.01;

        private double minDiffusionKoef = 0.1;
        private double maxDiffusionKoef = 4;
        private double diffusionKoef = 2;
        private double deltaDif = 0.1;


        public void IncNutrDensity()
        {
            nutrDensity = Math.Clamp(nutrDensity + deltaDens, minNutrDensity, maxNutrDensity);
            ModelParameters.nutrientParameters.InitialDensity = nutrDensity;
            NutrDensityTmp.text = $"{nutrDensity.ToString("0.00")}";
            ModelParameters.ShowInDebug();
        }

        public void DecNutrDensity()
        {
            nutrDensity = Math.Clamp(nutrDensity - deltaDens, minNutrDensity, maxNutrDensity);
            ModelParameters.nutrientParameters.InitialDensity = nutrDensity;
            NutrDensityTmp.text = $"{nutrDensity.ToString("0.00")}";
            ModelParameters.ShowInDebug();
        }

        public void IncDiffusionKoef()
        {
            diffusionKoef = Math.Clamp(diffusionKoef + deltaDif, minDiffusionKoef, maxDiffusionKoef);
            ModelParameters.nutrientParameters.NutrientDiffusion = diffusionKoef;
            DiffisionKoefTmp.text = $"{diffusionKoef.ToString("0.0")}";
            ModelParameters.ShowInDebug();
        }

        public void DecDiffusionKoef()
        {
            diffusionKoef = Math.Clamp(diffusionKoef -  deltaDif, minDiffusionKoef, maxDiffusionKoef);
            ModelParameters.nutrientParameters.NutrientDiffusion = diffusionKoef;
            DiffisionKoefTmp.text = $"{diffusionKoef.ToString("0.0")}";
            ModelParameters.ShowInDebug();
        }

        public override void ReadAll()
        {
            ModelParameters.nutrientParameters.InitialDensity = nutrDensity;
            ModelParameters.nutrientParameters.NutrientDiffusion = diffusionKoef;
        }
    }
}
