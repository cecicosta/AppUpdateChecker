using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class UpdateChecker : MonoBehaviour {

    public string changelogFileName;

    [SerializeField]
    private string currentVersion;
    [SerializeField]
    private string serverPath;
    [SerializeField]
    private string username;
    [SerializeField]
    private string password;

    public UnityEvent onFinished;
    
    private List<string> logsHistory = new List<string>();
    private Dictionary<string, string> changes = new Dictionary<string, string>();

    private string FilesPath {
        get {
            return Application.persistentDataPath + "/"; //GetDownloadsPath() + "/";
        }
    }

    void OnEnable() {
        StartCoroutine(CheckUpdates());
    }

    private IEnumerator CheckUpdates() {
        DownloadPopup.Instance.message.text = "Checando atualizações, Por favor, aguarde...";
        DownloadPopup.Instance.ShowPopup(true);
        DownloadPopup.Instance.buttom.onClick.AddListener(CancelDownload);

        //Get changelog from server
        FtpDownload.Instance.downloadWithFTP(serverPath + "/"+ changelogFileName, "", username, password);
        while (!FtpDownload.Instance.Done && !FtpDownload.Instance.Failed) {
            DownloadPopup.Instance.progressFill.fillAmount = FtpDownload.Instance.Progress;
            yield return null;
        }

        //Verify if the file download succeed
        if (FtpDownload.Instance.Failed) {
            DownloadPopup.Instance.message.text = "Falha ao verificar atualizações. Tente novamente mais tarde ou contate o suporte.";
            DownloadPopup.Instance.buttonText.text = "Fechar";
        } else {
            DownloadPopup.Instance.ShowPopup(false);
        }

        //Each line on the file represent a new version and contains the information of the updates
        string str = System.Text.Encoding.UTF8.GetString(FtpDownload.Instance.Bytes);
        //Parse the first line which must contains the headers and the last line, which is the last version
        logsHistory = new List<string>(System.Text.RegularExpressions.Regex.Split(str, "\r\n|\r|\n"));
        if (logsHistory.Count > 0) {
            string[] headers = logsHistory[0].Split(':');
            string[] infos = logsHistory[logsHistory.Count - 1].Split(':');
            for(int i=0; i<headers.Length && i< infos.Length; i++) {
                changes.Add(headers[i], infos[i]);
            }
        }

        string version;
        //Compare if the last version from the file is higher than the current running
        if(changes.TryGetValue("versao", out version) && version.CompareTo(currentVersion) == 1) {
            ChangelogPopup.Instance.Message = "Uma nova versão foi detectada. Gostaria de iniciar o download?" + "\n" +
                "\nVersão disponível: " + changes["versao"] + "\n" +
                "\nData: " + changes["data"] + "\n" +
                "\nMudanças detectadas:\n";
            string[] updates = changes["updates"].Split(',');
            foreach(string s in updates)
                ChangelogPopup.Instance.Message += "- " + s + "\n";

            int response = 0;
            ChangelogPopup.Instance.FirstButtonText = "Sim";
            ChangelogPopup.Instance.SecondButtonText = "Não";
            ChangelogPopup.Instance.FirstButtom.onClick.AddListener(() => { response = 1; });
            ChangelogPopup.Instance.SecondButtom.onClick.AddListener(() => { response = 2; CancelDownload(); });
            ChangelogPopup.Instance.ShowPopup(true);

            yield return new WaitUntil(() => response != 0);

            ChangelogPopup.Instance.ShowPopup(false);
            if (response == 1) {
                string fileName;
                changes.TryGetValue("nome", out fileName);
                StartCoroutine(DownloadUpdate(fileName));
            } 
        }
    }

    private IEnumerator DownloadUpdate(string filePath) {
        DownloadPopup.Instance.message.text = "Fazendo o download da nova versão. Por favor, aguarde...";
        DownloadPopup.Instance.ShowPopup(true);
        DownloadPopup.Instance.buttom.onClick.AddListener(CancelDownload);

        //Get changelog from server
        string fileName = Path.GetFileName(filePath);
        FtpDownload.Instance.downloadWithFTP(serverPath + "/" + filePath, FilesPath + fileName, username, password);
        while (!FtpDownload.Instance.Done && !FtpDownload.Instance.Failed) {
            DownloadPopup.Instance.progressFill.fillAmount = FtpDownload.Instance.Progress;
            yield return null; 
        }

        if (FtpDownload.Instance.Failed) {
            DownloadPopup.Instance.message.text = "O download falhou. Tente novamente mais tarde ou contate o suporte.";
            DownloadPopup.Instance.buttonText.text = "Fechar";
        } else {
            DownloadPopup.Instance.message.text = "Para instalar a nova versão, vá em Downlaods e execute o arquivo " + fileName;
            DownloadPopup.Instance.buttonText.text = "Ok";
            AddToAndroidDownloads(fileName);
        }
    }

    private void CancelDownload() {
        FtpDownload.Instance.Cancel();
        onFinished.Invoke();
    }

    private string GetDownloadsPath() {
        try {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass envClass = new AndroidJavaClass("android.os.Environment");
        string tag = envClass.GetStatic<string>("DIRECTORY_DOWNLOADS");
        AndroidJavaObject file = envClass.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", new object[] { tag });
        return file.Call<string>("getAbsolutePath");
#else
            return Application.dataPath;
#endif
        }
        catch(Exception e) {
            return e.Message;
        }
    }

    private void AddToAndroidDownloads(string fileName) {
        try {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass classPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject objActivity = classPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = objActivity.Call<AndroidJavaObject>("getApplicationContext");

            //string downloadService = context.Get<string>("DOWNLOAD_SERVICE");
            AndroidJavaObject manager = context.Call<AndroidJavaObject>("getSystemService", new object[] { "download" });
            manager.Call<long>("addCompletedDownload", fileName, fileName, true, "application/vnd.android.package-archive", FilesPath + fileName, FtpDownload.Instance.DownloadFileSize, true);

            //AndroidJavaObject mime = new AndroidJavaObject("java.lang.String", new object[] { "application/octet-stream" });
            //AndroidJavaObject path = new AndroidJavaObject("java.lang.String", new object[] { FilesPath + fileName });

            //AndroidJavaClass scanner = new AndroidJavaClass("android.media.MediaScannerConnection");
            //scanner.CallStatic("scanFile", new object[] { context, new object[] { path }, new object[] { mime }, null });
#endif
        }
        catch (Exception e) {
            DownloadPopup.Instance.message.text = e.Message;
            DownloadPopup.Instance.ShowPopup(true);
        }
    }
}
