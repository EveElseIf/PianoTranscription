$onnx_exists = Test-Path "./PianoTranscription.Core/transcription.onnx"
Write-Output "onnx exists=$onnx_exists"
if (-not($onnx_exists)) {
    Invoke-WebRequest -Uri "https://github.com/EveElseIf/PianoTranscription/releases/download/ONNX/transcription.onnx" -OutFile "./PianoTranscription.Core/transcription.onnx"
}
dotnet build
Copy-Item "PianoTranscription.Core/ffmpeg-win-x64/ffmpeg.exe" "PianoTranscription/bin/Debug/net6.0"
Copy-Item "PianoTranscription.Core/ffmpeg-win-x64/libwinpthread-1.dll" "PianoTranscription/bin/Debug/net6.0"
if ($args[0] -eq "dist") {
    Write-Output "Start build dist"

    dotnet publish PianoTranscription -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
    $publish_path = "PianoTranscription/bin/Release/net6.0/win-x64/publish/"
    Copy-Item "PianoTranscription.Core/ffmpeg-win-x64/ffmpeg.exe" $publish_path
    Copy-Item "PianoTranscription.Core/ffmpeg-win-x64/libwinpthread-1.dll" $publish_path
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native32.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dylib"
    Remove-Item $publish_path"onnxruntime_providers_shared.lib"
    Remove-Item $publish_path"onnxruntime.lib"
    Remove-Item $publish_path"PianoTranscription.Core.pdb"
    Remove-Item $publish_path"PianoTranscription.pdb"
    $out_dir_name = "pianotranscription-win-x64"
    $dist_path = "build/$out_dir_name"
    if (Test-Path $dist_path) {
        Remove-Item $dist_path -Force -Recurse
    }
    New-Item -ItemType Directory $dist_path
    Copy-Item -Path "$publish_path*" $dist_path
    Compress-Archive $dist_path "$out_dir_name.zip" -Force

    dotnet publish PianoTranscription.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
    $publish_path = "PianoTranscription.App/bin/Release/net6.0/win-x64/publish/"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native32.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dylib"
    Remove-Item $publish_path"onnxruntime_providers_shared.lib"
    Remove-Item $publish_path"onnxruntime.lib"
    Remove-Item $publish_path"PianoTranscription.Core.pdb"
    Remove-Item $publish_path"PianoTranscription.App.pdb"
    $out_dir_name = "pianotranscription-win-x64-gui"
    $dist_path = "build/$out_dir_name"
    if (Test-Path $dist_path) {
        Remove-Item $dist_path -Force -Recurse
    }
    New-Item -ItemType Directory $dist_path
    Copy-Item -Path "$publish_path*" $dist_path
    Compress-Archive $dist_path "$out_dir_name.zip" -Force

    Write-Output "Build dist Finished"
}