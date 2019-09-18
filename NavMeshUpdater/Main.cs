using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Net.NetworkInformation;

using System.Diagnostics;

using Json;

namespace NavMeshUpdater
{
    public partial class Main : Form
    {
        private readonly bool DEBUG = false;
        public static readonly string updaterJsonURL = "https://rootswitch.com/mirror/MQ2/MQ2Nav/updater.json";
        private bool pInit, downloadComplete, listsCleared;
        private int CurrentDownloadPct { get; set; } = 0;
        private int OverallDownloadPct { get; set; } = 0;
        private int localCount, remoteCount, missingCount, updateCount, totalCount, doneCount, counter;
        private string RemoteFile, CurrentFile;
        private Dictionary<string, string> LocalFileStore = new Dictionary<string, string>();
        private Dictionary<string, string> RemoteFileStore = new Dictionary<string, string>();
        private Dictionary<string, string> MissingFileStore = new Dictionary<string, string>();
        private Dictionary<string, string> ToUpdateFileStore = new Dictionary<string, string>();
        private static readonly string currentDirectory = Path.GetDirectoryName(Application.ExecutablePath);
        private readonly string meshDirectory = currentDirectory + "\\MQ2Nav";

        private void UpdateUI()
        {
            label1.Text = "Local Files: " + localCount.ToString();
            progressBar1.Value = CurrentDownloadPct;
            label3.Text = CurrentDownloadPct.ToString() + "%";
            progressBar2.Value = OverallDownloadPct;
            label4.Text = OverallDownloadPct.ToString() + "%";
            label5.Text = "Missing Files: " + missingCount.ToString();
            label6.Text = "Updates Needed: " + updateCount.ToString();
            groupBox2.Text = "Current File: " + CurrentFile;

            if (ActiveForm != null && ActiveForm.Visible)
            {
                ActiveForm.Refresh();
            }
        }

