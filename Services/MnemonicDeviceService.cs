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

        // MnemonicDeviceテーブルからPlcIdに基づいてデータを取得する
        public List<MnemonicDevice> GetMnemonicDevice(int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicDevice WHERE PlcId = @PlcId";
            return connection.Query<MnemonicDevice>(sql, new { PlcId = plcId }).ToList();
        }

        // MnemonicDeviceテーブルからPlcIdとMnemonicIdに基づいてデータを取得する
        public List<MnemonicDevice> GetMnemonicDeviceByMnemonic(int plcId, int mnemonicId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM MnemonicDevice WHERE PlcId = @PlcId AND MnemonicId = @MnemonicId";
            return connection.Query<MnemonicDevice>(sql, new { PlcId = plcId, MnemonicId = mnemonicId }).ToList();
        }

        // Processesのリストを受け取り、MnemonicDeviceテーブルに保存する
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

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [DeviceLabel] = @DeviceLabel,
                                [StartNum] = @StartNum, [OutCoilCount] = @OutCoilCount, [PlcId] = @PlcId,
                                [Comment1] = @Comment1, [Comment2] = @Comment2
                            WHERE [ID] = @ID",
                            parameters, transaction);
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
                            MemoryCategory = (int)MnemonicType.Process,
                            DeviceNumber = mnemonicStartNum + i,
                            Device = "L" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            Category = "工程",
                            Row_1 = "工程" + process.Id.ToString(),
                            Row_2 = row_2,
                            Row_3 = process.Comment1,
                            Row_4 = process.Comment2,
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

        // 修正: AccessRepository のインスタンス化に必要な connectionString を渡すように修正  
        public void SaveMnemonicDeviceProcessDetail(List<ProcessDetail> processes, int startNum, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var allExisting = GetMnemonicDeviceByMnemonic(plcId, (int)MnemonicType.ProcessDetail);
                var existingLookup = allExisting.ToDictionary(mnemonicDevice => mnemonicDevice.RecordId, mnemonicDevice => mnemonicDevice);
                var repository = new AccessRepository(_connectionString);
                var allMemoriesToSave = new List<Memory>(); // ★ 保存するメモリを蓄積するリスト

                int count = 0;
                foreach (ProcessDetail detail in processes)
                {
                    if (detail == null || detail.OperationId.HasValue) continue;

                    existingLookup.TryGetValue(detail.Id, out var existing);

                    var operation = repository.GetOperationById(detail.OperationId.Value);
                    var comment1 = operation?.OperationName ?? "";
                    var comment2 = detail.DetailName ?? "";

                    var parameters = new DynamicParameters();
                    parameters.Add("MnemonicId", (int)MnemonicType.ProcessDetail, DbType.Int32);
                    parameters.Add("RecordId", detail.Id, DbType.Int32);
                    parameters.Add("DeviceLabel", "L", DbType.String);
                    parameters.Add("StartNum", (count * 5 + startNum), DbType.Int32);
                    parameters.Add("OutCoilCount", 5, DbType.Int32);
                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("Comment1", comment1, DbType.String);
                    parameters.Add("Comment2", comment2, DbType.String);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"  
                            UPDATE [MnemonicDevice] SET  
                                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [DeviceLabel] = @DeviceLabel,  
                                [StartNum] = @StartNum, [OutCoilCount] = @OutCoilCount, [PlcId] = @PlcId,  
                                [Comment1] = @Comment1, [Comment2] = @Comment2  
                            WHERE [ID] = @ID",
                            parameters, transaction);
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
                            MemoryCategory = (int)MnemonicType.Process,
                            DeviceNumber = mnemonicStartNum + i,
                            Device = "L" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            Category = "工程詳細",
                            Row_1 = "詳細" + detail.Id.ToString(),
                            Row_2 = row_2,
                            Row_3 = comment1,
                            Row_4 = comment2,
                            MnemonicId = (int)MnemonicType.Process,
                            RecordId = detail.Id,
                            OutcoilNumber = i
                        };
                        allMemoriesToSave.Add(memory);
                    }
                    count++;
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
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [DeviceLabel] = @DeviceLabel,
                                [StartNum] = @StartNum, [OutCoilCount] = @OutCoilCount, [PlcId] = @PlcId,
                                [Comment1] = @Comment1, [Comment2] = @Comment2
                            WHERE [ID] = @ID",
                            parameters, transaction);
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
                        CY? cY = repository.GetCY(operation.CYId.Value);

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
                            MemoryCategory = (int)MnemonicType.Process,
                            DeviceNumber = mnemonicStartNum + i,
                            Device = "L" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            Category = "操作",
                            Row_1 = "操作" + operation.Id.ToString(),
                            Row_2 = row_2,
                            Row_3 = comment1,
                            Row_4 = comment2,
                            MnemonicId = (int)MnemonicType.Process,
                            RecordId = operation.Id,
                            OutcoilNumber = i
                        };
                        allMemoriesToSave.Add(memory);
                    }
                    count++;
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

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                            UPDATE [MnemonicDevice] SET
                                [MnemonicId] = @MnemonicId, [RecordId] = @RecordId, [DeviceLabel] = @DeviceLabel,
                                [StartNum] = @StartNum, [OutCoilCount] = @OutCoilCount, [PlcId] = @PlcId,
                                [Comment1] = @Comment1, [Comment2] = @Comment2
                            WHERE [ID] = @ID",
                            parameters, transaction);
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
                    int mnemonicStartNum = (count * 20 + startNum);
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
                            MemoryCategory = (int)MnemonicType.Process,
                            DeviceNumber = mnemonicStartNum + i,
                            Device = "L" + (mnemonicStartNum + i), // デバイス名の形式を修正
                            Category = "出力",
                            Row_1 = "出力" + cylinder.Id.ToString(),
                            Row_2 = row_2,
                            Row_3 = row_3,
                            Row_4 = comment1,
                            MnemonicId = (int)MnemonicType.Process,
                            RecordId = cylinder.Id,
                            OutcoilNumber = i
                        };
                        allMemoriesToSave.Add(memory);
                    }
                    count++;
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