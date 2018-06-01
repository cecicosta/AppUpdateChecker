using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DownloadPopup : Singleton<DownloadPopup> {

    public GameObject container;
    public Text message;
    public Button buttom;
    public Text buttonText;
    public Image progressFill;

    public void SetProgress(float progress) {
        progressFill.fillAmount = progress;
    }
    public void ShowPopup(bool show) {
        container.SetActive(show);
    }
}
