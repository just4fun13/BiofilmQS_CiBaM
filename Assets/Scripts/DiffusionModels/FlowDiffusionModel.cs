using Assets.Scripts.DiffusionModels;
using UnityEngine;
using System.Threading.Tasks;
using Unity.VisualScripting;
using System;
using System.Linq;
using CellularAutomaton;

namespace Assets.Scripts.NewGeneration
{
    public class FlowDiffusionModel : DiffusionModel
    {
        // НОВЫЙ МАССИВ: Массив для хранения значений фиктивного (теневого) слоя
        // Размер W (ширина), так как фиктивный слой находится под нижней границей (j=0).
        private double[] Cn_Shadow_Bottom;
        private Vector2 FlowU = Vector2.right;

        // ----------------------------------------------------------------------
        // Конструкторы
        // ----------------------------------------------------------------------

        public FlowDiffusionModel() : base()
        {
            InitializeRobinParameters();
        }

        public FlowDiffusionModel(double AreaW, double AreaH, CellGeometry bas, int n, double D, double U, double Mu) : base(AreaW, AreaH, bas, n, D, U, Mu)
        {
            InitializeRobinParameters();
        }

        private void InitializeRobinParameters()
        {
            Cn_Shadow_Bottom = new double[AreaWidthCells];
        }

        // ======================================================================
        // A) Расчет теневого слоя (Новый метод)
        // ======================================================================
        protected void CalculateShadowLayer()
        {
            double hy = GeometryBase.GetPosition(new Vector2Int(0, 1)).y - GeometryBase.GetPosition(new Vector2Int(0, 0)).y;
            int y_bottom = 0;
            Parallel.For(0, AreaWidthCells, parallelOptions, x =>
            {
                Cn_Shadow_Bottom[x] = Cn[x, y_bottom+1] -  2 * ConsumptionRate * hy / DiffusionKoef * Cn[x, y_bottom];
            });
            double sum = 0;
            for (int i = 0; i < AreaWidthCells; i++)
            {
                sum += Cn_Shadow_Bottom[i];
            }
        }
        // FlowU_dir должен быть единичным направлением (без скорости)
        private Vector2 FlowU_dir => FlowU.normalized;

        protected double ConvectionUpwindX(Vector2Int pos)
        {
            int i = pos.x, j = pos.y;

            // шаг по x (лучше из веса)
            double dx = Math.Sqrt(GeometryBase.GetSqrWeight(new Vector2Int(1, 0)));

            if (FlowVelocity >= 0)
            {
                int HorizontalNegativeNbr = Math.Max(i - 1, 0);
                return FlowVelocity * (Cn[i, j] - Cn[HorizontalNegativeNbr, j]) / dx;
            }
            else
            {
                int HorizontalPositiveNbr = Math.Min(i + 1, AreaWidthCells - 1);
                return FlowVelocity * (Cn[HorizontalPositiveNbr, j] - Cn[i, j]) / h;
            }
        }

        protected double ConvectionUpwindXUltra(Vector2Int pos)
        {
            double u = FlowVelocity;           // м/с
            if (Math.Abs(u) < 1e-16) return 0;

            Vector2 x0 = GeometryBase.GetPosition(pos);
            double c0 = Cn[pos.x, pos.y];

            var nbrs = GeometryBase.GetNbrs(pos);

            double num = 0.0;  // числитель
            double den = 0.0;  // сумма весов

            for (int k = 0; k < nbrs.Length; k++)
            {
                Vector2Int nb = pos + nbrs[k];
                if (!InBound(nb)) continue;

                Vector2 x1 = GeometryBase.GetPosition(nb);
                double dx = x1.x - x0.x; // метры

                // upstream выбор по знаку u
                if (u > 0 && dx >= -1e-12) continue;
                if (u < 0 && dx <= 1e-12) continue;

                double cos = GeometryBase.HorizontalImpact[k]; // cos(theta) ~ dx/|dr|
                double w = Math.Abs(cos); // вес "горизонтальности"

                if (w < 1e-6) continue;

                double dCdx_k;
                if (u > 0)
                    dCdx_k = (c0 - Cn[nb.x, nb.y]) / (-dx);
                else
                    dCdx_k = (Cn[nb.x, nb.y] - c0) / (dx);

                num += w * dCdx_k;
                den += w;
            }

            if (den < 1e-12) return 0.0;

            double dCdx = num / den;   // C/м
            return u * dCdx;           // C/с
        }

