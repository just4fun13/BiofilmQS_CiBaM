using System;
using System.Diagnostics;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CellularAutomaton
{
    public class ParameterHead : GenericSingletonClass<ParameterHead>
    {
        private int RoundCount = 10;

        private GridType Grid = GridType.Square;
        private int AreaW = 400;
        private float valToDP = 2.4f;
        private float maxT = 25;
        private float maxD = 100;

        private float minDP = 1.5f;
        private float maxDP = 2.5f;
        private float dDp = 0.1f;
        private int minMT = 10;
        private int maxMT = 40;
        private int curStep = 0;

        Stopwatch watch = new Stopwatch();

        private string parsName = "VorPars";

        public bool IsDone()
        {
            valToDP += dDp;
            valToDP = (float) Math.Round(valToDP, 1);
            if (valToDP > maxDP)
            {
                if (maxT < maxMT)
                {
                    valToDP = minDP;
                    maxT++;
                }
                else
                {
                    valToDP = minDP;
                    maxT = minMT;
                    Grid = (GridType)((int)Grid + 1);
                }
            }
            if (RoundCount <= 0)
                GameObject.FindObjectOfType<AudioSource>().Play();
            if (Grid == GridType.Unexpected && RoundCount>0)
            {
                Grid = GridType.Square;
                valToDP = minDP;
                maxT = minMT;
                RoundCount--;
            }
            return Grid == GridType.Unexpected;
                //maxT >= maxMT && valToDP >= maxDP && (int) Grid > 3;
        }

        private int maxStepCount => (int) ((maxDP - minDP) * 1f/ dDp) * (maxMT - minMT + 1) * 2 - 1;

        private int prog => curStep * 100 / maxStepCount;

/*        public float[] GetTheParams()
        {
            watch.Stop();
            Debug.Log(curStep + "_" + maxStepCount);
            curStep++;
            float[] pars = new float[6]
                {
                    AreaW, AreaH, valToDP, maxT, maxD, (float) Grid
                };
            GameObject.FindObjectOfType<TMP_Text>().text = $"{AreaW}x{AreaH}, μ={valToDP} [{minDP}-{maxDP}], tryCount={maxT} [{minMT}-{maxMT}], {Grid.ToString()}" +
                $"{Environment.NewLine} {prog}   Time: {watch.ElapsedMilliseconds}%";
            watch.Restart();

            return pars;
        }*/

        public void WriteParameters(float[] data)
        {
            AreaW = (int)data[0];
            valToDP =    data[1];
            maxT  = (int)data[2];
            maxD  = (int)data[3];
            Grid = (GridType)((int)data[4]);
            SavePars(data);
        }

        public float[] ReadParameters()
        {
            float[] pars = new float[5]
                {
                    AreaW, valToDP, maxT, maxD, (float) Grid
                };
            if (PlayerPrefs.HasKey(parsName))
            {
                string[] vars = PlayerPrefs.GetString(parsName).Split(' ');
                for (int i = 0; i < pars.Length; i++)
                    pars[i] = float.Parse(vars[i]);
            }
            return pars;
        }
        private void SavePars(float[] data)
        {
            string pars = string.Join(' ', data);
            PlayerPrefs.SetString(parsName, pars);
            PlayerPrefs.Save();
        }
    }
}
