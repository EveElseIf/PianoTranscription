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
# Warning
Please DO NOT build for other os platforms which you are not using, because msbuild sometimes will not copy the native libs in the right way!