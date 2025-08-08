using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Services.Access;

using System.Data;
using System.Data.OleDb;

namespace KdxDesigner.Services.MnemonicSpeedDevice
{
    /// <summary>
    /// ニーモニック速度デバイスのデータ操作を行うサービス実装
    /// </summary>
    internal class MnemonicSpeedDeviceService : IMnemonicSpeedDeviceService
    {
        private readonly string _connectionString;

        public MnemonicSpeedDeviceService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        public void DeleteSpeedTable()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "DELETE FROM MnemonicSpeedDevice";
            connection.Execute(sql);

        }

        // MnemonicDeviceテーブルからPlcIdとCycleIdに基づいてデータを取得する
        public List<Models.MnemonicSpeedDevice> GetMnemonicSpeedDevice(int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicSpeedDevice WHERE PlcId = @PlcId";
            return connection.Query<Models.MnemonicSpeedDevice>(sql, new { PlcId = plcId }).ToList();
        }

        public void Save(
            List<CY> cys,
            int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            // MnemonicDeviceテーブルの既存データを取得
            var allExisting = GetMnemonicSpeedDevice(plcId);

            int count = 0;
            foreach (CY cy in cys)
            {
                if (cy == null) continue;
                var existing = allExisting.SingleOrDefault(m => m.CylinderId == cy.Id);
                var speedDevice = "D" + (startNum + count).ToString();

                var parameters = new DynamicParameters();
                parameters.Add("CylinderId", cy.Id, DbType.Int32);
                parameters.Add("Device", speedDevice, DbType.String);
                parameters.Add("PlcId", plcId, DbType.Int32);

                if (existing != null)
                {
                    parameters.Add("ID", existing.ID, DbType.Int32);
                    connection.Execute(@"
                        UPDATE [MnemonicSpeedDevice] SET
                            [CylinderId] = @CylinderId,
                            [Device] = @Device,
                            [PlcId] = @PlcId
                        WHERE [ID] = @ID",
                        parameters, transaction);
                }
                else
                {
                    connection.Execute(@"
                        INSERT INTO [MnemonicSpeedDevice] (
                            [CylinderId], [Device], [PlcId]
                        ) VALUES (
                            @CylinderId, @Device, @PlcId
                        )",
                        parameters, transaction);
                }
                count++;
            }

            transaction.Commit();
        }



    }
}
