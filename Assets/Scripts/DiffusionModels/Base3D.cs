
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.NewGeneration
{
    public class Base3D : MonoBehaviour
    {
        public string name = "";

        readonly Vector3 offset = new Vector3(0.5f, 0.0f, 0.5f);
        readonly Vector3 half = -new Vector3(0.25f, 0.0f, 0.25f);


        public Vector3[] BaseV = new Vector3[3]
        {
            new Vector3(0, 0, 1),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 0),
        };

        public bool NeedShift = false;
        public GameObject elementPrefab;
        public Vector3Int[] neighbors = new Vector3Int[4];
        public Vector3Int[] shiftN = new Vector3Int[6];
        public double MinSqrWeight { get; private set; } = 10d;
        public Dictionary<Vector3Int, double> DirSqrWeight { get; private set; } = new Dictionary<Vector3Int, double>(); 
        private float scale = 1;
        public void SetScale(double s)
        {
            MinSqrWeight = 100;
            DirSqrWeight.Clear();
            scale = (float)s;
            Vector3Int pos = new Vector3Int(0, 0, 0);
            string debugString = "SetScaleDebug ";
            // calc for both positive and negative y layers in different directions
            for (int i = 0; i < 2; i++)
            {
                debugString += Environment.NewLine + "i = " + i.ToString() + " : ";
                pos += Vector3Int.one;
                foreach (Vector3Int nbr in GetNbrs(pos))
                {
                    if (DirSqrWeight.ContainsKey(nbr))
                        continue;
                    double sqrWeight = (GetPosition(pos+nbr) - GetPosition(pos)).sqrMagnitude;
                    debugString += $"V ={nbr}, dist = {Math.Sqrt(sqrWeight)},    ";
                    if (sqrWeight < MinSqrWeight)
                        MinSqrWeight = sqrWeight;
                    DirSqrWeight.Add(nbr, sqrWeight);
                }
            }
            debugString += Environment.NewLine + "MinDist = " + MinSqrWeight.ToString();
            //Debug.Log(debugString);
        }
        public double GetSqrWeight(Vector3Int nbr) => DirSqrWeight[nbr];
        public Vector3Int[] GetNbrs(Vector3Int pos)
        {
            if (!NeedShift)
                return neighbors;

            if (pos.y % 2 == 0)
                return neighbors;
            else
                return shiftN;
        }
        public Vector3 GetPosition(Vector3Int id)
        {
            if (!NeedShift )
                return scale * ( BaseV[0] * id.x + BaseV[1] * id.y + BaseV[2] * id.z);

            if ( id.y % 2 == 0)
                return scale * ( half + BaseV[0] * id.x + BaseV[1] * id.y + BaseV[2] * id.z);
            else
            {
                return scale * (half + BaseV[0] * id.x + BaseV[1] * id.y + BaseV[2] * id.z + offset  );
            }
        }
        public Vector3Int GetIdOfPoint(Vector3 pos)
        {
            int x, y, z;
            if (!NeedShift)
            {
                y = (int)Math.Round(pos.y / scale / BaseV[1].y);
                if (y % 2 == 0)
                {
                    x = (int)Math.Round( (pos.x / scale - half.x ) / BaseV[0].x);
                    z = (int)Math.Round( (pos.z / scale - half.z ) / BaseV[2].z);
                }
                else
                {
                    x = (int)Math.Round((pos.x / scale - half.x - offset.x) / BaseV[0].x);
                    z = (int)Math.Round((pos.z / scale - half.z - offset.z) / BaseV[2].z);
                }
            }
            else
            {
                x = (int)Math.Round(pos.x / scale / BaseV[0].x);
                y = (int)Math.Round(pos.y / scale / BaseV[1].y);
                z = (int)Math.Round(pos.z / scale / BaseV[2].z);
            }

            Vector3Int posInt = new Vector3Int(x, y, z);
            return posInt;
        }
    }


}
