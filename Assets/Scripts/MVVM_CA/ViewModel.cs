using Assets.Scripts.MVVM_CA.Models._2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vector4 = UnityEngine.Vector4;

namespace Assets.Scripts.MVVM_CA
{
    public class ViewModel
    {
        private Model model;
        private View2D view2D, view2Dahl, view2Dbac, view2Dnut;
        private View3D view3D;
        public ViewModel(Model _model, View2D v2D, View2D v2Dn, View2D v2Db, View2D v2Da, View3D v3D)
        {
            model = _model;
            view2D = v2D;
            view2Dbac = v2Db;
            view2Dahl = v2Da;
            view2Dnut = v2Dn;
            view3D = v3D;
        }
        public ViewModel(Model _model, View3D v3D)
        {
            model = _model;
            view3D = v3D;
        }
        public float[] dat()
        {
            return model.GetStockParams();
        }

        public void UpdateModel()
        {
            model.DoGrowthStep();
        }

        public void UpdateView()
        {

            //            view2D.UpdateVisual(model.S2D, model.C2D);
            //            view2D.UpdateVisualAHL(model.S2D, model.C2D, model.E2D, model.AHLthreshold);

            //    view2D.UpdateVisualAHL(model as Model2D);
            view2D.UpdateVisualFive(model as Model2D);
        }

        public void UpdateView3D()
        {
            view3D.UpdateVisual((model as Model3D).BiomassCells3D, model.YBounds);
            //view3D.UpdateVisual3D(model.BiomassCells, model.C3D);
        }
        public void UpdateView3DFromScratch()
        {
            view3D.RedrawFromScratch((model as Model3D).BiomassCells3D, model.YBounds);
        }
        public void ClearMemory()
        {
            view3D.ClearMemory();
        }

        public void AddNutrientAtHighLevel() => model.AddNutrientAtHighLevel();
        public float[] GetDat(Vector2Int vi)
        {
            Model2D model2 = (Model2D)model;
            if (model2.Substrate2D == null)
                return null;
            if (vi.x < 0 || vi.y < 0 || vi.x >= model2.Substrate2D.GetLength(0) || vi.y >= model2.Substrate2D.GetLength(1))
                return null;
            int ni, nj;
            ni = vi.x / model2.NutrGridSimpl;
            nj = vi.y / model2.NutrGridSimpl;
            if (model is Model2DWithAHL)
                return new float[]
                {
                    (float)(model2.Bacteria2D[vi.x, vi.y]),
                    (float)(model2.Substrate2D[ni, nj]),
                    (float)(model2.Ahl2D[ni, nj]),
                    (float)(model2.Ahl2D[ni, nj]),
                };
            else
                return new float[]
                {
                    (float)(model2.Bacteria2D[vi.x, vi.y]),
                    (float)(model2.Substrate2D[ni, nj]),0, 0
                };
        }
    
        public double GetFun(Vector2Int vi) 
            => model.CustomFun(vi);

        public double[] GetDat5(Vector2Int vi)
        {
            double[] v5 = new double [5];
            FiveStarModel model2 = (FiveStarModel)model;
            if (model2.Substrate2D == null)
                return v5;
            if (vi.x < 0 || vi.y < 0 || vi.x >= model2.Substrate2D.GetLength(0) || vi.y >= model2.Substrate2D.GetLength(1))
                return v5;
            int ni, nj;
            v5[0] =    model2.Bacteria2D [vi.x, vi.y];   // Bacteria (biomass)
            ni = vi.x / model2.NutrGridSimpl;
            nj = vi.y / model2.NutrGridSimpl;
            v5[1] =    model2.Substrate2D[ni, nj];   // Nutrient
            v5[2] =    model2.Ahl2D      [ni, nj];   // AHL
            v5[3] =    model2.Eps2D      [ni, nj];   // EPS
            v5[4] =    model2.Lactonas2D [ni, nj];    // Lactonase
            return v5;
        }
    }
}