        protected double ConvectionMine(Vector2Int pos)
        {
            double u = FlowVelocity;
            if (Math.Abs(u) < 1e-16) return 0.0;

            var dirs = GeometryBase.GetNbrs(pos);
            double c0 = Cn[pos.x, pos.y];

            double sumWC = 0.0; // Σ w*C
            double sumW = 0.0; // Σ w  (для центровки на границах)

            for (int k = 0; k < dirs.Length; k++)
            {
                double w = GeometryBase.HorizontalImpact[k]; // ВАЖНО: НЕ Sign() !!
                if (Math.Abs(w) < 1e-15) continue;
                Vector2Int nb = pos + dirs[k];

                double cnb;
                if (InBound(nb))
                {
                    cnb = Cn[nb.x, nb.y];
                }
                else
                {
                    if (nb.y < 0)
                        cnb = Cn_Shadow_Bottom[Math.Clamp(nb.x, 0, AreaWidthCells - 1)];
                    else
                        cnb = c0; // Neumann/зеркало по умолчанию
                }


                sumWC += w * cnb;
                sumW += w;
            }

            // центровка гарантирует, что константа не пролезет на границах
            double dCdx = (sumWC - c0 * sumW) / (GeometryBase.HorizontalImpactSum * h);

            // в твоей формуле adv вычитается: Cn + dt*(D*lap - adv)
            // значит тут возвращаем u*dCdx как "adv"
            return u * dCdx;
        }


        private const double C_IN = 1.0; // inflow слева

        private double BarthJespersenAlpha(Vector2Int p, Vector2 gradP)
        {
            double c0 = Cn[p.x, p.y];
            double cMin = c0, cMax = c0;

            // диапазон по соседям (можно добавить boundary значения при желании)
            foreach (var d in GeometryBase.GetNbrs(p))
            {
                var q = p + d;
                if (!InBound(q)) continue;
                double cq = Cn[q.x, q.y];
                if (cq < cMin) cMin = cq;
                if (cq > cMax) cMax = cq;
            }

            Vector2 xp = GeometryBase.GetPosition(p);
            double alpha = 1.0;

            foreach (var d in GeometryBase.GetNbrs(p))
            {
                var q = p + d;
                if (!InBound(q)) continue;

                Vector2 xq = GeometryBase.GetPosition(q);
                Vector2 xf = 0.5f * (xp + xq);

                double cPred = c0 + Vector2.Dot(gradP, xf - xp);

                if (cPred > c0)
                    alpha = Math.Min(alpha, (cMax - c0) / (cPred - c0 + 1e-30));
                else if (cPred < c0)
                    alpha = Math.Min(alpha, (cMin - c0) / (cPred - c0 + 1e-30));
            }

            if (alpha < 0) alpha = 0;
            if (alpha > 1) alpha = 1;
            return alpha;
        }

