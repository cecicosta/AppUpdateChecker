using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.IO;
using System;
using System.Threading;

public class FtpDownload : Singleton<FtpDownload> {

    private bool stop = false;
    private bool failed = false;
    public bool Failed {
        get {
            return failed;
        }
    }

    private bool done = false;
    public bool Done {
        get {
            return done;
        }
    }

    byte[] data;
    private Thread t;
    private float progress;
    private long downloadFileSize;

    public byte[] Bytes {
        get {
            return data;
        }
    }

    public float Progress {
        get {
            return progress;
        }
    }

    public long DownloadFileSize {
        get {
            return downloadFileSize;
        }
    }

    public void downloadWithFTP(string ftpUrl, string savePath = "", string userName = "", string password = "") {
        done = false;
        failed = false;
        stop = false;

        downloadFileSize = RequestFileSize(ftpUrl, userName, password);
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new System.Uri(ftpUrl));
        //request.Proxy = null;
        request.UsePassive = true;
        request.UseBinary = true;
        request.KeepAlive = true;
        request.ReadWriteTimeout = 30000;
        request.Timeout = 30000;
        
        //If username or password is NOT null then use Credential
        if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password)) {
            request.Credentials = new NetworkCredential(userName, password);
        }

        request.Method = WebRequestMethods.Ftp.DownloadFile;
        //If savePath is NOT null, we want to save the file to path
        //If path is null, we just want to return the file as array
        if (!string.IsNullOrEmpty(savePath)) {
            t = new System.Threading.Thread(delegate () {
                DownloadAndSave(request, savePath);
            });
            t.Start();
        } else {
            t = new System.Threading.Thread(delegate () {
                DownloadAsbyteArray(request);
            });
            t.Start();
        }

    }

    public void UploadFileWithFTP(string ftpUrl, Stream file, string userName = "", string password = "") {
        done = false;
        failed = false;
        stop = false;

        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new System.Uri(ftpUrl));
        //request.Proxy = null;

        request.UsePassive = true;
        request.UseBinary = true;
        request.KeepAlive = true;

        //If username or password is NOT null then use Credential
        if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password)) {
            request.Credentials = new NetworkCredential(userName, password);
        }

        request.Method = WebRequestMethods.Ftp.UploadFile;
        t = new System.Threading.Thread(delegate () {
            UploadFile(request, file);
        });
        t.Start();
    }

    internal void Cancel() {
        stop = true;
    }

    void UploadFile(FtpWebRequest ftprequest, System.IO.Stream file) {
        long sent = 0;
        try {
            WebResponse request = ftprequest.GetResponse();
            int read = 0;
            
            using (Stream output = request.GetResponseStream()) {
                byte[] buffer = new byte[16 * 1024];

                while (!stop && file.Position < file.Length) {
                    read = file.Read(buffer, 0, 16 * 1024 - 1);
                    output.Write(buffer, 0, read);
                    sent += read;
                    progress = (sent / 10000.0f) / (file.Length / 10000.0f);
                }
                done = true;
            }
        }
        catch (System.Exception e) {
            Debug.Log("Error to upload file: " + e.Message);
            failed = true;
        }
    }

    void DownloadAsbyteArray(FtpWebRequest ftprequest) {
        long received = 0;
        try {
            WebResponse request = ftprequest.GetResponse();

            using (Stream input = request.GetResponseStream()) {
                byte[] buffer = new byte[16 * 1024];
                using (MemoryStream ms = new MemoryStream()) {
                    int read;
                    while (!stop && input.CanRead && (read = input.Read(buffer, 0, buffer.Length)) > 0) {
                        ms.Write(buffer, 0, read);
                        received += read;
                        progress = (received / 10000.0f) / (downloadFileSize / 10000.0f);
                    }
                    done = true;
                    data = ms.ToArray();
                }
            }
        }
        catch (System.Exception e) {
            Debug.Log("Error to download file" + e.Message);
            failed = true;
        }
    }

    void DownloadAndSave(FtpWebRequest ftprequest, string savePath) {
        long received = 0;
        try {
            WebResponse request = ftprequest.GetResponse();
            Stream reader = request.GetResponseStream();

            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(savePath))) {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            }

            FileStream fileStream = new FileStream(savePath, FileMode.Create);

            int bytesRead = 0;
            byte[] buffer = new byte[2048];
            Debug.Log("Download Finished");
            while (!stop) {
                bytesRead = reader.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                    break;

                fileStream.Write(buffer, 0, bytesRead);
                received += bytesRead;
                progress = (received / 10000.0f) / (downloadFileSize / 10000.0f);
            }
            Debug.Log("Download Finished");
            fileStream.Close();
            done = true;
        }
        catch (System.Exception e) {
            Debug.Log(e.Message);
            failed = true;
        }
    }

    long RequestFileSize(string serverPath, string user, string pass) {
        FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(new Uri(serverPath));
        //request.Proxy = null;
        request.Credentials = new NetworkCredential(user, pass);
        request.Method = WebRequestMethods.Ftp.GetFileSize;
        request.ReadWriteTimeout = 30000;
        request.Timeout = 30000;

        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
        long size = response.ContentLength;
        response.Close();
        return size;
    }
}