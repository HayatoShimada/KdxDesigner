using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Services.Access;

using System.Data;
using System.Data.OleDb;
using System.Diagnostics;

namespace KdxDesigner.Services
{
    internal class MemoryService
    {
        private readonly string _connectionString;

        public MemoryService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
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

        // MemoryService クラス内
        private void ExecuteUpsertMemory(OleDbConnection connection, OleDbTransaction transaction, Memory memoryToSave, Memory? existingRecord)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var parameters = new DynamicParameters();

            // Memory オブジェクトのプロパティを DynamicParameters に設定
            parameters.Add("PlcId", memoryToSave.PlcId, DbType.Int32); // PlcIdはキーなので必須
            parameters.Add("Device", memoryToSave.Device ?? "", DbType.String); // Deviceはキーなので必須
            parameters.Add("MemoryCategory", memoryToSave.MemoryCategory ?? 0, DbType.Int32);
            parameters.Add("DeviceNumber", memoryToSave.DeviceNumber ?? 0, DbType.Int32);
            parameters.Add("DeviceNumber1", memoryToSave.DeviceNumber1 ?? "", DbType.String);
            parameters.Add("DeviceNumber2", memoryToSave.DeviceNumber2 ?? "", DbType.String);
            parameters.Add("Category", memoryToSave.Category ?? "", DbType.String);
            parameters.Add("Row_1", memoryToSave.Row_1 ?? "", DbType.String);
            parameters.Add("Row_2", memoryToSave.Row_2 ?? "", DbType.String);
            parameters.Add("Row_3", memoryToSave.Row_3 ?? "", DbType.String);
            parameters.Add("Row_4", memoryToSave.Row_4 ?? "", DbType.String);
            parameters.Add("Direct_Input", memoryToSave.Direct_Input ?? "", DbType.String);
            parameters.Add("Confirm", memoryToSave.Confirm ?? "", DbType.String);
            parameters.Add("Note", memoryToSave.Note ?? "", DbType.String);
            parameters.Add("UpdatedAt", now, DbType.String); // 更新日は常に現在時刻
            parameters.Add("GOT", memoryToSave.GOT ?? false, DbType.Boolean);
            parameters.Add("MnemonicId", memoryToSave.MnemonicId, DbType.Int32);
            parameters.Add("RecordId", memoryToSave.RecordId, DbType.Int32);
            parameters.Add("OutcoilNumber", memoryToSave.OutcoilNumber, DbType.Int32);

            if (existingRecord != null) // Update
            {
                // CreatedAt は更新しないため、ここではパラメータに追加しない
                // PlcId と Device は WHERE 句で使用される (既に parameters に含まれている)
                connection.Execute(@"
            UPDATE [Memory] SET
                [MemoryCategory] = @MemoryCategory, [DeviceNumber] = @DeviceNumber,
                [DeviceNumber1] = @DeviceNumber1, [DeviceNumber2] = @DeviceNumber2,
                [Category] = @Category, [Row_1] = @Row_1, [Row_2] = @Row_2,
                [Row_3] = @Row_3, [Row_4] = @Row_4, [Direct_Input] = @Direct_Input,
                [Confirm] = @Confirm, [Note] = @Note, [UpdatedAt] = @UpdatedAt, [GOT] = @GOT,
                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [OutcoilNumber] = @OutcoilNumber
            WHERE [PlcId] = @PlcId AND [Device] = @Device",
                parameters, transaction);
            }
            else // Insert
            {
                parameters.Add("CreatedAt", memoryToSave.CreatedAt ?? now, DbType.String); // 新規作成時のみ CreatedAt を設定
                connection.Execute(@"
            INSERT INTO [Memory] (
                [PlcId], [MemoryCategory], [DeviceNumber], [DeviceNumber1], [DeviceNumber2], [Device],
                [Category], [Row_1], [Row_2], [Row_3], [Row_4], [Direct_Input], [Confirm], [Note],
                [CreatedAt], [UpdatedAt], [GOT], [MnemonicId], [RecordId], [OutcoilNumber]
            ) VALUES (
                @PlcId, @MemoryCategory, @DeviceNumber, @DeviceNumber1, @DeviceNumber2, @Device,
                @Category, @Row_1, @Row_2, @Row_3, @Row_4, @Direct_Input, @Confirm, @Note,
                @CreatedAt, @UpdatedAt, @GOT, @MnemonicId, @RecordId, @OutcoilNumber
            )",
                parameters, transaction);
            }
        }

