using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Access;

using System.Data;
using System.Data.OleDb;

namespace KdxDesigner.Services
{
    internal class ErrorService
    {
        private readonly string _connectionString;

        public ErrorService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        public List<ErrorMessage> GetErrorMessage(int mnemonicId)
        {
            List<ErrorMessage> messages = new();

            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM ErrorMessage " +
                "WHERE MnemonicId = @MnemonicId ";
            messages = connection.Query<ErrorMessage>(sql, new
            {
                MnemonicId = mnemonicId
            }).ToList();

            return messages;
        }

        public List<Models.Error> GetErrors(int plcId, int cycleId, int mnemonicId)
        {
            List<Models.Error> errors = new();

            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM Error " +
                "WHERE PlcId = @PlcId " +
                "AND CycleId = @CycleId " +
                "AND MnemonicId = @MnemonicId";
            errors = connection.Query<Models.Error>(sql, new
            {
                PlcId = plcId,
                CycleId = cycleId,
                MnemonicId = mnemonicId
            }).ToList();

            return errors;
        }

        // Operationのリストを受け取り、Errorテーブルに保存する
        public void SaveMnemonicDeviceOperation(
            List<Models.Operation> operations,
            List<Models.IO> iOs,
            int startNum,
            int plcId,
            int cycleId
            )
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            // MnemonicDeviceテーブルの既存データを取得
            var allExisting = GetErrors(plcId, cycleId, (int)MnemonicType.Operation);
            var messages = GetErrorMessage((int)MnemonicType.Operation);

            int alarmCount = 0;
            foreach (Operation operation in operations)
            {
                if (operation == null) continue;
                var existing = allExisting.FirstOrDefault(m => m.RecordId == operation.Id);
                var category = operation.CategoryId;

                List<int> AlarmIds = new();
                switch (category)
                {
                    case 2 or 29 or 30: // 保持
                        AlarmIds.AddRange([1, 2, 5]);
                        break;
                    case 3 or 9 or 15 or 27: // 速度制御INV1
                        AlarmIds.AddRange([1, 2, 3, 4, 5]);
                        break;
                    case 4 or 10 or 16 or 28: // 速度制御INV2
                        AlarmIds.AddRange([1, 2, 3, 4, 3, 4, 5]);
                        break;
                    case 5 or 11 or 17:     // 速度制御INV3
                        AlarmIds.AddRange([1, 2, 3, 4, 3, 4, 3, 4, 5]);
                        break;
                    case 6 or 12 or 18: // 速度制御INV4
                        AlarmIds.AddRange([1, 2, 3, 4, 3, 4, 3, 4, 3, 4, 5]);
                        break;
                    case 7 or 13 or 19: // 速度制御INV5
                        AlarmIds.AddRange([1, 2, 3, 4, 3, 4, 3, 4, 3, 4, 3, 4, 5]);
                        break;
                    case 20:            // バネ
                        AlarmIds.AddRange([5]);
                        break;
                    case 31:            // サーボ
                        break;
                    default:
                        break;
                }

                foreach (int id in AlarmIds)
                {
                    string device = "M" + (startNum + alarmCount).ToString(); // 例: 01A01, 01A02, ...
                    var parameters = new DynamicParameters();
                    int errorTime = 1000; // エラー時間の初期値
                    string comment = messages.FirstOrDefault(m => m.AlarmId == id)?.BaseMessage ?? string.Empty;
                    string alarm = messages.FirstOrDefault(m => m.AlarmId == id)?.BaseAlarm ?? string.Empty;
                    // 将来的にメッセージの代入処理を追加する。

                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("CycleId", cycleId, DbType.Int32);
                    parameters.Add("Device", device, DbType.String);
                    parameters.Add("MnemonicId", (int)MnemonicType.Operation, DbType.Int32);
                    parameters.Add("RecordId", operation.Id, DbType.Int32);
                    parameters.Add("AlarmId", id, DbType.Int32);
                    parameters.Add("ErrorNum", alarmCount, DbType.Int32);
                    parameters.Add("AlarmComment", alarm, DbType.String);
                    parameters.Add("MessageComment", comment, DbType.String);
                    parameters.Add("ErrorTime", errorTime, DbType.Int32);
                    parameters.Add("ErrorTimeDevice", "", DbType.String);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        var sqlUpdate = @"
                        UPDATE [Error] SET
                            [PlcId] = ?,
                            [CycleId] = ?,
                            [Device] = ?,
                            [MnemonicId] = ?,
                            [RecordId] = ?,
                            [AlarmId] = ?,
                            [ErrorNum] = ?,
                            [AlarmComment] = ?,
                            [MessageComment] = ?,
                            [ErrorTime] = ?,
                            [ErrorTimeDevice] = ?
                        WHERE [ID] = ?";

                        var updateParams = new DynamicParameters();

                        updateParams.Add("p1", plcId, DbType.Int32);
                        updateParams.Add("p2", cycleId, DbType.Int32);
                        updateParams.Add("p3", device, DbType.String);
                        updateParams.Add("p4", (int)MnemonicType.Operation, DbType.Int32);
                        updateParams.Add("p5", operation.Id, DbType.Int32);
                        updateParams.Add("p6", id, DbType.Int32);
                        updateParams.Add("p7", alarmCount, DbType.Int32);
                        updateParams.Add("p8", alarm, DbType.String);
                        updateParams.Add("p9", comment, DbType.String);
                        updateParams.Add("p10", errorTime, DbType.Int32);
                        updateParams.Add("p11", "", DbType.String);
                        updateParams.Add("p12", id, DbType.String);


                        connection.Execute(sqlUpdate, updateParams, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
                        INSERT INTO [Error] (
                            [PlcId], 
                            [CycleId], 
                            [Device], 
                            [MnemonicId], 
                            [RecordId], 
                            [AlarmId],
                            [ErrorNum], 
                            [AlarmComment], 
                            [MessageComment],
                            [ErrorTime],
                            [ErrorTimeDevice])
                        VALUES
                            (@PlcId, 
                            @CycleId, 
                            @Device, 
                            @MnemonicId, 
                            @RecordId, 
                            @AlarmId, 
                            @ErrorNum, 
                            @AlarmComment, 
                            @MessageComment,
                            @ErrorTime,
                            @ErrorTimeDevice)",
                        parameters, transaction);
                    }
                    alarmCount++;
                }
            }
            transaction.Commit();
        }
    }
}
