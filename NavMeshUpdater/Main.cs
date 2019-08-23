using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net.NetworkInformation;

using System.Diagnostics;

using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Json;

namespace NavMeshUpdater
{
    public partial class Main : Form
    {
        public static bool DEBUG = true;
        public static string updaterJsonURL = "https://rootswitch.com/mirror/MQ2/MQ2Nav/updater.json";
        public static bool pInit = false;
        public static int CurrentDownloadPct { get; set; } = 0;
        public static int OverallDownloadPct { get; set; } = 0;
        public static bool pDownloading = false;
        public int localCount, remoteCount, missingCount, updateCount, totalCount, doneCount;
        public static string RemoteFile;
        public static Dictionary<string, string> LocalFileStore = new Dictionary<string, string>();
        public static Dictionary<string, string> RemoteFileStore = new Dictionary<string, string>();
        public static Dictionary<string, string> MissingFileStore = new Dictionary<string, string>();
        public static Dictionary<string, string> ToUpdateFileStore = new Dictionary<string, string>();
        public static readonly string currentDirectory = Path.GetDirectoryName(Application.ExecutablePath);
        public static readonly string meshDirectory = currentDirectory + "\\MQ2Nav";

        public void UpdateOverAllPct(int done, int total)
        {
            double calc = (done / total * 100);
            OverallDownloadPct = (int)Math.Truncate(calc);
            groupBox3.Refresh();
        }
        public void UpdateLocalFiles()
        {
            DirectoryInfo di = new DirectoryInfo(meshDirectory);
            if (!di.Exists) { di.Create(); }
            var localFiles = di.GetFiles("*.navmesh", SearchOption.TopDirectoryOnly);
            // Create a dictionary to compare against for updates.
            if (DEBUG)
            {
                Console.WriteLine("#################### LOCAL FILE STORE ####################");
            }
            foreach (FileInfo file in localFiles)
            {
                if (DEBUG)
                {
                    Console.WriteLine(file.Name.Replace(".navmesh", "") + "," + Utility.CalculateMD5(file.FullName) + "|" + file.FullName);
                }
                LocalFileStore.Add(file.Name.Replace(".navmesh", ""), Utility.CalculateMD5(file.FullName) + "|" + file.FullName);
            }
            localCount = (localFiles.Length > 0) ? localFiles.Length : 0;
            label1.Text = "Local Files: " + localCount.ToString();
        }

        public void UpdateRemoteFiles()
        {
            var zone = Zone.FromJson(RemoteFile);
            if (zone.Zones.Count() > 0)
            {
                if (DEBUG)
                {
                    Console.WriteLine("#################### REMOTE FILE STORE ####################");
                }
                foreach (KeyValuePair<string, ZoneValue> z in zone.Zones)
                {
                    var str = z.ToString().Replace("[", "");
                    var wrd = str.Split(',');
                    if (DEBUG)
                    {
                        Console.WriteLine(wrd[0].ToString() + "," + z.Value.Files.Mesh.Hash + "|" + z.Value.Files.Mesh.Size + "|" + z.Value.Expansion + "|" + z.Value.Files.Mesh.Link);
                    }
                    RemoteFileStore.Add(wrd[0].ToString(), z.Value.Files.Mesh.Hash + "|" + z.Value.Files.Mesh.Size + "|" + z.Value.Expansion + "|" + z.Value.Files.Mesh.Link);
                }
                remoteCount = (RemoteFileStore.Count() > 0) ? RemoteFileStore.Count() : 0;
                label2.Text = "Remote Files: " + remoteCount.ToString();
            }
            else
            {
                ErrorMessage("Updater Failed", "Updates file failed to download. No remote files found.");
            }
        }

        public void UpdateMissingFiles()
        {
            var zone = Zone.FromJson(RemoteFile);
            if (DEBUG)
            {
                Console.WriteLine("#################### MISSING FILE STORE ####################");
            }
            foreach (KeyValuePair<string, ZoneValue> z in zone.Zones)
            {
                var str = z.ToString().Replace("[", "");
                var wrd = str.Split(',');
                if (!LocalFileStore.ContainsKey(wrd[0]))
                {
                    if (DEBUG)
                    {
                        Console.WriteLine(wrd[0].ToString() + "," + z.Value.Files.Mesh.Link.ToString());
                    }
                    MissingFileStore.Add(wrd[0].ToString(), z.Value.Files.Mesh.Link.ToString());
                }
            }
            missingCount = (MissingFileStore.Count() > 0) ? MissingFileStore.Count() : 0;
            label5.Text = "Missing Files: " + missingCount.ToString();

        }

        public void CheckForUpdates()
        {
            var zone = Zone.FromJson(RemoteFile);
            if (DEBUG)
            {
                Console.WriteLine("#################### TO UPDATE FILE STORE ####################");
            }
            foreach (KeyValuePair<string, ZoneValue> z in zone.Zones)
            {
                var str = z.ToString().Replace("[", "");
                var wrd = str.Split(',');
                if (LocalFileStore.TryGetValue(wrd[0], out string value))
                {
                    var lArray = value.Split('|');
                    var lHash = lArray[0];
                    var rHash = z.Value.Files.Mesh.Hash.ToString();
                    if (lHash != rHash)
                    {
                        if (DEBUG)
                        {
                            Console.WriteLine(wrd[0] + "," + z.Value.Files.Mesh.Link.ToString());
                        }
                        ToUpdateFileStore.Add(wrd[0],z.Value.Files.Mesh.Link.ToString());
                    }
                }
            }
            updateCount = (ToUpdateFileStore.Count() > 0) ? ToUpdateFileStore.Count() : 0;
            label6.Text = "Updates Needed: " + updateCount.ToString();
        }

        public void GetRemoteUpdateFile()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    using (var wc = new WebClient())
                    {
                        RemoteFile = wc.DownloadString(updaterJsonURL);
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
                UpdateLocalFiles();
                UpdateRemoteFiles();
                UpdateMissingFiles();
                CheckForUpdates();
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

        private void Button1_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure you wish to do this?" + Environment.NewLine + Environment.NewLine + "This will overwrite any custom meshes you have created. If you have not created any custom meshes, or wish to overwrite your custom meshes, you may proceed.", "Confirmation: Are you Sure?", MessageBoxButtons.YesNo,MessageBoxIcon.Warning);
            if (dr == DialogResult.Yes)
            {
                button1.Enabled = false;

                totalCount = MissingFileStore.Count() + ToUpdateFileStore.Count();
                doneCount = 0;

                button1.Enabled = true;
                GetRemoteUpdateFile();
                UpdateLocalFiles();
                UpdateRemoteFiles();
                UpdateMissingFiles();
                CheckForUpdates();
                groupBox1.Refresh();
            }
            else if (dr == DialogResult.No)
            {
                ErrorMessage("Update Cancelled", "Update cancelled at user request.");
            }
        }

        // Exit Application
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();
        private void QuitToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();
        public static void ErrorMessage(string title, string error) => MessageBox.Show(error, title, MessageBoxButtons.OK, MessageBoxIcon.Error);


    }
}
