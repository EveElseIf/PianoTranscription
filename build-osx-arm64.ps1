if(-not $IsMacOS -or $PSVersionTable.OS.ToLower().Contains("x86")) {
    Write-Output "Please use arm64 MacOS to build this, otherwise, you will face a crash problem."
    Write-Output "You can add -force to ignore this limitation."
    if ((-not $args[0] -eq "-force") -and (-not $args[1] -eq "-force")) {
        Write-Output "Aborted."
        Exit 0
    }
}
$onnx_exists = ([System.IO.File]::Exists("./PianoTranscription.Core/transcription.onnx"))
Write-Output "onnx exists=$onnx_exists"
if (-not($onnx_exists)) {
    Invoke-WebRequest -Uri "https://github.com/EveElseIf/PianoTranscription/releases/download/ONNX/transcription.onnx" -OutFile "./PianoTranscription.Core/transcription.onnx"
}
chmod +x PianoTranscription.Core/ffmpeg-osx-arm64/ffmpeg
dotnet build ./PianoTranscription
dotnet build ./PianoTranscription.App
Copy-Item "PianoTranscription.Core/ffmpeg-osx-arm64/ffmpeg" "PianoTranscription/bin/Debug/net6.0"
Copy-Item "PianoTranscription.Core/ffmpeg-osx-arm64/ffmpeg" "PianoTranscription.App/bin/Debug/net6.0"
if ($args[0] -eq "dist" -or $args[1] -eq "dist") {
    Write-Output "Start build dist"

    dotnet publish PianoTranscription -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
    $publish_path = "PianoTranscription/bin/Release/net6.0/osx-arm64/publish/"
    Copy-Item "PianoTranscription.Core/ffmpeg-osx-arm64/ffmpeg" $publish_path
    Copy-Item "PianoTranscription/bin/Debug/net6.0/runtimes/osx*arm64/native/libonnxruntime.dylib" $publish_path
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native32.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dylib"
    Remove-Item $publish_path"PianoTranscription.Core.pdb"
    Remove-Item $publish_path"PianoTranscription.pdb"
    $out_dir_name = "pianotranscription-osx-arm64"
    $dist_path = "build/$out_dir_name"
    if ([System.IO.Directory]::Exists($dist_path)) {
        Remove-Item $dist_path -Force -Recurse
    }
    New-Item -ItemType Directory $dist_path
    Copy-Item -Path "$publish_path*" $dist_path
    tar -zcvf "$out_dir_name.tar.gz" -C build $out_dir_name

    dotnet publish PianoTranscription.App -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
    $publish_path = "PianoTranscription.App/bin/Release/net6.0/osx-arm64/publish/"
    Copy-Item "PianoTranscription.Core/ffmpeg-osx-arm64/ffmpeg" $publish_path
    Copy-Item "PianoTranscription/bin/Debug/net6.0/runtimes/osx*arm64/native/libonnxruntime.dylib" $publish_path
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native32.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dll"
    Remove-Item $publish_path"Melanchall_DryWetMidi_Native64.dylib"
    Remove-Item $publish_path"PianoTranscription.Core.pdb"
    Remove-Item $publish_path"PianoTranscription.App.pdb"
    Remove-Item $publish_path"ffmpeg.exe"
    Remove-Item $publish_path"libwinpthread-1.dll"
    $bundle_name = "PianoTranscription.app"
    $bundle_root = "build/$bundle_name/"
    if ([System.IO.Directory]::Exists($bundle_root)) {
        Remove-Item $bundle_root -Force -Recurse
    }
    New-Item -ItemType Directory $bundle_root
    New-Item -ItemType Directory $bundle_root"Contents"
    New-Item -ItemType Directory $bundle_root"Contents/MacOS"
    New-Item -ItemType Directory $bundle_root"Contents/Resources"
    Copy-Item -Path "$publish_path*" $bundle_root"Contents/MacOS"
    Copy-Item -Path "resources/Info.plist" $bundle_root"/Contents"
    Copy-Item -Path "resources/icon.icns" $bundle_root"/Contents/Resources"
    Copy-Item -Path "resources/en.lproj" $bundle_root"/Contents/Resources"
    Copy-Item -Path "resources/zh.lproj" $bundle_root"/Contents/Resources"
    tar -zcvf "pianotranscription-osx-arm64-gui.tar.gz" -C build $bundle_name

    Write-Output "Build dist Finished"
}