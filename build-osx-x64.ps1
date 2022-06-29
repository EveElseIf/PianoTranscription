$onnx_exists = ([System.IO.File]::Exists("./PianoTranscription.Core/transcription.onnx"))
Write-Output "onnx exists=$onnx_exists"
if (-not($onnx_exists)) {
    Invoke-WebRequest -Uri "https://github.com/EveElseIf/PianoTranscription/releases/download/ONNX/transcription.onnx" -OutFile "./PianoTranscription.Core/transcription.onnx"
}
chmod +x PianoTranscription.Core/ffmpeg-osx-x64/ffmpeg
dotnet build
Copy-Item "PianoTranscription.Core/ffmpeg-osx-x64/ffmpeg" "PianoTranscription/bin/Debug/net6.0"
Copy-Item "PianoTranscription.Core/ffmpeg-osx-x64/ffmpeg" "PianoTranscription.App/bin/Debug/net6.0"
if($args[0] -eq "dist") {
    Write-Output "Start build dist"

    dotnet publish PianoTranscription -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
    $publish_path = "PianoTranscription/bin/Release/net6.0/osx-x64/publish/"
    Copy-Item "PianoTranscription.Core/ffmpeg-osx-x64/ffmpeg" $publish_path
    Copy-Item "PianoTranscription/bin/Debug/net6.0/runtimes/osx*x64/native/libonnxruntime.dylib" $publish_path
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native32.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dylib"
    Remove-Item $publish_path"PianoTranscription.Core.pdb"
    Remove-Item $publish_path"PianoTranscription.pdb"
    $out_dir_name = "pianotranscription-osx-x64"
    $dist_path = "build/$out_dir_name"
    if ([System.IO.Directory]::Exists($dist_path)) {
        Remove-Item $dist_path -Force -Recurse
    }
    New-Item -ItemType Directory $dist_path
    Copy-Item -Path "$publish_path*" $dist_path
    tar -zcvf "$out_dir_name.tar.gz" -C build $out_dir_name

    dotnet publish PianoTranscription.App -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
    $publish_path = "PianoTranscription.App/bin/Release/net6.0/osx-x64/publish/"
    Copy-Item "PianoTranscription.Core/ffmpeg-osx-x64/ffmpeg" $publish_path
    Copy-Item "PianoTranscription/bin/Debug/net6.0/runtimes/osx*x64/native/libonnxruntime.dylib" $publish_path
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native32.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dylib"
    Remove-Item $publish_path"PianoTranscription.Core.pdb"
    Remove-Item $publish_path"PianoTranscription.App.pdb"
    $bundle_name = "PianoTranscription.app"
    $bundle_root = "build/$bundle_name/"
    if ([System.IO.Directory]::Exists($bundle_root)) {
        Remove-Item $bundle_root -Force -Recurse
    }
    New-Item -ItemType Directory $bundle_root
    New-Item -ItemType Directory $bundle_root"Contents"
    New-Item -ItemType Directory $bundle_root"Contents/MacOS"
    Copy-Item -Path "$publish_path*" $bundle_root"Contents/MacOS"
    Copy-Item -Path "resources/Info.plist" $bundle_root"/Contents"
    New-Item -ItemType Directory $bundle_root"Contents/Resources"
    tar -zcvf "pianotranscription-osx-x64-gui.tar.gz" -C build $bundle_name

    Write-Output "Build dist Finished"
}