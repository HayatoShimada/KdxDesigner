using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Models.Define;
using KdxDesigner.Services.Access;

using System.Data;
using System.Data.OleDb;

namespace KdxDesigner.Services
{
    internal class MnemonicDeviceService
    {
        private readonly string _connectionString;
        private readonly IAccessRepository _repository;
        private readonly MemoryService _memoryService;

        static MnemonicDeviceService()
        {
            // Shift_JIS エンコーディングを有効にする
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public MnemonicDeviceService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
            _repository = repository;
            _memoryService = new MemoryService(repository);
        }

        /// <summary>
        /// MnemonicDeviceテーブルから、指定されたPLCに基づいてデータを取得する。
        /// </summary>
        /// <param name="plcId"></param>
        /// <returns></returns>
        public List<MnemonicDevice> GetMnemonicDevice(int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicDevice WHERE PlcId = @PlcId";
            return connection.Query<MnemonicDevice>(sql, new { PlcId = plcId }).ToList();
        }

        /// <summary>
        /// MnemonicDeviceテーブルから、指定されたPLCとMnemonicIdに基づいてデータを取得する。
        /// </summary>
        /// <param name="plcId"></param>
        /// <param name="mnemonicId"></param>
        /// <returns></returns>
        public List<MnemonicDevice> GetMnemonicDeviceByMnemonic(int plcId, int mnemonicId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicDevice WHERE PlcId = @PlcId AND MnemonicId = @MnemonicId";
            return connection.Query<MnemonicDevice>(sql, new { PlcId = plcId, MnemonicId = mnemonicId }).ToList();
        }

        /// <summary>
        /// Processesのリストを受け取り、MnemonicDeviceテーブルに保存する。
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        public void SaveMnemonicDeviceProcess(List<Models.Process> processes, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.Process);
                var existingLookup = allExisting.ToDictionary(m => m.RecordId, m => m);
                var allMemoriesToSave = new List<Memory>(); // ★ 保存するメモリを蓄積するリスト
                int count = 0;
                foreach (Models.Process process in processes)
                {
                    if (process == null) continue;

                    existingLookup.TryGetValue(process.Id, out var existing);
                    var parameters = new DynamicParameters();

                    string input = process.ProcessName ?? "";

                    parameters.Add("MnemonicId", (int)MnemonicType.Process, DbType.Int32);
                    parameters.Add("RecordId", process.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "L", DbType.String);
                    parameters.Add("StartNum", (count * 5 + startNum), DbType.Int32);
                    parameters.Add("OutCoilCount", 5, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);


                    parameters.Add("Comment1", process.Comment1, DbType.String); // Memoryのrow_3兼用
                    parameters.Add("Comment2", process.Comment2, DbType.String); // Memoryのrow_4兼用

                    if (existing != null) // Update
                    {
                        var updateParams = new DynamicParameters();

                        // 1. UPDATE文を位置プレースホルダ(?)に変更
                        var sqlUpdate = @"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = ?, [RecordId] = ?, [DeviceLabel] = ?,
                                [StartNum] = ?, [OutCoilCount] = ?, [PlcId] = ?,
                                [Comment1] = ?, [Comment2] = ?
                            WHERE [ID] = ?";

                        // 2. パラメータをSQL文の出現順と完全に一致させる
                        // --- SET句のパラメータ ---
                        updateParams.Add("p1", (int)MnemonicType.Process, DbType.Int32);
                        updateParams.Add("p2", process.Id, DbType.Int32);
                        updateParams.Add("p3", "L", DbType.String);
                        updateParams.Add("p4", (count * 5 + startNum), DbType.Int32);
                        updateParams.Add("p5", 5, DbType.Int32);
                        updateParams.Add("p6", plcId, DbType.Int32);
                        updateParams.Add("p7", process.Comment1 ?? "", DbType.String);
                        updateParams.Add("p8", process.Comment2 ?? "", DbType.String);
                        // --- WHERE句のパラメータ ---
                        updateParams.Add("p9", existing.ID, DbType.Int32);

                        connection.Execute(sqlUpdate, updateParams, transaction);
                    }
                    else
                    {
                        // ★修正: SQLのパラメータ名と数を修正
                        connection.Execute(@"
                            INSERT INTO [MnemonicDevice] (
                                [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId], [Comment1], [Comment2]
                            ) VALUES (
                                @MnemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment1, @Comment2
                            )",
                            parameters, transaction);
                    }

                    // --- 2. 対応するMemoryレコードを生成し、リストに蓄積 ---
                    int mnemonicStartNum = (count * 5 + startNum);
                    for (int i = 0; i < 5; i++) // OutCoilCount=5 は固定と仮定
                    {
                        string row_2 = i switch
                        {
                            0 => "開始条件",
                            1 => "開始",
                            2 => "実行中",
                            3 => "終了条件",
                            4 => "終了",
                            _ => ""
                        };

                        var memory = new Memory
                        {
                            PlcId = plcId,
                            Device = "L" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            MemoryCategory = 1, // L
                            DeviceNumber = mnemonicStartNum + i,
                            DeviceNumber1 = "L" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            DeviceNumber2 = "",
                            Category = "工程",
                            Row_1 = "工程" + process.Id.ToString(),
                            Row_2 = row_2,
                            Row_3 = process.Comment1,
                            Row_4 = process.Comment2,
                            Direct_Input = "",
                            Confirm = "",
                            Note = "",
                            GOT = "false",
                            MnemonicId = (int)MnemonicType.Process,
                            RecordId = process.Id,
                            OutcoilNumber = i
                        };
                        allMemoriesToSave.Add(memory);
                    }
                    count++;
                }

