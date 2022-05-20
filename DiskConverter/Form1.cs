using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using RoboSharp;
using Windows.Foundation;
using System.Threading;
using Xabe.FFmpeg;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using Dasync.Collections;


namespace DiskConverter
{


    public partial class windowcase : Form
    {
        string ffmpeg = "C:\\ProgramData\\chocolatey\\lib\\ffmpeg\\tools\\ffmpeg\\bin\\";
        string localffmpeg = Environment.GetEnvironmentVariable("ffmpegpath",EnvironmentVariableTarget.User) ?? "-";
        string source;
        string target;
        public bool kill = false;
        long total = 1;
        long donebytes = 0;
        public CancellationTokenSource cancelsource;
        

        
        List<string> movlist = new();
        public windowcase()
        {
            InitializeComponent();
            this.Text = "EWISE Prores - h.264 converter";
            cancelsource = new CancellationTokenSource();
            if (localffmpeg != "-" && localffmpeg != null && localffmpeg != "") {
                FFmpeg.SetExecutablesPath(localffmpeg);
                if (!File.Exists(Path.Join(localffmpeg, "ffmpeg.exe")))
                {
                    MessageBox.Show("No ffmpeg found, re-install or set\ncorrect path with ffmpeg button before continuing.");
                };
            } else { 


            //FFmpegDownloader.GetLatestVersion(FFmpegVersion ffmpegVersion)
            FFmpeg.SetExecutablesPath("C:\\ProgramData\\chocolatey\\lib\\ffmpeg\\tools\\ffmpeg\\bin");
            };

        }

        
        private bool changeLabel(Label requestedLabel, string text)
        {
            if (requestedLabel.InvokeRequired)
            {
                try { requestedLabel.BeginInvoke((MethodInvoker)delegate () { requestedLabel.Text = text; requestedLabel.Visible = true; }); } catch { };
            }
            else
            {
                requestedLabel.Text = text;
            }
            return true;
        }

        private void changeProgress(ProgressBar requestedBar, int value, bool visswitch, bool showhide)
        {
            
            if (requestedBar.InvokeRequired)
            {
                requestedBar.BeginInvoke((MethodInvoker)delegate () { if (visswitch) { requestedBar.Visible = showhide; } else {
                        if (value > 100) { requestedBar.Value = 100; } else if (value < 0) { requestedBar.Value = 0; } else requestedBar.Value = value; }; });
            }
            else
            {
                requestedBar.Value = value;
            }
        }


