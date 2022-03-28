if (-not ([System.IO.File]::Exists("PianoTranscription.Core/transcription.onnx"))) {
    Invoke-WebRequest -Uri "https://github.com/EveElseIf/PianoTranscription/releases/download/ONNX/transcription.onnx" -OutFile "./PianoTranscription/transcription.onnx"
}
dotnet publish PianoTranscription -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
$publish_path = "PianoTranscription/bin/Release/net6.0/win-x64/publish/"
Remove-Item $publish_path"Melanchall_DryWetMidi_Native32.dll"
Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dll"
Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dylib"
Remove-Item $publish_path"onnxruntime_providers_shared.lib"
Remove-Item $publish_path"onnxruntime.lib"
Remove-Item $publish_path"PianoTranscription.Core.pdb"
Remove-Item $publish_path"PianoTranscription.pdb"