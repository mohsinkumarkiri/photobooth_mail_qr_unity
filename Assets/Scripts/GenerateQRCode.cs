using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using AOT;
using QRCodeShareMain;
using TMPro;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Toggle = UnityEngine.UI.Toggle;

public class GenerateQRCode : MonoBehaviour
{
    public static GenerateQRCode instance;

    [DllImport("__Internal")]
    private static extern void DownloadFile(string fileName, string content);
    [DllImport("__Internal")]
    private static extern void UploadFile(Action<string> callbackMethodName);

    [Header("Generate QR Code References")]
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Image showImageGenerate;
    [SerializeField] private Button generateQRCode;

    private enum Style
    {
        basic
    }
    private Style currentStyle = Style.basic;
    private Texture2D currentQRCodeGenerate = null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Debug.Log("GenerateQRCode instance set");
    }

    // Start is called before the first frame update
    void Start()
    {
        //generateQRCode.onClick.AddListener(OnClickGenerateQRCode);
        //generateQRCode.onClick.AddListener(setQRCodeContent);
    }

    private Texture2D HelloWorldQRCode(string content)
    {
        QRImageProperties properties = new QRImageProperties(500, 500, 50);
        Texture2D QRCodeImage = QRCodeShare.CreateQRCodeImage(content, properties);
        return QRCodeImage;
    }

    public void setQRCodeContent(string contentUrl)
    {
        Debug.Log("setQRCodeContent CALLED");

        if (contentText == null)
        {
            Debug.LogError("contentText is NULL");
            return;
        }

        contentText.text = contentUrl;
        Debug.Log("QR Code URL is: " + contentUrl);

        OnClickGenerateQRCode();
    }


    private void OnClickGenerateQRCode()
    {
        string content = contentText.text;
        

        switch (currentStyle)
        {
            case Style.basic:
                currentQRCodeGenerate = HelloWorldQRCode(content);
                break;
            default:
                break;
        }

        if (currentQRCodeGenerate != null)
        {
            ShowImage(showImageGenerate, currentQRCodeGenerate);
            // Enable the download image button is the texture is not null
            //downloadImage.interactable = true;
        }

    }

    private void ShowImage(Image showImage, Texture2D image)
    {
        showImage.sprite = ImageProcessing.ConvertTexture2DToSprite(image);
        float imageSize = Mathf.Max(showImage.GetComponent<RectTransform>().sizeDelta.x, showImage.GetComponent<RectTransform>().sizeDelta.y);

        showImage.GetComponent<RectTransform>().sizeDelta = image.width <= image.height ?
            new Vector2(imageSize / image.height * image.width, imageSize) :
            new Vector2(imageSize, imageSize * image.height / image.width);
    }
}
