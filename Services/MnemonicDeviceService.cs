using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Access;

using System; // Encoding.RegisterProvider を使うために追加
using System.Collections.Generic; // List, Dictionary のために追加
using System.Data;
using System.Data.OleDb;
using System.Linq; // FirstOrDefault を使うために追加
using System.Text;

namespace KdxDesigner.Services
{
    internal class MnemonicDeviceService
    {
        private readonly string _connectionString;

        static MnemonicDeviceService()
        {
            // Shift_JIS エンコーディングを有効にする
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public MnemonicDeviceService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        // MnemonicDeviceテーブルからPlcIdに基づいてデータを取得する
        public List<MnemonicDevice> GetMnemonicDevice(int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicDevice WHERE PlcId = @PlcId";
            return connection.Query<MnemonicDevice>(sql, new { PlcId = plcId }).ToList();
        }

        // MnemonicDeviceテーブルからPlcIdとMnemonicIdに基づいてデータを取得する
        public List<MnemonicDevice> GetMnemonicDeviceByMnemonic(int plcId, int mnemonicId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicDevice WHERE PlcId = @PlcId AND MnemonicId = @MnemonicId";
            return connection.Query<MnemonicDevice>(sql, new { PlcId = plcId, MnemonicId = mnemonicId }).ToList();
        }

        // Processesのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceProcess(List<Models.Process> processes, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.Process);
                var existingLookup = allExisting.ToDictionary(m => m.RecordId, m => m);

                int count = 0;
                foreach (Models.Process process in processes)
                {
                    if (process == null) continue;

                    existingLookup.TryGetValue(process.Id, out var existing);
                    var parameters = new DynamicParameters();

                    string input = process.ProcessName ?? "";
                    var result = SplitByByteLength(input, 8, 2); // ★このメソッドは修正済み

                    parameters.Add("MnemonicId", (int)MnemonicType.Process, DbType.Int32);
                    parameters.Add("RecordId", process.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "L", DbType.String);
                    parameters.Add("StartNum", (count * 5 + startNum), DbType.Int32);
                    parameters.Add("OutCoilCount", 5, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("Comment1", result[0], DbType.String); // result[0]は常に安全
                    parameters.Add("Comment2", result[1], DbType.String); // result[1]も常に安全

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [DeviceLabel] = @DeviceLabel,
                                [StartNum] = @StartNum, [OutCoilCount] = @OutCoilCount, [PlcId] = @PlcId,
                                [Comment1] = @Comment1, [Comment2] = @Comment2
                            WHERE [ID] = @ID",
                            parameters, transaction);
                    }
                    else
                    {
                        // ★修正: SQLのパラメータ名と数を修正
                        connection.Execute(@"
                            INSERT INTO [MnemonicDevice] (
                                [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId], [Comment1], [Comment2]
                            ) VALUES (
                                @MnemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment1, @Comment2
                            )",
                            parameters, transaction);
                    }
                    count++;
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // 修正: AccessRepository のインスタンス化に必要な connectionString を渡すように修正  
        public void SaveMnemonicDeviceProcessDetail(List<ProcessDetailDto> processes, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.ProcessDetail);
                var existingLookup = allExisting.ToDictionary(mnemonicDevice => mnemonicDevice.RecordId, mnemonicDevice => mnemonicDevice);
                var repository = new AccessRepository(_connectionString);

                int count = 0;
                foreach (ProcessDetailDto process in processes)
                {
                    if (process == null || !process.OperationId.HasValue) continue;

                    existingLookup.TryGetValue(process.Id, out var existing);

                    var operation = repository.GetOperationById(process.OperationId.Value);
                    var comment1 = operation?.OperationName ?? "";
                    var comment2 = process.DetailName ?? "";

                    var parameters = new DynamicParameters();
                    parameters.Add("MnemonicId", (int)MnemonicType.ProcessDetail, DbType.Int32);
                    parameters.Add("RecordId", process.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "L", DbType.String);
                    parameters.Add("StartNum", (count * 10 + startNum), DbType.Int32);
                    parameters.Add("OutCoilCount", 10, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("Comment1", comment1, DbType.String);
                    parameters.Add("Comment2", comment2, DbType.String);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"  
                            UPDATE [MnemonicDevice] SET  
                                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [DeviceLabel] = @DeviceLabel,  
                                [StartNum] = @StartNum, [OutCoilCount] = @OutCoilCount, [PlcId] = @PlcId,  
                                [Comment1] = @Comment1, [Comment2] = @Comment2  
                            WHERE [ID] = @ID",
                            parameters, transaction);
                    }
                    else
                    {
                        connection.Execute(@"  
                            INSERT INTO [MnemonicDevice] (  
                                [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId], [Comment1], [Comment2]  
                            ) VALUES (  
                                @MnemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment1, @Comment2  
                            )",
                            parameters, transaction);
                    }

                    count++;
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Operationのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceOperation(List<Operation> operations, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.Operation);
                var existingLookup = allExisting.ToDictionary(m => m.RecordId, m => m);

                int count = 0;
                foreach (Operation operation in operations)
                {
                    if (operation == null) continue;

                    existingLookup.TryGetValue(operation.Id, out var existing);

                    var parameters = new DynamicParameters();
                    parameters.Add("MnemonicId", (int)MnemonicType.Operation, DbType.Int32);
                    parameters.Add("RecordId", operation.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "M", DbType.String);
                    parameters.Add("StartNum", (count * 20 + startNum), DbType.Int32);
                    parameters.Add("OutCoilCount", 20, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("Comment1", operation.OperationName ?? "", DbType.String);
                    parameters.Add("Comment2", operation.OperationName ?? "", DbType.String);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [DeviceLabel] = @DeviceLabel,
                                [StartNum] = @StartNum, [OutCoilCount] = @OutCoilCount, [PlcId] = @PlcId,
                                [Comment1] = @Comment1, [Comment2] = @Comment2
                            WHERE [ID] = @ID",
                            parameters, transaction);
                    }
                    else
                    {
                        // ★修正: SQLのパラメータ名のタイプミスを修正
                        connection.Execute(@"
                            INSERT INTO [MnemonicDevice] (
                                [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId], [Comment1], [Comment2]
                            ) VALUES (
                                @MnemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment1, @Comment2
                            )",
                            parameters, transaction);
                    }
                    count++;
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Cylinderのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceCY(List<CY> cylinders, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.CY);
                var existingLookup = allExisting.ToDictionary(m => m.RecordId, m => m);

                int count = 0;
                foreach (CY cylinder in cylinders)
                {
                    if (cylinder == null) continue;

                    existingLookup.TryGetValue(cylinder.Id, out var existing);

                    var parameters = new DynamicParameters();
                    parameters.Add("MnemonicId", (int)MnemonicType.CY, DbType.Int32);
                    parameters.Add("RecordId", cylinder.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "M", DbType.String);
                    parameters.Add("StartNum", (count * 50 + startNum), DbType.Int32);
                    parameters.Add("OutCoilCount", 50, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("Comment1", cylinder.CYNum, DbType.String);
                    parameters.Add("Comment2", cylinder.CYNum, DbType.String);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [DeviceLabel] = @DeviceLabel,
                                [StartNum] = @StartNum, [OutCoilCount] = @OutCoilCount, [PlcId] = @PlcId,
                                [Comment1] = @Comment1, [Comment2] = @Comment2
                            WHERE [ID] = @ID",
                            parameters, transaction);
                    }
                    else
                    {
                        // ★修正: SQLのパラメータ名のタイプミスを修正
                        connection.Execute(@"
                            INSERT INTO [MnemonicDevice] (
                                [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId], [Comment1], [Comment2]
                            ) VALUES (
                                @MnemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment1, @Comment2
                            )",
                            parameters, transaction);
                    }
                    count++;
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// ★修正: ArgumentOutOfRangeExceptionを防ぐため、常にpartsで指定された数の要素を返すように修正。
        /// 足りない場合は空文字列で埋める。
        /// </summary>
        static List<string> SplitByByteLength(string input, int maxBytesPerPart, int parts)
        {
            List<string> result = new();
            if (string.IsNullOrEmpty(input))
            {
                // 入力がnullまたは空の場合、空文字列で埋めたリストを返す
                while (result.Count < parts)
                {
                    result.Add(string.Empty);
                }
                return result;
            }

            Encoding encoding = Encoding.GetEncoding("shift_jis");
            int currentByteCount = 0;
            StringBuilder sb = new();

            foreach (char c in input)
            {
                if (result.Count >= parts) break; // 必要なパーツ数に達したら終了

                int charByteLength = encoding.GetByteCount(new[] { c });
                if (currentByteCount + charByteLength > maxBytesPerPart)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    currentByteCount = 0;
                    if (result.Count == parts) break;
                }

                sb.Append(c);
                currentByteCount += charByteLength;
            }

            if (sb.Length > 0 && result.Count < parts)
            {
                result.Add(sb.ToString());
            }

            // ★修正点: 足りない部分を空文字列で埋める
            while (result.Count < parts)
            {
                result.Add(string.Empty);
            }

            return result;
        }
    }
}