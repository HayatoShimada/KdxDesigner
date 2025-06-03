using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;

using System.Data;
using System.Data.OleDb;

namespace KdxDesigner.Services
{
    internal class MnemonicTimerDeviceService
    {
        private readonly string _connectionString;

        public MnemonicTimerDeviceService(AccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        // MnemonicDeviceテーブルからPlcIdとCycleIdに基づいてデータを取得する
        public List<MnemonicTimerDevice> GetMnemonicTimerDevice(int plcId, int cycleId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicTimerDevice WHERE PlcId = @PlcId AND CycleId = @CycleId";
            return connection.Query<MnemonicTimerDevice>(sql, new { PlcId = plcId, CycleId = cycleId }).ToList();
        }

        // MnemonicDeviceテーブルからPlcIdとMnemonicIdに基づいてデータを取得する
        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByMnemonic(int plcId, int cycleId, int mnemonicId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicTimerDevice WHERE PlcId = @PlcId AND CycleId = @CycleId AND MnemonicId = @MnemonicId";
            return connection.Query<MnemonicTimerDevice>(sql, new { PlcId = plcId, CycleId = cycleId, MnemonicId = mnemonicId }).ToList();
        }

        // Operationのリストを受け取り、MnemonicTimerDeviceテーブルに保存する
        public void SaveWithOperation(
            List<Models.Timer> timers, 
            List<Operation> operations, 
            int startNum, int plcId, int cycleId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            // MnemonicDeviceテーブルの既存データを取得
            var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, cycleId, (int)MnemonicType.Operation);
            int count = 0;
            foreach (Operation operation in operations)
            {
                if (operation == null) continue;
                var existing = allExisting.FirstOrDefault(m => m.RecordId == operation.Id);
                var operationTimers = timers.Where(t => t.OperationId == operation.Id).ToList();
                if (operationTimers.Count == 0) continue; // Operationに関連するタイマーがない場合はスキップ

                foreach (Models.Timer timer in operationTimers)
                {
                    if (timer == null) continue;
                    var processTimerDevice = "T" + (startNum + count).ToString() ?? string.Empty;
                    var timerDevice = "ZR" + timer.TimerNum.ToString() ?? string.Empty;

                    var parameters = new DynamicParameters();
                    parameters.Add("MnemonicId", (int)MnemonicType.Operation, DbType.Int32);
                    parameters.Add("RecordId", operation.Id, DbType.Int32);
                    parameters.Add("TimerId", timer.ID, DbType.Int32);
                    parameters.Add("TimerCategoryId", timer.TimerCategoryId, DbType.Int32);
                    parameters.Add("ProcessTimerDevice", processTimerDevice, DbType.String);
                    parameters.Add("TimerDevice", timerDevice, DbType.String);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("CycleId", cycleId, DbType.Int32);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                            UPDATE [MnemonicTimerDevice] SET
                                [MnemonicId] = @MnemonicId,
                                [RecordId] = @RecordId,
                                [TimerId] = @TimerId,
                                [TimerCategoryId] = @ProcessTimerNum,
                                [ProcessTimerDevice] = @ProcessTimerDevice,
                                [TimerDevice] = @TimerDevice,
                                [PlcId] = @PlcId,
                                [CycleId] = @CycleId
                            WHERE [ID] = @ID",
                            parameters, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
                            INSERT INTO [MnemonicTimerDevice] (
                                [MnemonicId], [RecordId], [TimerId], [TimerCategoryId], [ProcessTimerDevice], [TimerDevice], [PlcId], [CycleId]
                            ) VALUES (
                                @MnemonicId, @RecordId, @TimerId, @TimerCategoryId, @ProcessTimerDevice, @TimerDevice, @PlcId, @CycleId
                            )",
                            parameters, transaction);
                    }
                    count++;
                }
            }

            transaction.Commit();
        }



    }
}
