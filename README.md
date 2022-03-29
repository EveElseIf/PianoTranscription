# PianoTranscription
A CLI application allows you transcript audio of piano music to midi data.
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
Clone source code, download onnx file from Release/ONNX and put it on directory 'PianoTranscription.Core'. Change directory to 'PianoTranscription' and run
```
dotnet build
```
---
If you are runing on Mac OS, at the first time, please run
```
dotnet tool restore
dotnet pwsh ./build-osx-arm64.ps1
```
You can change to ./build-osx-x64.ps1 depending on your cpu's architecture.
# Warning
Please DO NOT build for other os platforms which you are not using, because msbuild sometimes will not copy the native libs in the right way!
