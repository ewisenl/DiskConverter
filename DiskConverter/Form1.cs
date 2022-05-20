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
        public static bool kill = false;
        public static bool onlymovs = false;
        public static bool lowmovement = false;
        long total = 1;
        long nonmovtotal = 0;
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
            await foreach (string file in bigfiles) { total += new FileInfo(file).Length; 
                if (Path.GetExtension(file) == ".mp4") { mp4bytes+= new FileInfo(file).Length; }; 
                if (Path.GetExtension(file) != ".mov") { nonmovtotal += new FileInfo(file).Length; }; 
            };
       

            
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
            if (!onlymovs)
            {
                await roboCMD.Start();
            }

            progressBar1.Visible = true;
            progressBar2.Visible = true;
            progressBar3.Visible = true;
            progressBar4.Visible = true;
            progressBar5.Visible = true;
            etalabel.Visible = true;
            DateTime _starttime = DateTime.Now;
            donebytes = nonmovtotal;

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

                if (File.Exists(item.Replace(Path.GetFullPath(source), Path.GetFullPath(target))))
                {
                    donebytes = donebytes + mediaInfo.Size;
                    return;

                };

                IStream videoStream = (IStream)mediaInfo.VideoStreams.FirstOrDefault()?.SetCodec(VideoCodec.hevc);

                IStream audioStream = (IStream)mediaInfo.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.aac).SetChannels(2);

                IConversion conversion = convertMov(item, audioStream, videoStream);
                
                conversion.OnProgress += (sender, args) =>
                {
                    var percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
                    changeLabel(proglabel, percent.ToString() + "%");
                    changeProgress(progressbarnow, (int)percent,false,false);
                };







                IConversionResult conversionResult = await conversion.Start(cancelsource.Token);
                
               
                bag.Add(item);



                donebytes = donebytes + mediaInfo.Size;
                double totalprocent = ((double)donebytes / (double)total) * 100;
                double mp4totalprocent = ((double)donebytes-mp4bytes / (double)total- mp4bytes) * 100;
                //TimeSpan estimated = TimeSpan.FromSeconds((double)(100.00 / mp4totalprocent) * (double)conversionResult.EndTime.Subtract(conversionResult.StartTime).TotalSeconds);
                long bytespersecond = (long)mediaInfo.Size /(long)conversionResult.EndTime.Subtract(conversionResult.StartTime).TotalSeconds;
                TimeSpan estimated = TimeSpan.FromSeconds(((long)(total - donebytes - mp4bytes) / bytespersecond)/4);
                if (estimated.TotalSeconds > 0)
                {
                    changeLabel(etalabel, Math.Floor(estimated.TotalHours) + " uur, " + estimated.Minutes + " minuten...   Speed: "+Math.Round((double)(conversionResult.Duration.TotalSeconds/mediaInfo.Duration.TotalSeconds),2).ToString() + "×");
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

           // return FFmpeg.Conversions(vid, vid.Replace(Path.GetFullPath(source), Path.GetFullPath(target)));



            return FFmpeg.Conversions.New()
                
                .AddStream(audioStream, videoStream)
                .SetPriority(ProcessPriorityClass.Normal)
                .SetPixelFormat(PixelFormat.yuv420p)
                .AddParameter("-tune fastdecode")
                .SetPreset(ConversionPreset.VeryFast)
                .AddParameter("-g 10 -c:v h264 ")
                .AddParameter("-crf 21 -threads 5")
                .AddParameter(" -hwaccel_device 1 -hwaccel auto", ParameterPosition.PreInput)
                .SetOutputFormat(Format.mov)
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

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            onlymovs = !onlymovs;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            lowmovement = !lowmovement;
        }
    }
}