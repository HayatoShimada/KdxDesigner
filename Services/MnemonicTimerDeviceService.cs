using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Access;

using System; // Exception, Action のために追加
using System.Collections.Generic; // List, Dictionary のために追加
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Linq; // ToList, GroupBy, FirstOrDefault, SingleOrDefault 等のために追加

namespace KdxDesigner.Services
{
    public class MnemonicTimerDeviceService
    {
        private readonly string _connectionString;

        public MnemonicTimerDeviceService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        /// <summary>
        /// PlcIdとCycleIdに基づいてMnemonicTimerDeviceを取得するヘルパーメソッド
        /// </summary>
        /// <param name="plcId"></param>
        /// <param name="cycleId"></param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        public List<MnemonicTimerDevice> GetMnemonicTimerDevice(int plcId, int cycleId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicTimerDevice WHERE PlcId = @PlcId AND CycleId = @CycleId";
            return connection.Query<MnemonicTimerDevice>(sql, new { PlcId = plcId, CycleId = cycleId }).ToList();
        }

        /// <summary>
        /// MnemonicTimerDeviceをPlcIdとMnemonicIdで取得するヘルパーメソッド
        /// </summary>
        /// <param name="plcId">PlcId</param>
        /// <param name="cycleId">CycleId</param>
        /// <param name="mnemonicId">MnemonicId</param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByMnemonic(int plcId, int cycleId, int mnemonicId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicTimerDevice WHERE PlcId = @PlcId AND CycleId = @CycleId AND MnemonicId = @MnemonicId";
            return connection.Query<MnemonicTimerDevice>(sql, new { PlcId = plcId, CycleId = cycleId, MnemonicId = mnemonicId }).ToList();
        }

        /// <summary>
        /// MnemonicTimerDeviceをPlcIdとTimerIdで取得するヘルパーメソッド
        /// </summary>
        /// <param name="plcId">PlcId</param>
        /// <param name="timerId">TimerId</param>
        /// <returns>単一のMnemonicTimerDevice</returns>
        public MnemonicTimerDevice? GetMnemonicTimerDeviceByTimerId(int plcId, int timerId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicTimerDevice WHERE PlcId = @PlcId AND TimerId = @TimerId";
            return connection.Query<MnemonicTimerDevice>(sql, new { PlcId = plcId, TimerId = timerId }).FirstOrDefault();
        }

        /// <summary>
        /// MnemonicTimerDeviceを挿入または更新するヘルパーメソッド
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="deviceToSave"></param>
        /// <param name="existingRecord"></param>
        private void UpsertMnemonicTimerDevice(
            OleDbConnection connection, 
            OleDbTransaction transaction, 
            MnemonicTimerDevice deviceToSave, 
            MnemonicTimerDevice? existingRecord)
        {
            var parameters = new DynamicParameters(); // オブジェクトからパラメータを自動生成

            parameters.Add("MnemonicId", deviceToSave.MnemonicId, DbType.Int32);
            parameters.Add("RecordId", deviceToSave.RecordId, DbType.Int32);
            parameters.Add("TimerId", deviceToSave.TimerId, DbType.Int32);
            parameters.Add("TimerCategoryId", deviceToSave.TimerCategoryId, DbType.Int32);
            parameters.Add("ProcessTimerDevice", deviceToSave.ProcessTimerDevice, DbType.String);
            parameters.Add("TimerDevice", deviceToSave.TimerDevice, DbType.String);
            parameters.Add("PlcId", deviceToSave.PlcId, DbType.Int32); // result[0]は常に安全
            parameters.Add("CycleId", deviceToSave.CycleId, DbType.Int32); // result[1]も常に安全

            if (existingRecord != null)
            {
                parameters.Add("ID", existingRecord.ID, DbType.Int32); // WHERE句のIDを指定
                connection.Execute(@"
                    UPDATE [MnemonicTimerDevice] SET
                        [MnemonicId] = @MnemonicId,
                        [RecordId] = @RecordId,
                        [TimerId] = @TimerId,
                        [TimerCategoryId] = @TimerCategoryId,
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
        }

        /// <summary>
        /// Detailのリストを受け取り、MnemonicTimerDeviceテーブルに保存する
        /// </summary>
        /// <param name="timers"></param>
        /// <param name="details"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        /// <param name="cycleId"></param>
        /// <param name="count"></param>
        public void SaveWithDetail(
            List<Models.Timer> timers,
            List<ProcessDetail> details,
            int startNum, int plcId, int cycleId, out int count)
        {
            count = 0; // outパラメータの初期化
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 既存データを取得し、高速検索用に辞書に変換
                // キー: (RecordId, TimerId) でユニークと仮定
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, cycleId, (int)MnemonicType.ProcessDetail);
                var existingLookup = allExisting.ToDictionary(m => (RecordId: m.RecordId, TimerId: m.TimerId), m => m);

                // 修正: 'int?' 型を 'int' 型に変換して、ToDictionary のキーとして使用可能にする
                var timersByRecordId = timers
                    .Where(t => t.MnemonicId == (int)MnemonicType.ProcessDetail)
                    .GroupBy(t => t.RecordId ?? 0) // Null 許容型 'int?' をデフォルト値 '0' に変換
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (ProcessDetail detail in details)
                {
                    if (detail == null || !timersByRecordId.TryGetValue(detail.Id, out var operationTimers))
                    {
                        continue; // 操作データがない、または関連するタイマーがない場合はスキップ
                    }

                    foreach (Models.Timer timer in operationTimers)
                    {
                        if (timer == null) continue;

                        var processTimerDevice = "ST" + (startNum + count).ToString();
                        var timerDevice = "ZR" + timer.TimerNum.ToString();

                        // 複合キーで既存レコードを検索
                        existingLookup.TryGetValue((detail.Id, timer.ID), out var existingRecord);

                        var deviceToSave = new MnemonicTimerDevice
                        {
                            // IDはUPDATE時にのみ必要。Upsertヘルパー内で処理
                            MnemonicId = (int)MnemonicType.ProcessDetail,
                            RecordId = detail.Id,
                            TimerId = timer.ID,
                            TimerCategoryId = timer.TimerCategoryId,
                            ProcessTimerDevice = processTimerDevice,
                            TimerDevice = timerDevice,
                            PlcId = plcId,
                            CycleId = cycleId
                        };

                        UpsertMnemonicTimerDevice(connection, transaction, deviceToSave, existingRecord);
                        count++;
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"SaveWithOperation 失敗: {ex.Message}");
                // エラーログの記録や上位への例外通知など
                // Debug.WriteLine($"SaveWithOperation 失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Operationのリストを受け取り、MnemonicTimerDeviceテーブルに保存する
        /// </summary>
        /// <param name="timers"></param>
        /// <param name="operations"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        /// <param name="cycleId"></param>
        /// <param name="count"></param>
        public void SaveWithOperation(
            List<Models.Timer> timers,
            List<Operation> operations,
            int startNum, int plcId, int cycleId, out int count)
        {
            count = 0; // outパラメータの初期化
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 既存データを取得し、高速検索用に辞書に変換
                // キー: (RecordId, TimerId) でユニークと仮定
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, cycleId, (int)MnemonicType.Operation);
                var existingLookup = allExisting.ToDictionary(m => (RecordId: m.RecordId, TimerId: m.TimerId), m => m);

                // 修正: 'int?' 型を 'int' 型に変換して、ToDictionary のキーとして使用可能にする
                var timersByRecordId = timers
                    .Where(t => t.MnemonicId == (int)MnemonicType.Operation)
                    .GroupBy(t => t.RecordId ?? 0) // Null 許容型 'int?' をデフォルト値 '0' に変換
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (Operation operation in operations)
                {
                    if (operation == null || !timersByRecordId.TryGetValue(operation.Id, out var operationTimers))
                    {
                        continue; // 操作データがない、または関連するタイマーがない場合はスキップ
                    }

                    foreach (Models.Timer timer in operationTimers)
                    {
                        if (timer == null) continue;

                        var processTimerDevice = "ST" + (startNum + count).ToString();
                        var timerDevice = "ZR" + timer.TimerNum.ToString();

                        // 複合キーで既存レコードを検索
                        existingLookup.TryGetValue((operation.Id, timer.ID), out var existingRecord);

                        var deviceToSave = new MnemonicTimerDevice
                        {
                            // IDはUPDATE時にのみ必要。Upsertヘルパー内で処理
                            MnemonicId = (int)MnemonicType.Operation,
                            RecordId = operation.Id,
                            TimerId = timer.ID,
                            TimerCategoryId = timer.TimerCategoryId,
                            ProcessTimerDevice = processTimerDevice,
                            TimerDevice = timerDevice,
                            PlcId = plcId,
                            CycleId = cycleId
                        };

                        UpsertMnemonicTimerDevice(connection, transaction, deviceToSave, existingRecord);
                        count++;
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"SaveWithOperation 失敗: {ex.Message}");
                // エラーログの記録や上位への例外通知など
                // Debug.WriteLine($"SaveWithOperation 失敗: {ex.Message}");
                throw;
            }
        }

        // count変数を参照渡し(ref)に変更し、呼び出し元でインクリメントされた値を維持できるようにする
        /// <summary>
        /// Cylinderのリストを受け取り、MnemonicTimerDeviceテーブルに保存する
        /// </summary>
        /// <param name="timers"></param>
        /// <param name="cylinders"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        /// <param name="cycleId"></param>
        /// <param name="count"></param>
        public void SaveWithCY(
            List<Models.Timer> timers,
            List<CY> cylinders,
            int startNum, int plcId, int cycleId, ref int count)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 既存データを取得し、高速検索用に辞書に変換
                // キー: (RecordId, TimerCategoryId) でユニークと仮定
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, cycleId, (int)MnemonicType.CY);

                // 処理対象のタイマーをRecordId(Cylinder.Id)でグループ化
                var timersCylinder = timers
                    .Where(t => t.MnemonicId == (int)MnemonicType.CY)
                    .Where(t => t.CycleId == cycleId) // CycleIdでフィルタリング
                    .ToList();
                var repository = new AccessRepository(_connectionString);
                var timerCategory = repository.GetTimerCategory();

                foreach (var timer in timersCylinder)
                {
                    if (timer == null)
                    {
                        continue; // このカテゴリのタイマーは存在しないのでスキップ
                    }

                    var existingRecord = allExisting
                        .FirstOrDefault(m => m.RecordId == timer.RecordId
                        && m.TimerCategoryId == timer.TimerCategoryId);

                    var category = timerCategory.FirstOrDefault(c => c.ID == timer.TimerCategoryId);
                    var processTimerDevice = "ST" + (startNum + count).ToString();
                    var timerDevice = "ZR" + timer.TimerNum.ToString();

                    if (timer.RecordId == null) continue; // RecordIdがnullの場合はスキップ

                    var deviceToSave = new MnemonicTimerDevice
                    {
                        MnemonicId = (int)MnemonicType.CY,
                        RecordId = timer.RecordId.Value,
                        TimerId = timer.ID,
                        TimerCategoryId = timer.TimerCategoryId,
                        ProcessTimerDevice = processTimerDevice,
                        TimerDevice = timerDevice,
                        PlcId = plcId,
                        CycleId = cycleId
                    };

                    UpsertMnemonicTimerDevice(connection, transaction, deviceToSave, existingRecord);
                    count++;
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"SaveWithCY 失敗: {ex.Message}");
                throw;
            }
        }
    }
}