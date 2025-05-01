using KdxDesigner.Models;

using System.Net;

namespace KdxDesigner.Utils
{
    public static class LadderRow
    {
        public static LadderCsvRow AddLD(string address) => CreateRow(Command.LD, address);
        public static LadderCsvRow AddLDI(string address) => CreateRow(Command.LDI, address);
        public static LadderCsvRow AddAND(string address) => CreateRow(Command.AND, address);
        public static LadderCsvRow AddANI(string address) => CreateRow(Command.ANI, address);
        public static LadderCsvRow AddOUT(string address) => CreateRow(Command.OUT, address);
        public static LadderCsvRow AddSET(string address) => CreateRow(Command.SET, address);
        public static LadderCsvRow AddRST(string address) => CreateRow(Command.RST, address);
        public static LadderCsvRow AddOR(string address) => CreateRow(Command.OR, address);
        public static LadderCsvRow AddORI(string address) => CreateRow(Command.ORI, address);

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

        public static LadderCsvRow AddStatement(string statementComment)
        {
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

        private static LadderCsvRow CreateRow(string command, string address)
        {
            return new LadderCsvRow
            {
                StepNo = "\"\"",
                StepComment = "\"\"",
                Command = command,
                Address = $"\"{address}\"",
                Blank1 = "\"\"",
                PiStatement = "\"\"",
                Note = "\"\""
            };
        }
    }
}
