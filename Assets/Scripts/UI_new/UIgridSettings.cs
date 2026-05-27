using Assets.Scripts.MVVM_CA;
using Assets.Scripts.MVVM_CA.Models.ModelParams;
using CellularAutomaton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI_new
{
    public class UIgridSettings : UIcontent
    {


        [SerializeField] private TMP_Text DimensionTmp;
        [SerializeField] private TMP_Text ScaleTmp;
        [SerializeField] private TMP_InputField WidthTmp;
        [SerializeField] private TMP_InputField HeightTmp;
        [SerializeField] private TMP_InputField LengthhTmp;
        [SerializeField] private Image GridTypeImage;

        [SerializeField] private Sprite[] gridTypes2d;
        [SerializeField] private Sprite[] gridTypes3d;

        [SerializeField] private TMP_Text[] lengthCaptions;
        [SerializeField] private GameObject AreaLengthField;

        [SerializeField] private TMP_Text cellSizeText;
        [SerializeField] private TMP_Text cellSizeText2;
        [SerializeField] private TMP_Text scaleText;
        [SerializeField] private TMP_Text sqrText;
        [SerializeField] private TMP_Text totalSqrText;
        [SerializeField] private TMP_Text totalCells;
        [SerializeField] private TMP_Text cellVol;

        private int _ScaleId = 4;

        private string[] Dimensions = new string[] { "2D", "3D" };

        private ModelType[] Grids2 = { ModelType.SimpleSquare, ModelType.Hexagon, ModelType.ExtendedSquare };

        private ModelType[] Grids3 = { ModelType.SimpleCube, ModelType.TruncOct, ModelType.ExtendedCube };



        private bool DimensionIs2D = true;
        private int maxScaleId = 6;
        private int minScaleId = 4;
        private int GridTypeId = 0;


        private ModelType SelectedGrid => DimensionIs2D ? Grids2[GridTypeId] : Grids3[GridTypeId];

        public void NextDimension()
        {
            DimensionIs2D = !DimensionIs2D;
            if (DimensionIs2D)
                DimensionTmp.text = Dimensions[0];
            else
                DimensionTmp.text = Dimensions[1];
            AreaLengthField.SetActive(!DimensionIs2D);
            RefreshGridImage();
        }

        private void RefreshGridImage()
        {
            if (DimensionIs2D)
                GridTypeImage.sprite = gridTypes2d[GridTypeId];
            else
                GridTypeImage.sprite = gridTypes3d[GridTypeId];
            //RefreshLenCaptions();
        }

        public void NextGridType()
        {
            GridTypeId = (GridTypeId + 1) % 3;
            RefreshGridImage();
        }
        public void PrevGridType()
        {
            GridTypeId = (GridTypeId + 2) % 3;
            RefreshGridImage();
        }

        public void SetScale(int ScaleId)
        {
            _ScaleId = ScaleId;
            ModelParameters.geometryParameters.modelScale = _ScaleId;
            ScaleTmp.text = _ScaleId.ToString();
            RefreshLenCaptions();
        }


        public void RefreshLenCaptions()
        {
            float cellSize = Mathf.Pow(10, 6 - _ScaleId);
            cellSizeText.text  = $"{cellSize} μм";
            cellSizeText2.text = $"{cellSize} μм";
            scaleText.text = $"Масштаб:  1 :  10<sup>-{_ScaleId}</sup>";
            int AreaWidth = int.Parse(WidthTmp.text);
            int AreaHeight = int.Parse(HeightTmp.text);
            float realW = AreaWidth * cellSize;
            float realH = AreaHeight * cellSize;
            if (realW >= 100 && realH >= 100)
                sqrText.text = $"{(realW/1000f).ToString("0.0")} mm  ×  {(realH/1000f).ToString("0.0")} mm";
            else
                sqrText.text = $"{realW.ToString("0.0")} μм  ×  {(realH).ToString("0.0")} μм";
            float totalSqr = realH * realW;
            totalSqrText.text = $"Площадь: {(realW * realH) / 1e+6} mm<sup>2</sup>";
            cellVol.text = $"1.00  ×  10<sup>{((6- _ScaleId) * 3-18)}</sup> m<sup>2</sup>";
            totalCells.text = $"{AreaWidth*AreaHeight}";

            //ModelParameters.geometryParameters.gridType = SelectedGrid;
            //ModelParameters.geometryParameters.AreaWidth = int.Parse(WidthTmp.text);
            //ModelParameters.geometryParameters.AreaHeight = int.Parse(HeightTmp.text);
            //ModelParameters.geometryParameters.AreaLength = int.Parse(LengthhTmp.text);
            //foreach (var t in lengthCaptions)
            //    t.text = $"-{ScaleId}";
            //ModelParameters.ShowInDebug();
        }

        public override void ReadAll()
        {
            //ModelParameters.geometryParameters.modelScale = ScaleId;
            
            ModelParameters.geometryParameters.gridType = SelectedGrid;
            ModelParameters.geometryParameters.AreaWidth = int.Parse(WidthTmp.text);
            ModelParameters.geometryParameters.AreaHeight = int.Parse(HeightTmp.text);
            ModelParameters.geometryParameters.AreaLength = int.Parse(LengthhTmp.text);
        }

    }
}
