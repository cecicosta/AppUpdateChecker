using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WWWRequestFile : Singleton<WWWRequestFile> {
    private WWW www;
    private bool done = true;
    private bool success;
    private string errorMessage;

    public WWW Www {
        get {
            return www;
        }
    }

    public bool Done {
        get {
            return done;
        }
    }

    public bool Success {
        get {
            return success;
        }
    }

    public string ErrorMessage {
        get {
            return errorMessage;
        }
    }

    public void GetImage(string picturePathAndName, RawImage output, bool isExternal = false) {
        done = false;
        success = true;
        StartCoroutine(RequestImage(isExternal ? picturePathAndName : "file://" + picturePathAndName, output));
    }

    public void GetFile(string PathAndName, bool isExternal = false) {
        done = false;
        success = true;
        StartCoroutine(RequestFile(isExternal ? PathAndName : "file://" + PathAndName));
    }

    public float GetProgress() {
        return www.progress;
    }

    public byte[] GetBytes() {
        return www.bytes;
    }

    IEnumerator RequestFile(string url) {
        www = new WWW(url);
        yield return www;

        if (!string.IsNullOrEmpty(www.error)) {
            success = false;
            done = true;
            errorMessage = www.error;
            Debug.Log(www.error);
            yield break;
        }
        done = true;
    }

    IEnumerator RequestImage(string url, RawImage output) {
        Texture2D texture = new Texture2D(1, 1);
        www = new WWW(url);
        yield return www;

        if (!string.IsNullOrEmpty(www.error)) {
            success = false;
            done = true;
            errorMessage = www.error;
            Debug.Log(www.error);
            yield break;
        }

        www.LoadImageIntoTexture(texture);
        Vector2 imgSize = output.rectTransform.rect.size;
        //Adjust the image size to fit the thumbnail withouth overflowing the cell
        float texAspect = (float)texture.width / (float)texture.height;
        if (texAspect >= 1) {
            output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (int)imgSize.x);
            output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (int)(imgSize.x / texAspect));
        } else {
            output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (int)(imgSize.y * texAspect));
            output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (int)imgSize.y);
        }

        output.texture = texture;
        done = true;
    }

}

