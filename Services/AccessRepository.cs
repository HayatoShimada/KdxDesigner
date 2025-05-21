using Dapper;

using KdxDesigner.Models;

using System;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Transactions;

namespace KdxDesigner.Services
{
    public class AccessRepository
    {
        private readonly string? _connectionString;

        public AccessRepository()
        {
            // TEST環境ではこのパスを変更して、ACCESSファイルをTEST用にすること。
            _connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Z:\\検図\\電気設計変更用\\@04_スズキ\\KDX_Designer.accdb;";

        }

        public List<Company> GetCompanies()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, CompanyName, CreatedAt FROM Company";
            return connection.Query<Company>(sql).ToList();
        }

        public List<Model> GetModels()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, ModelName, CompanyId FROM Model";
            return connection.Query<Model>(sql).ToList();
        }

        public List<PLC> GetPLCs()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, PlcName, ModelId, Maker FROM PLC";
            return connection.Query<PLC>(sql).ToList();
        }

        // Cycle
        public List<Cycle> GetCycles()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, PlcId, CycleName FROM Cycle";
            return connection.Query<Cycle>(sql).ToList();
        }

        public List<Models.Process> GetProcesses()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, ProcessName, CycleId, TestStart, TestCondition, AutoCondition, AutoMode, AutoStart, ProcessCategory, FinishId, ILStart FROM Process";
            return connection.Query<Models.Process>(sql).ToList();
        }

        public List<Machine> GetMachines()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, MacineName, ShortName FROM Macine";
            return connection.Query<Machine>(sql).ToList();
        }

        public List<DriveMain> GetDriveMains()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, DriveMainName FROM DriveMain";
            return connection.Query<DriveMain>(sql).ToList();
        }

        public List<DriveSub> GetDriveSubs()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, DriveSubName, DriveMainId FROM DriveSub";
            return connection.Query<DriveSub>(sql).ToList();
        }

        public List<CY> GetCYs()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, PlcId, PUCO, CYNum, OilNum, MacineId, DriveSub, PlaceId, CYNameSub, SensorId, FlowType FROM CY";
            return connection.Query<CY>(sql).ToList();
        }


        // Operation
        public List<Operation> GetOperations()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = @"
SELECT Id, OperationName, CYId, CategoryId, Stay, Start, Finish, Valve1,
       S1, S2, S3, S4, S5, SS1, SS2, SS3, SS4, PIL, SC, FC
FROM Operation";
            return connection.Query<Operation>(sql).ToList();
        }

        public Operation? GetOperationById(int id)
        {
            using var connection = new OleDbConnection(_connectionString);
            return connection.QueryFirstOrDefault<Operation>(
                "SELECT * FROM Operation WHERE Id = @Id", new { Id = id });
        }

        public void UpdateOperation(Operation operation)
        {
            using var connection = new OleDbConnection(_connectionString);

            var sql = @"
UPDATE Operation SET
    OperationName = @OperationName,
    CYId = @CYId,
    CategoryId = @CategoryId,
    Stay = @Stay,
    Start = @Start,
    Finish = @Finish,
    Valve1 = @Valve1,
    S1 = @S1,
    S2 = @S2,
    S3 = @S3,
    S4 = @S4,
    S5 = @S5,
    SS1 = @SS1,
    SS2 = @SS2,
    SS3 = @SS3,
    SS4 = @SS4,
    PIL = @PIL,
    SC = @SC,
    FC = @FC
WHERE Id = @Id";

            connection.Execute(sql, operation);
        }


        // ProcessDetail
        public List<ProcessDetail> GetProcessDetails()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = @"
SELECT Id, ProcessId, OperationId, DetailName,
       StartIds, FinishIds,
       StartSensor, CategoryId, FinishSensor
FROM ProcessDetail";
            return connection.Query<ProcessDetail>(sql).ToList();
        }

        public List<ProcessDetailDto> GetProcessDetailDtos()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = @"
SELECT 
    pd.Id,
    pd.ProcessId,
    p.ProcessName,
    pd.OperationId,
    o.OperationName,
    pd.DetailName,
    pd.StartIds,
    pd.FinishIds,
    pd.StartSensor,
    pd.CategoryId,
    c.CategoryName,
    pd.FinishSensor
