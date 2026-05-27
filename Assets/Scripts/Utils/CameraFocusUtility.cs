using UnityEngine;
using Cinemachine;

public static class CameraFocusUtility
{
    public static void FocusFullscreen(
        Camera cam,
        float areaWidth,
        float areaHeight,
        CinemachineVirtualCamera vcam = null,
        float padding = 0.5f
    )
    {
        Rect fullscreenRect = new Rect(0f, 0f, 1f, 1f);

        FocusInViewport(
            cam,
            areaWidth,
            areaHeight,
            fullscreenRect,
            vcam,
            padding
        );
    }

    public static void FocusWindowMode(
        Camera cam,
        float areaWidth,
        float areaHeight,
        CinemachineVirtualCamera vcam = null,
        float padding = 0.5f
    )
    {
        Rect windowRect = new Rect(
            0.29f,  // X
            0.10f,  // Y
            0.45f,  // W
            0.85f   // H
        );

        FocusInViewport(
            cam,
            areaWidth,
            areaHeight,
            windowRect,
            vcam,
            padding
        );
    }

    public static void FocusInViewport(
        Camera cam,
        float areaWidth,
        float areaHeight,
        Rect viewportRect,
        CinemachineVirtualCamera vcam = null,
        float padding = 0.5f
    )
    {
        if (cam == null)
        {
            Debug.LogWarning("CameraFocusUtility: Camera is null.");
            return;
        }

        if (vcam != null)
            vcam.gameObject.SetActive(false);

        cam.orthographic = true;
        cam.transform.rotation = Quaternion.identity;
        cam.ResetProjectionMatrix();

        // Задаем область экрана, которую занимает камера
        cam.rect = viewportRect;

        // Центр расчетной области
        float centerX = areaWidth / 2f;
        float centerY = areaHeight / 2f;

        // Aspect именно окна камеры, а не всего экрана
        float viewportPixelWidth = Screen.width * viewportRect.width;
        float viewportPixelHeight = Screen.height * viewportRect.height;

        if (viewportPixelHeight <= 0f)
        {
            Debug.LogWarning("CameraFocusUtility: viewport height is zero.");
            return;
        }

        float aspect = viewportPixelWidth / viewportPixelHeight;

        // Orthographic size = половина видимой высоты
        float sizeByHeight = areaHeight / 2f;
        float sizeByWidth = areaWidth / (2f * aspect);

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth) + padding;
        cam.transform.position = new Vector3(centerX, centerY, -10f);
    }
}