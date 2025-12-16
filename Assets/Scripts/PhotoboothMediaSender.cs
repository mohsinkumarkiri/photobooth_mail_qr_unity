using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PhotoBoothMediaSender : MonoBehaviour
{
    public static PhotoBoothMediaSender instance;
    /* -------------------- UI -------------------- */
    [Header("UI References")]
    public Image sourceImage;
    public Button sendImageButton;
    public Button sendVideoButton;
    public Button sendPhotoboothMedia;

    /* -------------------- Email -------------------- */
    [Header("Email Settings")]
    public string apiUrl = "https://mohsin-photobooth-mailer1.vercel.app/api/send-photo";
    public string userEmail = "test@example.com";

    /* -------------------- Cloudinary -------------------- */
    [Header("Cloudinary Settings")]
    public string cloudName = "dx28spymd";
    public string uploadPreset = "photobooth_videos";

    public bool isBusy = false;
    private string videoFolderPath;

    /* -------------------- UNITY -------------------- */
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        videoFolderPath = Path.Combine(Application.streamingAssetsPath, "Videos");
        EnsureVideoDirectoryExists();

        if (sendImageButton != null)
            sendImageButton.onClick.AddListener(SendImageEmail);

        if (sendVideoButton != null)
            sendVideoButton.onClick.AddListener(UploadVideoAndSendEmail);

        if (sendPhotoboothMedia != null)
            sendPhotoboothMedia.onClick.AddListener(_sendPhotoboothMedia);
    }

    /* -------------------- DIRECTORY -------------------- */
    void EnsureVideoDirectoryExists()
    {
        if (!Directory.Exists(videoFolderPath))
        {
            Directory.CreateDirectory(videoFolderPath);
            Debug.Log("? Created directory: " + videoFolderPath);
        }
        else
        {
            Debug.Log("? Video directory exists: " + videoFolderPath);
        }
    }

    /* ======================================================
       =============== IMAGE EMAIL FUNCTION =================
       ====================================================== */
    public void SendImageEmail()
    {
        if (isBusy)
        {
            Debug.LogWarning("? Process already running.");
            return;
        }

        if (sourceImage == null || sourceImage.sprite == null)
        {
            Debug.LogError("? Source Image missing.");
            return;
        }

        StartCoroutine(SendImageEmailCoroutine());
    }

    IEnumerator SendImageEmailCoroutine()
    {
        isBusy = true;
        Debug.Log("?? Sending image email...");

        Texture2D texture = SpriteToTexture2D(sourceImage.sprite);
        byte[] jpgBytes = texture.EncodeToJPG(85);
        string base64Image = System.Convert.ToBase64String(jpgBytes);

        EmailPayload payload = new EmailPayload
        {
            mailTo = userEmail,
            imageData = base64Image,
            videoUrl = null
        };

        yield return SendEmailRequest(payload);
        isBusy = false;
    }

    /* ======================================================
       =============== VIDEO UPLOAD FUNCTION ================
       ====================================================== */
    public void UploadVideoAndSendEmail()
    {
        if (isBusy)
        {
            Debug.LogWarning("? Process already running.");
            return;
        }

        string videoPath = GetFirstVideoFile();

        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogError("? No video found in StreamingAssets/Videos");
            return;
        }

        StartCoroutine(UploadVideoCoroutine(videoPath));
    }

    IEnumerator UploadVideoCoroutine(string videoFilePath)
    {
        isBusy = true;
        Debug.Log("?? Uploading video: " + videoFilePath);

        byte[] videoBytes = File.ReadAllBytes(videoFilePath);
        Debug.Log($"?? Video size: {videoBytes.Length / (1024f * 1024f):F2} MB");

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", videoBytes, Path.GetFileName(videoFilePath), "video/mp4");
        form.AddField("upload_preset", uploadPreset);

        string uploadUrl = $"https://api.cloudinary.com/v1_1/{cloudName}/video/upload";

        UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form);
        request.timeout = 120;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("? Video upload failed: " + request.error);
            isBusy = false;
            yield break;
        }

        CloudinaryResponse response =
            JsonUtility.FromJson<CloudinaryResponse>(request.downloadHandler.text);

        Debug.Log("?? Video URL: " + response.secure_url);
        if (GenerateQRCode.instance != null)
        {
            GenerateQRCode.instance.setQRCodeContent(response.secure_url);
        }
        else
        {
            Debug.LogError("GenerateQRCode instance is NULL");
        }

        // Now send email with video URL
        EmailPayload payload = new EmailPayload
        {
            mailTo = userEmail,
            imageData = null,
            videoUrl = response.secure_url
        };

        yield return SendEmailRequest(payload);
        isBusy = false;
    }

    /* ======================================================
       =============== EMAIL REQUEST ========================
       ====================================================== */
    IEnumerator SendEmailRequest(EmailPayload payload)
    {
        string jsonData = JsonUtility.ToJson(payload);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("?? Sending email request...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("? Email failed: " + request.error);
            Debug.LogError("?? Response: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("? Email sent successfully!");
            Debug.Log("?? Response: " + request.downloadHandler.text);
        }
    }

    /* -------------------- HELPERS -------------------- */
    Texture2D SpriteToTexture2D(Sprite sprite)
    {
        Texture2D tex = new Texture2D(
            (int)sprite.rect.width,
            (int)sprite.rect.height,
            TextureFormat.RGB24,
            false
        );

        Color[] pixels = sprite.texture.GetPixels(
            (int)sprite.rect.x,
            (int)sprite.rect.y,
            (int)sprite.rect.width,
            (int)sprite.rect.height
        );

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    string GetFirstVideoFile()
    {
        if (!Directory.Exists(videoFolderPath))
            return null;

        string[] files = Directory.GetFiles(videoFolderPath, "*.mp4");
        return files.Length > 0 ? files[0] : null;
    }

    public void _sendPhotoboothMedia()
    {
        if (isBusy)
        {
            Debug.LogWarning("? Process already running. Wait for it to finish.");
            return;
        }

        StartCoroutine(SendImageAndVideoCoroutine());
    }

    IEnumerator SendImageAndVideoCoroutine()
    {
        isBusy = true;
        Debug.Log("?? Starting combined image + video email process...");

        string base64Image = null;
        string videoUrl = null;

        // --- Step 1: Process Image ---
        if (sourceImage != null && sourceImage.sprite != null)
        {
            Debug.Log("?? Converting sprite to Base64...");
            Texture2D texture = SpriteToTexture2D(sourceImage.sprite);
            byte[] jpgBytes = texture.EncodeToJPG(85);
            base64Image = System.Convert.ToBase64String(jpgBytes);
            Debug.Log($"? Image processed ({jpgBytes.Length / 1024f:F2} KB), Base64 length: {base64Image.Length}");
        }
        else
        {
            Debug.LogWarning("? Source image missing, skipping image.");
        }

        // --- Step 2: Upload Video ---
        string videoPath = GetFirstVideoFile();
        if (!string.IsNullOrEmpty(videoPath))
        {
            Debug.Log("?? Uploading video: " + videoPath);

            byte[] videoBytes = File.ReadAllBytes(videoPath);
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", videoBytes, Path.GetFileName(videoPath), "video/mp4");
            form.AddField("upload_preset", uploadPreset);

            string uploadUrl = $"https://api.cloudinary.com/v1_1/{cloudName}/video/upload";
            UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form);
            request.timeout = 120;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("? Video upload failed: " + request.error);
            }
            else
            {
                CloudinaryResponse response =
                    JsonUtility.FromJson<CloudinaryResponse>(request.downloadHandler.text);
                videoUrl = response.secure_url;
                Debug.Log("? Video uploaded successfully: " + videoUrl);

                if (GenerateQRCode.instance != null)
                {
                    GenerateQRCode.instance.setQRCodeContent(videoUrl);
                    Debug.Log("Url is sent to QR script" + videoUrl);
                }
                else
                {
                    Debug.LogError("GenerateQRCode instance is NULL");
                }

            }
        }
        else
        {
            Debug.LogWarning("? No video found, skipping video upload.");
        }

        // --- Step 3: Send combined email ---
        if (string.IsNullOrEmpty(base64Image) && string.IsNullOrEmpty(videoUrl))
        {
            Debug.LogWarning("? Nothing to send. Aborting email.");
            isBusy = false;
            yield break;
        }

        EmailPayload payload = new EmailPayload
        {
            mailTo = userEmail,
            imageData = base64Image,
            videoUrl = videoUrl
        };

        // Debug: print payload before sending
        Debug.Log($"?? Sending payload:\n{JsonUtility.ToJson(payload)}");

        yield return SendEmailRequest(payload);

        isBusy = false;
        Debug.Log("?? Combined email process finished.");
    }


    /* -------------------- DATA -------------------- */
    [System.Serializable]
    private class EmailPayload
    {
        public string mailTo;
        public string imageData;
        public string videoUrl;
    }

    [System.Serializable]
    private class CloudinaryResponse
    {
        public string secure_url;
    }
}