        private void UpdateOverAllPct(int done, int total)
        {
            int count = ((done * 100) / total) + 1;
            OverallDownloadPct = count;
            UpdateUI();
        }
        private void UpdateLocalFiles()
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
                    Console.WriteLine(file.Name.Replace(".navmesh", "") + "," + CalculateMD5(file.FullName) + "|" + file.FullName);
                }
                LocalFileStore.Add(file.Name.Replace(".navmesh", ""), CalculateMD5(file.FullName) + "|" + file.FullName);
            }
            localCount = (localFiles.Length > 0) ? localFiles.Length : 0;
            label1.Text = "Local Files: " + localCount.ToString();
        }

        private void UpdateRemoteFiles()
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

        private int GetExpansionIndex(string expak)
        {
            int index;
            switch(expak)
            {
                case "classic":
                    index = 0;
                    break;
                case "kunark":
                    index = 1;
                    break;
                case "sov":
                    index = 2;
                    break;
                case "sol":
                    index = 3;
                    break;
                case "pop":
                    index = 4;
                    break;
                case "loy":
                    index = 5;
                    break;
                case "ldon":
                    index = 6;
                    break;
                case "god":
                    index = 7;
                    break;
                case "oow":
                    index = 8;
                    break;
                case "don":
                    index = 9;
                    break;
                case "dodh":
                    index = 10;
                    break;
                case "por":
                    index = 11;
                    break;
                case "tss":
                    index = 12;
                    break;
                case "tbs":
                    index = 13;
                    break;
                case "sof":
                    index = 14;
                    break;
                case "sod":
                    index = 15;
                    break;
                case "uf":
                    index = 16;
                    break;
                case "hot":
                    index = 17;
                    break;
                case "voa":
                    index = 18;
                    break;
                case "rof":
                    index = 19;
                    break;
                case "cotf":
                    index = 20;
                    break;
                case "tds":
                    index = 21;
                    break;
                case "tbm":
                    index = 22;
                    break;
                case "eok":
                    index = 23;
                    break;
                case "ros":
                    index = 24;
                    break;
                case "tbl":
                    index = 25;
                    break;
                case "other":
                    index = 26;
                    break;
                default:
                    index = 26;
                    break;
            }
            return index;
        }

        private void UpdateMissingFiles()
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

        private void CheckForUpdates()
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
                        ToUpdateFileStore.Add(wrd[0], z.Value.Files.Mesh.Link.ToString());
                    }
                }
            }
            updateCount = (ToUpdateFileStore.Count() > 0) ? ToUpdateFileStore.Count() : 0;
            label6.Text = "Updates Needed: " + updateCount.ToString();
        }

        private void GetRemoteUpdateFile()
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
            InitializeComponent();
            if (!pInit)
            {
                SplashScreen.ShowSplashScreen();
                if (Properties.Settings.Default.updateMissing)
                {
                    checkBox1.Checked = true;
                }
                else
                {
                    checkBox1.Checked = false;
                }
                if (Properties.Settings.Default.updateUpdates)
                {
                    checkBox2.Checked = true;
                }
                else
                {
                    checkBox2.Checked = false;
                }
                GetRemoteUpdateFile();
                UpdateLocalFiles();
                UpdateRemoteFiles();
                UpdateMissingFiles();
                CheckForUpdates();
                Thread.Sleep(2000);
                SplashScreen.CloseForm();
                pInit = true;
            }
            Point WinLoc = new Point
            {
                X = Properties.Settings.Default.LocX,
                Y = Properties.Settings.Default.LocY
            };
            this.Location = WinLoc;
            textBox1.Text = "Idle";
            UpdateUI();
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void TabPage2_GotFocus(Object sender, EventArgs e)
        {
            if (!listsCleared)
            {
                MissingFileStore.Clear();
                ToUpdateFileStore.Clear();
                missingCount = 0;
                updateCount = 0;
                UpdateUI();
                listsCleared = true;
            }
        }

        // Open an email for Bug Report
        private void BugReportToolStripMenuItem_Click(object sender, EventArgs e) => OpenURL("mailto:wired420@gmail.com?subject=BugReport&body=I%20Think%20I%20Found%20This%20Bug");
        // Open an email for Feature Request
        private void FeatureRequestToolStripMenuItem1_Click(object sender, EventArgs e) => OpenURL("mailto:wired420@gmail.com?subject=FeatureRequest&body=I%20Want%20This%20Feature");

        private void Button1_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Are you sure you wish to do this?" + Environment.NewLine + Environment.NewLine + "This will overwrite any custom meshes you have created. If you have not created any custom meshes, or wish to overwrite your custom meshes, you may proceed.", "Confirmation: Are you Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr == DialogResult.Yes)
            {
                button1.Enabled = false;
                int mCount = MissingFileStore.Count();
                int uCount = ToUpdateFileStore.Count();
                if (checkBox1.Checked)
                {
                    totalCount += mCount;
                }
                if (checkBox2.Checked)
                {
                    totalCount += uCount;
                }

                if (checkBox1.Checked)
                {
                    textBox1.Text = "Downloading Missing Files";
                    UpdateUI();
                    foreach (KeyValuePair<String, String> md in MissingFileStore)
                    {
                        if (!this.IsDisposed)
                        {
                            string fn = md.Key.ToString();
                            string fl = md.Value.ToString();
                            var success = DownloadFile(fl, meshDirectory + "\\" + fn + ".navmesh", fn + ".navmesh");
                            if (DEBUG)
                            {
                                Console.WriteLine("Download Complete - " + success);
                            }
                            doneCount++;
                            missingCount--;
                            localCount++;
                        }
                    }
                }

                if (checkBox2.Checked)
                {
                    textBox1.Text = "Downloading Updates";
                    UpdateUI();
                    foreach (KeyValuePair<String, String> tu in ToUpdateFileStore)
                    {
                        if (!this.IsDisposed)
                        {
                            string fn = tu.Key.ToString();
                            string fl = tu.Value.ToString();
                            var success = DownloadFile(fl, meshDirectory + "\\" + fn + ".navmesh", fn + ".navmesh");
                            if (DEBUG)
                            {
                                Console.WriteLine("Download Complete - " + success);
                            }
                            doneCount++;
                            updateCount--;
                        }
                    }
                }

                textBox1.Text = "Updating Local Database";
                UpdateUI();
                button1.Enabled = true;
                GetRemoteUpdateFile();
                LocalFileStore.Clear();
                UpdateLocalFiles();
                RemoteFileStore.Clear();
                UpdateRemoteFiles();
                if (checkBox1.Checked)
                {
                    MissingFileStore.Clear();
                    UpdateMissingFiles();
                }
                if (checkBox2.Checked)
                {
                    ToUpdateFileStore.Clear();
                    CheckForUpdates();
                }
                OverallDownloadPct = 0;
                textBox1.Text = "Idle";
                UpdateUI();
            }
            else if (dr == DialogResult.No)
            {
                ErrorMessage("Update Cancelled", "Update cancelled at user request.");
            }
        }

        // Exit Application
        private void ExitToolStripMenuItem1_Click(object sender, EventArgs e) => Application.Exit();


        private void MainForm_LocationChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.LocX = Location.X;
            Properties.Settings.Default.LocY = Location.Y;
            Properties.Settings.Default.Save();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.updateMissing = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.updateUpdates = checkBox2.Checked;
            Properties.Settings.Default.Save();
        }

        private static void ErrorMessage(string title, string error) => MessageBox.Show(error, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        private bool DownloadFile(string url, string fullPathWhereToSave, string filename)
        {
            try
            {
                CurrentFile = filename;
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fullPathWhereToSave));

                if (File.Exists(fullPathWhereToSave))
                {
                    File.Delete(fullPathWhereToSave);
                }
                using (WebClient client = new WebClient())
                {
                    var ur = new Uri(url);
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(WebClientDownloadProgressChanged);
                    client.DownloadFileCompleted += WebClientDownloadCompleted;
                    client.DownloadFileAsync(ur, fullPathWhereToSave);
                    while (!downloadComplete)
                    {
                        Application.DoEvents();
                    }
                    downloadComplete = false;
                    return File.Exists(fullPathWhereToSave);
                }
            }
            catch (Exception e)
            {
                ErrorMessage("Download Error", "Unable to complete file download. Please try again or use the menu Help -> Bug Report." + Environment.NewLine + "Error: " + e);
                CurrentDownloadPct = 0;
                return false;
            }
        }

        private void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            counter++;
            if (counter % 333 == 0)
            {
                CurrentDownloadPct = e.ProgressPercentage;
                UpdateUI();
            }
        }

        private void WebClientDownloadCompleted(object sender, AsyncCompletedEventArgs args)
        {
            CurrentFile = "none";
            downloadComplete = true;
            CurrentDownloadPct = 0;
            UpdateOverAllPct(doneCount, totalCount);
            UpdateUI();
        }
        // For Generating File Hashes
        private string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        // For opening a web browser.
        public static void OpenURL(string url) => Process.Start(url);

    }
}
