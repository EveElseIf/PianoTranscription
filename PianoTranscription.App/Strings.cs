namespace PianoTranscription.App
{
    internal interface Strings
    {
        string WelcomeInfoFormat { get; }
        string Title { get; }
        string SelectBtn { get; }
        string StartBtn { get; }
        string Idle { get; }
        string Running { get; }
        string Stop { get; }
        string NoFileSelected { get; }
        string Log { get; }
        string AudioFile { get; }
        string SelectedFileFormat { get; }
        string StartFormat { get; }
        string ORTInited { get; }
        string SavingFileFormat { get; }
        string CompletedFormat { get; }
        string Canceled { get; }
    }
    internal static class StringExt
    {
        public static string Format(this string str, params object[] os) => string.Format(str, os);
    }
}
