using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class WWWRequestFile : UC_Singleton<WWWRequestFile> {
    private WWW www;
    private bool done = true;
    private bool success;
    private string errorMessage;
    private int progress;

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

    public void WebRequestDownload(string url, string outputPath) {
        WebClient client = new WebClient();
        client.DownloadFile(url, outputPath);
        client.DownloadProgressChanged += UpdateProgress;
        client.DownloadFileCompleted += DownloadCompleted;
    }

    private void DownloadCompleted(object sender, AsyncCompletedEventArgs e) {
        throw new NotImplementedException();
    }

    private void UpdateProgress(object sender, DownloadProgressChangedEventArgs e) {
        progress = e.ProgressPercentage;
    }

}

