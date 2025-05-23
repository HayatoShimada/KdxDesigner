﻿using System.ComponentModel;

namespace KdxDesigner.Models.Define
{
    public class AppSettings
    {
        [Description("エラーリセット信号")]
        public string ErrorResetSignal { get; set; } = "M9999";

        [Description("リセット信号")]
        public string ResetSignal { get; set; } = "M3002";

        [Description("一時停止信号")]
        public string PauseSignal { get; set; } = "M3300";

        [Description("一時停止遅延信号")]
        public string PauseDelaySignal { get; set; } = "M3301";

        [Description("ソフトリセット信号")]
        public string SoftResetSignal { get; set; } = "M1234";

        [Description("アドレスオフセット")]
        public int AddressOffset { get; set; } = 1000;

        [Description("タイマーアドレスオフセット")]
        public int TimerAddressOffset { get; set; } = 2000;

        [Description("OFF確認信号")]
        public string OffConfirmSignal { get; set; } = "M4000";

        [Description("常時ON信号")]
        public string AlwaysON { get; set; } = "SM400";

        [Description("常時OFF信号")]
        public string AlwaysOFF { get; set; } = "SM401";
    }
}
