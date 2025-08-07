using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils.MnemonicCommon
{
    public static class LadderRow
    {
        // アドレスを入力すると、LD命令等を生成する
        public static LadderCsvRow AddLD(string address) => CreateRow(Command.LD, address);
        public static LadderCsvRow AddLDI(string address) => CreateRow(Command.LDI, address);
        public static LadderCsvRow AddLDP(string address) => CreateRow(Command.LDP, address);
        public static LadderCsvRow AddORP(string address) => CreateRow(Command.ORP, address);
        public static LadderCsvRow AddLDF(string address) => CreateRow(Command.LDF, address);
        public static LadderCsvRow AddAND(string address) => CreateRow(Command.AND, address);
        public static LadderCsvRow AddANI(string address) => CreateRow(Command.ANI, address);
        public static LadderCsvRow AddOUT(string address) => CreateRow(Command.OUT, address);
        public static LadderCsvRow AddINC(string address) => CreateRow(Command.INC, address);
        public static LadderCsvRow AddSET(string address) => CreateRow(Command.SET, address);
        public static LadderCsvRow AddRST(string address) => CreateRow(Command.RST, address);
        public static LadderCsvRow AddOR(string address) => CreateRow(Command.OR, address);
        public static LadderCsvRow AddORI(string address) => CreateRow(Command.ORI, address);
        public static LadderCsvRow AddCJ(string address) => CreateRow(Command.CJ, address);
        public static LadderCsvRow AddPLS(string address) => CreateRow(Command.PLS, address);
        public static LadderCsvRow AddORB() => CreateRow(Command.ORB);
        public static LadderCsvRow AddANB() => CreateRow(Command.ANB);
        public static LadderCsvRow AddNOP() => CreateRow(Command.NOP);
        public static LadderCsvRow AddMPS() => CreateRow(Command.MPS);
        public static LadderCsvRow AddMRD() => CreateRow(Command.MRD);
        public static LadderCsvRow AddMPP() => CreateRow(Command.MPP);
        public static LadderCsvRow AddMEP() => CreateRow(Command.MEP);
        public static LadderCsvRow AddFEND() => CreateRow(Command.FEND);
        public static LadderCsvRow AddRET() => CreateRow(Command.RET);
        public static LadderCsvRow AddPoint(string address) => CreateRow($"\"{address}\"");
        public static LadderCsvRow MPCreate(string mp)
        {
            switch (mp)
            {
                case "MPS":
                    return AddMPS();
                case "MRD":
                    return AddMRD();
                case "MPP":
                    return AddMPP();
                case "MEP":
                    return AddMEP();
                default:
                    throw new ArgumentException($"Invalid MP command: {mp}");
            }
        }

        // ソースとデスティネーションを入力すると、MOV命令等を生成する
        public static List<LadderCsvRow> AddMOVSet(string source, string destination)
            => CreateMOV(Command.MOV, source, destination);

        public static List<LadderCsvRow> AddMOVPSet(string source, string destination)
            => CreateMOV(Command.MOVP, source, destination);

        public static List<LadderCsvRow> AddTimer(string source, string destination)
            => CreateMOV(Command.OUTH, source, destination);
        public static List<LadderCsvRow> AddLDE(string source, string destination)
            => CreateMOV(Command.LDE, source, destination);
        public static List<LadderCsvRow> AddLDG(string source, string destination)
            => CreateMOV(Command.LDG, source, destination);
        public static List<LadderCsvRow> AddLDL(string source, string destination)
            => CreateMOV(Command.LDL, source, destination);
        public static List<LadderCsvRow> AddLDN(string source, string destination)
            => CreateMOV(Command.LDN, source, destination);
        public static List<LadderCsvRow> AddORE(string source, string destination)
=> CreateMOV(Command.ORE, source, destination);
        public static List<LadderCsvRow> AddORG(string source, string destination)
=> CreateMOV(Command.ORG, source, destination);
        public static List<LadderCsvRow> AddORL(string source, string destination)
=> CreateMOV(Command.ORL, source, destination);
        public static List<LadderCsvRow> AddORN(string source, string destination)
=> CreateMOV(Command.ORN, source, destination);
        public static List<LadderCsvRow> AddANDG(string source, string destination)
            => CreateMOV(Command.ANDG, source, destination);
        public static List<LadderCsvRow> AddANDL(string source, string destination)
            => CreateMOV(Command.ANDL, source, destination);
        public static List<LadderCsvRow> AddANDN(string source, string destination)
           => CreateMOV(Command.ANDN, source, destination);

        // 引数3
        public static List<LadderCsvRow> AddBMOVSet(string source, string destination, string count)
            => CreateBMOV(Command.BMOV, source, destination, count);
        public static List<LadderCsvRow> AddFMOVSet(string source, string destination, string count)
            => CreateBMOV(Command.FMOV, source, destination, count);
        public static List<LadderCsvRow> AddSUBP(string source, string destination, string count)
            => CreateBMOV(Command.SUBP, source, destination, count);


        private static List<LadderCsvRow> CreateMOV(string command, string source, string destination)
        {
            return new List<LadderCsvRow>
            {
                CreateRow(command, source),
                CreateRow("", destination)
            };
        }

        private static List<LadderCsvRow> CreateBMOV(string command, string source, string destination, string count)
        {
            return new List<LadderCsvRow>
            {
                CreateRow(command, source),
                CreateRow("", destination),
                CreateRow("", count)
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
