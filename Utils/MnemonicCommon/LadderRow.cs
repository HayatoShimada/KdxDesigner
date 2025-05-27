using KdxDesigner.Models.Define;

using System.Net;

namespace KdxDesigner.Utils.MnemonicCommon
{
    public static class LadderRow
    {
        // アドレスを入力すると、LD命令等を生成する
        public static LadderCsvRow AddLD(string address) => CreateRow(Command.LD, address);
        public static LadderCsvRow AddLDI(string address) => CreateRow(Command.LDI, address);
        public static LadderCsvRow AddLDP(string address) => CreateRow(Command.LDP, address);
        public static LadderCsvRow AddLDF(string address) => CreateRow(Command.LDF, address);
        public static LadderCsvRow AddAND(string address) => CreateRow(Command.AND, address);
        public static LadderCsvRow AddANI(string address) => CreateRow(Command.ANI, address);
        public static LadderCsvRow AddOUT(string address) => CreateRow(Command.OUT, address);
        public static LadderCsvRow AddSET(string address) => CreateRow(Command.SET, address);
        public static LadderCsvRow AddRST(string address) => CreateRow(Command.RST, address);
        public static LadderCsvRow AddOR(string address) => CreateRow(Command.OR, address);
        public static LadderCsvRow AddORI(string address) => CreateRow(Command.ORI, address);
        public static LadderCsvRow AddCJ(string address) => CreateRow(Command.CJ, address);
        public static LadderCsvRow AddORB() => CreateRow(Command.ORB);


        // ソースとデスティネーションを入力すると、MOV命令等を生成する
        public static List<LadderCsvRow> AddMOVSet(string source, string destination)
            => CreateMOV(Command.MOV, source, destination);

        public static List<LadderCsvRow> AddMOVPSet(string source, string destination)
            => CreateMOV(Command.MOVP, source, destination);

        private static List<LadderCsvRow> CreateMOV(string command, string source, string destination)
        {
            return new List<LadderCsvRow>
            {
                CreateRow(command, source),
                CreateRow("", destination)
            };
        }

        // ステートメントを追加する
        public static LadderCsvRow AddStatement(string statementComment)
        {
            Console.WriteLine($"[LadderRow] Statement: {statementComment}");

            return new LadderCsvRow
            {
                StepNo = "\"\"",
                StepComment = statementComment,
                Command = "\"\"",
                Address = "\"\"",
                Blank1 = "\"\"",
                PiStatement = "\"\"",
                Note = "\"\""
            };
        }

        // 共通メソッド
        private static LadderCsvRow CreateRow(string command, string address = "")
        {
            var row = new LadderCsvRow
            {
                StepNo = "\"\"",
                StepComment = "\"\"",
                Command = command,
                Address = $"\"{address}\"",
                Blank1 = "\"\"",
                PiStatement = "\"\"",
                Note = "\"\""
            };

            Console.WriteLine($"[LadderRow] Added: Command={command}, Address={address}");
            return row;
        }

    }
}
