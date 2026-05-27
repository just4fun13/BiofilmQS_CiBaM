using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class CameraFilter : MonoBehaviour
{

    [SerializeField] private bool r = true;
    [SerializeField] private bool g = true;
    [SerializeField] private bool b = true;

    private float redOnly   => r ? 1f : 0f;
    private float greenOnly => g ? 1f : 0f;
    private float blueOnly  => b ? 1f : 0f;

    private ColorGrading colorGrading;

    void Start()
    {
        PostProcessVolume volume = GetComponent<PostProcessVolume>();
        volume.profile.TryGetSettings(out colorGrading);
    }

    void Update()
    {
        colorGrading.colorFilter.value = new Vector4(redOnly, greenOnly, blueOnly, 0);
    }
}