using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using KdxDesigner.Services.Difinitions;

using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Text;

using static Dapper.SqlMapper;

namespace KdxDesigner.Services.Memory
{
    /// <summary>
    /// メモリデータの操作を行うサービス実装
    /// </summary>
    internal class MemoryService : IMemoryService
    {
        private readonly string _connectionString;

        public MemoryService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        // AccessRepository.cs に以下を追加:
        public List<Models.Memory> GetMemories(int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM Memory WHERE PlcId = @PlcId";
            return connection.Query<Models.Memory>(sql, new { PlcId = plcId }).ToList();
        }

        public List<MemoryCategory> GetMemoryCategories()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MemoryCategory";
            return connection.Query<MemoryCategory>(sql).ToList();
        }

        /// <summary>
        /// Memory オブジェクトをデータベースに保存または更新します。
        /// </summary>
        /// <param name="connection">リポジトリ接続情報</param>
        /// <param name="transaction">データベースのトランザクション</param>
        /// <param name="memoryToSave">保存するデータ</param>
        /// <param name="existingRecord">保存したい場所に既にデータが存在する場合の処理</param>
        private void ExecuteUpsertMemory(OleDbConnection connection, OleDbTransaction transaction, Models.Memory memoryToSave, Models.Memory? existingRecord)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var parameters = new DynamicParameters();
            var debugParams = new List<string>(); // ★ デバッグ情報格納用のリスト

            // ★ AddParameterヘルパーを使ってパラメータを追加＆デバッグ情報を記録
            AddParameter(parameters, debugParams, "PlcId", memoryToSave.PlcId, DbType.Int32);
            AddParameter(parameters, debugParams, "Device", memoryToSave.Device ?? "", DbType.String);
            AddParameter(parameters, debugParams, "MemoryCategory", memoryToSave.MemoryCategory ?? 0, DbType.Int32);
            AddParameter(parameters, debugParams, "DeviceNumber", memoryToSave.DeviceNumber ?? 0, DbType.Int32);
            AddParameter(parameters, debugParams, "DeviceNumber1", memoryToSave.DeviceNumber1 ?? "", DbType.String);
            AddParameter(parameters, debugParams, "DeviceNumber2", memoryToSave.DeviceNumber2 ?? "", DbType.String);
            AddParameter(parameters, debugParams, "Category", memoryToSave.Category ?? "", DbType.String);
            AddParameter(parameters, debugParams, "Row_1", memoryToSave.Row_1 ?? "", DbType.String);
            AddParameter(parameters, debugParams, "Row_2", memoryToSave.Row_2 ?? "", DbType.String);
            AddParameter(parameters, debugParams, "Row_3", memoryToSave.Row_3 ?? "", DbType.String);
            AddParameter(parameters, debugParams, "Row_4", memoryToSave.Row_4 ?? "", DbType.String);
            AddParameter(parameters, debugParams, "Direct_Input", memoryToSave.Direct_Input ?? "", DbType.String);
            AddParameter(parameters, debugParams, "Confirm", memoryToSave.Confirm ?? "", DbType.String);
            AddParameter(parameters, debugParams, "Note", memoryToSave.Note ?? "", DbType.String);
            AddParameter(parameters, debugParams, "GOT", memoryToSave.GOT ?? "", DbType.String);
            AddParameter(parameters, debugParams, "MnemonicId", memoryToSave.MnemonicId ?? 0, DbType.Int32);
            AddParameter(parameters, debugParams, "RecordId", memoryToSave.RecordId ?? 0, DbType.Int32);
            AddParameter(parameters, debugParams, "OutcoilNumber", memoryToSave.OutcoilNumber ?? 0, DbType.Int32);

