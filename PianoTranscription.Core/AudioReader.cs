using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PianoTranscription.Core
{
    public unsafe class AudioReader
    {
        private readonly string _ffmpegPath = "ffmpeg";
        public AudioReader(string? rootDir = "", string? ffmpegPath = null)
        {
            if (ffmpegPath is null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var path = Path.Combine(rootDir, "ffmpeg.exe");
                    if (File.Exists(path))
                        _ffmpegPath = path;
                }
                else
                {
                    var path = Path.Combine(rootDir, "ffmpeg");
                    if (File.Exists(path))
                        _ffmpegPath = path;
                }
            }
            else
                _ffmpegPath = ffmpegPath;
        }

        public byte[] DecodeAndResampleTo16000hzMonoPcm16le(string path)
        {
            if (!File.Exists(path))
                throw new IOException();
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo(_ffmpegPath, $"-i pipe:0 -ac 1 -ar 16000 -f s16le pipe:1")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            using var os = p.StandardOutput.BaseStream;
            using var @is = p.StandardInput.BaseStream;

            var buf = new byte[1024];
            var ms = new MemoryStream();
            var bytes = File.ReadAllBytes(path);
            var task = Task.Run(() =>
            {
                int length;
                while ((length = os.Read(buf, 0, buf.Length)) > 0)
                {
                    ms.Write(buf, 0, length);
                }
            });

            @is.Write(bytes, 0, bytes.Length);
            @is.Close();
            task.Wait();
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new FormatException();
            var output = ms.ToArray();
            return output;
        }

        public float[] ParseAndNormalizePcmData(byte[] pcmData)
        {
            fixed (byte* input = pcmData)
            {
                var parsed = (short*)input;
                var output = new float[pcmData.Length / 2];
                // normalize
                for (var i = 0; i < pcmData.Length / 2; i++)
                {
                    output[i] = (float)parsed[i] / 32767;
                }

                return output;
            }
        }
    }
}