                // --- 3. ループ完了後、蓄積した全Memoryレコードを同じトランザクションで一括保存 ---
                if (allMemoriesToSave.Any())
                {
                    _memoryService.SaveMemoriesInternal(plcId, allMemoriesToSave, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// processDetailのリストを受け取り、MnemonicDeviceテーブルに保存する。
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="startNum"></param>
        /// <param name="plcId"></param>
        public void SaveMnemonicDeviceProcessDetail(List<ProcessDetail> processes, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.ProcessDetail);
                var existingLookup = allExisting.ToDictionary(m => m.RecordId, m => m);

                var allMemoriesToSave = new List<Memory>();
                int count = 0;

                foreach (ProcessDetail detail in processes)
                {
                    // 1. detail自体がnullの場合のみスキップする
                    if (detail == null) continue;

                    existingLookup.TryGetValue(detail.Id, out var existing);

                    // 2. OperationId がある場合のみ、Operation情報を取得
                    Operation? operation = null;
                    if (detail.OperationId.HasValue)
                    {
                        // ★ パフォーマンス改善: new せずに、フィールドの _repository を使う
                        operation = _repository.GetOperationById(detail.OperationId.Value);
                    }

                    string shortName = "";
                    if (detail.CategoryId.HasValue)
                    {
                        // 3. CategoryId がある場合のみ、カテゴリ情報を取得
                        var category = _repository.GetProcessDetailCategoryById(detail.CategoryId.Value);
                        if (category != null)
                        {
                            shortName = category.ShortName ?? "";
                        }
                    }

                    int mnemonicStartNum = (count * 5 + startNum);
                    // AccessRepositoryは、このメソッドのクラスのフィールドとして保持されている
                    // _repository を使うのが望ましいですが、ここでは元のコードの形式を維持します。
                    var repository = new AccessRepository(_connectionString);

                    // 結果を保持する変数を先に宣言し、デフォルト値を設定
                    string comment1 = "";
                    string comment2 = detail.Comment ?? "";

                    // 1. CYIdが存在するかどうかを正しくチェック
                    if (operation != null)
                    {
                        // 2. IDを使ってCYオブジェクトを取得
                        CY? cY = repository.GetCYById(operation.CYId!.Value);

                        // 3. CYオブジェクトが取得でき、かつその中にMacineIdが存在するかチェック
                        if (cY != null && cY.MacineId.HasValue)
                        {
                            // 4. MacineIdを使ってMachineオブジェクトを取得
                            Machine? machine = repository.GetMachineById(cY.MacineId.Value);

                            // 5. Machineオブジェクトが取得できたことを確認してから、コメントを生成
                            if (machine != null)
                            {
                                // CYNumやShortNameがnullの場合も考慮し、空文字列として結合
                                comment1 = (cY.CYNum ?? "") + (machine.ShortName ?? "");
                            }
                            else
                            {
                                // Machineが見つからなかった場合のエラーハンドリング（任意）
                                // 例: _errorAggregator.AddError(...);
                            }
                        }
                        else
                        {
                            // CYが見つからなかったか、CYにMacineIdがなかった場合のエラーハンドリング（任意）
                        }
                    }
                    else
                    {
                        comment1 = shortName; // Operationがない場合は、カテゴリのショートネームを使用
                    }

                    // --- MnemonicDeviceのUPSERT ---
                    var parameters = new DynamicParameters();
                    parameters.Add("MnemonicId", (int)MnemonicType.ProcessDetail, DbType.Int32);
                    parameters.Add("RecordId", detail.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "L", DbType.String);
                    parameters.Add("StartNum", (count * 5 + startNum), DbType.Int32); // ProcessDetailは10点区切り
                    parameters.Add("OutCoilCount", 5, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("Comment1", comment1, DbType.String);
                    parameters.Add("Comment2", comment2, DbType.String);

                    if (existing != null) // Update
                    {
                        var sqlUpdate = @"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = ?, [RecordId] = ?, [DeviceLabel] = ?,
                                [StartNum] = ?, [OutCoilCount] = ?, [PlcId] = ?,
                                [Comment1] = ?, [Comment2] = ?
                            WHERE [ID] = ?";

                        var updateParams = new DynamicParameters();
                        // パラメータをSQL文の '?' の出現順と完全に一致させる
                        updateParams.Add("p1", (int)MnemonicType.ProcessDetail, DbType.Int32);
                        updateParams.Add("p2", detail.Id, DbType.Int32);
                        updateParams.Add("p3", "L", DbType.String);
                        updateParams.Add("p4", (count * 5 + startNum), DbType.Int32); // ProcessDetailは10点区切り
                        updateParams.Add("p5", 5, DbType.Int32);
                        updateParams.Add("p6", plcId, DbType.Int32);
                        updateParams.Add("p7", comment1, DbType.String);
                        updateParams.Add("p8", comment2, DbType.String);
                        updateParams.Add("p9", existing.ID, DbType.Int32); // WHERE句のパラメータ

                        connection.Execute(sqlUpdate, updateParams, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
                            INSERT INTO [MnemonicDevice] (
                                [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId], [Comment1], [Comment2]
                            ) VALUES (
                                @MnemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment1, @Comment2
                            )",
                            parameters, transaction);
                    }

                    // --- 対応するMemoryレコードを生成し、リストに蓄積 ---
                    for (int i = 0; i < 5; i++) // OutCoilCount=10
                    {
                        string row_2 = i switch
                        {
                            0 => "開始条件",
                            1 => "開始",
                            2 => "実行中",
                            3 => "操作釦",
                            4 => "終了",
                            _ => ""
                        };

                        var memory = new Memory
                        {
                            PlcId = plcId,
                            Device = "L" + (mnemonicStartNum + i),
                            MemoryCategory = 1, // L
                            DeviceNumber = mnemonicStartNum + i,
                            DeviceNumber1 = "L" + (mnemonicStartNum + i),
                            Category = "工程詳細",
                            Row_1 = "詳細" + detail.Id.ToString(),
                            Row_2 = row_2,
                            Row_3 = comment1,
                            Row_4 = comment2,
                            MnemonicId = (int)MnemonicType.ProcessDetail,
                            RecordId = detail.Id,
                            OutcoilNumber = i
                        };
                        allMemoriesToSave.Add(memory);
                    }
                    count++;
                }

                // --- ループ完了後、蓄積した全Memoryレコードを同じトランザクションで一括保存 ---
                if (allMemoriesToSave.Any())
                {
                    _memoryService.SaveMemoriesInternal(plcId, allMemoriesToSave, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Operationのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceOperation(List<Operation> operations, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.Operation);
                var existingLookup = allExisting.ToDictionary(m => m.RecordId, m => m);
                var allMemoriesToSave = new List<Memory>(); // ★ 保存するメモリを蓄積するリスト

                int count = 0;
                foreach (Operation operation in operations)
                {
                    if (operation == null) continue;

                    existingLookup.TryGetValue(operation.Id, out var existing);

                    var parameters = new DynamicParameters();
                    parameters.Add("MnemonicId", (int)MnemonicType.Operation, DbType.Int32);
                    parameters.Add("RecordId", operation.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "M", DbType.String);
                    parameters.Add("StartNum", (count * 20 + startNum), DbType.Int32);
                    parameters.Add("OutCoilCount", 20, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("Comment1", operation.OperationName ?? "", DbType.String);
                    parameters.Add("Comment2", operation.OperationName ?? "", DbType.String);

                    if (existing != null)
                    {
                        var sqlUpdate = @"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = ?, [RecordId] = ?, [DeviceLabel] = ?,
                                [StartNum] = ?, [OutCoilCount] = ?, [PlcId] = ?,
                                [Comment1] = ?, [Comment2] = ?
                            WHERE [ID] = ?";

                        var updateParams = new DynamicParameters();
                        // パラメータをSQL文の '?' の出現順と完全に一致させる
                        updateParams.Add("p1", (int)MnemonicType.Operation, DbType.Int32);
                        updateParams.Add("p2", operation.Id, DbType.Int32);
                        updateParams.Add("p3", "M", DbType.String);
                        updateParams.Add("p4", (count * 20 + startNum), DbType.Int32);
                        updateParams.Add("p5", 20, DbType.Int32);
                        updateParams.Add("p6", plcId, DbType.Int32);
                        updateParams.Add("p7", operation.OperationName ?? "", DbType.String);
                        updateParams.Add("p8", operation.OperationName ?? "", DbType.String);
                        updateParams.Add("p9", existing.ID, DbType.Int32); // WHERE句のパラメータ

                        connection.Execute(sqlUpdate, updateParams, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
                            INSERT INTO [MnemonicDevice] (
                                [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId], [Comment1], [Comment2]
                            ) VALUES (
                                @MnemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment1, @Comment2
                            )",
                            parameters, transaction);
                    }

                    int mnemonicStartNum = (count * 20 + startNum);
                    // AccessRepositoryは、このメソッドのクラスのフィールドとして保持されている
                    // _repository を使うのが望ましいですが、ここでは元のコードの形式を維持します。
                    var repository = new AccessRepository(_connectionString);

                    // 結果を保持する変数を先に宣言し、デフォルト値を設定
                    string comment1 = "";
                    string comment2 = "";

                    // 1. CYIdが存在するかどうかを正しくチェック
                    if (operation.CYId.HasValue)
                    {
                        // 2. IDを使ってCYオブジェクトを取得
                        CY? cY = repository.GetCYById(operation.CYId.Value);

                        // 3. CYオブジェクトが取得でき、かつその中にMacineIdが存在するかチェック
                        if (cY != null && cY.MacineId.HasValue)
                        {
                            // 4. MacineIdを使ってMachineオブジェクトを取得
                            Machine? machine = repository.GetMachineById(cY.MacineId.Value);

                            // 5. Machineオブジェクトが取得できたことを確認してから、コメントを生成
                            if (machine != null)
                            {
                                // CYNumやShortNameがnullの場合も考慮し、空文字列として結合
                                comment1 = (cY.CYNum ?? "") + (machine.ShortName ?? "");
                            }
                            else
                            {
                                // Machineが見つからなかった場合のエラーハンドリング（任意）
                                // 例: _errorAggregator.AddError(...);
                            }
                        }
                        else
                        {
                            // CYが見つからなかったか、CYにMacineIdがなかった場合のエラーハンドリング（任意）
                        }
                    }
                    else
                    {
                        // OperationにCYIdが設定されていなかった場合のエラーハンドリング（任意）
                    }

                    for (int i = 0; i < 20; i++) // OutCoilCount=5 は固定と仮定
                    {
                        string row_2 = i switch
                        {
                            0 => "自動運転",
                            1 => "操作ｽｲｯﾁ",
                            2 => "手動運転",
                            3 => "ｶｳﾝﾀ",
                            4 => "個別ﾘｾｯﾄ",
                            5 => "操作開始",
                            6 => "出力可",
                            7 => "開始",
                            8 => "切指令",
                            9 => "予備",
                            10 => "速度1",
                            11 => "速度2",
                            12 => "速度3",
                            13 => "速度4",
                            14 => "速度5",
                            15 => "強制減速",
                            16 => "終了位置",
                            17 => "出力切",
                            18 => "BK作動",
                            19 => "完了",

                            _ => ""
                        };

                        var memory = new Memory
                        {
                            PlcId = plcId,
                            Device = "M" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            MemoryCategory = 2, // M
                            DeviceNumber = mnemonicStartNum + i,
                            DeviceNumber1 = "M" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            DeviceNumber2 = "",
                            Category = "操作",
                            Row_1 = "操作" + operation.Id.ToString(),
                            Row_2 = row_2,
                            Row_3 = comment1,
                            Row_4 = comment2,
                            Direct_Input = "",
                            Confirm = "",
                            Note = "",
                            GOT = "False",
                            MnemonicId = (int)MnemonicType.Operation,
                            RecordId = operation.Id,
                            OutcoilNumber = i
                        };
                        allMemoriesToSave.Add(memory);
                    }
                    count++;
                }
                // --- ループ完了後、蓄積した全Memoryレコードを同じトランザクションで一括保存 ---
                if (allMemoriesToSave.Any())
                {
                    _memoryService.SaveMemoriesInternal(plcId, allMemoriesToSave, connection, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Cylinderのリストを受け取り、MnemonicDeviceテーブルに保存する
        public void SaveMnemonicDeviceCY(List<CY> cylinders, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.CY);
                var existingLookup = allExisting.ToDictionary(m => m.RecordId, m => m);
                var allMemoriesToSave = new List<Memory>(); // ★ 保存するメモリを蓄積するリスト

                int count = 0;
                foreach (CY cylinder in cylinders)
                {
                    if (cylinder == null) continue;
                    existingLookup.TryGetValue(cylinder.Id, out var existing);

                    var parameters = new DynamicParameters();
                    parameters.Add("MnemonicId", (int)MnemonicType.CY, DbType.Int32);
                    parameters.Add("RecordId", cylinder.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "M", DbType.String);
                    parameters.Add("StartNum", (count * 50 + startNum), DbType.Int32);
                    parameters.Add("OutCoilCount", 50, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("Comment1", cylinder.CYNum, DbType.String);
                    parameters.Add("Comment2", cylinder.CYNum, DbType.String);

                    if (existing != null) // Update
                    {
                        var sqlUpdate = @"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = ?, [RecordId] = ?, [DeviceLabel] = ?,
                                [StartNum] = ?, [OutCoilCount] = ?, [PlcId] = ?,
                                [Comment1] = ?, [Comment2] = ?
                            WHERE [ID] = ?";

                        var updateParams = new DynamicParameters();
                        // パラメータをSQL文の '?' の出現順と完全に一致させる
                        updateParams.Add("p1", (int)MnemonicType.CY, DbType.Int32);
                        updateParams.Add("p2", cylinder.Id, DbType.Int32);
                        updateParams.Add("p3", "M", DbType.String);
                        updateParams.Add("p4", (count * 50 + startNum), DbType.Int32); // Cylinderは100点区切り
                        updateParams.Add("p5", 50, DbType.Int32);
                        updateParams.Add("p6", plcId, DbType.Int32);
                        updateParams.Add("p7", cylinder.CYNum ?? "", DbType.String);
                        updateParams.Add("p8", cylinder.CYNum ?? "", DbType.String);
                        updateParams.Add("p9", existing.ID, DbType.Int32); // WHERE句のパラメータ

                        connection.Execute(sqlUpdate, updateParams, transaction);
                    }
                    else
                    {
                        // ★修正: SQLのパラメータ名のタイプミスを修正
                        connection.Execute(@"
                            INSERT INTO [MnemonicDevice] (
                                [MnemonicId], [RecordId], [DeviceLabel], [StartNum], [OutCoilCount], [PlcId], [Comment1], [Comment2]
                            ) VALUES (
                                @MnemonicId, @RecordId, @DeviceLabel, @StartNum, @OutCoilCount, @PlcId, @Comment1, @Comment2
                            )",
                            parameters, transaction);
                    }
                    int mnemonicStartNum = (count * 50 + startNum);
                    // AccessRepositoryは、このメソッドのクラスのフィールドとして保持されている
                    // _repository を使うのが望ましいですが、ここでは元のコードの形式を維持します。
                    var repository = new AccessRepository(_connectionString);

                    // 結果を保持する変数を先に宣言し、デフォルト値を設定
                    string comment1 = "";



                    // 3. CYオブジェクトが取得でき、かつその中にMacineIdが存在するかチェック
                    if (cylinder != null && cylinder.MacineId.HasValue)
                    {
                        // 4. MacineIdを使ってMachineオブジェクトを取得
                        Machine? machine = repository.GetMachineById(cylinder.MacineId.Value);

                        // 5. Machineオブジェクトが取得できたことを確認してから、コメントを生成
                        if (machine != null)
                        {
                            // CYNumやShortNameがnullの場合も考慮し、空文字列として結合
                            comment1 = (cylinder.CYNum ?? "") + (machine.ShortName ?? "");
                        }
                        else
                        {
                            // Machineが見つからなかった場合のエラーハンドリング（任意）
                            // 例: _errorAggregator.AddError(...);
                        }
                    }
                    else
                    {
                        // CYが見つからなかったか、CYにMacineIdがなかった場合のエラーハンドリング（任意）
                    }

                    for (int i = 0; i < 50; i++) // OutCoilCount=5 は固定と仮定
                    {
                        string row_2 = i switch
                        {
                            0 => "行き方向",
                            1 => "帰り方向",
                            2 => "行き方向",
                            3 => "帰り方向",
                            4 => "初回",
                            5 => "行き方向",
                            6 => "帰り方向",
                            7 => "行き手動",
                            8 => "帰り手動",
                            9 => "シングル",
                            10 => "指令ON",
                            11 => "予備",
                            12 => "行き方向",
                            13 => "帰り方向",
                            14 => "IL無効",
                            15 => "行き自動",
                            16 => "帰り自動",
                            17 => "行き手動",
                            18 => "帰り手動",
                            19 => "保持出力",
                            20 => "保持出力",
                            21 => "速度指令",
                            22 => "速度指令",
                            23 => "速度指令",
                            24 => "速度指令",
                            25 => "速度指令",
                            26 => "速度指令",
                            27 => "速度指令",
                            28 => "速度指令",
                            29 => "速度指令",
                            30 => "速度指令",
                            31 => "強制減速",
                            32 => "予備",
                            33 => "高速停止",
                            34 => "停止時　",
                            35 => "行きOK",
                            36 => "帰りOK",
                            37 => "指令OK",
                            38 => "予備",
                            39 => "予備",
                            40 => "ｻｰﾎﾞ軸",
                            41 => "ｻｰﾎﾞ作動",
                            42 => "ｻｰﾎﾞJOG",
                            43 => "ｻｰﾎﾞJOG",
                            44 => "",
                            45 => "",
                            46 => "",
                            47 => "",
                            48 => "",
                            49 => "",

                            _ => ""
                        };

                        string row_3 = i switch
                        {
                            0 => "自動指令",
                            1 => "自動指令",
                            2 => "手動指令",
                            3 => "手動指令",
                            4 => "帰り指令",
                            5 => "自動保持",
                            6 => "自動保持",
                            7 => "JOG",
                            8 => "JOG",
                            9 => "OFF指令",
                            10 => "",
                            11 => "",
                            12 => "自動",
                            13 => "自動",
                            14 => "",
                            15 => "ILOK",
                            16 => "ILOK",
                            17 => "ILOK",
                            18 => "ILOK",
                            19 => "行き",
                            20 => "帰り",
                            21 => "1",
                            22 => "2",
                            23 => "3",
                            24 => "4",
                            25 => "5",
                            26 => "6",
                            27 => "7",
                            28 => "8",
                            29 => "9",
                            30 => "10",
                            31 => "",
                            32 => "",
                            33 => "記憶",
                            34 => "ﾌﾞﾚｰｷ待ち",
                            35 => "",
                            36 => "",
                            37 => "",
                            38 => "",
                            39 => "",
                            40 => "停止",
                            41 => "ｴﾗｰ発生",
                            42 => "行きOK",
                            43 => "帰りOK",
                            44 => "",
                            45 => "",
                            46 => "",
                            47 => "",
                            48 => "",
                            49 => "",

                            _ => ""
                        };

                        var memory = new Memory
                        {
                            PlcId = plcId,
                            Device = "M" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            MemoryCategory = 2, // M
                            DeviceNumber = mnemonicStartNum + i,
                            DeviceNumber1 = "M" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            DeviceNumber2 = "",
                            Category = "出力",
                            Row_1 = "出力" + cylinder.Id.ToString(),
                            Row_2 = row_2,
                            Row_3 = row_3,
                            Row_4 = comment1,
                            Direct_Input = "",
                            Confirm = "",
                            Note = "",
                            GOT = "False",
                            MnemonicId = (int)MnemonicType.CY,
                            RecordId = cylinder.Id,
                            OutcoilNumber = i
                        };
                        allMemoriesToSave.Add(memory);
                    }
                    count++;
                }
                // --- ループ完了後、蓄積した全Memoryレコードを同じトランザクションで一括保存 ---
                if (allMemoriesToSave.Any())
                {
                    _memoryService.SaveMemoriesInternal(plcId, allMemoriesToSave, connection, transaction);
                }

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