        private (int PlcId, string Device) GetMemoryKey(Memory memory)
        {
           
            if (string.IsNullOrEmpty(memory.Device))
                throw new ArgumentException("Memory Device cannot be null or empty for key generation.", nameof(memory.Device));

            return (memory.PlcId, memory.Device);
        }

        public void SaveMemories(int plcId, List<Memory> memories, Action<string>? progressCallback = null)
        {
            if (memories == null || !memories.Any())
            {
                progressCallback?.Invoke($"保存対象のメモリデータがありません (PlcId: {plcId})。");
                return;
            }

            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. 渡された plcId を使用して、関連する既存レコードのみをDBから取得
                var existingForThisPlcId = connection.Query<Memory>(
                    "SELECT * FROM Memory WHERE PlcId = @PlcId", // SQLクエリで直接フィルタリング
                    new { PlcId = plcId },
                    transaction
                ).ToList();

                // 2. 取得した既存レコードからルックアップ用辞書を作成
                var existingLookup = new Dictionary<(int PlcId, string Device), Memory>();
                foreach (var mem in existingForThisPlcId)
                {
                    // GetMemoryKey は (mem.PlcId.Value, mem.Device) を返すことを想定
                    // mem.PlcId はこの時点で引数の plcId と一致しているはず
                    if (mem.PlcId == plcId && !string.IsNullOrEmpty(mem.Device))
                    {
                        existingLookup[GetMemoryKey(mem)] = mem;
                    }
                }

                for (int i = 0; i < memories.Count; i++)
                {
                    var memoryToSave = memories[i];

                    // 3. 入力される Memory オブジェクトの検証
                    if (memoryToSave == null)
                    {
                        progressCallback?.Invoke($"[{i + 1}/{memories.Count}] スキップ: null のメモリデータです。");
                        continue;
                    }

                    // memoryToSave の PlcId が引数の plcId と一致するか確認
                    if (memoryToSave.PlcId != plcId)
                    {
                        progressCallback?.Invoke($"[{i + 1}/{memories.Count}] スキップ: PlcId ({memoryToSave.PlcId.ToString() ?? "null"}) が指定された PlcId ({plcId}) と一致しません。Device: {memoryToSave.Device}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(memoryToSave.Device))
                    {
                        progressCallback?.Invoke($"[{i + 1}/{memories.Count}] スキップ: Device が null または空です (PlcId: {plcId})。");
                        continue;
                    }

                    progressCallback?.Invoke($"[{i + 1}/{memories.Count}] 保存中: {memoryToSave.Device} (PlcId: {plcId})");

                    // GetMemoryKey を使って既存レコードを検索
                    existingLookup.TryGetValue(GetMemoryKey(memoryToSave), out Memory? existingRecord);

                    // ExecuteUpsertMemory ヘルパーメソッドを呼び出し
                    // memoryToSave.PlcId は検証済みなので、引数の plcId と一致している
                    ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);
                }

                transaction.Commit();
                progressCallback?.Invoke($"メモリデータの保存が完了しました (PlcId: {plcId})。");

                // 件数確認 (引数の plcId を使用)
                var finalCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM [Memory] WHERE PlcId = @PlcId", new { PlcId = plcId });
                Debug.WriteLine($"[確認] Memory テーブルのレコード数 (PlcId={plcId}): {finalCount}");
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // エラー発生時はロールバック
                Debug.WriteLine($"[ERROR] SaveMemories 処理中にエラーが発生しました (PlcId={plcId}): {ex.Message}");
                progressCallback?.Invoke($"エラーが発生しました (PlcId={plcId}): {ex.Message}");
                throw; // 上位の呼び出し元に例外を通知して処理を中断させる
            }
        }


