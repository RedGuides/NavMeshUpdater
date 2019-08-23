using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;


namespace NavMeshUpdater
{
    class FileDownloader
    {
        private readonly string _url;
        private readonly string _fullPathWhereToSave;
        private bool _result = false;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        public FileDownloader(string url, string fullPathWhereToSave)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException("url");
            if (string.IsNullOrEmpty(fullPathWhereToSave)) throw new ArgumentNullException("fullPathWhereToSave");

            this._url = url;
            this._fullPathWhereToSave = fullPathWhereToSave;
        }

        public bool StartDownload(int timeout)
        {
            try
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(_fullPathWhereToSave));

                if (File.Exists(_fullPathWhereToSave))
                {
                    File.Delete(_fullPathWhereToSave);
                }
                using (WebClient client = new WebClient())
                {
                    var ur = new Uri(_url);
                    // client.Credentials = new NetworkCredential("username", "password");
                    client.DownloadProgressChanged += WebClientDownloadProgressChanged;
                    client.DownloadFileCompleted += WebClientDownloadCompleted;
                    Main.pDownloading = true;
                    client.DownloadFileAsync(ur, _fullPathWhereToSave);
                    _semaphore.Wait(timeout);
                    return _result && File.Exists(_fullPathWhereToSave);
                }
            }
            catch (Exception e)
            {
                Main.ErrorMessage("Download Error", "Unable to complete file download. Please try again or use the menu Help -> Bug Report." + Environment.NewLine + "Error: " + e);
                Main.CurrentDownloadPct = 0;
                Main.pDownloading = false;
                return false;
            }
            finally
            {
                this._semaphore.Dispose();
            }
        }

        private void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Main.CurrentDownloadPct = e.ProgressPercentage;
        }

        private void WebClientDownloadCompleted(object sender, AsyncCompletedEventArgs args)
        {
            _result = !args.Cancelled;
            if (!_result)
            {
                Main.ErrorMessage("Download Error", "Unable to complete file download. Please try again or use the menu Help -> Bug Report.");
            }
            Main.CurrentDownloadPct = 0;
            Main.pDownloading = false;
            _semaphore.Release();
        }

        public static bool DownloadFile(string url, string fullPathWhereToSave, int timeoutInMilliSec)
        {
            return new FileDownloader(url, fullPathWhereToSave).StartDownload(timeoutInMilliSec);
        }
    }
}