        void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);
            changeLabel(currentFil, diSource.FullName);
            CopyAll(diSource, diTarget);
        }

        void CopyAll(DirectoryInfo dirsource, DirectoryInfo dirtarget)
        {
            Directory.CreateDirectory(dirtarget.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in dirsource.GetFiles())
            {
                changeLabel(currentFil, fi.Name);



                if (fi.Extension.Length != 4) { continue; };
                    if (fi.Extension == ".MOV") { continue; };
                    if (fi.Extension == ".mov") { continue; };
                    if (fi.Extension == ".MXF") { continue; };
                    if (fi.Extension == ".pek") { continue; };
                    if (fi.Extension == ".PEK") { continue; };
                    if (fi.Extension == ".bin") { continue; };
                    if (fi.Extension == ".BIN") { continue; };
                    if (fi.FullName.IndexOf("$RECYCLE.BIN") > -1) { continue; };
                if (fi.FullName.IndexOf(" Volume ") > -1) { continue; };
                if (File.Exists(fi.FullName.Replace(source,target))) { continue; };
                
                




               


                fi.CopyTo(Path.Combine(dirtarget.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in dirsource.GetDirectories())
            {
                if (diSourceSubDir.Name.IndexOf("RECYCLE") > -1) { continue; };
                if (diSourceSubDir.Name.IndexOf("FOUND.") > -1) { continue; };
                if (diSourceSubDir.Name.IndexOf("Volume ") > -1) { continue; };
                changeLabel(currentFil, diSourceSubDir.Name);
                
                DirectoryInfo nextTargetSubDir =
                    dirtarget.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
        private void imputbutton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))

                {
                    source = Path.GetFullPath(fbd.SelectedPath).TrimEnd(Path.DirectorySeparatorChar);
                    source = source +Path.DirectorySeparatorChar;
                    inputlabel.Text = source;
                    if (target != null) { convertbutton.Visible = true; };
                }
            }
        }

        private void targetbutton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))

                {
                    target = Path.GetFullPath(fbd.SelectedPath).TrimEnd(Path.DirectorySeparatorChar);
                    target = target + Path.DirectorySeparatorChar;
                    targetlabel.Text = target;
                    if (source != null) { convertbutton.Visible = true; };
                }
            }
        }


        private void copyprogresHandler(object sender, CopyProgressEventArgs args) { 
            changeLabel(currentproglabel, Math.Round(args.CurrentFileProgress,0).ToString()+"%");  
        }

        private void currentCopyHanler(object sender, FileProcessedEventArgs args)
        {
            changeLabel(currentFil, args.ProcessedFile.Name); 
            donebytes += args.ProcessedFile.Size;

            double totalprocent = ((double)donebytes / (double)total) * 100;
            changeLabel(totalproglabel, totalprocent.ToString("0.00", new System.Globalization.CultureInfo("en-US", false)) + "%");
            changeProgress(totalBar, (int)totalprocent,false,false);


        }

        private void copyFileFinishHandler(object sender, RoboCommandCompletedEventArgs args)
        {
            
            changeLabel(currentFil, "Converting MOV files...");
        }


        private async void convertbutton_Click(object sender, EventArgs e)
        {
            etawordlabel.Visible = true;
            
            convertbutton.Visible = false;
            targetlabel.Visible = false;
            totaalwordlabel.Visible = true;
            inputbutton.Visible = false;
            targetbutton.Visible = false;
            currentFil.Visible = true;
            bool[] bezet = new bool[5] { false, false, false, false, false };
            changeLabel(currentFil, "Copying folders and small files...");
            long mp4bytes = 0;
            
            
            totalBar.Visible = true;
            totalproglabel.Visible = true;
        
            Thread.Sleep(300);
            List<string> bigfiles = new List<string>(Directory.GetFiles(source, "*.*",SearchOption.AllDirectories).SkipWhile(name=> 
                Path.GetExtension(name)==".pek" ||
                Path.GetExtension(name) == ".BIN" ||
                Path.GetExtension(name) == ".pek" 
                ).ToArray());
            Thread.Sleep(300);
            await foreach (string file in bigfiles) { total += new FileInfo(file).Length; if (Path.GetExtension(file) == ".mp4") { mp4bytes+= new FileInfo(file).Length; }; };
       

            
            changeLabel(currentFil,"Copying non-video files...");
           

            CopyOptions copt = new();
            copt.Source = source;
            copt.Destination = target;
            
            RoboCommand roboCMD  = new();
            roboCMD.CopyOptions = copt;
            roboCMD.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            roboCMD.SelectionOptions.ExcludeFiles = "*.mov *.pek *.BIN $.*";
            roboCMD.SelectionOptions.ExcludeAttributes = "h";
            roboCMD.OnFileProcessed += currentCopyHanler;
            roboCMD.OnCopyProgressChanged += copyprogresHandler;
            roboCMD.OnCommandCompleted += copyFileFinishHandler;
            await roboCMD.Start();

            progressBar1.Visible = true;
            progressBar2.Visible = true;
            progressBar3.Visible = true;
            progressBar4.Visible = true;
            progressBar5.Visible = true;
            DateTime _starttime = DateTime.Now;


            changeLabel(currentFil,"Converting Prores to H.264");
            changeLabel(currentproglabel," ");

            List<string> movque = new List<string>(Directory.GetFiles(source, "*.mov", SearchOption.AllDirectories).Where(name => !name.Contains("_Pling", StringComparison.OrdinalIgnoreCase) && !name.Contains("Pool", StringComparison.OrdinalIgnoreCase) && !name.Contains(" kort", StringComparison.OrdinalIgnoreCase)).ToArray());
            
            
            int tremolo = 0;
            var bag = new System.Collections.Concurrent.ConcurrentBag<string>(movque);
             await bag.ParallelForEachAsync (async item =>
            {
                Thread.Sleep(100);


                int mytrem = tremolo % 5;
                
                
                tremolo++;
                for (var b = 0; b < 5; b++) { if (bezet[b] == false) { bezet[b] = true; mytrem = b; break; }; };

                Label filelabel = file1label;
                Label proglabel = proglabel1;

                ProgressBar progressbarnow = progressBar1;
                switch (mytrem) {

                    case 0: filelabel = file1label; progressbarnow = progressBar1; proglabel = proglabel1; break;
                    case 1: filelabel = file2label; progressbarnow = progressBar2; proglabel = proglabel2; break;
                    case 2: filelabel = file3label; progressbarnow = progressBar3; proglabel = proglabel3; break;
                    case 3: filelabel = file4label; progressbarnow = progressBar4; proglabel = proglabel4; break;
                    case 4: filelabel = file5label; progressbarnow = progressBar5; proglabel = proglabel5; break;
                };
                
                changeLabel(filelabel, item);
                changeProgress(progressbarnow,0, true, true);
                
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(item);

                    IStream videoStream = (IStream)mediaInfo.VideoStreams.FirstOrDefault()?.SetCodec(VideoCodec.h264);

                IStream audioStream = (IStream)mediaInfo.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.aac);

                IConversion conversion =  convertMov(item, audioStream, videoStream);
                
                conversion.OnProgress += (sender, args) =>
                {
                    var percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
                    changeLabel(proglabel, percent.ToString() + "%");
                    changeProgress(progressbarnow, (int)percent,false,false);
                };







                IConversionResult conversionResult = await conversion.Start(cancelsource.Token);
                
               
                bag.Add(item);



                donebytes = donebytes + new FileInfo(item).Length;
                double totalprocent = ((double)donebytes / (double)total) * 100;
                double mp4totalprocent = ((double)donebytes-mp4bytes / (double)total- mp4bytes) * 100;
                TimeSpan estimated = TimeSpan.FromSeconds((double)(100.00 / mp4totalprocent) * (double)conversionResult.EndTime.Subtract(_starttime).TotalSeconds);
                if (estimated.TotalSeconds > 0)
                {
                    changeLabel(etalabel, Math.Floor(estimated.TotalHours) + " uur, " + estimated.Minutes + " minuten...");
                };
                changeLabel(totalproglabel, totalprocent.ToString("0.00", new System.Globalization.CultureInfo("en-US", false)) + "%");
                changeProgress(totalBar, (int)totalprocent, false, false);
           
            bezet[mytrem] = false;
                changeProgress(progressbarnow, 0, true, false);
            }, maxDegreeOfParallelism: 5);
            var count = bag.Count;

            donebutton.Visible = true;
            
            changeProgress(totalBar, 100,false,false);
            changeLabel(currentFil, "Klaar.");
            MessageBox.Show("Schijf converteren is klaar");

        }

        


        IConversion convertMov(string vid, IStream audioStream, IStream videoStream) {


            Thread.Sleep(30);

            return FFmpeg.Conversions.New()
                .AddStream(audioStream, videoStream)
                .SetPriority(ProcessPriorityClass.BelowNormal)
                .SetPixelFormat(PixelFormat.yuv420p)
                .AddParameter("-tune fastdecode")
                .AddParameter("-g 15")
                .AddParameter("-crf 12")
                .SetOutputFormat(Format.mov)
                .UseMultiThread(8)
                .SetPreset(ConversionPreset.SuperFast)
                .SetOutput(vid.Replace(Path.GetFullPath(source), Path.GetFullPath(target)));



        }

       

        
        private void donebutton_Click(object sender, EventArgs e)
        {

            cancelsource.Cancel();
            if (kill) {
                
                Process.Start("cmd.exe", "/C TASKKILL /IM ffmpeg.exe /F");
                
            };
            Application.Exit();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))

                {
                    string newffmpegpath = Path.GetFullPath(fbd.SelectedPath).TrimEnd(Path.DirectorySeparatorChar);
                    FFmpeg.SetExecutablesPath(newffmpegpath);
                    try { Environment.SetEnvironmentVariable("ffmpegpath", newffmpegpath, EnvironmentVariableTarget.User); } catch { };
                    if (!File.Exists(Path.Join(newffmpegpath, "ffmpeg.exe")))
                    {
                        MessageBox.Show("No ffmpeg found, re-install or set\ncorrect path with ffmpeg button before continuing.");
                    }
                    else { MessageBox.Show("ffMpeg OK!"); };
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            kill = !kill;
        }
    }
}