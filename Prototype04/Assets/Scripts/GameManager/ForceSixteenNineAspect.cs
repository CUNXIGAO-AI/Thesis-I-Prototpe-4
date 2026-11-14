using UnityEngine;

/// <summary>
/// Keeps gameplay at 16:9. Extra monitor space becomes black bars instead of stretching.
/// </summary>
public class ForceSixteenNineAspect : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;
    private Camera letterboxCamera;
    private int lastWidth;
    private int lastHeight;

    private void Start()
    {
        if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;

        EnsureLetterboxCamera();
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void EnsureLetterboxCamera()
    {
        if (letterboxCamera != null) return;

        var go = new GameObject("16:9 Letterbox Camera");
        DontDestroyOnLoad(go);
        letterboxCamera = go.AddComponent<Camera>();
        letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
        letterboxCamera.backgroundColor = Color.black;
        letterboxCamera.cullingMask = 0;
        letterboxCamera.depth = -100;
        letterboxCamera.rect = new Rect(0f, 0f, 1f, 1f);
        letterboxCamera.allowHDR = false;
        letterboxCamera.allowMSAA = false;
    }

    private void Apply()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        if (lastWidth <= 0 || lastHeight <= 0) return;

        float windowAspect = (float)lastWidth / lastHeight;
        Rect viewport;

        if (windowAspect > TargetAspect)
        {
            float width = TargetAspect / windowAspect;
            viewport = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }
        else
        {
            float height = windowAspect / TargetAspect;
            viewport = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || cam == letterboxCamera) continue;
            cam.rect = viewport;
        }
    }
}
