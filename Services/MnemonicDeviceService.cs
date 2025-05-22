using Dapper;

using KdxDesigner.Models;

using System.Data;
using System.Data.OleDb;

namespace KdxDesigner.Services
{
    internal class MnemonicDeviceService
    {
        private readonly string _connectionString;

        public MnemonicDeviceService(AccessRepository repository)
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

        // Processesのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceProcess(List<Models.Process> processes, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            // MnemonicDeviceテーブルの既存データを取得
            var allExisting = GetMnemonicDevice(plcId);

            int count = 0;
            foreach (Models.Process process in processes)
            {
                if (process == null) continue;

                var existing = allExisting.FirstOrDefault(m =>
                    m.PlcId == plcId && m.RecordId == process.Id);

                var parameters = new DynamicParameters();
                parameters.Add("MnemonicId", 1, DbType.Int32);
                parameters.Add("RecordId", process.Id, DbType.Int32);
                parameters.Add("DeviceLabel", "L", DbType.String);
                parameters.Add("StartNum", (count * 5 + startNum), DbType.Int32);
                parameters.Add("OutCoilCount", 5, DbType.Int32);
                parameters.Add("PlcId", plcId, DbType.Int32);
                parameters.Add("Comment", process.ProcessName, DbType.String);

                if (existing != null)
                {
                    parameters.Add("ID", existing.ID, DbType.Int64);
                    connection.Execute(@"
                        UPDATE [MnemonicDevice] SET
                            [MnemonicId] = @NemonicId,
                            [RecordId] = @RecordId,
                            [DeviceLabel] = @DeviceLabel,
                            [StartNum] = @StartNum,
                            [OutCoilCount] = @OutCoilCount,
                            [PlcId] = @PlcId
                            [Comment] = @Comment
                        WHERE [ID] = @ID",
                        parameters, transaction);
                }
                else
                {
                    connection.Execute(@"
                        INSERT INTO [MnemonicDevice] (
                            [MnemonicId], 
                            [RecordId], 
                            [DeviceLabel], 
                            [StartNum], 
                            [OutCoilCount], 
                            [PlcId], 
                            [Comment]
                        ) VALUES (
                            @NemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment
                        )",
                        parameters, transaction);
                }
                count++;
            }

            transaction.Commit();
        }

        // ProcessDetailのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceProcessDetail(List<ProcessDetailDto> processes, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            // MnemonicDeviceテーブルの既存データを取得
            var allExisting = GetMnemonicDevice(plcId);

            int count = 0;
            foreach (Models.ProcessDetailDto process in processes)
            {
                if (process == null) continue;

                var existing = allExisting.FirstOrDefault(m =>
                    m.PlcId == plcId && m.RecordId == process.Id);

                var parameters = new DynamicParameters();
                parameters.Add("MnemonicId", 2, DbType.Int32);
                parameters.Add("RecordId", process.Id, DbType.Int32);
                parameters.Add("DeviceLabel", "L", DbType.String);
                parameters.Add("StartNum", (count * 10 + startNum), DbType.Int32);
                parameters.Add("OutCoilCount", 10, DbType.Int32);
                parameters.Add("PlcId", plcId, DbType.Int32);

                if (existing != null)
                {
                    parameters.Add("ID", existing.ID, DbType.Int64);
                    connection.Execute(@"
                        UPDATE [MnemonicDevice] SET
                            [MnemonicId] = @NemonicId,
                            [RecordId] = @RecordId,
                            [DeviceLabel] = @DeviceLabel,
                            [StartNum] = @StartNum,
                            [OutCoilCount] = @OutCoilCount,
                            [PlcId] = @PlcId
                        WHERE [ID] = @ID",
                        parameters, transaction);
                }
                else
                {
                    connection.Execute(@"
                        INSERT INTO [MnemonicDevice] (
                            [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId]
                        ) VALUES (
                            @NemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId
                        )",
                        parameters, transaction);
                }

                count++;
            }

            transaction.Commit();
        }

    }
}
