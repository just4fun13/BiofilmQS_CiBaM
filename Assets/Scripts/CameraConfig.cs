using DG.Tweening;
using UnityEngine;

namespace CellularAutomaton
{
    public class CameraConfig
    {
        private static Camera camera;

        private static void GetCam()
        {
            camera = Camera.main;
        }

        const float k = 1.6f / 0.9f;

        public static void SetCameraBounds(Vector4 bounds)
        {
            if (camera == null) GetCam();
            Vector2 center = new Vector2((bounds.x + bounds.z) / 2f, (bounds.y + bounds.w) / 2f);
            float hUnits = (bounds.w - bounds.y) / 2f * 1.2f;
            float wUnity = (bounds.z - bounds.x) / 2f / k;
            float size = Mathf.Max(hUnits , wUnity) + 2;
            Vector3 pos = new Vector3(center.x, center.y - hUnits + size , -10f);
//                (Vector3)center - 10 * Vector3.forward + Vector3.up * (size - center.y/2f - 1);
            camera.transform.DOMove(pos, Time.deltaTime);
            camera.DOOrthoSize(size, Time.deltaTime);
        }
    }
}
