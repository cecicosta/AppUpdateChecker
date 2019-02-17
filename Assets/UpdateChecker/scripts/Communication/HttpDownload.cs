using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class HttpDownload : IDownloadClient{
    private WWW www;
    private bool done;
    private string errorMessage;
    private float progress;
    private WebClient client;
    private bool failed;
    private byte[] data;
    private long downloadFileSize;

    public override bool Done {
        get {
            return done;
        }
    }

    public override bool Failed {
        get {
            return failed;
        }
    }

    public override string ErrorMessage {
        get {
            return errorMessage;
        }
    }

    public override float Progress {
        get {
            return progress;
        }
    }

    public override byte[] Bytes {
        get {
            return data;
        }
    }

    public override long DownloadFileSize {
        get {
            return downloadFileSize;
        }
    }

    public override void Download(string url, string outputPath) {
        //Get file size if possible
        try {
            WebClient consultLenghtWebClient = new WebClient();
            consultLenghtWebClient.OpenRead(url);
            downloadFileSize = Convert.ToInt64(consultLenghtWebClient.ResponseHeaders["Content-Length"]);
        }catch(Exception e) {
            done = true;
            failed = true;
            errorMessage = e.Message;
            Debug.Log(e.Message);
        }

        try {
            client = new WebClient();
            done = false;
            failed = false;
            progress = 0;

            if (!string.IsNullOrEmpty(outputPath) && !Directory.Exists(new FileInfo(outputPath).Directory.FullName)) {
                Directory.CreateDirectory(new FileInfo(outputPath).Directory.FullName);
            }

            client.DownloadProgressChanged += UpdateProgress;
            if (!string.IsNullOrEmpty(outputPath)) {
                client.DownloadFileCompleted += DownloadFileCompleted;
                client.DownloadFileAsync(new Uri(url), outputPath);
            } else {
                client.DownloadDataCompleted += DownloadDataCompleted;
                client.DownloadDataAsync(new Uri(url));
            }
        } catch (Exception e) {
            done = true;
            failed = true;
            errorMessage = e.Message;
            Debug.Log(e.Message);
        }
    }

    private void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e) {
        done = true;
        data = e.Result;
        downloadFileSize = e.Result.LongLength;
        if (e.Error != null) {
            failed = true;
            errorMessage = e.Error.Message;
        }
    }

    private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e) {
        done = true;
        if (e.Error != null) {
            failed = true;
            errorMessage = e.Error.Message;
        }
    }

    private void UpdateProgress(object sender, DownloadProgressChangedEventArgs e) {
        progress = e.ProgressPercentage / 100f;
    }

    public override void Cancel() {
        client.CancelAsync();
    }

    public new static IDownloadClient CreateNewInstance (){
        DestroyInstance();
        GameObject client = null;
        if (_Instance == null) {
           client = new GameObject("HttpDownloadClient", new Type[] { typeof(HttpDownload) });
        }
        return Instance;
    }
}

