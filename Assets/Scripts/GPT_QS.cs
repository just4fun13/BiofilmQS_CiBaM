using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPT_QS : MonoBehaviour
{

    public int gridSize;
    public float gridSpacing;
    public float simulationSpeed;
    public float nutrientDiffusionRate;
    public float nutrientDecayRate;
    public float extracellularMatrixProductionRate;
    public float extracellularMatrixDecayRate;
    public float activationThreshold;
    public float repressionThreshold;
    public float lasQSProductionRate;
    public float rhlQSProductionRate;
    public float qsDiffusionRate;
    public float qsDecayRate;
    public float dt;

    private float[,] biomass;
    private float[,] nutrients;
    private float[,] extracellularMatrix;
    private float[,] lasQS;
    private float[,] rhlQS;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;

    void Start()
    {
        biomass = new float[gridSize, gridSize];
        nutrients = new float[gridSize, gridSize];
        extracellularMatrix = new float[gridSize, gridSize];
        lasQS = new float[gridSize, gridSize];
        rhlQS = new float[gridSize, gridSize];

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateGrid();
    }

    void Update()
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                // Compute nutrient diffusion and decay
                float dn = NutrientDiffusion(i, j) - nutrientDecayRate * nutrients[i, j] * dt;

                // Compute extracellular matrix production and decay
                float de = extracellularMatrixProductionRate * biomass[i, j] * dt - extracellularMatrixDecayRate * extracellularMatrix[i, j] * dt;

                // Compute Las QS signal production, diffusion, and decay
                float dal = LasQSProduction(i, j) * dt;
                float dlq = qsDiffusionRate * (LasQS(i - 1, j) + LasQS(i + 1, j) + LasQS(i, j - 1) + LasQS(i, j + 1) - 4 * lasQS[i, j]) * dt;
                float dlqd = qsDecayRate * lasQS[i, j] * dt;

                // Compute Rhl QS signal production, diffusion, and decay
                float dar = RhlQSProduction(i, j) * dt;
                float drq = qsDiffusionRate * (RhlQS(i - 1, j) + RhlQS(i + 1, j) + RhlQS(i, j - 1) + RhlQS(i, j + 1) - 4 * rhlQS[i, j]) * dt;
                float drqd = qsDecayRate * rhlQS[i, j] * dt;

                // Update biomass, nutrients, extracellular matrix, and QS signals
                biomass[i, j] += Growth(dn) * dt;
                nutrients[i, j] = Mathf.Max(nutrients[i, j] + dn, 0f);
                extracellularMatrix[i, j] += de;
                lasQS[i, j] += dal - dlq - dlqd;
                rhlQS[i, j] += dar - drq - drqd;
            }
        }

        // Update QS signals based on cell density
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                float n = biomass[i, j] + extracellularMatrix[i, j];
                float a = Mathf.Clamp01((n - repressionThreshold) / (activationThreshold - repressionThreshold));
                float lq = Mathf.Clamp01(lasQS[i, j] * a);
                float rq = Mathf.Clamp01(rhlQS[i, j] * a);
                lasQS[i, j] = lq;
                rhlQS[i, j] = rq;
            }
        }

        UpdateGrid();
        Wait(simulationSpeed);
    }

    float Growth(float dn)
    {
        return dn / (dn + 1f);
    }

    float NutrientDiffusion(int i, int j)
    {
        float dn = nutrientDiffusionRate * (Nutrient(i - 1, j) + Nutrient(i + 1, j) + Nutrient(i, j - 1) + Nutrient(i, j + 1) - 4 * nutrients[i, j]);
        return dn;
    }

    float LasQSProduction(int i, int j)
    {
        return lasQSProductionRate * biomass[i, j];
    }

    float RhlQSProduction(int i, int j)
    {
        return rhlQSProductionRate * extracellularMatrix[i, j];
    }

    float LasQS(int i, int j)
    {
        return GetCellProperty(i, j, lasQS);
    }

    float RhlQS(int i, int j)
    {
        return GetCellProperty(i, j, rhlQS);
    }

    float Nutrient(int i, int j)
    {
        return GetCellProperty(i, j, nutrients);
    }

    float GetCellProperty(int i, int j, float[,] array)
    {
        if (i < 0 || j < 0 || i >= gridSize || j >= gridSize)
        {
            return 0f;
        }
        else
        {
            return array[i, j];
        }
    }

    void CreateGrid()
    {
        vertices = new Vector3[gridSize * gridSize];
        colors = new Color[gridSize * gridSize];
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                vertices[i * gridSize + j] = new Vector3(i * gridSpacing, 0f, j * gridSpacing);
                colors[i * gridSize + j] = new Color(0f, 0f, 0f);
            }
        }

        triangles = new int[(gridSize - 1) * (gridSize - 1) * 6];
        int ti = 0;
        for (int i = 0; i < gridSize - 1; i++)
        {
            for (int j = 0; j < gridSize - 1; j++)
            {
                int vi = i * gridSize + j;
                triangles[ti++] = vi;
                triangles[ti++] = vi + gridSize + 1;
                triangles[ti++] = vi + gridSize;

                triangles[ti++] = vi;
                triangles[ti++] = vi + 1;
                triangles[ti++] = vi + gridSize + 1;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
    }

    void UpdateGrid()
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                float n = biomass[i, j] + extracellularMatrix[i, j];
                colors[i * gridSize + j] = new Color(n, n, n);
            }
        }
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }

    void Wait(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end)
        {
            // wait
        }
    }


}