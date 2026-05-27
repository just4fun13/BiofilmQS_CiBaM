using System;
using UnityEngine;

[Serializable]
public class SimulationConfig
{
    public string version = "0.1-paper";
    public string comment = "Successful simulation preset";

    public int width;
    public int height;
    public string gridType;

    public float unitSizeMicrom;
    public float timeStepSeconds;
    public float totalTimeHours;

    public float nutrientDiffusion;
    public float initialNutrient;
    public float muMax;
    public float initialCells;
    public float spreadProbability;
    public float ks;
    public float yxs;

    public float ahlDiffusion;
    public float ahlAlpha;
    public float ahlBeta;
    public float ahlDegradation;
    public float hillCoefficient;
    public float ahlThreshold;
    public float ahlReference;

    public float liquidWashout;
    public float bacteriaWashout;
    public float nutrientInflow;

    public int randomSeed;
}