        // GetMemories, GetMemoryCategories は変更なし
        public bool SaveMnemonicMemories(MnemonicDevice device)
        {
            if (device?.PlcId == null) return false; // PlcId が必須

            using var connection = new OleDbConnection(_connectionString);
            var difinitionsService = new DifinitionsService(_connectionString); // DifinitionsServiceのインスタンスを作成

            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var existingForPlcIdList = connection.Query<Memory>("SELECT * FROM Memory WHERE PlcId = @PlcId", new { device.PlcId }, transaction).ToList();
                var existingLookup = existingForPlcIdList.Where(m => !string.IsNullOrEmpty(m.Device))
                                                        .ToDictionary(m => m.Device!, m => m); // Deviceで検索 (PlcIdは共通)

                int deviceLabelCategoryId = device.DeviceLabel switch
                {
                    "L" => 1,
                    "M" => 2,
                    _ => 1, // TODO: エラー処理または明確なデフォルト値
                };
                string mnemonicTypeBasedCategoryString = device.MnemonicId switch
                {
                    1 => "工程",
                    2 => "工程詳細",
                    3 => "操作",
                    4 => "出力",
                    _ => "なし", // TODO: エラー処理または明確なデフォルト値
                };
                var difinitions = device.MnemonicId switch
                {
                    1 => difinitionsService.GetDifinitions("Process"),
                    2 => difinitionsService.GetDifinitions("Detail"),
                    3 => difinitionsService.GetDifinitions("Operation"),
                    4 => difinitionsService.GetDifinitions("Cylinder"),
                    _ => new List<Difinitions>(), // TODO: エラー処理または明確なデフォルト値
                };

                for (int i = 0; i < device.OutCoilCount; i++)
                {
                    var deviceNum = device.StartNum + i;
                    var deviceString = device.DeviceLabel + deviceNum.ToString();

                    var memoryToSave = new Memory
                    {
                        PlcId = device.PlcId,
                        MemoryCategory = deviceLabelCategoryId,
                        DeviceNumber = deviceNum,
                        DeviceNumber1 = deviceString,
                        DeviceNumber2 = "",
                        Device = deviceString,
                        Category = mnemonicTypeBasedCategoryString,
                        Row_1 = difinitions.Where(d => d.Label == "").Single(d => d.OutCoilNumber == i).Comment1,
                        Row_2 = difinitions.Single(d => d.OutCoilNumber == i).Comment1,
                        Row_3 = device.Comment2, // Outcoilのインデックスとして
                        Row_4 = device.Comment2,
                        Direct_Input = "",
                        Confirm = mnemonicTypeBasedCategoryString + device.Comment1 + i.ToString(),
                        Note = "",
                        // CreatedAt, UpdatedAt は ExecuteUpsertMemory で処理
                        GOT = false,
                        MnemonicId = device.MnemonicId, // MnemonicDevice の ID
                        RecordId = device.RecordId, // MnemonicDevice の ID
                        OutcoilNumber = i
                    };

                    existingLookup.TryGetValue(memoryToSave.Device!, out Memory? existingRecord);
                    ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);
                }
                transaction.Commit();
                Debug.WriteLine($"[確認] SaveMnemonicMemories 完了 (MnemonicDevice ID: {device.ID}, PlcId: {device.PlcId})");
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"[ERROR] MnemonicDevice ID={device.ID} のMemory保存失敗 → {ex.Message}");
                return false;
            }
        }

        // SaveMnemonicTimerMemoriesZR と SaveMnemonicTimerMemoriesT も同様のパターンで修正します。
        // Memoryオブジェクトの構築ロジックは各メソッド固有ですが、保存部分はExecuteUpsertMemoryを呼び出します。

        public bool SaveMnemonicTimerMemoriesZR(MnemonicTimerDevice device)
        {
            if (device?.PlcId == null || string.IsNullOrEmpty(device.TimerDevice) || !device.TimerDevice.StartsWith("ZR")) return false;

            using var connection = new OleDbConnection(_connectionString);
            var difinitionsService = new DifinitionsService(_connectionString); // DifinitionsServiceのインスタンスを作成
            var dinitions = new List<Difinitions>();

            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var existingForPlcIdList = connection.Query<Memory>("SELECT * FROM Memory WHERE PlcId = @PlcId", new { device.PlcId }, transaction).ToList();
                var existingLookup = existingForPlcIdList.Where(m => !string.IsNullOrEmpty(m.Device))
                                                        .ToDictionary(m => m.Device!, m => m);


                string mnemonicTypeBasedCategoryString = device.MnemonicId switch
                {
                    1 => "工程ﾀｲﾏ",
                    2 => "詳細ﾀｲﾏ",
                    3 => "操作ﾀｲﾏ",
                    4 => "出力ﾀｲﾏ",
                    _ => "なし",
                };

                var tDeviceNumStr = device.TimerDevice.Replace("ZR", "");
                if (int.TryParse(tDeviceNumStr, out int tDeviceNum))
                {
                    var memoryToSave = new Memory
                    {
                        PlcId = device.PlcId,
                        MemoryCategory = 0, // TODO: ZR用の適切なMemoryCategory IDを決定する
                        DeviceNumber = tDeviceNum,
                        DeviceNumber1 = device.TimerDevice,
                        Device = device.TimerDevice,
                        Category = mnemonicTypeBasedCategoryString,
                        Row_1 = mnemonicTypeBasedCategoryString,
                        Row_2 = device.Comment1,
                        Row_3 = device.Comment2,
                        Row_4 = device.Comment3,
                        Note = "",
                        MnemonicId = device.MnemonicId,
                        RecordId = device.RecordId,
                    };

                    existingLookup.TryGetValue(memoryToSave.Device!, out Memory? existingRecord);
                    ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);

                    transaction.Commit();
                    Debug.WriteLine($"[確認] SaveMnemonicTimerMemoriesZR 完了 (MnemonicTimerDevice ID: {device.ID}, PlcId: {device.PlcId})");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"[WARN] Invalid TimerDevice format for ZR: {device.TimerDevice}");
                    transaction.Rollback(); // 不正なデータなのでロールバック
                    return false;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"[ERROR] MnemonicTimerDevice ID={device.ID} のMemory(ZR)保存失敗 → {ex.Message}");
                return false;
            }
        }

        public bool SaveMnemonicTimerMemoriesT(MnemonicTimerDevice device)
        {
            if (device?.PlcId == null || string.IsNullOrEmpty(device.ProcessTimerDevice) || !device.ProcessTimerDevice.StartsWith("T")) return false;

            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var existingForPlcIdList = connection.Query<Memory>("SELECT * FROM Memory WHERE PlcId = @PlcId", new { device.PlcId }, transaction).ToList();
                var existingLookup = existingForPlcIdList.Where(m => !string.IsNullOrEmpty(m.Device))
                                                        .ToDictionary(m => m.Device!, m => m);

                string mnemonicTypeBasedCategoryString = device.MnemonicId switch
                {
                    1 => "工程タイマT",
                    2 => "工程詳細タイマT",
                    3 => "操作タイマT",
                    4 => "出力タイマT",
                    _ => "タイマT",
                };

                var dDeviceNumStr = device.ProcessTimerDevice.Replace("T", "");
                if (int.TryParse(dDeviceNumStr, out int dDeviceNum))
                {
                    var memoryToSave = new Memory
                    {
                        PlcId = device.PlcId,
                        MemoryCategory = 0, // TODO: Tデバイス用の適切なMemoryCategory IDを決定する
                        DeviceNumber = dDeviceNum,
                        DeviceNumber1 = device.ProcessTimerDevice,
                        Device = device.ProcessTimerDevice,
                        Category = mnemonicTypeBasedCategoryString,
                        Row_1 = mnemonicTypeBasedCategoryString,
                        Row_2 = device.Comment1,
                        Row_3= device.Comment2,
                        Row_4 = device.Comment3,
                        MnemonicId = device.MnemonicId,
                        RecordId = device.RecordId,// MnemonicTimerDeviceのIDをMemoryのMnemonicDeviceIdにマッピング
                                                                 // 他のフィールドは必要に応じて設定
                    };

                    existingLookup.TryGetValue(memoryToSave.Device!, out Memory? existingRecord);
                    ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);

                    transaction.Commit();
                    Debug.WriteLine($"[確認] SaveMnemonicTimerMemoriesT 完了 (MnemonicTimerDevice ID: {device.ID}, PlcId: {device.PlcId})");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"[WARN] Invalid ProcessTimerDevice format for T: {device.ProcessTimerDevice}");
                    transaction.Rollback();
                    return false;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"[ERROR] MnemonicTimerDevice ID={device.ID} のMemory(T)保存失敗 → {ex.Message}");
                return false;
            }
        }
    }
}
