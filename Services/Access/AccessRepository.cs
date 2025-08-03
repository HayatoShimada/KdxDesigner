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
        
        public List<CY> GetCyList(int plcId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM CY WHERE PlcId = @PlcId ORDER BY CYNum";
            return connection.Query<CY>(sql, new { PlcId = plcId }).ToList();
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

        public void AddTimer(Models.Timer timer)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = @"
INSERT INTO Timer (ID, CycleId, TimerCategoryId, TimerNum, TimerName, MnemonicId, Example)
VALUES (?, ?, ?, ?, ?, ?, ?)";
            
            var parameters = new
            {
                timer.ID,
                timer.CycleId,
                timer.TimerCategoryId,
                timer.TimerNum,
                timer.TimerName,
                timer.MnemonicId,
                timer.Example
            };
            
            connection.Execute(sql, parameters);
        }

        public void UpdateTimer(Models.Timer timer)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = @"
UPDATE Timer SET
    CycleId = ?,
    TimerCategoryId = ?,
    TimerNum = ?,
    TimerName = ?,
    MnemonicId = ?,
    Example = ?
WHERE ID = ?";
            
            var parameters = new
            {
                timer.CycleId,
                timer.TimerCategoryId,
                timer.TimerNum,
                timer.TimerName,
                timer.MnemonicId,
                timer.Example,
                timer.ID
            };
            
            connection.Execute(sql, parameters);
        }

        public void DeleteTimer(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "DELETE FROM Timer WHERE ID = @ID";
            connection.Execute(sql, new { ID = id });
        }

        public List<int> GetTimerRecordIds(int timerId)
        {
            try
            {
                using var connection = new OleDbConnection(ConnectionString);
                var sql = "SELECT RecordId FROM TimerRecordIds WHERE TimerId = @TimerId";
                return connection.Query<int>(sql, new { TimerId = timerId }).ToList();
            }
            catch (OleDbException)
            {
                // テーブルが存在しない場合は空のリストを返す
                return new List<int>();
            }
        }

        public void AddTimerRecordId(int timerId, int recordId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "INSERT INTO TimerRecordIds (TimerId, RecordId) VALUES (@TimerId, @RecordId)";
            connection.Execute(sql, new { TimerId = timerId, RecordId = recordId });
        }

        public void DeleteTimerRecordId(int timerId, int recordId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "DELETE FROM TimerRecordIds WHERE TimerId = @TimerId AND RecordId = @RecordId";
            connection.Execute(sql, new { TimerId = timerId, RecordId = recordId });
        }

        public void DeleteAllTimerRecordIds(int timerId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "DELETE FROM TimerRecordIds WHERE TimerId = @TimerId";
            connection.Execute(sql, new { TimerId = timerId });
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
                // すべてのフィールドを更新
                // Dapperの疑似位置パラメータ構文を使用（OleDb対応）
                var sql = @"
UPDATE ProcessDetail SET
    DetailName = ?DetailName?,
    OperationId = ?OperationId?,
    StartSensor = ?StartSensor?,
    FinishSensor = ?FinishSensor?,
    CategoryId = ?CategoryId?,
    BlockNumber = ?BlockNumber?,
    SkipMode = ?SkipMode?,
    SortNumber = ?SortNumber?,
    Comment = ?Comment?,
    ILStart = ?ILStart?
WHERE Id = ?Id?";

                connection.Execute(sql, new
                {
                    DetailName = processDetail.DetailName ?? "",
                    OperationId = processDetail.OperationId,
                    StartSensor = processDetail.StartSensor ?? "",
                    FinishSensor = processDetail.FinishSensor ?? "",
                    CategoryId = processDetail.CategoryId,
                    BlockNumber = processDetail.BlockNumber,
                    SkipMode = processDetail.SkipMode ?? "",
                    SortNumber = processDetail.SortNumber,
                    Comment = processDetail.Comment ?? "",
                    ILStart = processDetail.ILStart ?? "",
                    Id = processDetail.Id
                }, transaction);
                
                transaction.Commit();
                
                System.Diagnostics.Debug.WriteLine($"ProcessDetail updated successfully - Id: {processDetail.Id}, StartSensor: {processDetail.StartSensor}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"UpdateProcessDetail error for Id {processDetail.Id}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SQL: UPDATE ProcessDetail SET StartSensor = '{processDetail.StartSensor}' WHERE Id = {processDetail.Id}");
                throw;
            }
        }


        public int AddProcessDetail(ProcessDetail processDetail)
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // 新しい工程詳細を挿入
                var sql = @"
