using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class DiffusionController : MonoBehaviour
{
    [Header("Параметры модели")]
    public int Nx = 100;
    public int Ny = 100;
    public float Lx = 1f;
    public float Ly = 1f;
    public float D = 0.1f;
    public float dt = 0.00025f;
    public float TMax = 10f;

    [Header("Ресурсы")]
    public ComputeShader DiffusionShader2D;

    private RenderTexture inputTex;
    private RenderTexture outputTex;
    private ComputeBuffer scratchBuffer;

    private float currentTime = 0f;
    private int kernelFirstStep, kernelSecondStep;

    [Header("Визуализация")]
    public RawImage display;
    public Renderer quadRenderer;

    [SerializeField] private bool debugMode;

    void Start()
    {
        InitializeTextures();
        InitializeScratchBuffer();
        InitializeSource();
        SetupShader();
        LogShaderResources();
    }
    void InitializeTextures()
    {
        inputTex = CreateRenderTexture(Nx, Ny);
        outputTex = CreateRenderTexture(Nx, Ny);
        ClearTexture(inputTex);
        ClearTexture(outputTex);
        Debug.Log($"Initialize Textures done");
    }
    void InitializeScratchBuffer()
    {
        int bufferSize = Nx * Ny;
        scratchBuffer = new ComputeBuffer(bufferSize, sizeof(float));
        Debug.Log($"Initialize Scratch Buffer done");
    }

    void InitializeSource()
    {
        Texture2D sourceTex = new Texture2D(Nx, Ny);
        Color[] colors = new Color[Nx * Ny];

        for (int x = 0; x < Nx; x++)
        {
            for (int y = 0; y < Ny; y++)
            {
                // Источник в центральной части верхней границы
                if (x > 0.4f * Nx && x < 0.6f * Nx && y >= Ny - 3)
                    colors[x + y * Nx] = Color.red;//new Color(10, 0, 0, 0); // 10 — концентрация
                else
                    colors[x + y * Nx] = Color.black;
                
            }
        }
        RectTransform rect = display.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(Nx, Ny); // Размер в пикселях
        sourceTex.SetPixels(colors);
        sourceTex.Apply();
        display.texture = sourceTex;
        Graphics.Blit(sourceTex, inputTex);
        Debug.Log($"Initialize Source done");
    }

    void SetupShader()
    {
        kernelFirstStep = DiffusionShader2D.FindKernel("FirstStepX");
        kernelSecondStep = DiffusionShader2D.FindKernel("SecondStepY");
        if (kernelFirstStep < 0 || kernelSecondStep < 0)
        {
            Debug.LogError("Не удалось найти ядра в шейдере. Проверьте имена и компиляцию.");
            return;
        }

        float dx = Lx / (Nx - 1);
        float dy = Ly / (Ny - 1);
        float alpha = D * dt / (2 * dx * dx);
        float beta = D * dt / (2 * dy * dy);

        DiffusionShader2D.SetInt("NX", Nx);
        DiffusionShader2D.SetInt("NY", Ny);
        DiffusionShader2D.SetFloat("ALPHA", alpha);
        DiffusionShader2D.SetFloat("BETA", beta);

        DiffusionShader2D.SetTexture(kernelFirstStep, "uIn", inputTex);
        DiffusionShader2D.SetTexture(kernelSecondStep, "uOut", outputTex);
        DiffusionShader2D.SetBuffer(kernelFirstStep, "scratchBuffer", scratchBuffer);
        DiffusionShader2D.SetBuffer(kernelSecondStep, "scratchBuffer", scratchBuffer);
    }

    void LogShaderResources()
    {
        if (!debugMode) return;

        Debug.Log($"[Shader Debug]");
        Debug.Log($" - inputTex: {(inputTex != null ? "✓" : "✗")}");
        Debug.Log($" - outputTex: ${(outputTex != null ? "✓" : "✗")}");
        Debug.Log($" - scratchBuffer: ${(scratchBuffer != null ? "✓" : "✗")}");
        Debug.Log($" - kernelFirstStep index: {kernelFirstStep}");
        Debug.Log($" - kernelSecondStep index: {kernelSecondStep}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            RunDiffusionStep();
    }



    RenderTexture CreateRenderTexture(int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 0);
        rt.enableRandomWrite = true;
        rt.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.format = RenderTextureFormat.RFloat;
        rt.Create();
        return rt;
    }

    void ClearTexture(RenderTexture tex)
    {
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = tex;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = active;
    }




    public void RunDiffusionStep()
    {
        DiffusionShader2D.SetTexture(kernelFirstStep, "uIn", inputTex);
        DiffusionShader2D.Dispatch(kernelFirstStep, 1, Ny, 1);

       /* DiffusionShader2D.SetTexture(kernelSecondStep, "uOut", outputTex);
        DiffusionShader2D.Dispatch(kernelSecondStep, Nx, 1, 1);

        // Обмен текстур
        var temp = inputTex;
        inputTex = outputTex;
        outputTex = temp;

        // Обновление отображения
        if (display != null)
            display.texture = inputTex;

        if (quadRenderer != null)
            quadRenderer.material.SetTexture("_MainTex", inputTex);*/
    }

    void OnDestroy()
    {
        inputTex?.Release();
        outputTex?.Release();
        scratchBuffer?.Release();
    }
}