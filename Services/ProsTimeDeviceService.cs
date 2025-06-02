using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;

using System.Data;
using System.Data.OleDb;
using System.Reflection;

namespace KdxDesigner.Services
{
    internal class ProsTimeDeviceService
    {
        private readonly string _connectionString;

        public ProsTimeDeviceService(AccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        // MnemonicDeviceテーブルからPlcIdに基づいてデータを取得する
        public List<ProsTime> GetProsTimeByPlcId(int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM ProsTime WHERE PlcId = @PlcId";
            return connection.Query<ProsTime>(sql, new { PlcId = plcId }).ToList();
        }

        // MnemonicDeviceテーブルからPlcIdとMnemonicIdに基づいてデータを取得する
        public List<ProsTime> GetProsTimeByMnemonicId(int plcId, int mnemonicId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM ProsTime WHERE PlcId = @PlcId AND MnemonicId = @MnemonicId";
            return connection.Query<ProsTime>(sql, new { PlcId = plcId, MnemonicId = mnemonicId }).ToList();
        }

        // Operationのリストを受け取り、ProsTimeテーブルに保存する
        public void SaveProsTime(List<Operation> operations, int startCurrent, int startPrevious, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            // MnemonicDeviceテーブルの既存データを取得
            var allExisting = GetProsTimeByMnemonicId(plcId, (int)MnemonicType.Operation);

            int count = 0;
            foreach (Operation operation in operations)
            {
                if (operation == null) continue;
                var existing = allExisting.FirstOrDefault(m => m.RecordId == operation.Id);
                var category = operation.CategoryId;

                int? prosTimeCount;

                // 工程タイム回路の数が何個になるか。
                switch (category)
                {
                    case 2 or 29 or 30 or 20: // 保持
                        prosTimeCount = 4;
                        break;
                    case 3 or 9 or 15 or 27: // 速度制御INV1
                        prosTimeCount = 4 + 2;
                        break;
                    case 4 or 10 or 16 or 28: // 速度制御INV2
                        prosTimeCount = 4 + 4;
                        break;
                    case 5 or 11 or 17:     // 速度制御INV3
                        prosTimeCount = 4 + 6;
                        break;
                    case 6 or 12 or 18: // 速度制御INV4
                        prosTimeCount = 4 + 8;
                        break;
                    case 7 or 13 or 19: // 速度制御INV5
                        prosTimeCount = 4 + 12;
                        break;
                    case 31:            // サーボ
                        prosTimeCount = 0;
                        break;
                    default:
                        prosTimeCount = 0;
                        break;
                }

                for (int  i = 0; i < prosTimeCount; i++)
                {
                    string currentDevice = "ZR" + (startCurrent + count + prosTimeCount).ToString(); 
                    string previousDevice = "ZR" + (startPrevious + count + prosTimeCount).ToString();


                    var parameters = new DynamicParameters();

                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("MnemonicId", (int)MnemonicType.Operation, DbType.Int32);
                    parameters.Add("RecordId", operation.Id, DbType.Int32);
                    parameters.Add("SortId", i, DbType.Int32);
                    parameters.Add("CurrentDevice", currentDevice, DbType.String);
                    parameters.Add("PreviousDevice", previousDevice, DbType.String);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                        UPDATE [Error] SET
                            [PlcId] = @PlcId,
                            [MnemonicId] = @MnemonicId,
                            [RecordId] = @RecordId,
                            [SortId] = @SortId,
                            [CurrentDevice] = @CurrentDevice,
                            [PreviousDevice] = @PreviousDevice
                        WHERE [ID] = @ID",
                            parameters, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
    INSERT INTO [Error] (
        [PlcId], 
        [MnemonicId], 
        [RecordId], 
        [SortId],
        [CurrentDevice], 
        [PreviousDevice])
    VALUES
        (@PlcId, 
        @MnemonicId, 
        @RecordId, 
        @SortId, 
        @CurrentDevice,
        @PreviousDevice)",
    parameters, transaction);

                    }
                }
                count += prosTimeCount ?? 0;
            }
            transaction.Commit();
        }
    }
}
