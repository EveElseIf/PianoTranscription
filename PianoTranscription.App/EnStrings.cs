namespace PianoTranscription.App
{
    internal class EnStrings : Strings
    {
        public string WelcomeInfoFormat => "Welcome to Piano Transcription Helper version {0}, for more information, please visit https://github.com/EveElseIf/PianoTranscription.";

        public string Title => "Piano Transcription Helper";

        public string SelectBtn => "Select a file to transcript";

        public string StartBtn => "Start transcript and save";

        public string Idle => "Status: Idle";

        public string Running => "Status: Running";

        public string Stop => "Stop";

        public string NoFileSelected => "No file selected";

        public string Log => "Log";

        public string AudioFile => "Audio File";

        public string SelectedFileFormat => "Selected file: {0}";

        public string StartFormat => "Start, file name: {0}.";

        public string ORTInited => "Onnxruntime initialized.";

        public string SavingFileFormat => "Saving file to: {0}.";

        public string CompletedFormat => "Transcription completed, total time usage: {0:g}.";

        public string Canceled => "Task is canceled.";
    }
}
