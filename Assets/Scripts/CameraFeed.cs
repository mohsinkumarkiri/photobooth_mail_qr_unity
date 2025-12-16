using UnityEngine;
using UnityEngine.UI;

public class CameraFeed : MonoBehaviour
{
    //public RawImage rawImage;            // UI RawImage to show camera feed
    public RawImage touchDesignerFeed;            // UI RawImage to show camera feed
    //public AspectRatioFitter aspectFitter; // (Optional) Keep correct ratio
    private WebCamTexture webCamTexture;

    public string preferredCamera = "Dell Webcam WB7022"; // optional camera name

    void Start()
    {
        // If you need a specific camera, find it
        string deviceName = preferredCamera;

        // If name not found, default to the first available camera
        foreach (var device in WebCamTexture.devices)
        {
            if (device.name.Contains(preferredCamera))
            {
                deviceName = device.name;
                break;
            }
        }

        // Create webcam texture
        webCamTexture = new WebCamTexture(deviceName, 1920, 1080, 60);

        // Assign it to RawImage
        //rawImage.texture = webCamTexture;
        touchDesignerFeed.texture = webCamTexture;

        // Set aspect ratio if the component exists
        //if (aspectFitter != null)
        //{
        //    aspectFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;
        //}

        // Start camera
        webCamTexture.Play();
    }

    void OnDisable()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();
    }
}
