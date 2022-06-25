using Avalonia.Controls;
using Avalonia.Threading;
using PianoTranscription.Core;
using PianoTranscription.Core.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PianoTranscription.App
{
    public partial class MainWindow : Window
    {
        private Transcriptor transcriptor = null;
        private string selectedFile;
        private readonly string version;
        private readonly Strings strings;
        private bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource;
        public MainWindow()
        {
            InitializeComponent();
            strings = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? new ZhStrings() : new EnStrings();
            version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

            Title = strings.Title;
            selectFileBtn.Content = strings.SelectBtn;
            startBtn.Content = strings.StartBtn;
            statusTextBlock.Text = strings.Idle;
            stopBtn.Content = strings.Stop;
            fileNameTextBlock.Text = strings.NoFileSelected;
            logTitleTextBlock.Text = strings.Log;

            selectFileBtn.Click += SelectFileBtn_Click;
            startBtn.Click += StartBtn_Click;
            stopBtn.Click += StopBtn_Click;
            stopBtn.IsVisible = false;

            Log(strings.WelcomeInfoFormat.Format(version));
        }


        private async void StartBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (isRunning) return;
            if (string.IsNullOrEmpty(selectedFile)) return;

            var dialog = new SaveFileDialog();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name = "midi",
                Extensions = new List<string>() { "mid" }
            });
            var file = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(file)) return;

            statusTextBlock.Text = strings.Running;
            progressTextBlcok.IsVisible = true;
            stopBtn.IsVisible = true;
            isRunning = true;
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                await ProcessFile(selectedFile, file, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Log(strings.Canceled);
            }
            catch (Exception ex)
            {
                Log("Error!\n" + ex.ToString());
            }
            finally
            {
                statusTextBlock.Text = strings.Idle;
                cancellationTokenSource.Dispose();
                stopBtn.IsVisible = false;
                isRunning = false;
            }
        }

        private async void SelectFileBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (isRunning) return;
            var dialog = new OpenFileDialog();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name = strings.AudioFile,
                Extensions = new List<string>() { "mp3", "wav", "flac", "ape", "ogg" }
            });
            var file = await dialog.ShowAsync(this);
            if (file is null || file.Length == 0) return;
            selectedFile = file[0];
            fileNameTextBlock.Text = strings.SelectedFileFormat.Format(selectedFile);
            progressBar.Value = 0;
            progressBar.Maximum = 1;
            progressTextBlcok.IsVisible = false;
        }
        private void StopBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }
        private Task ProcessFile(string path, string outpath, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                Log(strings.StartFormat.Format(path));
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var reader = new AudioReader();
                var pcm = reader.DecodeAndResampleTo16000hzMonoPcm16le(path);
                var data = reader.ParseAndNormalizePcmData(pcm);
                if (transcriptor is null)
                {
                    string localpath = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                    transcriptor = new Transcriptor(File.ReadAllBytes(Path.Combine(localpath, "transcription.onnx")));
                    Log(strings.ORTInited);
                }
                var dict = transcriptor.Transcript(data, (a, b) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        progressBar.Value = a;
                        progressBar.Maximum = b;
                        progressTextBlcok.Text = $"{a}/{b}";
                    });
                }, token);
                var bytes = MidiWriter.WriteEventsToMidi(dict);
                Log(strings.SavingFileFormat.Format(outpath));
                await File.WriteAllBytesAsync(outpath, bytes);
                Log(strings.CompletedFormat.Format(stopwatch.Elapsed));
            });
        }
        private void Log(string content)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                logTextBox.Text += content + "\n\n";
            });
        }
    }
}
