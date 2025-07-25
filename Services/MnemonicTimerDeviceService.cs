﻿using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Access;
using KdxDesigner.ViewModels;

using System.Data;
using System.Data.OleDb;

namespace KdxDesigner.Services
{
    /// <summary>
    /// MnemonicTimerDeviceのデータ操作を行うサービスクラス
    /// </summary>
    public class MnemonicTimerDeviceService
    {
        private readonly string _connectionString;
        private readonly MainViewModel _mainViewModel;

        /// <summary>
        /// MnemonicTimerDeviceServiceのコンストラクタ
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="mainViewModel"></param>
        public MnemonicTimerDeviceService(
            IAccessRepository repository,
            MainViewModel mainViewModel)
        {
            _connectionString = repository.ConnectionString;
            _mainViewModel = mainViewModel;
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
        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByCycle(int plcId, int cycleId, int mnemonicId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicTimerDevice WHERE PlcId = @PlcId AND CycleId = @CycleId AND MnemonicId = @MnemonicId";
            return connection.Query<MnemonicTimerDevice>(sql, new { PlcId = plcId, CycleId = cycleId, MnemonicId = mnemonicId }).ToList();
        }

        /// <summary>
        /// MnemonicTimerDeviceをPlcIdとMnemonicIdで取得するヘルパーメソッド
        /// </summary>
        /// <param name="plcId">PlcId</param>
        /// <param name="cycleId">CycleId</param>
        /// <param name="mnemonicId">MnemonicId</param>
        /// <returns>MnemonicTimerDeviceのリスト</returns>
        public List<MnemonicTimerDevice> GetMnemonicTimerDeviceByMnemonic(int plcId, int mnemonicId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicTimerDevice WHERE PlcId = @PlcId AND MnemonicId = @MnemonicId";
            return connection.Query<MnemonicTimerDevice>(sql, new { PlcId = plcId, MnemonicId = mnemonicId }).ToList();
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
            parameters.Add("Comment1", deviceToSave.Comment1, DbType.String);


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
                        [CycleId] = @CycleId,
                        [Comment1] = @Comment1
                    WHERE [ID] = @ID",
                    parameters, transaction);
            }
            else
            {
                connection.Execute(@"
                    INSERT INTO [MnemonicTimerDevice] (
                        [MnemonicId], [RecordId], [TimerId], [TimerCategoryId], [ProcessTimerDevice], [TimerDevice], [PlcId], [CycleId], [Comment1]
                    ) VALUES (
                        @MnemonicId, @RecordId, @TimerId, @TimerCategoryId, @ProcessTimerDevice, @TimerDevice, @PlcId, @CycleId, @Comment1
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
            int startNum, int plcId, ref int count)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. 既存データを取得し、(RecordId, TimerId)の複合キーを持つ辞書に変換
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.ProcessDetail);
                var existingLookup = allExisting.ToDictionary(m => (m.RecordId, m.TimerId), m => m);

                // 2. ProcessDetailに関連するタイマーをRecordIdごとに整理した辞書を作成
                var timersByRecordId = new Dictionary<int, List<Models.Timer>>();
                var detailTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.ProcessDetail);

                foreach (var timer in detailTimersSource)
                {
                    if (string.IsNullOrWhiteSpace(timer.RecordIds)) continue;

                    // RecordIdsをセミコロンで分割し、各IDに対して処理
                    var recordIdStrings = timer.RecordIds.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var idStr in recordIdStrings)
                    {
                        if (int.TryParse(idStr.Trim(), out int recordId))
                        {
                            if (!timersByRecordId.ContainsKey(recordId))
                            {
                                timersByRecordId[recordId] = new List<Models.Timer>();
                            }
                            timersByRecordId[recordId].Add(timer);
                        }
                    }
                }

                // 3. ProcessDetailをループし、関連するタイマーを処理
                foreach (ProcessDetail detail in details)
                {
                    if (detail == null) continue;

                    // 現在のProcessDetailに対応するタイマーがあるか、辞書から取得
                    if (timersByRecordId.TryGetValue(detail.Id, out var detailTimers))
                    {
                        foreach (Models.Timer timer in detailTimers)
                        {
                            if (timer == null) continue;

                            var processTimerDevice = "ST" + (count + _mainViewModel.DeviceStartT);
                            var timerDevice = "ZR" + (timer.TimerNum + _mainViewModel.TimerStartZR);

                            // 複合キー (Detail.Id, Timer.ID) で既存レコードを検索
                            existingLookup.TryGetValue((detail.Id, timer.ID), out var existingRecord);

                            var deviceToSave = new MnemonicTimerDevice
                            {
                                MnemonicId = (int)MnemonicType.ProcessDetail,
                                RecordId = detail.Id, // ★ 現在のdetail.IdをRecordIdとして設定
                                TimerId = timer.ID,
                                TimerCategoryId = timer.TimerCategoryId,
                                ProcessTimerDevice = processTimerDevice,
                                TimerDevice = timerDevice,
                                PlcId = plcId,
                                CycleId = timer.CycleId,
                                Comment1 = timer.TimerName
                            };

                            UpsertMnemonicTimerDevice(connection, transaction, deviceToSave, existingRecord);
                            count++;
                        }
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
            int startNum, int plcId, ref int count)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. 既存データを取得し、(RecordId, TimerId)の複合キーを持つ辞書に変換
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.Operation);
                var existingLookup = allExisting.ToDictionary(m => (m.RecordId, m.TimerId), m => m);