        private Vector2 LeastSquaresGrad(Vector2Int p)
        {
            Vector2 xp = GeometryBase.GetPosition(p);
            double Cp = Cn[p.x, p.y];

            double a11 = 0, a12 = 0, a22 = 0;
            double b1 = 0, b2 = 0;

            foreach (var d in GeometryBase.GetNbrs(p))
            {
                Vector2Int q = p + d;
                if (!InBound(q)) continue;

                Vector2 xq = GeometryBase.GetPosition(q);
                double Cq = Cn[q.x, q.y];

                double dx = xq.x - xp.x;
                double dy = xq.y - xp.y;
                double dC = Cq - Cp;

                a11 += dx * dx;
                a12 += dx * dy;
                a22 += dy * dy;

                b1 += dx * dC;
                b2 += dy * dC;
            }

            double det = a11 * a22 - a12 * a12;
            if (Math.Abs(det) < 1e-20)
                return Vector2.zero;

            double gx = (b1 * a22 - b2 * a12) / det;
            double gy = (-b1 * a12 + b2 * a11) / det;

            return new Vector2((float)gx, (float)gy); // ∂C/∂x, ∂C/∂y
        }


        private double ConvectionFVM_MUSCL_Hex(Vector2Int p)
        {
            // постоянная скорость вправо
            Vector2 uVec = new Vector2((float)FlowVelocity, 0f);
            if (Math.Abs(FlowVelocity) < 1e-16) return 0.0;

            double Af = GeometryBase.GetFaceLength();
            double V = GeometryBase.GetCellArea();
            double scale = Af / V;

            Vector2 xp = GeometryBase.GetPosition(p);
            double Cp = Cn[p.x, p.y];

            // 2nd order reconstruction from P
            Vector2 gradP = LeastSquaresGrad(p);
            gradP *= (float)BarthJespersenAlpha(p, gradP);

            double fluxSum = 0.0;

            var nbrs = GeometryBase.GetNbrs(p);
            for (int k = 0; k < nbrs.Length; k++)
            {
                Vector2Int q = p + nbrs[k];

                // FACE normal ~ (xq-xp)/|...| for regular orthogonal mesh
                Vector2 xq;
                bool inb = InBound(q);
                if (inb) 
                    xq = GeometryBase.GetPosition(q);
                else
                {
                    // для границы нам нужна только нормаль; возьмём "виртуальное" положение соседа
                    // через направление к соседу по шаблону сетки:
                    // берем позицию как xp + (pos(neighbor)-pos(p)) если бы сосед существовал.
                    // проще: использовать твою геометрию шагов через GetPosition на "вне" тоже:
                    xq = GeometryBase.GetPosition(q);
                }

                Vector2 e = xq - xp;

                float dist = e.magnitude;
                if (dist < 1e-12f) continue;

                Vector2 n = e / dist;                // unit normal from p to q
                double un = Vector2.Dot(uVec, n);    // m/s

                // точка грани (для регулярной сетки)
                Vector2 xf = 0.5f * (xp + xq);

                double Cf;

                if (un >= 0.0)
                {
                    // upwind = P
                    Cf = Cp + Vector2.Dot(gradP, xf - xp);
                }
                else
                {
                    // upwind = Q (или inflow)
                    if (!inb)
                    {
                        // INLET слева: когда поток входит в домен (un<0 относительно нормали из p кнаружи)
                        Cf = C_IN;
                    }
                    else
                    {
                        double Cq = Cn[q.x, q.y];
                        Vector2 gradQ = LeastSquaresGrad(q);
                        gradQ *= (float)BarthJespersenAlpha(q, gradQ);

                        Cf = Cq + Vector2.Dot(gradQ, xf - xq);
                    }
                }

                fluxSum += un * Cf; // Af и /V вынесены в scale
            }

            return scale * fluxSum; // div(uC) [C/s]
        }

