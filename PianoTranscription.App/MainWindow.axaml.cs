using Avalonia.Controls;
using Avalonia.Threading;
using PianoTranscription.Core;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PianoTranscription.App
{
    public partial class MainWindow : Window
    {
        private Transcriptor transcriptor = null;
        public MainWindow()
        {
            InitializeComponent();
            selectFileBtn.Click += SelectFileBtn_Click;
        }

        private async void SelectFileBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name = "audio",
                Extensions = new List<string>() { "mp3", "wav", "flac" }
            });
            var file = await dialog.ShowAsync(this);
            if (file.Length == 0) return;
            statusTextBlock.Text = "Running";
            await ProcessFile(file[0]);
            statusTextBlock.Text = "Idle";
        }
        private Task ProcessFile(string path)
        {
            return Task.Run(() =>
            {
                var reader = new AudioReader();
                var pcm = reader.DecodeAndResampleTo16000hzMonoPcm16le(path);
                var data = reader.ParseAndNormalizePcmData(pcm);
                if (transcriptor is null)
                {
                    transcriptor = new Transcriptor(File.ReadAllBytes("transcription.onnx"));
                }
                var dict = transcriptor.Transcript(data, (a, b) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        progressBar.Value = a;
                        progressBar.Maximum = b;
                    });
                });
            });
        }
    }
}
