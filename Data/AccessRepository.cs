using System.Data.OleDb;
using System.Reflection.PortableExecutable;

using Dapper;

using KdxDesigner.Models;

namespace KdxDesigner.Data
{
    public class AccessRepository
    {
        private readonly string _connectionString;

        public AccessRepository(string connectionString)
        {
            _connectionString = connectionString;
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

        public List<Cycle> GetCycles()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT PlcId, CycleName FROM Cycle";
            return connection.Query<Cycle>(sql).ToList();
        }

        public List<Process> GetProcesses()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, ProcessName, CycleId FROM Process";
            return connection.Query<Process>(sql).ToList();
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

        public List<Operation> GetOperations()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = @"
SELECT Id, OperationName, CYId, CategoryId, Stay, Start, Finish, Valve1,
       S1, S2, S3, S4, S5, SS1, SS2, SS3, SS4, PIL, SC, FC
FROM Operation";
            return connection.Query<Operation>(sql).ToList();
        }

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


    }
}
