using Avalonia.Controls;
using Avalonia.Threading;
using PianoTranscription.Core;
using PianoTranscription.Core.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PianoTranscription.App
{
    public partial class MainWindow : Window
    {
        private Transcriptor transcriptor = null;
        private string selectedFile;
        public MainWindow()
        {
            InitializeComponent();
            selectFileBtn.Click += SelectFileBtn_Click;
            startBtn.Click += StartBtn_Click;
            Log("Welcome, for more information, please visit https://github.com/EveElseIf/PianoTranscription");
        }

        private async void StartBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFile)) return;

            var dialog = new SaveFileDialog();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name = "midi",
                Extensions = new List<string>() { "mid" }
            });
            var file = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(file)) return;

            statusTextBlock.Text = "Status: Running";
            progressTextBlcok.IsVisible = true;
            try
            {
                await ProcessFile(selectedFile, file);
            }
            catch (Exception ex)
            {
                Log("Error!\n" + ex.ToString());
            }
            finally
            {
                statusTextBlock.Text = "Status: Idle";
            }
        }

        private async void SelectFileBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name = "audio",
                Extensions = new List<string>() { "mp3", "wav", "flac", "ape", "ogg" }
            });
            var file = await dialog.ShowAsync(this);
            if (file.Length == 0) return;
            selectedFile = file[0];
            fileNameTextBlock.Text = string.Format("Selected file: {0}", selectedFile);
            progressBar.Value = 0;
            progressBar.Maximum = 1;
            progressTextBlcok.IsVisible = false;
        }
        private Task ProcessFile(string path,string outpath)
        {
            return Task.Run(async () =>
            {
                Log(string.Format("Start, file name: {0}.", path));
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var reader = new AudioReader();
                var pcm = reader.DecodeAndResampleTo16000hzMonoPcm16le(path);
                var data = reader.ParseAndNormalizePcmData(pcm);
                if (transcriptor is null)
                {
                    string localpath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    transcriptor = new Transcriptor(File.ReadAllBytes(Path.Combine(localpath, "transcription.onnx")));
                    Log("Onnxruntime initialized.");
                }
                var dict = transcriptor.Transcript(data, (a, b) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        progressBar.Value = a;
                        progressBar.Maximum = b;
                        progressTextBlcok.Text = $"{a}/{b}";
                    });
                });
                var bytes = MidiWriter.WriteEventsToMidi(dict);
                Log(string.Format("Saving file to: {0}", outpath));
                await File.WriteAllBytesAsync(outpath, bytes);
                Log(string.Format("Transcription completed, total time usage: {0:g}", stopwatch.Elapsed));
            });
        }
        private void Log(string content)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                OutputTextBox.Text += content + "\n\n";
            });
        }
    }
}
