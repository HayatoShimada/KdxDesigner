using Dapper;

using KdxDesigner.Models;

using System.Data;
using System.Data.OleDb;

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

        public List<Cycle> GetCycles()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM Cycle";
            return connection.Query<Cycle>(sql).ToList();
        }

        public List<Models.Process> GetProcesses()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM Process";
            return connection.Query<Models.Process>(sql).ToList();
        }

        public List<Models.Machine> GetMachines()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT Id, MacineName, ShortName FROM Macine";
            return connection.Query<Models.Machine>(sql).ToList();
        }

        public Models.Machine? GetMachineById(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            return connection.QueryFirstOrDefault<Models.Machine>(
                "SELECT * FROM Macine WHERE Id = @Id", new { Id = id });
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

        public DriveSub? GetDriveSubById(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            return connection.QueryFirstOrDefault<DriveSub>(
                "SELECT * FROM DriveSub WHERE Id = @Id", new { Id = id });
        }

        public List<CY> GetCYs()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM CY";
            return connection.Query<CY>(sql).ToList();
        }

        public CY? GetCYById(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            return connection.QueryFirstOrDefault<CY>(
                "SELECT * FROM CY WHERE Id = @Id", new { Id = id });
        }

        public List<Models.Timer> GetTimers()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM Timer";
            return connection.Query<Models.Timer>(sql).ToList();
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
            var sql = @"SELECT * FROM Operation";
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
            var sql = "SELECT * FROM ProcessDetail";
            return connection.Query<ProcessDetail>(sql).ToList();
        }

        public List<ProcessDetailCategory> GetProcessDetailCategories()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM ProcessDetailCategory";
            return connection.Query<ProcessDetailCategory>(sql).ToList();
        }

        public ProcessDetailCategory? GetProcessDetailCategoryById(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            return connection.QueryFirstOrDefault<ProcessDetailCategory>(
                "SELECT * FROM ProcessDetailCategory WHERE ID = @ID", new { ID = id });
        }

        public void UpdateProcessDetail(ProcessDetail processDetail)
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // ProcessFlowViewで編集される主要なフィールドのみを更新
                // Dapperの疑似位置パラメータ構文を使用（OleDb対応）
                var sql = @"
UPDATE ProcessDetail SET
    StartIds = ?StartIds?, 
    StartSensor = ?StartSensor?
WHERE Id = ?Id?";

                connection.Execute(sql, new
                {
                    StartIds = processDetail.StartIds ?? "",
                    StartSensor = processDetail.StartSensor ?? "",
                    Id = processDetail.Id
                }, transaction);
                
                transaction.Commit();
                
                System.Diagnostics.Debug.WriteLine($"ProcessDetail updated successfully - Id: {processDetail.Id}, StartIds: {processDetail.StartIds}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"UpdateProcessDetail error for Id {processDetail.Id}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SQL: UPDATE ProcessDetail SET StartIds = '{processDetail.StartIds}', StartSensor = '{processDetail.StartSensor}' WHERE Id = {processDetail.Id}");
                throw;
            }
        }


        public List<IO> GetIoList()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM IO";
            return connection.Query<IO>(sql).ToList();
        }

        public List<TimerCategory> GetTimerCategory()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM TimerCategory";
            return connection.Query<TimerCategory>(sql).ToList();
        }

        public List<Servo> GetServos(int? plcId, int? cylinderId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = string.Empty;
            if (plcId == null && cylinderId == null)
            {
                sql = "SELECT * FROM Servo";

            }
            else if (plcId != null && cylinderId == null)
            {
                sql = "SELECT * FROM Servo WHERE PlcId = @PlcId";
            }
            else
            {
                sql = plcId == null && cylinderId != null
                    ? "SELECT * FROM Servo WHERE CylinderId = @CylinderId"
                    : "SELECT * FROM Servo WHERE PlcId = @PlcId AND CylinderId = @CylinderId";
            }
            return connection.Query<Servo>(sql, new { PlcId = plcId, CycleId = cylinderId }).ToList();
        }

        public void UpdateIoLinkDevices(IEnumerable<IO> ioRecordsToUpdate)
        {
            if (!ioRecordsToUpdate.Any()) return;

            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // Idをキーに、LinkDeviceを更新する
                // Dapperの疑似位置パラメータ構文を使用（OleDb対応）
                const string sql = "UPDATE [IO] SET [LinkDevice] = ?LinkDevice? WHERE [Id] = ?Id?";

                // DapperのExecuteはリストを渡すと自動的にループ処理してくれる
                connection.Execute(sql, ioRecordsToUpdate.Select(io => new { LinkDevice = io.LinkDevice, Id = io.Id }), transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw; // エラーを上位に通知
            }
        }

        /// <summary>
        /// IOレコードのリストを更新し、同時に変更履歴を保存します。
        /// これらの一連の処理は単一のトランザクション内で実行されます。
        /// </summary>
        public void UpdateAndLogIoChanges(List<IO> iosToUpdate, List<IOHistory> histories)
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. IOテーブルの更新
                if (iosToUpdate.Any())
                {
                    // Dapperの疑似位置パラメータ構文を使用（OleDb対応）
                    var sqlUpdate = @"UPDATE IO SET 
                                        IOText = ?IOText?, XComment = ?XComment?, YComment = ?YComment?, 
                                        FComment = ?FComment?, Address = ?Address?, IOName = ?IOName?, 
                                        IOExplanation = ?IOExplanation?, IOSpot = ?IOSpot?, UnitName = ?UnitName?, 
                                        System = ?System?, StationNumber = ?StationNumber?, IONameNaked = ?IONameNaked?, 
                                        PlcId = ?PlcId?, LinkDevice = ?LinkDevice?
                                    WHERE Id = ?Id?";

                    foreach (var io in iosToUpdate)
                    {
                        connection.Execute(sqlUpdate, new
                        {
                            IOText = io.IOText ?? "",
                            XComment = io.XComment ?? "",
                            YComment = io.YComment ?? "",
                            FComment = io.FComment ?? "",
                            Address = io.Address ?? "",
                            IOName = io.IOName ?? "",
                            IOExplanation = io.IOExplanation ?? "",
                            IOSpot = io.IOSpot ?? "",
                            UnitName = io.UnitName ?? "",
                            System = io.System ?? "",
                            StationNumber = io.StationNumber ?? "",
                            IONameNaked = io.IONameNaked ?? "",
                            PlcId = io.PlcId ?? 0,
                            LinkDevice = io.LinkDevice ?? "",
                            Id = io.Id
                        }, transaction);
                    }
                }

                // 2. IOHistoryテーブルへの挿入
                if (histories.Any())
                {
                    // Dapperの疑似位置パラメータ構文を使用（OleDb対応）
                    var sqlInsertHistory = @"INSERT INTO IOHistory 
                                               (IoId, PropertyName, OldValue, NewValue, ChangedAt, ChangedBy) 
                                           VALUES 
                                               (?IoId?, ?PropertyName?, ?OldValue?, ?NewValue?, ?ChangedAt?, ?ChangedBy?)";

                    // 各履歴をループし、匿名オブジェクトで実行
                    foreach (var history in histories)
                    {
                        connection.Execute(sqlInsertHistory, new
                        {
                            IoId = history.IoId,
                            PropertyName = history.PropertyName,
                            OldValue = history.OldValue,
                            NewValue = history.NewValue,
                            ChangedAt = history.ChangedAt,
                            ChangedBy = history.ChangedBy
                        }, transaction);
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw; // エラーを上位に通知
            }
        }
    }
}
