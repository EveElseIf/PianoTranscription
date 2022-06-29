# PianoTranscription
A CLI application allows you to transcript audio of piano music to midi data.

Now there's also a simple GUI application to make it more user friendly.
# Installation
Download archieve from release page depending on your os, and simply extract all files.
# Usage
Get help.
```
./PianoTranscription -h
```
Transcript a file.
```
./PianoTranscription -i xxx.mp3 xxx.mid
```
Transcript files in a directory.
```
./PianoTranscription -d inputdir outputdir
```
# How To Build
Clone source code, run the build script below for the first time. The script will help you download the ONNX file and copy the FFmpeg executable to the build output directory.

Please notice that without the ONNX file, you cannot build the solution.

For first time, please run
```
dotnet tool restore
dotnet pwsh ./build-xxx-xxx.ps1
```
After this, you can just run
```
dotnet build
```
Run an application, simply use one of two
```
dotnet run --project PianoTranscription
dotnet run --project PianoTranscription.App
```
If you want to build a dist, please run
```
dotnet pwsh ./build-xxx-xxx.ps1 dist
```

## Warning
Please just build osx-arm64 version on an osx-arm64 host. If you build it on other hosts, you may face a crash problem.