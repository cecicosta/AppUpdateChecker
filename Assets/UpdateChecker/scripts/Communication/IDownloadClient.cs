using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IDownloadClient: UC_Singleton <IDownloadClient>{
    public abstract bool Done { get; }
    public abstract bool Failed { get; } 
    public abstract string ErrorMessage { get; }
    public abstract float Progress { get; }
    public abstract byte[] Bytes { get; }
    public abstract long DownloadFileSize { get; }
    public static IDownloadClient CreateNewInstance() { DestroyInstance(); return Instance;  }
    
    public abstract void Download(string url, string outputPath);
    public virtual void SetCredentials(string userName, string password) { }
    public abstract void Cancel();
}