                // 2. タイマーをRecordIdごとに整理した辞書を作成
                var timersByRecordId = new Dictionary<int, List<Models.Timer>>();
                var operationTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.Operation);

                foreach (var timer in operationTimersSource)
                {
                    if (string.IsNullOrWhiteSpace(timer.RecordIds)) continue;

                    // RecordIdsをセミコロンで分割し、各IDに対して処理
                    var recordIdStrings = timer.RecordIds.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var idStr in recordIdStrings)
                    {
                        if (int.TryParse(idStr.Trim(), out int recordId))
                        {
                            // 辞書にキーがなければ新しいリストを作成
                            if (!timersByRecordId.ContainsKey(recordId))
                            {
                                timersByRecordId[recordId] = new List<Models.Timer>();
                            }
                            // 対応するIDのリストにタイマーを追加
                            timersByRecordId[recordId].Add(timer);
                        }
                    }
                }

                // 3. Operationをループし、関連するタイマーを処理
                foreach (Operation operation in operations)
                {
                    if (operation == null) continue;

                    // 現在のOperationに対応するタイマーがあるか、辞書から取得
                    if (timersByRecordId.TryGetValue(operation.Id, out var operationTimers))
                    {
                        foreach (Models.Timer timer in operationTimers)
                        {
                            if (timer == null) continue;

                            // デバイス番号の計算
                            var processTimerDevice = "ST" + (count + _mainViewModel.DeviceStartT);
                            var timerDevice = "ZR" + (timer.TimerNum + _mainViewModel.TimerStartZR);

                            // 複合キーで既存レコードを検索
                            existingLookup.TryGetValue((operation.Id, timer.ID), out var existingRecord);

                            var deviceToSave = new MnemonicTimerDevice
                            {
                                MnemonicId = (int)MnemonicType.Operation,
                                RecordId = operation.Id, // ★ TimerのRecordIdsではなく、現在のoperation.Idを使う
                                TimerId = timer.ID,
                                TimerCategoryId = timer.TimerCategoryId,
                                ProcessTimerDevice = processTimerDevice,
                                TimerDevice = timerDevice,
                                PlcId = plcId,
                                CycleId = timer.CycleId,
                                Comment1 = timer.TimerName
                            };

                            UpsertMnemonicTimerDevice(connection, transaction, deviceToSave, existingRecord);
                            count++;
                        }
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
        /// Cylinderのリストを受け取り、MnemonicTimerDeviceテーブルに保存する
        /// </summary>
        /// <param name="timers"></param>
        /// <param name="cylinders"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        /// <param name="count"></param>
        public void SaveWithCY(
            List<Models.Timer> timers,
            List<CY> cylinders,
            int startNum, int plcId, ref int count)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. 既存データを取得し、(RecordId, TimerId)の複合キーを持つ辞書に変換
                var allExisting = GetMnemonicTimerDeviceByMnemonic(plcId, (int)MnemonicType.CY);
                var existingLookup = allExisting.ToDictionary(m => (m.RecordId, m.TimerId), m => m);

                // 2. CYに関連するタイマーをRecordIdごとに整理した辞書を作成
                var timersByRecordId = new Dictionary<int, List<Models.Timer>>();
                var cylinderTimersSource = timers.Where(t => t.MnemonicId == (int)MnemonicType.CY);

                foreach (var timer in cylinderTimersSource)
                {
                    if (string.IsNullOrWhiteSpace(timer.RecordIds)) continue;

                    var recordIdStrings = timer.RecordIds.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var idStr in recordIdStrings)
                    {
                        if (int.TryParse(idStr.Trim(), out int recordId))
                        {
                            if (!timersByRecordId.ContainsKey(recordId))
                            {
                                timersByRecordId[recordId] = new List<Models.Timer>();
                            }
                            timersByRecordId[recordId].Add(timer);
                        }
                    }
                }

                // 3. Cylinderをループし、関連するタイマーを処理
                foreach (CY cylinder in cylinders)
                {
                    if (cylinder == null) continue;

                    // 現在のCylinderに対応するタイマーがあるか、辞書から取得
                    if (timersByRecordId.TryGetValue(cylinder.Id, out var cylinderTimers))
                    {
                        foreach (Models.Timer timer in cylinderTimers)
                        {
                            if (timer == null) continue;

                            var processTimerDevice = "ST" + (count + _mainViewModel.DeviceStartT);
                            var timerDevice = "ZR" + (timer.TimerNum + _mainViewModel.TimerStartZR);

                            // 複合キー (Cylinder.Id, Timer.ID) で既存レコードを検索
                            existingLookup.TryGetValue((cylinder.Id, timer.ID), out var existingRecord);

                            var deviceToSave = new MnemonicTimerDevice
                            {
                                MnemonicId = (int)MnemonicType.CY,
                                RecordId = cylinder.Id, // ★ 現在のcylinder.IdをRecordIdとして設定
                                TimerId = timer.ID,
                                TimerCategoryId = timer.TimerCategoryId,
                                ProcessTimerDevice = processTimerDevice,
                                TimerDevice = timerDevice,
                                PlcId = plcId,
                                CycleId = timer.CycleId,
                                Comment1 = timer.TimerName
                            };

                            UpsertMnemonicTimerDevice(connection, transaction, deviceToSave, existingRecord);
                            count++;
                        }
                    }
                }
                // ★★★ 修正箇所 エンド ★★★

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