INSERT INTO ProcessDetail (
    ProcessId, OperationId, DetailName, StartSensor, 
    CategoryId, FinishSensor, BlockNumber, SkipMode, 
    CycleId, SortNumber, Comment, ILStart
) VALUES (
    ?ProcessId?, ?OperationId?, ?DetailName?, ?StartSensor?, 
    ?CategoryId?, ?FinishSensor?, ?BlockNumber?, ?SkipMode?, 
    ?CycleId?, ?SortNumber?, ?Comment?, ?ILStart?
)";
                connection.Execute(sql, new
                {
                    processDetail.ProcessId,
                    processDetail.OperationId,
                    processDetail.DetailName,
                    processDetail.StartSensor,
                    processDetail.CategoryId,
                    processDetail.FinishSensor,
                    processDetail.BlockNumber,
                    processDetail.SkipMode,
                    processDetail.CycleId,
                    processDetail.SortNumber,
                    processDetail.Comment,
                    processDetail.ILStart
                }, transaction);

                // 挿入されたレコードのIDを取得
                var getIdSql = "SELECT @@IDENTITY";
                var newId = connection.QuerySingle<int>(getIdSql, transaction: transaction);

                transaction.Commit();
                return newId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteProcessDetail(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // 関連するレコードを先に削除
                // ProcessDetailConnectionテーブルから削除
                var deleteConnectionsSql = @"
                    DELETE FROM ProcessDetailConnection 
                    WHERE FromProcessDetailId = ?fromId? OR ToProcessDetailId = ?toId?";
                connection.Execute(deleteConnectionsSql, new { fromId = id, toId = id }, transaction);

                // ProcessDetailFinishテーブルから削除
                var deleteFinishesSql = @"
                    DELETE FROM ProcessDetailFinish 
                    WHERE ProcessDetailId = ?processId? OR FinishProcessDetailId = ?finishId?";
                connection.Execute(deleteFinishesSql, new { processId = id, finishId = id }, transaction);

                // ProcessDetailテーブルから削除
                var sql = "DELETE FROM ProcessDetail WHERE Id = ?id?";
                connection.Execute(sql, new { id }, transaction);

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
                // AddressとPlcIdをキーに、LinkDeviceを更新する
                // Dapperの疑似位置パラメータ構文を使用（OleDb対応）
                const string sql = "UPDATE [IO] SET [LinkDevice] = ?LinkDevice? WHERE [Address] = ?Address? AND [PlcId] = ?PlcId?";

                // DapperのExecuteはリストを渡すと自動的にループ処理してくれる
                connection.Execute(sql, ioRecordsToUpdate.Select(io => new { LinkDevice = io.LinkDevice, Address = io.Address, PlcId = io.PlcId }), transaction);

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
                                        FComment = ?FComment?, IOName = ?IOName?, 
                                        IOExplanation = ?IOExplanation?, IOSpot = ?IOSpot?, UnitName = ?UnitName?, 
                                        System = ?System?, StationNumber = ?StationNumber?, IONameNaked = ?IONameNaked?, 
                                        LinkDevice = ?LinkDevice?
                                    WHERE Address = ?Address? AND PlcId = ?PlcId?";

                    foreach (var io in iosToUpdate)
                    {
                        connection.Execute(sqlUpdate, new
                        {
                            IOText = io.IOText ?? "",
                            XComment = io.XComment ?? "",
                            YComment = io.YComment ?? "",
                            FComment = io.FComment ?? "",
                            IOName = io.IOName ?? "",
                            IOExplanation = io.IOExplanation ?? "",
                            IOSpot = io.IOSpot ?? "",
                            UnitName = io.UnitName ?? "",
                            System = io.System ?? "",
                            StationNumber = io.StationNumber ?? "",
                            IONameNaked = io.IONameNaked ?? "",
                            LinkDevice = io.LinkDevice ?? "",
                            Address = io.Address,
                            PlcId = io.PlcId
                        }, transaction);
                    }
                }

                // 2. IOHistoryテーブルへの挿入
                if (histories.Any())
                {
                    // Dapperの疑似位置パラメータ構文を使用（OleDb対応）
                    var sqlInsertHistory = @"INSERT INTO IOHistory 
                                               (IoAddress, IoPlcId, PropertyName, OldValue, NewValue, ChangedAt, ChangedBy) 
                                           VALUES 
                                               (?IoAddress?, ?IoPlcId?, ?PropertyName?, ?OldValue?, ?NewValue?, ?ChangedAt?, ?ChangedBy?)";

                    // 各履歴をループし、匿名オブジェクトで実行
                    foreach (var history in histories)
                    {
                        connection.Execute(sqlInsertHistory, new
                        {
                            IoAddress = history.IoAddress,
                            IoPlcId = history.IoPlcId,
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

        // ProcessDetailConnection
        public List<ProcessDetailConnection> GetProcessDetailConnections(int cycleId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = @"
                SELECT pdc.* 
                FROM ProcessDetailConnection pdc
                INNER JOIN ProcessDetail pd ON pdc.ToProcessDetailId = pd.Id
                WHERE pd.CycleId = ?CycleId?";
            return connection.Query<ProcessDetailConnection>(sql, new { CycleId = cycleId }).ToList();
        }

        public List<ProcessDetailConnection> GetConnectionsByToId(int toProcessDetailId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM ProcessDetailConnection WHERE ToProcessDetailId = ?ToProcessDetailId?";
            return connection.Query<ProcessDetailConnection>(sql, new { ToProcessDetailId = toProcessDetailId }).ToList();
        }

        public List<ProcessDetailConnection> GetConnectionsByFromId(int fromProcessDetailId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM ProcessDetailConnection WHERE FromProcessDetailId = ?FromProcessDetailId?";
            return connection.Query<ProcessDetailConnection>(sql, new { FromProcessDetailId = fromProcessDetailId }).ToList();
        }

        public void AddProcessDetailConnection(ProcessDetailConnection connection)
        {
            using var conn = new OleDbConnection(ConnectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                var sql = @"
                    INSERT INTO ProcessDetailConnection 
                    (FromProcessDetailId, ToProcessDetailId) 
                    VALUES (?FromProcessDetailId?, ?ToProcessDetailId?)";
                
                conn.Execute(sql, new 
                { 
                    FromProcessDetailId = connection.FromProcessDetailId,
                    ToProcessDetailId = connection.ToProcessDetailId
                }, transaction);
                
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteProcessDetailConnection(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "DELETE FROM ProcessDetailConnection WHERE Id = ?Id?";
            connection.Execute(sql, new { Id = id });
        }

        public void DeleteConnectionsByFromAndTo(int fromId, int toId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "DELETE FROM ProcessDetailConnection WHERE FromProcessDetailId = ?FromId? AND ToProcessDetailId = ?ToId?";
            connection.Execute(sql, new { FromId = fromId, ToId = toId });
        }

        // ProcessDetailFinish
        public List<ProcessDetailFinish> GetProcessDetailFinishes(int cycleId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = @"
                SELECT pdf.* 
                FROM ProcessDetailFinish pdf
                INNER JOIN ProcessDetail pd ON pdf.ProcessDetailId = pd.Id
                WHERE pd.CycleId = ?CycleId?";
            return connection.Query<ProcessDetailFinish>(sql, new { CycleId = cycleId }).ToList();
        }

        public List<ProcessDetailFinish> GetFinishesByProcessDetailId(int processDetailId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM ProcessDetailFinish WHERE ProcessDetailId = ?ProcessDetailId?";
            return connection.Query<ProcessDetailFinish>(sql, new { ProcessDetailId = processDetailId }).ToList();
        }

        public List<ProcessDetailFinish> GetFinishesByFinishId(int finishProcessDetailId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM ProcessDetailFinish WHERE FinishProcessDetailId = ?FinishProcessDetailId?";
            return connection.Query<ProcessDetailFinish>(sql, new { FinishProcessDetailId = finishProcessDetailId }).ToList();
        }

        public int AddOperation(Operation operation)
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
                    INSERT INTO Operation (
                        OperationName, CYId, CategoryId, Stay, GoBack, 
                        Start, Finish, Valve1, S1, S2, S3, S4, S5, 
                        SS1, SS2, SS3, SS4, PIL, SC, FC, CycleId, 
                        SortNumber, Con
                    ) VALUES (
                        ?OperationName?, ?CYId?, ?CategoryId?, ?Stay?, ?GoBack?, 
                        ?Start?, ?Finish?, ?Valve1?, ?S1?, ?S2?, ?S3?, ?S4?, ?S5?, 
                        ?SS1?, ?SS2?, ?SS3?, ?SS4?, ?PIL?, ?SC?, ?FC?, ?CycleId?, 
                        ?SortNumber?, ?Con?
                    )";
                
                connection.Execute(sql, new
                {
                    operation.OperationName,
                    operation.CYId,
                    operation.CategoryId,
                    operation.Stay,
                    operation.GoBack,
                    operation.Start,
                    operation.Finish,
                    operation.Valve1,
                    operation.S1,
                    operation.S2,
                    operation.S3,
                    operation.S4,
                    operation.S5,
                    operation.SS1,
                    operation.SS2,
                    operation.SS3,
                    operation.SS4,
                    operation.PIL,
                    operation.SC,
                    operation.FC,
                    operation.CycleId,
                    operation.SortNumber,
                    operation.Con
                }, transaction);

                var getIdSql = "SELECT @@IDENTITY";
                var newId = connection.QuerySingle<int>(getIdSql, transaction: transaction);

                transaction.Commit();
                return newId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        public void AddProcessDetailFinish(ProcessDetailFinish finish)
        {
            using var conn = new OleDbConnection(ConnectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                var sql = @"
                    INSERT INTO ProcessDetailFinish 
                    (ProcessDetailId, FinishProcessDetailId) 
                    VALUES (?ProcessDetailId?, ?FinishProcessDetailId?)";
                conn.Execute(sql, new { ProcessDetailId = finish.ProcessDetailId, FinishProcessDetailId = finish.FinishProcessDetailId }, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteProcessDetailFinish(int id)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "DELETE FROM ProcessDetailFinish WHERE Id = ?Id?";
            connection.Execute(sql, new { Id = id });
        }

        public void DeleteFinishesByProcessAndFinish(int processDetailId, int finishProcessDetailId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "DELETE FROM ProcessDetailFinish WHERE ProcessDetailId = ?ProcessDetailId? AND FinishProcessDetailId = ?FinishProcessDetailId?";
            connection.Execute(sql, new { ProcessDetailId = processDetailId, FinishProcessDetailId = finishProcessDetailId });
        }

        public List<MnemonicTimerDevice> GetMnemonicTimerDevices()
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = "SELECT * FROM MnemonicTimerDevice";
            return connection.Query<MnemonicTimerDevice>(sql).ToList();
        }

        public void UpdateMnemonicTimerDevice(MnemonicTimerDevice device)
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
                    UPDATE MnemonicTimerDevice SET 
                        TimerCategoryId = ?TimerCategoryId?, 
                        ProcessTimerDevice = ?ProcessTimerDevice?, 
                        TimerDevice = ?TimerDevice?, 
                        Comment1 = ?Comment1?, 
                        Comment2 = ?Comment2?, 
                        Comment3 = ?Comment3?
                    WHERE MnemonicId = ?MnemonicId? 
                      AND RecordId = ?RecordId? 
                      AND TimerId = ?TimerId?";
                
                connection.Execute(sql, new
                {
                    device.TimerCategoryId,
                    device.ProcessTimerDevice,
                    device.TimerDevice,
                    device.Comment1,
                    device.Comment2,
                    device.Comment3,
                    device.MnemonicId,
                    device.RecordId,
                    device.TimerId
                }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteMnemonicTimerDevice(int mnemonicId, int recordId, int timerId)
        {
            using var connection = new OleDbConnection(ConnectionString);
            var sql = @"
                DELETE FROM MnemonicTimerDevice 
                WHERE MnemonicId = ?MnemonicId? 
                  AND RecordId = ?RecordId? 
                  AND TimerId = ?TimerId?";
            
            connection.Execute(sql, new { MnemonicId = mnemonicId, RecordId = recordId, TimerId = timerId });
        }

        public void AddMnemonicTimerDevice(MnemonicTimerDevice device)
        {
            using var connection = new OleDbConnection(ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
                    INSERT INTO MnemonicTimerDevice (
                        MnemonicId, RecordId, TimerId, TimerCategoryId, 
                        ProcessTimerDevice, TimerDevice, PlcId, CycleId, 
                        Comment1, Comment2, Comment3
                    ) VALUES (
                        ?MnemonicId?, ?RecordId?, ?TimerId?, ?TimerCategoryId?, 
                        ?ProcessTimerDevice?, ?TimerDevice?, ?PlcId?, ?CycleId?, 
                        ?Comment1?, ?Comment2?, ?Comment3?
                    )";
                
                connection.Execute(sql, new
                {
                    device.MnemonicId,
                    device.RecordId,
                    device.TimerId,
                    device.TimerCategoryId,
                    device.ProcessTimerDevice,
                    device.TimerDevice,
                    device.PlcId,
                    device.CycleId,
                    device.Comment1,
                    device.Comment2,
                    device.Comment3
                }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