            if (existingRecord != null) // Update
            {
                // ★★★ 修正箇所 スタート ★★★

                // 1. UPDATE文のプレースホルダを '?' に変更
                var sqlUpdate = @"
                    UPDATE [Memory] SET
                        [MemoryCategory] = ?, [DeviceNumber] = ?, [DeviceNumber1] = ?, 
                        [DeviceNumber2] = ?, [Category] = ?, [Row_1] = ?, [Row_2] = ?, 
                        [Row_3] = ?, [Row_4] = ?, [Direct_Input] = ?, [Confirm] = ?, 
                        [Note] = ?, [UpdatedAt] = ?, [GOT] = ?, [MnemonicId] = ?, 
                        [RecordId] = ?, [OutcoilNumber] = ?
                    WHERE [PlcId] = ? AND [Device] = ?";

                var updateParams = new DynamicParameters();

                // 2. パラメータをSQL文の '?' の出現順と完全に一致させる
                // --- SET句のパラメータ ---
                updateParams.Add("p1", memoryToSave.MemoryCategory ?? 0, DbType.Int32);
                updateParams.Add("p2", memoryToSave.DeviceNumber ?? 0, DbType.Int32);
                updateParams.Add("p3", memoryToSave.DeviceNumber1 ?? "", DbType.String);
                updateParams.Add("p4", memoryToSave.DeviceNumber2 ?? "", DbType.String);
                updateParams.Add("p5", memoryToSave.Category ?? "", DbType.String);
                updateParams.Add("p6", memoryToSave.Row_1 ?? "", DbType.String);
                updateParams.Add("p7", memoryToSave.Row_2 ?? "", DbType.String);
                updateParams.Add("p8", memoryToSave.Row_3 ?? "", DbType.String);
                updateParams.Add("p9", memoryToSave.Row_4 ?? "", DbType.String);
                updateParams.Add("p10", memoryToSave.Direct_Input ?? "", DbType.String);
                updateParams.Add("p11", memoryToSave.Confirm ?? "", DbType.String);
                updateParams.Add("p12", memoryToSave.Note ?? "", DbType.String);
                updateParams.Add("p13", now, DbType.String); // UpdatedAt
                updateParams.Add("p14", "", DbType.String);
                updateParams.Add("p15", memoryToSave.MnemonicId ?? 0, DbType.Int32);
                updateParams.Add("p16", memoryToSave.RecordId ?? 0, DbType.Int32);
                updateParams.Add("p17", memoryToSave.OutcoilNumber ?? 0, DbType.Int32);
                // --- WHERE句のパラメータ ---
                updateParams.Add("p18", memoryToSave.PlcId, DbType.Int32);
                updateParams.Add("p19", memoryToSave.Device ?? "", DbType.String);

                connection.Execute(sqlUpdate, updateParams, transaction);
                
                // ★★★ 修正箇所 エンド ★★★
            }
            else // Insert
            {
                string paramsString = ToDebugString(parameters);

                connection.Execute(@"
            INSERT INTO [Memory] (
                [PlcId], [Device], [MemoryCategory], [DeviceNumber], [DeviceNumber1], [DeviceNumber2],
                [Category], [Row_1], [Row_2], [Row_3], [Row_4], [Direct_Input], [Confirm], [Note],
                [GOT], [MnemonicId], [RecordId], [OutcoilNumber]
            ) VALUES (
                @PlcId, @Device, @MemoryCategory, @DeviceNumber, @DeviceNumber1, @DeviceNumber2,
                @Category, @Row_1, @Row_2, @Row_3, @Row_4, @Direct_Input, @Confirm, @Note,
                @GOT, @MnemonicId, @RecordId, @OutcoilNumber
            )",
                parameters, transaction);
            }
        }

        private (int PlcId, string Device) GetMemoryKey(Models.Memory memory)
        {
           
            if (string.IsNullOrEmpty(memory.Device))
                throw new ArgumentException("Memory Device cannot be null or empty for key generation.", nameof(memory.Device));

            return (memory.PlcId, memory.Device);
        }

        public void SaveMemories(int plcId, List<Models.Memory> memories, Action<string>? progressCallback = null)
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
                var existingForThisPlcId = connection.Query<Models.Memory>(
                    "SELECT * FROM Memory WHERE PlcId = @PlcId", // SQLクエリで直接フィルタリング
                    new { PlcId = plcId },
                    transaction
                ).ToList();

                // 2. 取得した既存レコードからルックアップ用辞書を作成
                var existingLookup = new Dictionary<(int PlcId, string Device), Models.Memory>();
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
                    existingLookup.TryGetValue(GetMemoryKey(memoryToSave), out Models.Memory? existingRecord);

                    // ExecuteUpsertMemory ヘルパーメソッドを呼び出し
                    // memoryToSave.PlcId は検証済みなので、引数の plcId と一致している
                    ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);
                }

                transaction.Commit();
                progressCallback?.Invoke($"メモリデータの保存が完了しました (PlcId: {plcId})。");

                // 件数確認 (引数の plcId を使用)
                var finalCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM [Memory] WHERE PlcId = @PlcId", new { PlcId = plcId });
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // エラー発生時はロールバック
                Debug.WriteLine($"[ERROR] SaveMemories 処理中にエラーが発生しました (PlcId={plcId}): {ex.Message}");
                progressCallback?.Invoke($"エラーが発生しました (PlcId={plcId}): {ex.Message}");
                throw; // 上位の呼び出し元に例外を通知して処理を中断させる
            }
        }

        /// <summary>
        /// ★【新規】既存のトランザクション内でメモリリストを保存するための内部メソッド。
        /// </summary>
        internal void SaveMemoriesInternal(int plcId, List<Models.Memory> memories, OleDbConnection connection, OleDbTransaction transaction, Action<string>? progressCallback = null)
        {
            var existingForThisPlcId = connection.Query<Models.Memory>(
                "SELECT * FROM Memory WHERE PlcId = @PlcId",
                new { PlcId = plcId },
                transaction
            ).ToList();

            var existingLookup = existingForThisPlcId
                .Where(m => !string.IsNullOrEmpty(m.Device))
                .ToDictionary(m => (m.PlcId, m.Device), m => m);

            for (int i = 0; i < memories.Count; i++)
            {
                var memoryToSave = memories[i];
                if (memoryToSave == null) continue;

                progressCallback?.Invoke($"[{i + 1}/{memories.Count}] Memory保存中: {memoryToSave.Device}");

                existingLookup.TryGetValue((memoryToSave.PlcId, memoryToSave.Device), out Models.Memory? existingRecord);
                ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);
            }
        }


        // GetMemories, GetMemoryCategories は変更なし
        public bool SaveMnemonicMemories(Models.MnemonicDevice device)
        {
            if (device?.PlcId == null) return false; // PlcId が必須

            using var connection = new OleDbConnection(_connectionString);
            var difinitionsService = new Difinitions.DifinitionsService(_connectionString); // DifinitionsServiceのインスタンスを作成

            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var existingForPlcIdList = connection.Query<Models.Memory>("SELECT * FROM Memory WHERE PlcId = @PlcId", new { device.PlcId }, transaction).ToList();
                var existingLookup = existingForPlcIdList.Where(m => !string.IsNullOrEmpty(m.Device))
                                                        .ToDictionary(m => m.Device!, m => m); // Deviceで検索 (PlcIdは共通)

                int deviceLabelCategoryId = device.DeviceLabel switch
                {
                    "L" => 1,
                    "M" => 2,
                    "B" => 3,
                    "D" => 4,
                    "ZR" => 5,
                    "W" => 6,
                    "T" => 7,
                    "C" => 8,
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
                    _ => new List<Models.Difinitions>(), // TODO: エラー処理または明確なデフォルト値
                };

                for (int i = 0; i < device.OutCoilCount; i++)
                {
                    var deviceNum = device.StartNum + i;
                    var deviceString = device.DeviceLabel + deviceNum.ToString();

                    var memoryToSave = new Models.Memory
                    {
                        PlcId = device.PlcId,
                        MemoryCategory = deviceLabelCategoryId,
                        DeviceNumber = deviceNum,
                        DeviceNumber1 = deviceString,
                        DeviceNumber2 = "",
                        Device = deviceString,
                        Category = mnemonicTypeBasedCategoryString,
                        Row_1 = difinitions.Where(d => d.Label == "").Single(d => d.OutCoilNumber == i).Comment1,
                        Row_2 = difinitions.Single(d => d.OutCoilNumber == i).Comment2,
                        Row_3 = device.Comment2, // Outcoilのインデックスとして
                        Row_4 = device.Comment2,
                        Direct_Input = "",
                        Confirm = mnemonicTypeBasedCategoryString + device.Comment1 + i.ToString(),
                        Note = "",
                        // CreatedAt, UpdatedAt は ExecuteUpsertMemory で処理
                        GOT = "False",
                        MnemonicId = device.MnemonicId, // MnemonicDevice の ID
                        RecordId = device.RecordId, // MnemonicDevice の ID
                        OutcoilNumber = i
                    };

                    existingLookup.TryGetValue(memoryToSave.Device!, out Models.Memory? existingRecord);
                    ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);
                }
                transaction.Commit();
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
            var dinitions = new List<Models.Difinitions>();

            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var existingForPlcIdList = connection.Query<Models.Memory>("SELECT * FROM Memory WHERE PlcId = @PlcId", new { device.PlcId }, transaction).ToList();
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
                    var memoryToSave = new Models.Memory
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

                    existingLookup.TryGetValue(memoryToSave.Device!, out Models.Memory? existingRecord);
                    ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);

                    transaction.Commit();
                    return true;
                }
                else
                {
                    transaction.Rollback(); // 不正なデータなのでロールバック
                    return false;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"[ERROR] MnemonicTimerDevice MnemonicID={device.MnemonicId} RecordID={device.RecordId} のMemory(ZR)保存失敗 → {ex.Message}");
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
                var existingForPlcIdList = connection.Query<Models.Memory>("SELECT * FROM Memory WHERE PlcId = @PlcId", new { device.PlcId }, transaction).ToList();
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
                    var memoryToSave = new Models.Memory
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

                    existingLookup.TryGetValue(memoryToSave.Device!, out Models.Memory? existingRecord);
                    ExecuteUpsertMemory(connection, transaction, memoryToSave, existingRecord);

                    transaction.Commit();
                    return true;
                }
                else
                {
                    transaction.Rollback();
                    return false;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"[ERROR] MnemonicTimerDevice MnemonicId={device.MnemonicId} RecordId={device.RecordId} のMemory(T)保存失敗 → {ex.Message}");
                return false;
            }
        }

        private void AddParameter<T>(DynamicParameters parameters, List<string> debugList, string name, T value, DbType dbType)
        {
            parameters.Add(name, value, dbType);
            debugList.Add($"  - @{name}: '{value}' (Type: {dbType})");
        }

        /// <summary>
        /// DynamicParametersの中身をデバッグ用の文字列に変換します。
        /// </summary>
        private string ToDebugString(DynamicParameters parameters)
        {
            var sb = new StringBuilder();

            // DynamicParametersはテンプレートを介してパラメータ名にアクセスする必要がある
            var template = (IDynamicParameters)parameters;

            // Get a reference to the private list of parameters
            // This uses reflection and might be brittle if Dapper's internal structure changes.
            // A simpler approach might be needed if this fails.
            // Let's try a safer public interface first.

            // Dapperの内部実装にアクセスするのは推奨されないため、
            // パラメータ名の一覧を取得する公式な方法を使います。
            var parameterNames = parameters.ParameterNames;

            if (parameterNames.Any())
            {
                foreach (var name in parameterNames)
                {
                    // パラメータ名を使って値を取得する (この方法は少しトリッキーです)
                    // DapperのDynamicParametersは、直接キーで値を取得する簡単な公開メソッドがありません。
                    // そのため、デバッグでは方法1（デバッga）がはるかに優れています。

                    // ここでは、デバッグ出力のための一つのアプローチを示します。
                    // (リフレクションを使ったより複雑な方法もありますが、ここでは省略します)
                    sb.AppendLine($"  - @{name}"); // パラメータ名だけを出力するだけでも有用
                }
            }
            else
            {
                sb.AppendLine("(No parameters found)");
            }

            return sb.ToString();
        }
    }
}
