using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangelogPopup : UC_Singleton<ChangelogPopup> {

    public GameObject container;

    [SerializeField] private Text message;
    [SerializeField] private Button firstButton;
    [SerializeField] private Button secondButton;
    [SerializeField] private Text firtButtonText;
    [SerializeField] private Text secondButtonText;


    public string Message {
        get { return message.text; }
        set { message.text = value; }
    }
    public string FirstButtonText {
        get { return firtButtonText.text; }
        set { firtButtonText.text = value; }
    }
    public string SecondButtonText {
        get { return secondButtonText.text; }
        set { secondButtonText.text = value; }
    }
    public Button FirstButtom { get { return firstButton; } }
    public Button SecondButtom { get { return secondButton; } }

    public void ShowPopup(bool show) {
        container.SetActive(show);
    }
}
