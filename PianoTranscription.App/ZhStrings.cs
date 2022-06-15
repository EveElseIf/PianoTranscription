namespace PianoTranscription.App
{
    internal class ZhStrings : Strings
    {
        public string WelcomeInfoFormat => "欢迎使用AI扒谱小助手（版本{0}），若需要更多信息，请访问https://github.com/EveElseIf/PianoTranscription。";

        public string Title => "AI扒谱小助手";

        public string SelectBtn => "选择音频文件";

        public string StartBtn => "运行扒谱并保存";

        public string Idle => "状态：空闲";

        public string Running => "状态：运行中";

        public string Stop => "停止";

        public string NoFileSelected => "未选择文件";

        public string Log => "日志";

        public string AudioFile => "音频文件";

        public string SelectedFileFormat => "已选择文件：{0}";

        public string StartFormat => "开始运行，文件名称：{0}。";

        public string ORTInited => "Onnxruntime已初始化。";

        public string SavingFileFormat => "将文件保存到：{0}。";

        public string CompletedFormat => "运行完成，总共花费时间：{0:g}。";

        public string Canceled => "任务已取消。";
    }
}