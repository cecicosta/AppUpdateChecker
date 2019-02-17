using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DownloadPopup : UC_Singleton<DownloadPopup> {

    public GameObject container;
    public Text message;
    public Button buttom;
    public Text buttonText;
    public Image progressFill;
    public Text progress;

    public void SetProgress(float progress) {
        progressFill.fillAmount = progress;
        this.progress.text = ((int)(progress * 100)) + "%";
    }
    public void ShowPopup(bool show) {
        container.SetActive(show);
    }
}
