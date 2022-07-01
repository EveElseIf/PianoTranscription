using PianoTranscription;
using PianoTranscription.Core;
using PianoTranscription.Core.Midi;
using System.CommandLine;
using System.Diagnostics;

var langTag = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
var lang = langTag == "zh" ? Langs.Zh : Langs.En;


var op1 = new Option<FileInfo>(
    new[] { "-i", "--input" }, "Specify the input file").ExistingOnly();
var op2 = new Option<DirectoryInfo>(
    new[] { "-d", "--dir" }, "Specify the directory which contains the files to be transcripted").ExistingOnly();
var arg1 = new Argument<string>(
    "output", "Specify the output path or directory");

var rootCommand = new RootCommand
{
    op1,op2, arg1
};
rootCommand.Description = "Transcript an audio file or files in a directory to midi file(s) for piano music.";

rootCommand.SetHandler<FileInfo, DirectoryInfo, string>(
    Handler,
    op1, op2, arg1);
return rootCommand.Invoke(args);

void Handler(FileInfo inFile, DirectoryInfo inDir, string output)
{
    Console.ResetColor();
    if (inDir is not null) // handle directory input
    {
        DirectoryInfo outDir;
        try
        {
            outDir = new DirectoryInfo(output);
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid argument: {output}");
            return;
        }
        if (!outDir.Exists)
            Directory.CreateDirectory(outDir.FullName);
        List<(string, string)> tasks = new();
        var files = inDir.GetFiles();
        foreach (var file in files)
        {
            if (!file.IsAudioFile()) continue;
            var outFile = new FileInfo(Path.Combine(outDir.FullName, file.NameWithoutExtension() + ".mid"));
            tasks.Add((file.FullName, outFile.FullName));
        }
        HandleAudios(tasks.ToArray());
    }
    else if (inFile is not null) // handle file input
    {
        if (!inFile.IsAudioFile())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"File extension \"{inFile.Extension}\" is not supported");
            return;
        }
        var outFile = new FileInfo(output);
        HandleAudios((inFile.FullName, outFile.FullName));
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("One of options 'input' and 'dir' must be set.\n");
        Console.ResetColor();
        rootCommand.Invoke("-h");
        return;
    }
}

static void HandleAudios(params (string, string)[] in_out)
{
    var wg = new Stopwatch();
    wg.Start();
    var reader = new AudioReader(AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
    Transcriptor t;
    try
    {
        string _localPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        t = new Transcriptor(File.ReadAllBytes(Path.Combine(_localPath, "transcription.onnx")));
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);
        Console.WriteLine("Create onnxruntime session failed, please check your environment.");
        return;
    }
    foreach (var i in in_out)
    {
        var w = new Stopwatch();
        w.Start();
        try
        {
            var pcmData = reader.DecodeAndResampleTo16000hzMonoPcm16le(i.Item1);
            var input = reader.ParseAndNormalizePcmData(pcmData);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Start transcripting {i.Item1} to {i.Item2}");
            Console.ResetColor();
            var outdict = t.Transcript(input);
            var midibytes = MidiWriter.WriteEventsToMidi(outdict);
            File.WriteAllBytes(i.Item2, midibytes);
        }
        catch (FormatException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Input file processing failed, check error information.");
            Console.ResetColor();
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }
        w.Stop();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Complete transcription,time usage: {w.Elapsed:g}");
        Console.ResetColor();
    }
    wg.Stop();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"OK, total time usage: {wg.Elapsed:g}");
    Console.ResetColor();
}

enum Langs
{
    Zh,
    En
}