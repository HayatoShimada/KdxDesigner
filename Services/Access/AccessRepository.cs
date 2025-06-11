using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Utils.ini;

using Microsoft.Win32; // ← ファイル選択ダイアログ用

using System.Data.OleDb;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows;



namespace KdxDesigner.Services.Access
{
    public class AccessRepository : IAccessRepository
    {
        // 接続文字列をプロパティとして公開
        public string ConnectionString { get; }

        // コンストラクタで接続文字列を受け取る
        public AccessRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            }
            ConnectionString = connectionString;
        }


        public List<Company> GetCompanies()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, CompanyName, CreatedAt FROM Company";
            return connection.Query<Company>(sql).ToList();
        }

        public List<Model> GetModels()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, ModelName, CompanyId FROM Model";
            return connection.Query<Model>(sql).ToList();
        }

        public List<PLC> GetPLCs()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, PlcName, ModelId, Maker FROM PLC";
            return connection.Query<PLC>(sql).ToList();
        }

        // Cycle
        public List<Cycle> GetCycles()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM Cycle";
            return connection.Query<Cycle>(sql).ToList();
        }

        public List<Models.Process> GetProcesses()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, ProcessName, CycleId, TestStart, TestCondition, TestMode, AutoCondition, AutoMode, AutoStart, ProcessCategoryId, FinishId, ILStart FROM Process";
            return connection.Query<Models.Process>(sql).ToList();
        }

        public List<Models.Machine> GetMachines()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, MacineName, ShortName FROM Macine";
            return connection.Query<Models.Machine>(sql).ToList();
        }

        public List<DriveMain> GetDriveMains()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, DriveMainName FROM DriveMain";
            return connection.Query<DriveMain>(sql).ToList();
        }

        public List<DriveSub> GetDriveSubs()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, DriveSubName, DriveMainId FROM DriveSub";
            return connection.Query<DriveSub>(sql).ToList();
        }

        public List<CY> GetCYs()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM CY";
            return connection.Query<CY>(sql).ToList();
        }
        public List<Models.Timer> GetTimersByCycleId(int cycleId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM Timer WHERE CycleId = @CycleId";
            return connection.Query<Models.Timer>(sql, new { CycleId = cycleId }).ToList();
        }

        // Operation
        public List<Operation> GetOperations()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = @"
SELECT Id, OperationName, CYId, CategoryId, GoBack, Start, Finish, Valve1,
       S1, S2, S3, S4, S5, SS1, SS2, SS3, SS4, PIL, SC, FC
FROM Operation";
            return connection.Query<Operation>(sql).ToList();
        }

        public Operation? GetOperationById(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            return connection.QueryFirstOrDefault<Operation>(
                "SELECT * FROM Operation WHERE Id = @Id", new { Id = id });
        }

        public List<Length>? GetLengthByPlcId(int plcId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            return connection.Query<Length>(
                "SELECT * FROM Length WHERE PlcId = @PlcId", new { PlcId = plcId }).ToList();
        }

        public void UpdateOperation(Operation operation)
        {
            using var connection = new OleDbConnection(ConnectionString);

            var sql = @"
UPDATE Operation SET
    OperationName = @OperationName,
    CYId = @CYId,
    CategoryId = @CategoryId,
    GoBack = @GoBack,
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
            using var connection = new OleDbConnection(ConnectionString);
            var sql = @"
SELECT Id, ProcessId, OperationId, DetailName,
       StartIds, FinishIds,
       
, CategoryId, FinishSensor
FROM ProcessDetail";
            return connection.Query<ProcessDetail>(sql).ToList();
        }

        public List<ProcessDetailDto> GetProcessDetailDtos()
        {
            using var connection = new OleDbConnection(ConnectionString);
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
    pd.FinishSensor,
    p.CycleId As CycleId
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
            using var connection = new OleDbConnection(ConnectionString);
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
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, IOText, XComment, YComment, FComment, Address, IOName, IOExplanation, IOSpot, UnitName, System, StationNumber, IONameNaked FROM IO";
            return connection.Query<IO>(sql).ToList();
        }

        public List<TimerCategory> GetTimerCategory()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM TimerCategory";
            return connection.Query<TimerCategory>(sql).ToList();
        }

    }

}
