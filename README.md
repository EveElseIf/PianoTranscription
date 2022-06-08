# PianoTranscription
A CLI application allows you to transcript audio of piano music to midi data.

Now there's also a simple GUI application to make it more user friendly.
# Installation
Download archieve from release page depending on your os, then simply extract all files.
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
Clone source code, download onnx file from Release/ONNX and put it into directory 'PianoTranscription.Core'. Or you can just run the commands below to download the ONNX file automatically. Please notice that without the ONNX file, you cannot build the solution.

For first time, please run
```
dotnet tool restore
dotnet pwsh ./build-xxx-xxx.ps1
```
After this, you can just run
```
dotnet build
```
If you want to build a dist, please run
```
dotnet pwsh ./build-xxx-xxx.ps1 dist
```
