using RpanList.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RpanList
{
    class Download
    {
        public event EventHandler DownloadEnded;
        public Process ffProcess;
        public RpanData data;
        public TextBlock tabHeader = new TextBlock
        {

        };
        public TabItem tab = new TabItem
        {

        };
        public Grid tabGrid = new Grid
        {

        };
        public TextBox logBox = new TextBox
        {
            FontFamily = new FontFamily("Consolas")
        };

        public Download(RpanData data)
        {
            this.data = data;
            string title = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(data.post.title.ToLowerInvariant());

            //string args = "-o \"" + Properties.Settings.Default.downloadDir + title + ".mp4\" " + data.stream.hls_url;
            string headers = "\"Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.7 Accept - Language: en - us,en; q = 0.5 Accept - Encoding: gzip, deflate Accept: text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8 User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.122 Safari/537.36\"";
            string args = "-y -headers " + headers + " -i " + data.stream.hls_url + " -c copy -f mp4 -bsf:a aac_adtstoasc \"file:" + Environment.ExpandEnvironmentVariables(Properties.Settings.Default.downloadDir) + title + ".mp4\"";
            ffProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    FileName = Properties.Settings.Default.ytdlPath,
                    Arguments = args
                }
            };
            ffProcess.OutputDataReceived += FfProcess_DataReceived;
            ffProcess.ErrorDataReceived += FfProcess_DataReceived;
            ffProcess.Exited += FfProcess_Exited;
            string finalTitle;
            if (title.Length > 15)
            {
                finalTitle = string.Concat(title.Take(15)) + "...";
            }
            else
            {
                finalTitle = title;
            }
            tabHeader.Text = "Download \"" + finalTitle + "\"";

            tab.Header = new HeaderedContentControl { Content = tabHeader };
            tabGrid.Children.Add(logBox);
            tab.Content = tabGrid;
            ffProcess.Start();
            ffProcess.BeginOutputReadLine();
            ffProcess.BeginErrorReadLine();
        }

        private void FfProcess_Exited(object sender, EventArgs e)
        {
            DownloadEnded?.Invoke(this, EventArgs.Empty);
        }

        private void FfProcess_DataReceived(object sender, DataReceivedEventArgs e)
        {
            new Thread(() =>
            {
                logBox.Dispatcher.BeginInvoke((Action)(() => logBox.AppendText(e.Data + "\n")));
            }).Start();
        }

        public void EndDownload()
        {
            ffProcess.StandardInput.Write("q");
            //Process.Start("taskkill", "/pid " + ffProcess.Id);
        }

        //private static void KillProcessAndChildren(int pid)
        //{
        //    // Cannot close 'system idle process'.
        //    if (pid == 0)
        //    {
        //        return;
        //    }
        //    ManagementObjectSearcher searcher = new ManagementObjectSearcher
        //            ("Select * From Win32_Process Where ParentProcessID=" + pid);
        //    ManagementObjectCollection moc = searcher.Get();
        //    foreach (ManagementObject mo in moc)
        //    {
        //        KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
        //    }
        //    try
        //    {
        //        Process proc = Process.GetProcessById(pid);
        //        proc.StandardInput.Write("q");
        //    }
        //    catch (ArgumentException)
        //    {
        //        // Process already exited.
        //    }
        //}
    }
}
