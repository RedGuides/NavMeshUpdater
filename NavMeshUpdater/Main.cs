using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net.NetworkInformation;

using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Json;

namespace NavMeshUpdater
{
    public partial class Main : Form
    {
        public static string updaterJsonURL = "https://rootswitch.com/mirror/MQ2/MQ2Nav/updater.json";
        public static bool pInit = false;
        public static int CurrentDownloadPct { get; set; } = 0;
        public static int OverallDownloadPct { get; set; } = 0;
        public static bool pDownloading = false;
        public int localCount, remoteCount, missingCount, updateCount;
        public static Dictionary<string, string> LocalFileStore = new Dictionary<string, string>();
        public static Dictionary<string, string> RemoteFileStore = new Dictionary<string, string>();
        public static Dictionary<string, string> MissingFileStore = new Dictionary<string, string>();
        public static Dictionary<string, string> ToUpdateFileStore = new Dictionary<string, string>();
        public static Zone zone = new Zone();
        // Where to put the files
        public static readonly string currentDirectory = Path.GetDirectoryName(Application.ExecutablePath);

        public void UpdateLocalCount()
        {
            DirectoryInfo di = new DirectoryInfo(currentDirectory);
            var localFiles = di.GetFiles("*.navmesh", SearchOption.TopDirectoryOnly);
            // Create a dictionary to compare against for updates.
            foreach (FileInfo file in localFiles)
            {
                LocalFileStore.Add(file.Name.Replace(".navmesh", ""), Utility.CalculateMD5(file.FullName) + "|" + file.FullName);
            }
            localCount = (localFiles.Length > 0) ? localFiles.Length : 0;
            label1.Text = "Local Files: " + localCount.ToString();
        }

        public void UpdateRemoteCount()
        {
            if (zone.Zones.Count() > 0)
            {
                foreach (KeyValuePair<string, ZoneValue> z in zone.Zones)
                {
                    RemoteFileStore.Add(z.Value.Shortname, z.Value.Files.Mesh.Hash + "|" + z.Value.Files.Mesh.Size + "|" + z.Value.Files.Mesh.Link);
                }
                remoteCount = zone.Zones.Count();
                label2.Text = "Remote Files: " + remoteCount.ToString();
            }
            else
            {
                ErrorMessage("Updater Failed","Updates file failed to download. No remote files found.");
            }
        }

        public void UpdateMissingFiles()
        {
            if (localCount < remoteCount)
            {
                missingCount = remoteCount - localCount;
                label5.Text = "Missing Files: " + missingCount.ToString();
            }
            else
            {
                missingCount = 0;
                label5.Text = "Missing Files: " + missingCount.ToString();
            }
        }
        public void GetRemoteUpdateFile()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    using (var wc = new WebClient())
                    {
                        var remoteFile = wc.DownloadString(updaterJsonURL);
                        zone = Zone.FromJson(remoteFile);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorMessage("Updater Failed", "Failed to download remote update file." + Environment.NewLine + e.ToString());
            }

        }

        public Main()
        {
            SplashScreen.ShowSplashScreen();
            InitializeComponent();
            if (!pInit)
            {
                GetRemoteUpdateFile();
                UpdateLocalCount();
                UpdateRemoteCount();
                UpdateMissingFiles();
                //CheckForUpdates();
                label3.Text = CurrentDownloadPct + "%";
                label4.Text = OverallDownloadPct + "%";
                groupBox1.Refresh();


                pInit = true;
            }
            if (pDownloading)
            {
                label3.Text = CurrentDownloadPct + "%";
                label4.Text = OverallDownloadPct + "%";
                groupBox1.Refresh();
            }
            Thread.Sleep(2000);
            SplashScreen.CloseForm();

        }
        // Open PM on forum for Bug Request
        private void ReportBugToolStripMenuItem_Click(object sender, EventArgs e) => Utility.OpenURL("https://www.redguides.com/community/conversations/add?title=BugReport:NavmeshUpdater&to=wired420");
        // Open PM on forum for Feature Request
        private void FeatureRequestToolStripMenuItem_Click(object sender, EventArgs e) => Utility.OpenURL("https://www.redguides.com/community/conversations/add?title=FeatureRequest:NavmeshUpdater&to=wired420");
        // Exit Application
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();
        private void QuitToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();
        public static void ErrorMessage(string title, string error) => MessageBox.Show(error, title, MessageBoxButtons.OK, MessageBoxIcon.Error);


    }
}
