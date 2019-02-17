using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UpdateChecker : MonoBehaviour {

    public enum ClientType {
        FtpClient,
        HttpClient
    }
    public ClientType clientType = ClientType.HttpClient;

    public string changelogFileName;

    [SerializeField]
    private string currentVersion;
    [SerializeField]
    private string serverPath;
    [SerializeField]
    private string username;
    [SerializeField]
    private string password;

    public Text version;
    public UnityEvent onFinished;
    
    private List<string> logsHistory = new List<string>();
    private Dictionary<string, string> changes = new Dictionary<string, string>();
    IDownloadClient client;

    private string FilesPath {
        get {
            string outputPath = GetDownloadsPath();
            if (!string.IsNullOrEmpty(outputPath) && !Directory.Exists(outputPath)) {
                Debug.Log("using path: " + outputPath); 
                return outputPath + "/";
            }
            Debug.Log("using path: " + Application.persistentDataPath);
            return Application.persistentDataPath + "/"; 
        }
    }

    void OnEnable() {
        version.text = "v" + currentVersion;
        StartCoroutine(CheckUpdates());
    }

    private IEnumerator CheckUpdates() {
        DownloadPopup.Instance.message.text = "Checando atualizações, Por favor, aguarde...";
        DownloadPopup.Instance.ShowPopup(true);
        DownloadPopup.Instance.buttom.onClick.AddListener(CancelDownload);

        //Check the type of the selected client
        client = null;
        if (clientType == ClientType.FtpClient) {
            client = FtpDownload.CreateNewInstance();
        } else if(clientType == ClientType.HttpClient) {
            client = HttpDownload.CreateNewInstance();
        }

        //Starts the download process
        client.SetCredentials(username, password);
        client.Download(serverPath + "/" + changelogFileName, "");
        while (!client.Done && !client.Failed) {
            DownloadPopup.Instance.SetProgress(client.Progress);
            yield return null;
        }

        //Verify if the file download succeed
        if (client.Failed) {
            DownloadPopup.Instance.message.text = "Falha ao verificar atualizações. Tente novamente mais tarde ou contate o suporte. Erro: " + client.ErrorMessage;
            DownloadPopup.Instance.buttonText.text = "Fechar";
        } else {
            DownloadPopup.Instance.ShowPopup(false);
        }

        //Each line on the file represent a new version and contains the information of the updates
        string str = System.Text.Encoding.UTF8.GetString(FtpDownload.Instance.Bytes);
        //Parse the first line which must contains the headers and the last line, which is the last version
        logsHistory = new List<string>(System.Text.RegularExpressions.Regex.Split(str, "\r\n|\r|\n"));
        string[] headers = null;
        string[] infos = null;
        if (logsHistory.Count > 0) {
            headers = logsHistory[0].Split(':');
            infos = logsHistory[logsHistory.Count - 1].Split(':');
            for(int i=0; i<headers.Length && i< infos.Length; i++) {
                changes.Add(headers[i], infos[i]);
            }
        }

        
        string version;
        //Compare if the last version from the file is higher than the current running
        if(changes.TryGetValue("versao", out version) && version.CompareTo(currentVersion) == 1) {
            string fileName;
            changes.TryGetValue(headers[0], out fileName);
           
            string message = FileExists(FilesPath + fileName)? "Um arquivo referente à atualização detectada já existe. Gostaria de fazer o download novamente?\n": 
            "Uma nova versão foi detectada. Gostaria de iniciar o download?\n";

            ChangelogPopup.Instance.Message = message +
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
            ChangelogPopup.Instance.SecondButtom.onClick.AddListener(() => { response = 2; });
            ChangelogPopup.Instance.ShowPopup(true);

            //Wait until the user choose an option
            yield return new WaitUntil(() => response != 0);

            ChangelogPopup.Instance.ShowPopup(false);
            if (response == 1) {
                StartCoroutine(DownloadUpdate(fileName));
            }
            //Ask if the user wnats to execute the file, if it already exists and he choose to not download again 
            else if (FileExists(FilesPath + fileName)) { 
                ChangelogPopup.Instance.Message = "Gostaria de executar o arquivo encontrado?";
                ChangelogPopup.Instance.FirstButtonText = "Sim";
                ChangelogPopup.Instance.SecondButtonText = "Não";
                ChangelogPopup.Instance.FirstButtom.onClick.AddListener(() => { RunFile(fileName); ChangelogPopup.Instance.ShowPopup(false);  onFinished.Invoke(); });
                ChangelogPopup.Instance.SecondButtom.onClick.AddListener(() => { ChangelogPopup.Instance.ShowPopup(false); onFinished.Invoke(); });
                ChangelogPopup.Instance.ShowPopup(true);
            } else {
                onFinished.Invoke();
            }
        } else {
            onFinished.Invoke();
        }
    }

    private IEnumerator DownloadUpdate(string filePath) {
        DownloadPopup.Instance.message.text = "Fazendo o download da nova versão. Por favor, aguarde...";
        DownloadPopup.Instance.ShowPopup(true);
        DownloadPopup.Instance.buttom.onClick.AddListener(CancelDownload);

        //Get changelog from server
        string fileName = Path.GetFileName(filePath);
        fileName = IndexedFilename(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));

        client = null;
        //Check the type of the selected client
        if (clientType == ClientType.FtpClient) {
            client = FtpDownload.CreateNewInstance();
        } else if (clientType == ClientType.HttpClient) {
            client = HttpDownload.CreateNewInstance();
        }

        //Starts the Download process
        client.SetCredentials(username, password);
        client.Download(serverPath + "/" + filePath, FilesPath + fileName);
        while (!client.Done && !client.Failed) {
            DownloadPopup.Instance.SetProgress(client.Progress);
            yield return null;
        }

        if (client.Failed) {
            DownloadPopup.Instance.message.text = "O download falhou. Tente novamente mais tarde ou contate o suporte. Erro:" + client.ErrorMessage;
            DownloadPopup.Instance.buttonText.text = "Fechar";
        } else {
            DownloadPopup.Instance.ShowPopup(false);
            ChangelogPopup.Instance.Message = "O arquivo " + fileName + " foi salvo em 'Downlaods'. Pressione 'Ok' para executar a intalação ou 'Fechar' para instalar posteriormente";
            ChangelogPopup.Instance.FirstButtonText = "Ok";
            ChangelogPopup.Instance.SecondButtonText = "Fechar";
            AddToAndroidDownloads(fileName);
            ChangelogPopup.Instance.ShowPopup(true);
            ChangelogPopup.Instance.FirstButtom.onClick.AddListener(() => { RunFile(fileName); CancelDownload(); ChangelogPopup.Instance.ShowPopup(false); });
            ChangelogPopup.Instance.SecondButtom.onClick.AddListener(() => { CancelDownload(); ChangelogPopup.Instance.ShowPopup(false); });
        }
    }

    private void CancelDownload() {
        if (client != null)
            client.Cancel();
        onFinished.Invoke();
    }

    private bool FileExists(string filePath) {
        return File.Exists(filePath);
    }

    string IndexedFilename(string stub, string extension) {
        int ix = 0;
        if(!FileExists(FilesPath + stub + extension)) {
            return stub + extension;
        }

        string filename = null;
        do {
            ix++;
            filename = String.Format("{0}{1}{2}", stub, "(" + ix + ")", extension);
        } while (File.Exists(filename));
        return filename;
    }

    private void RunFile(string fileName) {
        try {
#if UNITY_ANDROID && !UNITY_EDITOR
            //Get context
            AndroidJavaClass classPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject objActivity = classPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = objActivity.Call<AndroidJavaObject>("getApplicationContext");

            AndroidJavaClass intentObj = new AndroidJavaClass("android.content.Intent");
            string ACTION_VIEW = intentObj.GetStatic<string>("ACTION_VIEW");
            int FLAG_ACTIVITY_NEW_TASK = intentObj.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK");
            int FLAG_GRANT_READ_URI_PERMISSION = intentObj.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", ACTION_VIEW);

            string packageName = context.Call<string>("getPackageName");
            string authority = packageName + ".fileprovider";

            AndroidJavaObject fileObj = new AndroidJavaObject("java.io.File", FilesPath + fileName);
            AndroidJavaClass fileProvider = new AndroidJavaClass("android.support.v4.content.FileProvider");
            AndroidJavaObject uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", context, authority, fileObj);

            intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
            intent.Call<AndroidJavaObject>("addFlags", FLAG_ACTIVITY_NEW_TASK);
            intent.Call<AndroidJavaObject>("addFlags", FLAG_GRANT_READ_URI_PERMISSION);

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            currentActivity.Call("startActivity", intent);
#endif
        } catch (Exception e) {
            DownloadPopup.Instance.message.text = e.Message;
            Debug.Log(e.Message);
            DownloadPopup.Instance.ShowPopup(true);
        }
    }

    private string GetDownloadsPath() {
        try {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass envClass = new AndroidJavaClass("android.os.Environment");
        string tag = envClass.GetStatic<string>("DIRECTORY_DOWNLOADS");
        AndroidJavaObject file = envClass.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", new object[] { tag });
        return file.Call<string>("getAbsolutePath");
#else
            return Application.persistentDataPath;
#endif
        }
        catch(Exception e) {
            Debug.Log(e.Message);
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
            manager.Call<long>("addCompletedDownload", fileName, fileName, true, "application/vnd.android.package-archive", FilesPath + fileName, client.DownloadFileSize, true);

            //AndroidJavaObject mime = new AndroidJavaObject("java.lang.String", new object[] { "application/octet-stream" });
            //AndroidJavaObject path = new AndroidJavaObject("java.lang.String", new object[] { FilesPath + fileName });

            //AndroidJavaClass scanner = new AndroidJavaClass("android.media.MediaScannerConnection");
            //scanner.CallStatic("scanFile", new object[] { context, new object[] { path }, new object[] { mime }, null });
#endif
        }
        catch (Exception e) {
            DownloadPopup.Instance.message.text = e.Message;
            Debug.Log(e.Message);
            DownloadPopup.Instance.ShowPopup(true);
        }
    }
}