FROM 
    (((ProcessDetail AS pd
    LEFT JOIN Process AS p ON pd.ProcessId = p.Id)
    LEFT JOIN Operation AS o ON pd.OperationId = o.Id)
    LEFT JOIN ProcessDetailCategory AS c ON pd.CategoryId = c.Id)
";
            return connection.Query<ProcessDetailDto>(sql).ToList();
        }

        public void SaveProcessDetailDtos(List<ProcessDetailDto> details)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // ① 全削除
                var deleteSql = "DELETE FROM ProcessDetail";
                connection.Execute(deleteSql, transaction: transaction);

                // ② 全挿入
                var insertSql = @"
INSERT INTO ProcessDetail 
(Id, ProcessId, OperationId, DetailName, StartIds, FinishIds, StartSensor, CategoryId, FinishSensor)
VALUES
(@Id, @ProcessId, @OperationId, @DetailName, @StartIds, @FinishIds, @StartSensor, @CategoryId, @FinishSensor)
";

                foreach (var detail in details)
                {
                    connection.Execute(insertSql, detail, transaction: transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<IO> GetIoList()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, IOText, XComment, YComment, FComment, Address, IOName, IOExplanation, IOSpot, UnitName, System, StationNumber, IONameNaked FROM IO";
            return connection.Query<IO>(sql).ToList();
        }

        // AccessRepository.cs に以下を追加:

        public List<Memory> GetMemories(int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM Memory WHERE PlcId = @PlcId";
            return connection.Query<Memory>(sql, new { PlcId = plcId }).ToList();
        }

        public List<MemoryCategory> GetMemoryCategories()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MemoryCategory";
            return connection.Query<MemoryCategory>(sql).ToList();
        }

        public void SaveDeviceListProcess(List<Models.Process> processes, int startNum)
        {
            int count = 0;
            foreach (Models.Process process in processes)
            {
                if (process == null) continue;

                // 既にテーブルに存在するか検索
                var existing = allExisting.FirstOrDefault(m =>
                m.PlcId == memory.PlcId && m.Device == memory.Device);

                var parameters = new DynamicParameters();
                parameters.Add("NemonicId", 1, DbType.Int32);
                parameters.Add("RecordId", process.Id, DbType.Int32);
                parameters.Add("DeviceLabel", "L", DbType.String);
                parameters.Add("StartNum", (count * 10 + startNum), DbType.Int32);
                parameters.Add("OutCoilCount", 10, DbType.Int32);

                // 5/21ココマデ
                if (existing != null)
                {
                    parameters.Add("ID", existing.ID, DbType.Int64);
                    connection.Execute(@"
UPDATE [Memory] SET
    [MemoryCategory] = @MemoryCategory,
    [DeviceNumber] = @DeviceNumber,
    [DeviceNumber1] = @DeviceNumber1,
    [DeviceNumber2] = @DeviceNumber2,
    [Category] = @Category,
    [Row_1] = @Row_1,
    [Row_2] = @Row_2,
    [Row_3] = @Row_3,
    [Row_4] = @Row_4,
    [Direct_Input] = @Direct_Input,
    [Confirm] = @Confirm,
    [Note] = @Note,
    [UpdatedAt] = @UpdatedAt,
    [GOT] = @GOT
WHERE [ID] = @ID",
                    parameters, transaction);
                }
                else
                {
                    connection.Execute(@"
INSERT INTO [Memory] (
    [PlcId], [MemoryCategory], [DeviceNumber],
    [DeviceNumber1], [DeviceNumber2], [Device],
    [Category], [Row_1], [Row_2], [Row_3], [Row_4],
    [Direct_Input], [Confirm], [Note],
    [CreatedAt], [UpdatedAt], [GOT]
) VALUES (
    @PlcId, @MemoryCategory, @DeviceNumber,
    @DeviceNumber1, @DeviceNumber2, @Device,
    @Category, @Row_1, @Row_2, @Row_3, @Row_4,
    @Direct_Input, @Confirm, @Note,
    @CreatedAt, @UpdatedAt, @GOT
)",
                    parameters, transaction);
                }

                count++;
            }

        }

        public void SaveMemories(List<Memory> memories, Action<string>? progressCallback = null)
        {
            using var connection = new OleDbConnection(_connectionString);

            // ✅ 接続先 Access ファイルのパスをログ出力
            Debug.WriteLine($"[接続文字列] {_connectionString}");

            connection.Open();
            using var transaction = connection.BeginTransaction();

            var allExisting = connection.Query<Memory>("SELECT * FROM Memory", transaction: transaction).ToList();

            for (int i = 0; i < memories.Count; i++)
            {
                var memory = memories[i];
                progressCallback?.Invoke($"[{i + 1}/{memories.Count}] 保存中: {memory.Device}");

                try
                {
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    var existing = allExisting.FirstOrDefault(m =>
                        m.PlcId == memory.PlcId && m.Device == memory.Device);

                    var parameters = new DynamicParameters();
                    parameters.Add("PlcId", memory.PlcId ?? 0, DbType.Int32);
                    parameters.Add("MemoryCategory", memory.MemoryCategory ?? 0, DbType.Int32);
                    parameters.Add("DeviceNumber", memory.DeviceNumber ?? 0, DbType.Int32);
                    parameters.Add("DeviceNumber1", memory.DeviceNumber1 ?? "", DbType.String);
                    parameters.Add("DeviceNumber2", memory.DeviceNumber2 ?? "", DbType.String);
                    parameters.Add("Device", memory.Device ?? "", DbType.String);
                    parameters.Add("Category", memory.Category ?? "", DbType.String);
                    parameters.Add("Row_1", memory.Row_1 ?? "", DbType.String);
                    parameters.Add("Row_2", memory.Row_2 ?? "", DbType.String);
                    parameters.Add("Row_3", memory.Row_3 ?? "", DbType.String);
                    parameters.Add("Row_4", memory.Row_4 ?? "", DbType.String);
                    parameters.Add("Direct_Input", memory.Direct_Input ?? "", DbType.String);
                    parameters.Add("Confirm", memory.Confirm ?? "", DbType.String);
                    parameters.Add("Note", memory.Note ?? "", DbType.String);
                    parameters.Add("CreatedAt", memory.CreatedAt ?? now, DbType.String);
                    parameters.Add("UpdatedAt", now, DbType.String);
                    parameters.Add("GOT", memory.GOT ?? false, DbType.Boolean);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int64);
                        connection.Execute(@"
UPDATE [Memory] SET
    [MemoryCategory] = @MemoryCategory,
    [DeviceNumber] = @DeviceNumber,
    [DeviceNumber1] = @DeviceNumber1,
    [DeviceNumber2] = @DeviceNumber2,
    [Category] = @Category,
    [Row_1] = @Row_1,
    [Row_2] = @Row_2,
    [Row_3] = @Row_3,
    [Row_4] = @Row_4,
    [Direct_Input] = @Direct_Input,
    [Confirm] = @Confirm,
    [Note] = @Note,
    [UpdatedAt] = @UpdatedAt,
    [GOT] = @GOT
WHERE [ID] = @ID",
                        parameters, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
INSERT INTO [Memory] (
    [PlcId], [MemoryCategory], [DeviceNumber],
    [DeviceNumber1], [DeviceNumber2], [Device],
    [Category], [Row_1], [Row_2], [Row_3], [Row_4],
    [Direct_Input], [Confirm], [Note],
    [CreatedAt], [UpdatedAt], [GOT]
) VALUES (
    @PlcId, @MemoryCategory, @DeviceNumber,
    @DeviceNumber1, @DeviceNumber2, @Device,
    @Category, @Row_1, @Row_2, @Row_3, @Row_4,
    @Direct_Input, @Confirm, @Note,
    @CreatedAt, @UpdatedAt, @GOT
)",
                        parameters, transaction);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Device={memory.Device} 保存失敗 → {ex.Message}");
                    throw;
                }
            }

            transaction.Commit();

            // ✅ 挿入後、件数確認
            var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM [Memory]");
            Debug.WriteLine($"[確認] Memory テーブルのレコード数: {count}");
        }




    }
}
