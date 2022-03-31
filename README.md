# PianoTranscription
A CLI application allows you to transcript audio of piano music to midi data.
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
Clone source code, download onnx file from Release/ONNX and put it on directory 'PianoTranscription.Core'. 
For first time, please run
```
dotnet tool restore
dotnet pwsh ./build-xxx-xxx.ps1
```
After this, you can just run
```
dotnet build
```
