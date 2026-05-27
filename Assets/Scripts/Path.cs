using System.Collections.Generic;
using UnityEngine;

namespace CellularAutomaton
{
    public class Path 
    {
        private static List<Vector2> path = new List<Vector2>();

        private static List<Line> Lines = new List<Line>();

        private static Vector2 outsizePoint = new Vector2(-10000, 10000);

        public static void TryAddPoint(Vector2 point)
        {
            if (path.Count < 3 || IsInside(point))
            {
                AddPoint(point);
            }
        }

        private static void ShowPath()
        {
            string s = $"({path.Count})PATH: ";
            foreach (Vector2 p in path)
                s += $"[{p}], ";
            Debug.Log(s);
        }

        private static void AddPoint(Vector2 newPoint)
        {
            ShowPath();
            if (Lines.Count < 3)
            {
                foreach (Vector2 p in path)
                {
                    Line line = new Line(p, newPoint);
                    Lines.Add(line);
                }
                path.Add(newPoint);
            }
            else // find closest line and replace it with 2 new lines
            {
                float minDist = 10000, minDist2 = 10000;
                Vector2 minP = Vector2.zero, minP2 = Vector2.zero;
                foreach (Vector2 p in path)
                {
                    float dist = (p-newPoint).sqrMagnitude;
                    if (dist < minDist)
                    {
                        minP2 = minP;
                        minDist2 = minDist;
                        minP = p;
                        minDist = dist;
                    }
                    else
                    if (dist < minDist2)
                    {
                        minP2 = p;
                        minDist2 = dist;
                    }
                }
                Debug.Log($"Closest points: {minP} {minP2}");
                Line l = null, l2 = null; minDist = 10000;
                foreach (Line lx in Lines)
                {
                    float dist = lx.GetDistanceToTheLine(newPoint);
                    if (dist < minDist)
                    {
                        l = lx;
                        minDist = dist;
                    }

                }
                l2 = Lines.Find(x => HaveCommmonPoint(x, l) && (x.P1 == minP2 || x.P2 == minP2));

                float duration = 10f;
                //if ((l.P1 == minP && l.P2 == minP2) || (l.P2 == minP && l.P1 == minP2))
                   if (l2 == null || AreTheyConnected(minP, minP2))
                    {
                    Debug.Log("NEIGHBORS");
                        Debug.DrawLine(l.P1, l.P2, Color.blue, duration);
                        Vector2 p1 = l.P1; Vector2 p2 = l.P2;
                        Lines.Add(new Line(newPoint, l.P1));
                        Lines.Add(new Line(newPoint, l.P2));
                        Lines.Remove(l);
                        path.Add(newPoint);
                   }
                    else
                    {
                        Debug.Log($" NOT NEIGHBORS l={l};  l2={l2}");
                        DrawPoint(minP, duration, Color.cyan, 0.35f);
                        DrawPoint(minP2, duration, Color.cyan, 0.35f);
                        Vector2 comP = CommonPoint(l, l2);
                        l.DrawLine(10f, Color.magenta);
                        l2.DrawLine(10f, Color.magenta);
                        Lines.Add(new Line(newPoint, UncommonPoint(comP, l)));
                        Lines.Add(new Line(newPoint, UncommonPoint(comP, l2)));
                        Lines.Remove(l);
                        Lines.Remove(l2);
                        path.Remove(comP);
                        path.Add(newPoint);
                    }
            }
        }

        public static void DrawPath(float duration)
        {
            foreach (Line l in Lines)
                l.DrawLine(duration, Color.red);
            int k = 0;
            foreach (Vector2 p in path)
            {
                k++;
//                DrawPoint(p, duration, Color.green, 0.1f);
                for (int i = 0; i < k; i++)
                {
                    Vector2 px = p + i * 0.15f * Vector2.right;
                    Debug.DrawLine(px, px + 0.5f * Vector2.up, Color.green, duration);
                }
            }

        }

        private static void DrawPoint(Vector2 p, float duration, Color col, float dl)
        {
            Debug.DrawLine(p + new Vector2(1, 0) * dl, p + new Vector2(1, 1) * dl,  col, duration);
            Debug.DrawLine(p + new Vector2(1, 1) * dl, p + new Vector2(0, 1) * dl,  col, duration);
            Debug.DrawLine(p + new Vector2(0, 1) * dl, p + new Vector2(0, -1) * dl, col, duration);
            Debug.DrawLine(p + new Vector2(0, -1) * dl, p + new Vector2(1, 0) * dl, col, duration);
        }

        private static bool AreTheyConnected(Vector2 p1, Vector2 p2)
        {
            return Lines.Exists(x => (x.P1 == p1 && x.P2 == p2) || (x.P1 == p2 && x.P2 == p1) );
        }

        private static bool CrossLines(Line l1, Line l2)
        {
            if (l1.xMin > l2.xMax || l2.xMin > l1.xMax || l1.yMin > l2.yMax || l2.yMin > l1.yMax)
                return false;
            float zp = 0.01f;
            float detA = l1.a * l2.b - l1.b * l2.a;
            if (Mathf.Abs(detA) <= 0.1f)
                return false;
            float x = (-l2.b * l1.c + l1.b * l2.c) / detA;
            float y = (l2.a * l1.c - l1.a * l2.c) / detA;

            float xMin = Mathf.Max(l1.xMin, l2.xMin); float yMin = Mathf.Max(l1.yMin, l2.yMin); float xMax = Mathf.Min(l1.xMax, l2.xMax); float yMax = Mathf.Min(l1.yMax, l2.yMax);
            if (x > xMin + zp && x < xMax - zp && y > yMin + zp && y < yMax - zp)
                return true;
            return false;
        }

        private static bool IsInside(Vector2 point)
        {
            Line testLine = new Line(outsizePoint, point);
            int crossCount = 0;
            foreach (Line line in Lines)
                if (CrossLines(testLine, line))
                    crossCount++;
            return crossCount % 2 == 0;
        }

        private static bool HaveCommmonPoint(Line l1, Line l2) => (l1.P1 == l2.P1) || (l1.P1 == l2.P2) || (l1.P2 == l2.P1) || (l1.P2 == l2.P2) ;

        private static Vector2 CommonPoint(Line l1, Line l2)
        {
            if (l1.P1 == l2.P1 || l1.P1 == l2.P2)
                return l1.P1;
            else
                return l1.P2;
        }

        private static Vector2 UncommonPoint(Vector2 point, Line l)
        {
            if (l.P1 == point) return l.P2;
            else return l.P1;
        }
    }
}
