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
        public void SaveProsTime(List<Operation> operations, int startCurrent, int startPrevious, int startCylinder, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            // 修正: List<T> の Sort メソッドを使用するために、ラムダ式を Comparison<T> に変換する必要があります。
            // 変更前: operations = operations.Sort(o => o.Id);
            operations.Sort((o1, o2) => o1.Id.CompareTo(o2.Id));

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
                        prosTimeCount = 5;
                        break;
                    case 3 or 9 or 15 or 27: // 速度制御INV1
                        prosTimeCount = 5 + 2;
                        break;
                    case 4 or 10 or 16 or 28: // 速度制御INV2
                        prosTimeCount = 5 + 4;
                        break;
                    case 5 or 11 or 17:     // 速度制御INV3
                        prosTimeCount = 5 + 6;
                        break;
                    case 6 or 12 or 18: // 速度制御INV4
                        prosTimeCount = 5 + 8;
                        break;
                    case 7 or 13 or 19: // 速度制御INV5
                        prosTimeCount = 5 + 12;
                        break;
                    case 31:            // サーボ
                        prosTimeCount = 5;
                        break;
                    default:
                        prosTimeCount = 0;
                        break;
                }

                for (int  i = 0; i < prosTimeCount; i++)
                {
                    string currentDevice = "ZR" + (startCurrent + count ).ToString(); 
                    string previousDevice = "ZR" + (startPrevious + count).ToString();
                    string cylinderDevice = "ZR" + (startCylinder + count).ToString();

                    count++;
                    var parameters = new DynamicParameters();

                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("MnemonicId", (int)MnemonicType.Operation, DbType.Int32);
                    parameters.Add("RecordId", operation.Id, DbType.Int32);
                    parameters.Add("SortId", i, DbType.Int32);
                    parameters.Add("CurrentDevice", currentDevice, DbType.String);
                    parameters.Add("PreviousDevice", previousDevice, DbType.String);
                    parameters.Add("CylinderDevice", cylinderDevice, DbType.String);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                        UPDATE [ProsTime] SET
                            [PlcId] = @PlcId,
                            [MnemonicId] = @MnemonicId,
                            [RecordId] = @RecordId,
                            [SortId] = @SortId,
                            [CurrentDevice] = @CurrentDevice,
                            [PreviousDevice] = @PreviousDevice,
                            [CylinderDevice] = @CylinderDevice
                        WHERE [ID] = @ID",
                            parameters, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
    INSERT INTO [ProsTime] (
        [PlcId], 
        [MnemonicId], 
        [RecordId], 
        [SortId],
        [CurrentDevice], 
        [PreviousDevice],
        [CylinderDevice]        
)
    VALUES
        (@PlcId, 
        @MnemonicId, 
        @RecordId, 
        @SortId, 
        @CurrentDevice,
        @PreviousDevice,
        @CylinderDevice
)",
    parameters, transaction);

                    }
                }
            }
            transaction.Commit();
        }
    }
}
