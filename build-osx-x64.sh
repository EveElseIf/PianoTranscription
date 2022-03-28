if [ ! -f "./PianoTranscription.Core/transcription.onnx" ]
    then
        wget -O "./PianoTranscription.Core/transcription.onnx" "https://github.com/EveElseIf/PianoTranscription/releases/download/ONNX/transcription.onnx"
fi
dotnet publish PianoTranscription -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
publish_path="PianoTranscription/bin/Release/net6.0/osx-x64/publish/"
rm $publish_path"Melanchall_DryWetMidi_Native32.dll"
rm $publish_path"Melanchall_DryWetMidi_Native64.dll"
rm $publish_path"Melanchall_DryWetMidi_Native64.dylib"
rm $publish_path"PianoTranscription.Core.pdb"
rm $publish_path"PianoTranscription.pdb"