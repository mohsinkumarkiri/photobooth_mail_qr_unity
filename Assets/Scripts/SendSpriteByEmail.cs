using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class SendSpriteByEmail : MonoBehaviour
{
    [Header("UI References")]
    public Image sourceImage;      // The Image component holding the Sprite
    public Button postButton;      // Button that triggers send

    [Header("Email Settings")]
    public string apiUrl = "https://mohsin-photobooth-mailer1.vercel.app/api/send-photo";
    public string userEmail = "test@example.com";

    private bool isSending = false;

    void Start()
    {
        if (postButton != null)
            postButton.onClick.AddListener(OnPostClicked);
    }

    void OnPostClicked()
    {
        if (isSending)
        {
            Debug.LogWarning("? Already sending email. Please wait.");
            return;
        }

        if (sourceImage == null || sourceImage.sprite == null)
        {
            Debug.LogError("? Source Image or Sprite is missing.");
            return;
        }

        StartCoroutine(SendEmailCoroutine());
    }

    IEnumerator SendEmailCoroutine()
    {
        isSending = true;
        Debug.Log("?? Starting email send process...");

        // Convert Sprite to Texture2D
        Texture2D texture = SpriteToTexture2D(sourceImage.sprite);

        if (texture == null)
        {
            Debug.LogError("? Failed to convert sprite to texture.");
            isSending = false;
            yield break;
        }

        Debug.Log($"?? Texture created: {texture.width}x{texture.height}");

        // Encode to JPG
        byte[] jpgBytes = texture.EncodeToJPG(85); // 85% quality
        if (jpgBytes == null || jpgBytes.Length == 0)
        {
            Debug.LogError("? JPG encoding failed.");
            isSending = false;
            yield break;
        }

        Debug.Log($"?? JPG size: {jpgBytes.Length / 1024f:F2} KB");

        // Convert to Base64
        string base64Image = System.Convert.ToBase64String(jpgBytes);
        Debug.Log($"?? Base64 length: {base64Image.Length}");

        // Create JSON payload
        EmailPayload payload = new EmailPayload
        {
            mailTo = userEmail,
            imageData = base64Image
        };

        string jsonData = JsonUtility.ToJson(payload);
        Debug.Log("?? JSON Payload created.");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("?? Sending POST request to server...");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"? Request failed: {request.error}");
            Debug.LogError($"?? Server response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.Log("? Email sent successfully!");
            Debug.Log($"?? Server response: {request.downloadHandler.text}");
        }

        isSending = false;
    }

    // Converts a Sprite to a readable Texture2D
    Texture2D SpriteToTexture2D(Sprite sprite)
    {
        try
        {
            if (sprite.rect.width != sprite.texture.width ||
                sprite.rect.height != sprite.texture.height)
            {
                Texture2D newTex = new Texture2D(
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

                newTex.SetPixels(pixels);
                newTex.Apply();
                return newTex;
            }

            return sprite.texture;
        }
        catch (System.Exception e)
        {
            Debug.LogError("? SpriteToTexture2D error: " + e.Message);
            return null;
        }
    }

    [System.Serializable]
    private class EmailPayload
    {
        public string mailTo;
        public string imageData;
    }
}
