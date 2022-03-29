$onnx_exists=([System.IO.File]::Exists("./PianoTranscription.Core/transcription.onnx"))
Write-Output "onnx exists="$onnx_exists
if (-not($onnx_exists)) {
    Invoke-WebRequest -Uri "https://github.com/EveElseIf/PianoTranscription/releases/download/ONNX/transcription.onnx" -OutFile "./PianoTranscription.Core/transcription.onnx"
}
dotnet publish PianoTranscription -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
$publish_path = "PianoTranscription/bin/Release/net6.0/linux-x64/publish/"
if($args[0] -eq "dist") {
    Write-Output "Start build dist"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native32.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dylib"
    Remove-Item $publish_path"PianoTranscription.Core.pdb"
    Remove-Item $publish_path"PianoTranscription.pdb"
    if(([System.IO.Directory]::Exists("./build"))){
        Remove-Item "./build" -Force -Recurse
    }
    New-Item -ItemType Directory "build/pianotranscription-linux-x64"
    Copy-Item -Path $publish_path"*" "build/pianotranscription-linux-x64"
}