        private double ConvectionFVM_UpwindHex(Vector2Int p)
        {
            double u = FlowVelocity;
            if (Math.Abs(u) < 1e-16) return 0.0;

            double Af = GeometryBase.GetFaceLength();
            double V = GeometryBase.GetCellArea();
            double scale = Af / V;

            double Cp = Cn[p.x, p.y];

            double fluxSum = 0.0;

            var nbrs = GeometryBase.GetNbrs(p);
            for (int k = 0; k < nbrs.Length; k++)
            {
                Vector2Int q = p + nbrs[k];

                // ВАЖНО: используем нормаль грани по X из геометрии, а не "центр-центр"
                double nx = GeometryBase.HorizontalImpact[k]; // n_x for this face
                double un = u * nx;                           // u·n  (since u=(u,0))

                if (Math.Abs(un) < 1e-15) continue;

                double Cf;

                if (un > 0.0)
                {
                    // поток выходит из p через грань -> upwind = p
                    Cf = Cp;
                }
                else
                {
                    // поток входит в p -> upwind = "снаружи" (если граница) или сосед q
                    if (!InBound(q))
                    {
                        // это ИМЕННО inflow-грань (nx<0 при u>0)
                        Cf = C_IN;
                    }
                    else
                    {
                        Cf = Cn[q.x, q.y];
                    }
                }

                fluxSum += un * Cf; // Af/V вынесен в scale
            }

            return scale * fluxSum; // div(uC) [C/s]
        }
        private double DiffuseWithShadowLayer(Vector2Int pos)
        {
            double av = 0;
            int k = 0;
            Vector2Int[] nbrs = GeometryBase.GetNbrs(pos);

            foreach (Vector2Int nbr in nbrs)
            {
                k++; // считаем направление, даже если оно "упирается" в границу
                Vector2Int v = pos + nbr;
                double w = GeometryBase.GetSqrWeight(nbr);

                if (InBound(v))
                {
                    av += (Cn[v.x, v.y] - Cn[pos.x, pos.y]) / w;
                }
                else
                {
                    if (v.y == -1 && v.x > 0 && v.x < AreaWidthCells) // фиктивный узел 
                    {

                        av += (Cn_Shadow_Bottom[v.x] - Cn[pos.x, pos.y]) / w;
                    }
                }
            }

            if (k == 0)
                return 0; // на всякий случай

            av = av * timeScale / k;
            return av;
        }



        // --- Переопределение метода диффузии ---
        protected override void DiffuseSubstrate()
        {
            // 1. Применяем общие граничные условия (например, Дирихле на левой границе)
            BoundaryCondition?.Apply(Cn, GeometryBase);

            // 2. Рассчитываем теневой слой Робина
            if (ConsumptionRate > 0) 
                CalculateShadowLayer();

            double[,] cNew = new double[AreaWidthCells, AreaHeightCells];

            // 3. Общий расчет диффузии (включая нижнюю границу с теневым слоем)
            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    Vector2Int pos = new Vector2Int(i, j);
                    // Для j=0 будет использован CalculateLaplacianWithShadow, 
                    // который подставит теневой узел.
                    // Для j>0 будет использован BalancedAverage.
                    double laplacian;
                    if ( j == 0 && ConsumptionRate > 0)
                    {
                        laplacian = DiffuseWithShadowLayer(pos);
                    }
                    else
                    {
                        laplacian = BalancedAverage(pos);
                    }

                    double adv = ConvectionMine  (pos); // ConvectionUpwindX   ConvectionUpwindX ConvectionUpwindXUltra



                    //double adv = ConvectionMUSCL_General(pos);//ConvectionUpwind(pos) ;

                    cNew[i, j] = Cn[i, j] + deltaTime * (DiffusionKoef * laplacian - adv);
                    /*
                                        double diff = DiffusionFVM(pos);
                                        double adv = ConvectionMUSCL_General(pos);
                                        cNew[i, j] = Cn[i, j] + deltaTime * (diff - adv);
                    */
                }
            });

            // 4. Явное применение граничных условий Потока (Правая граница, x = W-1)
            // Это нужно, если нужно заменить Неймана (симметрию) на Поток J.
            // ApplyFlowBoundaryCondition(cNew); 

            // 5. Обновление сетки
            Parallel.For(0, AreaWidthCells, parallelOptions, i =>
            {
                for (int j = 0; j < AreaHeightCells; j++)
                {
                    Cn[i, j] = cNew[i, j];
                }
            });

            iter++;
        }
    }
}