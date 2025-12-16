using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CloudinaryVideoUploader : MonoBehaviour
{
    [Header("Cloudinary Settings")]
    public string cloudName = "dx28spymd";
    public string uploadPreset = "photobooth_videos";

    private string videoFolderPath;

    void Start()
    {
        // StreamingAssets/Videos path
        videoFolderPath = Path.Combine(Application.streamingAssetsPath, "Videos");

        Debug.Log("?? Video folder path: " + videoFolderPath);

        EnsureVideoDirectoryExists();
    }

    // Ensures StreamingAssets/Videos exists
    void EnsureVideoDirectoryExists()
    {
        if (!Directory.Exists(videoFolderPath))
        {
            Directory.CreateDirectory(videoFolderPath);
            Debug.Log("? Created directory: " + videoFolderPath);
        }
        else
        {
            Debug.Log("? Video directory already exists.");
        }
    }

    // Public method to start upload
    public void UploadVideoFromStreamingAssets()
    {
        string videoPath = GetFirstVideoFile();

        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogError("? No video file found in StreamingAssets/Videos");
            return;
        }

        Debug.Log("?? Found video file: " + videoPath);
        StartCoroutine(UploadVideoCoroutine(videoPath));
    }

    // Finds first MP4 video in directory
    string GetFirstVideoFile()
    {
        if (!Directory.Exists(videoFolderPath))
            return null;

        string[] files = Directory.GetFiles(videoFolderPath, "*.mp4");

        if (files.Length == 0)
            return null;

        return files[0];
    }

    IEnumerator UploadVideoCoroutine(string videoFilePath)
    {
        Debug.Log("?? Starting video upload...");

        byte[] videoBytes;

        try
        {
            videoBytes = File.ReadAllBytes(videoFilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("? Failed to read video file: " + e.Message);
            yield break;
        }

        Debug.Log($"?? Video size: {videoBytes.Length / (1024f * 1024f):F2} MB");

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", videoBytes, Path.GetFileName(videoFilePath), "video/mp4");
        form.AddField("upload_preset", uploadPreset);

        string uploadUrl = $"https://api.cloudinary.com/v1_1/{cloudName}/video/upload";
        Debug.Log("?? Upload URL: " + uploadUrl);

        UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form);
        request.timeout = 120; // 2 minutes for large files

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("? Video upload failed");
            Debug.LogError("?? Error: " + request.error);
            Debug.LogError("?? Response: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("? Video uploaded successfully!");
            Debug.Log("?? Response: " + request.downloadHandler.text);

            CloudinaryResponse response =
                JsonUtility.FromJson<CloudinaryResponse>(request.downloadHandler.text);

            Debug.Log("?? Video URL: " + response.secure_url);

            // TODO: Send this URL to your email API
        }
    }

    [System.Serializable]
    private class CloudinaryResponse
    {
        public string secure_url;
    }
}
