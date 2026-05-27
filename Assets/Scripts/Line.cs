using UnityEngine;


namespace CellularAutomaton
{
    public class Line
    {
        public float a, b, c;
        public float xMin, xMax, yMin, yMax;
        public int lineType = 0;
        public float length = 0f;
        Vector2 pStart, pEnd;
        public float alpha;
        private float sqrtAB, AB;
        private Vector3[]border = new Vector3[2];
        public Line(Vector2 p1, Vector2 p0)
        {
            pStart = p1; pEnd = p0;
            if (p1 == p0)
            {
                Debug.LogError($"trying add line with same points {p1}");
                return;
            }
            if (Mathf.Abs(p0.x - p1.x) < 0.1f)
            {
                if (p1.y > p0.y)
                {
                    yMin = p0.y;
                    yMax = p1.y;
                }
                else
                {
                    yMin = p1.y;
                    yMax = p0.y;
                }
                xMin = p0.x - 0.1f;
                xMax = p0.x + 0.1f;
                lineType = 1;
            }
            else
            {
                if (p0.x > p1.x)
                {
                    xMax = p0.x;
                    xMin = p1.x;
                }
                else
                {
                    xMin = p0.x;
                    xMax = p1.x;
                }
                if (Mathf.Abs(p0.y - p1.y) < 0.1f)
                {
                    yMax = p0.y + 0.1f;
                    yMin = p0.y - 0.1f;
                }
                else
                if (p1.y > p0.y)
                {
                    yMin = p0.y;
                    yMax = p1.y;
                }
                else
                {
                    yMin = p1.y;
                    yMax = p0.y;
                }
                lineType = 2;
            }
            a = p1.y - p0.y;
            b = p0.x - p1.x;
            c = p0.y * p1.x - p1.y * p0.x;
            sqrtAB = Mathf.Sqrt(a * a + b * b);
            length = (p0 - p1).magnitude;
            if (b == 0f)
                alpha = 0f;
            else
                alpha = Mathf.Atan2(b, -a) * Mathf.Rad2Deg;
            AB = a*a + b*b;
            sqrtAB = Mathf.Sqrt(AB);
            if (p1.y == p0.y)
            {
                // line eq: x - x0 = 0
                border[0] = new Vector3(1f, 0f, -p0.x);
                border[1] = new Vector3(1f, 0f, -p1.x);
            }
            else if (p1.x == p0.x)
            {
                // line eq: y - y0 = 0
                border[0] = new Vector3(0f, 1f, -p0.y);
                border[1] = new Vector3(0f, 1f, -p1.y);
            }
            else
            {
                float k = (p0.y - p1.y) / (p0.x - p1.x);
                float sqrtK = Mathf.Sqrt(1f + k * k);
                border[0] = new Vector3(1f, k, -p0.x - k * p0.y);
                border[1] = new Vector3(1f, k, -p1.x - k * p1.y);
            }
        }

        public void DrawLine(float tm, Color col)
        {
            Debug.DrawLine(pStart, pEnd, col, tm);
        }

        public float GetVal(Vector2 p)
        {
            return a * p.x + b * p.y + c;
        }

        public Vector2 P1 =>  pStart;
        public Vector2 P2 => pEnd;

        public void GetProjection(Vector2 p, out bool hasOrNot, out Vector2 projectionVector)
        {
            if (a == 0)
            {
                if (p.x > xMax)
                {
                    projectionVector = new Vector2(xMax, -c / b);
                    hasOrNot = false;
                    return;
                }
                if (p.x < xMin)
                {
                    projectionVector = new Vector2(xMin, -c / b);
                    hasOrNot = false;
                    return;
                }
                projectionVector = new Vector2(p.x, -c / b);
                hasOrNot = true;
                return;
            }
            if (b == 0)
            {
                if (p.y > yMax)
                {
                    projectionVector = new Vector2(-c / a, yMax);
                    hasOrNot = false;
                    return;
                }
                if (p.y < yMin)
                {
                    projectionVector = new Vector2(-c / a, yMin);
                    hasOrNot = false;
                    return;
                }
                hasOrNot = true;
                projectionVector = new Vector2(-c / a, p.y);
                return;
            }
            float k2 = b / a;
            float d = p.y - k2 * p.x;
            float a2 = k2, b2 = -1f, c2 = d;
            float detA = a * b2 - b * a2;
            if (detA == 0)
            {
                projectionVector = Vector2.zero;
                hasOrNot = false;
                return;
            }

            float x = (-b2 * c + b * c2) / detA;
            float y = (a2 * c - a * c2) / detA;

            if (lineType == 1)
            {
                float prog = (y - yMin) / (yMax - yMin);
                if (prog > 1f)
                {
                    hasOrNot = false;
                    if (pStart.y == yMax)
                        projectionVector = pStart;
                    else
                        projectionVector = pEnd;
                    return;
                }
                if (prog < 0f)
                {
                    hasOrNot = false;
                    if (pStart.y == yMin)
                        projectionVector = pStart;
                    else
                        projectionVector = pEnd;
                    return;
                }
            }
            else
            {
                float prog = (x - xMin) / (xMax - xMin);
                if (prog > 1f)
                {
                    hasOrNot = false;
                    if (pStart.x == xMax)
                        projectionVector = pStart;
                    else
                        projectionVector = pEnd;
                    return;
                }
                if (prog < 0f)
                {
                    hasOrNot = false;
                    if (pStart.x == xMin)
                        projectionVector = pStart;
                    else
                        projectionVector = pEnd;
                    return;
                }
            }

            hasOrNot = true;
            projectionVector = new Vector2(x, y);

        }

        public float BorderVal(int borderId, Vector2 po) => border[borderId].x * po.x + border[borderId].y * po.y + border[borderId].z;        
        public bool InRange(Vector2 point) => (BorderVal(0,point) * BorderVal(1,point) < 0);

        public float GetDistanceToTheLine(Vector2 point)
        {
            if (InRange(point))
            {
                return Mathf.Abs(GetVal(point)) / sqrtAB;
            }
            else
            {
                return Mathf.Sqrt(Mathf.Min( (point-pEnd).sqrMagnitude, (point-pStart).sqrMagnitude)  );
            }
        }

    }
}