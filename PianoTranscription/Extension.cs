namespace PianoTranscription
{
    internal static class Extension
    {
        private static readonly string[] audioExts = new[]
        {
            ".mp3",
            ".wav",
            ".flac",
            ".ogg",
            ".ape"
        };
        public static bool IsAudioFile(this FileInfo info)
        {
            var ext = info.Extension;
            if (audioExts.Any(x => x == ext.ToLower())) return true;
            else return false;
        }
        public static string NameWithoutExtension(this FileInfo info)
        {
            return Path.GetFileNameWithoutExtension(info.FullName);
        }
    }
}
