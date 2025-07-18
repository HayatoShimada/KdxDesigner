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
                const string sql = "UPDATE [IO] SET [LinkDevice] = @LinkDevice WHERE [Id] = @Id";

                // DapperのExecuteはリストを渡すと自動的にループ処理してくれる
                connection.Execute(sql, ioRecordsToUpdate.Select(io => new { io.LinkDevice, io.Id }), transaction);

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
                    // ★★★ 修正箇所 1: SQL文を位置プレースホルダ (?) に変更 ★★★
                    var sqlUpdate = @"UPDATE IO SET 
                                        IOText = ?, XComment = ?, YComment = ?, 
                                        FComment = ?, Address = ?, IOName = ?, 
                                        IOExplanation = ?, IOSpot = ?, UnitName = ?, 
                                        System = ?, StationNumber = ?, IONameNaked = ?, 
                                        PlcId = ?, LinkDevice = ?
                                    WHERE Id = ?";

                    foreach (var io in iosToUpdate)
                    {
                        var parameters = new DynamicParameters();

                        // ★★★ 修正箇所 2: パラメータを追加する順番をSQL文と完全に一致させる ★★★
                        // --- SET句のパラメータ ---
                        parameters.Add("p1", io.IOText ?? "", DbType.String);
                        parameters.Add("p2", io.XComment ?? "", DbType.String);
                        parameters.Add("p3", io.YComment ?? "", DbType.String);
                        parameters.Add("p4", io.FComment ?? "", DbType.String);
                        parameters.Add("p5", io.Address ?? "", DbType.String);
                        parameters.Add("p6", io.IOName ?? "", DbType.String);
                        parameters.Add("p7", io.IOExplanation ?? "", DbType.String);
                        parameters.Add("p8", io.IOSpot ?? "", DbType.String);
                        parameters.Add("p9", io.UnitName ?? "", DbType.String);
                        parameters.Add("p10", io.System ?? "", DbType.String);
                        parameters.Add("p11", io.StationNumber ?? "", DbType.String);
                        parameters.Add("p12", io.IONameNaked ?? "", DbType.String);
                        parameters.Add("p13", io.PlcId ?? 0, DbType.Int32);
                        parameters.Add("p14", io.LinkDevice ?? "", DbType.String);
                        // --- WHERE句のパラメータ ---
                        parameters.Add("p15", io.Id, DbType.Int32);

                        connection.Execute(sqlUpdate, parameters, transaction);
                    }
                }

                // 2. IOHistoryテーブルへの挿入
                if (histories.Any())
                {
                    var sqlInsertHistory = @"INSERT INTO IOHistory 
                                               (IoId, PropertyName, OldValue, NewValue, ChangedAt, ChangedBy) 
                                           VALUES 
                                               (@IoId, @PropertyName, @OldValue, @NewValue, @ChangedAt, @ChangedBy)";

                    // 各履歴をループし、DynamicParametersを使って型を明示的に指定する
                    foreach (var history in histories)
                    {
                        var historyParams = new DynamicParameters();
                        historyParams.Add("@IoId", history.IoId, DbType.Int32);
                        historyParams.Add("@PropertyName", history.PropertyName, DbType.String);
                        historyParams.Add("@OldValue", history.OldValue, DbType.String);
                        historyParams.Add("@NewValue", history.NewValue, DbType.String);
                        // DateTimeオブジェクトをDbType.DateTimeとして渡す
                        historyParams.Add("@ChangedAt", history.ChangedAt, DbType.String);
                        historyParams.Add("@ChangedBy", history.ChangedBy, DbType.String);

                        connection.Execute(sqlInsertHistory, historyParams, transaction);
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
