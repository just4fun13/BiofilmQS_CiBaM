using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellularAutomaton;

namespace Assets.Scripts.MVVM_CA.Models.ModelParams
{
    [Serializable]
    public class ModelParameters
    {
        public static MainParameters mainParameters = new MainParameters();
        public static GeometryParameters geometryParameters = new GeometryParameters();
        public static NutrientParameters nutrientParameters = new NutrientParameters();
        public static BacteriaParameters bacteriaParameters = new BacteriaParameters();
        public static AHLParameters aHLParameters = new AHLParameters();
        public static void ShowInDebug()
        {
            string pars = $"MaxTIme = {mainParameters.MaxTimeInHours}, DU = {mainParameters.DynamicUpdate}" +
                $"DS = {mainParameters.DoScreens}, TimeStep = {mainParameters.TimeStep}, WL = {mainParameters.WriteLog}" +
                $"{geometryParameters.gridType} : [{geometryParameters.AreaWidth}," +
                $"{geometryParameters.AreaHeight}, {geometryParameters.AreaLength}] scale:" +
                $"{geometryParameters.modelScale};  nutrient: D = {nutrientParameters.NutrientDiffusion}," +
                $"Cno = {nutrientParameters.InitialDensity}; bacteria : Ks  = {bacteriaParameters.kS}," +
                $"n0 = {bacteriaParameters.InitialInoculationCount}, ," +
                $"mu = {bacteriaParameters.mUmax}, yXS = {bacteriaParameters.Yxs};" +
                $"alpha = {aHLParameters.alpha}, betta = {aHLParameters.betta};" +
                $"Dahl = {aHLParameters.AHLdiffusion}, degr = {aHLParameters.degradationK}," +
                $"ahlPowerK = {aHLParameters.powerK}, AHLth = {aHLParameters.AHLthreshold}";
            Debug.Log(pars);
        }


        public static bool Is2d => geometryParameters.gridType == ModelType.SimpleSquare ||
                                   geometryParameters.gridType == ModelType.Hexagon ||
                                   geometryParameters.gridType == ModelType.ExtendedSquare;
    }
}
