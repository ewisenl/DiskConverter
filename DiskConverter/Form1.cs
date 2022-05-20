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
using RoboSharp;

namespace DiskConverter
{


    public partial class windowcase : Form
    {
        string ffmpeg = "C:\\ProgramData\\chocolatey\\lib\\ffmpeg\\tools\\ffmpeg\\bin\\ffmpeg.exe";
        string source;
        string target;
        long total = 1;
        long donebytes = 0;
        CancellationTokenSource cancelsource = new();
        
        
        List<string> movlist = new();
        public windowcase()
        {
            InitializeComponent();

            

           
            //FFmpegDownloader.GetLatestVersion(FFmpegVersion ffmpegVersion)
            FFmpeg.SetExecutablesPath("C:\\ProgramData\\chocolatey\\lib\\ffmpeg\\tools\\ffmpeg\\bin");


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

        private void changeProgress(ProgressBar requestedBar, int value)
        {
            if (value > 100) { value = 100; };
            if (value < 0) { value = 0; };
            if (requestedBar.InvokeRequired)
            {
                requestedBar.BeginInvoke((MethodInvoker)delegate () { requestedBar.Value = value; ; });
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
            changeLabel(currentproglabel, args.CurrentFileProgress.ToString()+"%");  
        }

        private void currentCopyHanler(object sender, FileProcessedEventArgs args)
        {
            changeLabel(currentFil, args.ProcessedFile.Name); 
            donebytes += args.ProcessedFile.Size;

            double totalprocent = ((double)donebytes / (double)total) * 100;
            changeLabel(totalproglabel, totalprocent.ToString("0.00", new System.Globalization.CultureInfo("en-US", false)) + "%");
            changeProgress(totalBar, (int)totalprocent);


        }

        private void copyFileFinishHandler(object sender, RoboCommandCompletedEventArgs args)
        {
            donebytes = args.Results.BytesStatistic.Total;
            changeLabel(currentFil, "Converting MOV files...");
        }


        private async void convertbutton_Click(object sender, EventArgs e)
        {
            convertbutton.Visible = false;
            inputbutton.Visible = false;
            targetbutton.Visible = false;
            currentFil.Visible = true;
            bool[] bezet = new bool[5] { false, false, false, false, false };
            changeLabel(currentFil, "Copying folders and small files...");


            progressBar1.Visible = true;
            progressBar2.Visible = true;
            progressBar3.Visible = true;
            progressBar4.Visible = true;
            progressBar5.Visible = true;
            totalBar.Visible = true;
            totalproglabel.Visible = true;
        
            Thread.Sleep(300);
            List<string> bigfiles = new List<string>(Directory.GetFiles(source, "*.*",SearchOption.AllDirectories).SkipWhile(name=> 
                Path.GetExtension(name)==".pek" ||
                Path.GetExtension(name) == ".BIN" ||
                Path.GetExtension(name) == ".pek" ||
                Path.GetExtension(name) == ""
                ).ToArray());
            Thread.Sleep(300);
            await foreach (string file in bigfiles) { total += new FileInfo(file).Length; };
       

            
            changeLabel(currentFil,"Copying non-video files...");
           

            CopyOptions copt = new();
            copt.Source = source;
            copt.Destination = target;
            
            RoboCommand roboCMD  = new();
            roboCMD.CopyOptions = copt;
            roboCMD.CopyOptions.CopySubdirectoriesIncludingEmpty = true;
            roboCMD.SelectionOptions.ExcludeFiles = "*.mov *.pek *.BIN $*.* .*.*";
            roboCMD.OnFileProcessed += currentCopyHanler;
            roboCMD.OnCopyProgressChanged += copyprogresHandler;
            roboCMD.OnCommandCompleted += copyFileFinishHandler;
            await roboCMD.Start();


            


            changeLabel(currentFil,"Done copying non-video files...");

            List<string> movque = new List<string>(Directory.GetFiles(source, "*.mov", SearchOption.AllDirectories).Where(name => !name.Contains("_Pling", StringComparison.OrdinalIgnoreCase) && !name.Contains("Pool", StringComparison.OrdinalIgnoreCase) && !name.Contains(" kort", StringComparison.OrdinalIgnoreCase)).ToArray());
            
            
            int tremolo = 0;
            var bag = new System.Collections.Concurrent.ConcurrentBag<string>(movque);
             await bag.ParallelForEachAsync (async item =>
            {
                changeLabel(currentFil, Path.GetDirectoryName(item));


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
                
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(item);

                    IStream videoStream = (IStream)mediaInfo.VideoStreams.FirstOrDefault()
                        ?.SetCodec(VideoCodec.h264);
                    IStream audioStream = (IStream)mediaInfo.AudioStreams.FirstOrDefault()
                        ?.SetCodec(AudioCodec.aac);
                IConversion conversion = convertMov(item, audioStream, videoStream);

                conversion.OnProgress += (sender, args) =>
                {
                    var percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
                    changeLabel(proglabel, percent.ToString() + "%");
                    changeProgress(progressbarnow, (int)percent);
                };



                try
                {
                    IConversionResult conversionResult = await conversion.Start(cancelsource.Token);

                    bag.Add(conversionResult.ToString());
                }
                catch { };

                donebytes = donebytes + mediaInfo.Size;
                double totalprocent = ((double)donebytes / (double)total)*100;
                
                changeLabel(totalproglabel, totalprocent.ToString("0.00", new System.Globalization.CultureInfo("en-US", false)) + "%");
                changeProgress(totalBar, (int)totalprocent);
                bezet[mytrem] = false;
            }, maxDegreeOfParallelism: 5);
            var count = bag.Count;

            donebutton.Visible = true;
            
            changeProgress(totalBar, 100);
            changeLabel(currentFil, "Klaar.");
            MessageBox.Show("Schijf converteren is klaar");

        }

        


        IConversion convertMov(string vid, IStream audioStream, IStream videoStream) {




            return FFmpeg.Conversions.New()
                .AddStream(audioStream, videoStream)
                .SetPriority(ProcessPriorityClass.BelowNormal)
                .SetPixelFormat(PixelFormat.yuv420p)
                .SetOutputFormat(Format.mov)
                .UseMultiThread(8)
                .AddParameter("-bufsize 1000M")
                .AddParameter(" -rtbufsize 1000M",ParameterPosition.PreInput)
                .SetPreset(ConversionPreset.SuperFast)
                .SetOutput(vid.Replace(Path.GetFullPath(source), Path.GetFullPath(target)));



        }

       

        
        private void donebutton_Click(object sender, EventArgs e)
        {

            cancelsource.Cancel();
            Application.Exit();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void windowcase_Load(object sender, EventArgs e)
        {

        }
    }
}