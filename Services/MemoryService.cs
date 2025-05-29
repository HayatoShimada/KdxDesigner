using Dapper;

using KdxDesigner.Models;

using System.Data;
using System.Data.OleDb;
using System.Diagnostics;

namespace KdxDesigner.Services
{
    internal class MemoryService
    {
        private readonly string _connectionString;

        public MemoryService(AccessRepository repository)
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

        public void SaveMemories(List<Memory> memories, Action<string>? progressCallback = null)
        {
            using var connection = new OleDbConnection(_connectionString);

            connection.Open();
            using var transaction = connection.BeginTransaction();

            var allExisting = connection.Query<Memory>("SELECT * FROM Memory", transaction: transaction).ToList();

            for (int i = 0; i < memories.Count; i++)
            {
                var memory = memories[i];
                progressCallback?.Invoke($"[{i + 1}/{memories.Count}] 保存中: {memory.Device}");

                try
                {
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    var existing = allExisting.FirstOrDefault(m =>
                        m.PlcId == memory.PlcId && m.Device == memory.Device);

                    var parameters = new DynamicParameters();
                    parameters.Add("PlcId", memory.PlcId ?? 0, DbType.Int32);
                    parameters.Add("MemoryCategory", memory.MemoryCategory ?? 0, DbType.Int32);
                    parameters.Add("DeviceNumber", memory.DeviceNumber ?? 0, DbType.Int32);
                    parameters.Add("DeviceNumber1", memory.DeviceNumber1 ?? "", DbType.String);
                    parameters.Add("DeviceNumber2", memory.DeviceNumber2 ?? "", DbType.String);
                    parameters.Add("Device", memory.Device ?? "", DbType.String);
                    parameters.Add("Category", memory.Category ?? "", DbType.String);
                    parameters.Add("Row_1", memory.Row_1 ?? "", DbType.String);
                    parameters.Add("Row_2", memory.Row_2 ?? "", DbType.String);
                    parameters.Add("Row_3", memory.Row_3 ?? "", DbType.String);
                    parameters.Add("Row_4", memory.Row_4 ?? "", DbType.String);
                    parameters.Add("Direct_Input", memory.Direct_Input ?? "", DbType.String);
                    parameters.Add("Confirm", memory.Confirm ?? "", DbType.String);
                    parameters.Add("Note", memory.Note ?? "", DbType.String);
                    parameters.Add("CreatedAt", memory.CreatedAt ?? now, DbType.String);
                    parameters.Add("UpdatedAt", now, DbType.String);
                    parameters.Add("GOT", memory.GOT ?? false, DbType.Boolean);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                            UPDATE [Memory] SET
                                [MemoryCategory] = @MemoryCategory,
                                [DeviceNumber] = @DeviceNumber,
                                [DeviceNumber1] = @DeviceNumber1,
                                [DeviceNumber2] = @DeviceNumber2,
                                [Category] = @Category,
                                [Row_1] = @Row_1,
                                [Row_2] = @Row_2,
                                [Row_3] = @Row_3,
                                [Row_4] = @Row_4,
                                [Direct_Input] = @Direct_Input,
                                [Confirm] = @Confirm,
                                [Note] = @Note,
                                [UpdatedAt] = @UpdatedAt,
                                [GOT] = @GOT
                            WHERE [ID] = @ID",
                        parameters, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
                            INSERT INTO [Memory] (
                                [PlcId], [MemoryCategory], [DeviceNumber],
                                [DeviceNumber1], [DeviceNumber2], [Device],
                                [Category], [Row_1], [Row_2], [Row_3], [Row_4],
                                [Direct_Input], [Confirm], [Note],
                                [CreatedAt], [UpdatedAt], [GOT]
                            ) VALUES (
                                @PlcId, @MemoryCategory, @DeviceNumber,
                                @DeviceNumber1, @DeviceNumber2, @Device,
                                @Category, @Row_1, @Row_2, @Row_3, @Row_4,
                                @Direct_Input, @Confirm, @Note,
                                @CreatedAt, @UpdatedAt, @GOT
                            )",
                        parameters, transaction);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Device={memory.Device} 保存失敗 → {ex.Message}");
                    throw;
                }
            }

            transaction.Commit();

            // ✅ 挿入後、件数確認
            var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM [Memory]");
            Debug.WriteLine($"[確認] Memory テーブルのレコード数: {count}");
        }

        // MnemonicDeviceのリストからメモリを登録する
        public bool SaveMnemonicMemories(MnemonicDevice device)
        {
            using var connection = new OleDbConnection(_connectionString);
            var count = 0;

            connection.Open();
            using var transaction = connection.BeginTransaction();
            var allExisting = connection.Query<Memory>("SELECT * FROM Memory", transaction: transaction).ToList();
            try
            {
                // MnemonicDeviceの情報をMemoryに変換
                int category = 0;
                switch (device.DeviceLabel)
                {
                    case "L":
                        category = 1;
                        break;
                    case "M":
                        category = 2;
                        break;
                    default:
                        // 誰かエラー処理書いて
                        category = 1;
                        break;
                }
                var memoryCategory = "";
                switch (device.MnemonicId)
                {
                    case 1:
                        memoryCategory = "工程";
                        break;
                    case 2:
                        memoryCategory = "工程詳細";
                        break;
                    case 3:
                        memoryCategory = "操作";
                        break;
                    case 4:
                        memoryCategory = "出力";
                        break;
                    default:
                        // 誰かエラー処理書いて
                        memoryCategory = "なし";
                        break;
                }

                // OutCoilCount分だけクエリを実行
                while (count < device.OutCoilCount)
                {
                    var deviceNum = device.StartNum + count;
                    var deviceString = device.DeviceLabel + deviceNum.ToString();

                    var memory = new Memory
                    {
                        PlcId = device.PlcId,
                        MemoryCategory = category,
                        DeviceNumber = device.StartNum + count,
                        DeviceNumber1 = deviceString,
                        DeviceNumber2 = "",
                        Device = deviceString,
                        Category = memoryCategory,
                        Row_1 = memoryCategory,
                        Row_2 = device.Comment,
                        Row_3 = count.ToString(),
                        Row_4 = "",
                        Direct_Input = "",
                        Confirm = memoryCategory + device.Comment + count.ToString() + "",
                        Note = "",
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        GOT = false,
                        MnemonicDeviceId = (int?)device.ID,
                        OutcoilNumber = count
                    };

                    count++;


                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var existing = allExisting.FirstOrDefault(m =>
                        m.PlcId == memory.PlcId && m.Device == memory.Device);

                    var parameters = new DynamicParameters();
                    parameters.Add("PlcId", memory.PlcId ?? 0, DbType.Int32);
                    parameters.Add("MemoryCategory", memory.MemoryCategory ?? 0, DbType.Int32);
                    parameters.Add("DeviceNumber", memory.DeviceNumber ?? 0, DbType.Int32);
                    parameters.Add("DeviceNumber1", memory.DeviceNumber1 ?? "", DbType.String);
                    parameters.Add("DeviceNumber2", memory.DeviceNumber2 ?? "", DbType.String);
                    parameters.Add("Device", memory.Device ?? "", DbType.String);
                    parameters.Add("Category", memory.Category ?? "", DbType.String);
                    parameters.Add("Row_1", memory.Row_1 ?? "", DbType.String);
                    parameters.Add("Row_2", memory.Row_2 ?? "", DbType.String);
                    parameters.Add("Row_3", memory.Row_3 ?? "", DbType.String);
                    parameters.Add("Row_4", memory.Row_4 ?? "", DbType.String);
                    parameters.Add("Direct_Input", memory.Direct_Input ?? "", DbType.String);
                    parameters.Add("Confirm", memory.Confirm ?? "", DbType.String);
                    parameters.Add("Note", memory.Note ?? "", DbType.String);
                    parameters.Add("CreatedAt", memory.CreatedAt ?? now, DbType.String);
                    parameters.Add("UpdatedAt", now, DbType.String);
                    parameters.Add("GOT", memory.GOT ?? false, DbType.Boolean);
                    parameters.Add("MnemonicDeviceId", memory.MnemonicDeviceId ?? 0, DbType.Int32);
                    parameters.Add("OutcoilNumber", memory.OutcoilNumber ?? 0, DbType.Int32);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                        UPDATE [Memory] SET
                            [MemoryCategory] = @MemoryCategory,
                            [DeviceNumber] = @DeviceNumber,
                            [DeviceNumber1] = @DeviceNumber1,
                            [DeviceNumber2] = @DeviceNumber2,
                            [Category] = @Category,
                            [Row_1] = @Row_1,
                            [Row_2] = @Row_2,
                            [Row_3] = @Row_3,
                            [Row_4] = @Row_4,
                            [Direct_Input] = @Direct_Input,
                            [Confirm] = @Confirm,
                            [Note] = @Note,
                            [UpdatedAt] = @UpdatedAt,
                            [GOT] = @GOT,
                            [MnemonicDeviceId] = @MnemonicDeviceId,
                            [OutcoilNumber] = @OutcoilNumber
                        WHERE [ID] = @ID",
                        parameters, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
                        INSERT INTO [Memory] (
                            [PlcId], [MemoryCategory], [DeviceNumber],
                            [DeviceNumber1], [DeviceNumber2], [Device],
                            [Category], [Row_1], [Row_2], [Row_3], [Row_4],
                            [Direct_Input], [Confirm], [Note],
                            [CreatedAt], [UpdatedAt], [GOT],
                            [MnemonicDeviceId], [OutcoilNumber]
                        ) VALUES (
                            @PlcId, @MemoryCategory, @DeviceNumber,
                            @DeviceNumber1, @DeviceNumber2, @Device,
                            @Category, @Row_1, @Row_2, @Row_3, @Row_4,
                            @Direct_Input, @Confirm, @Note,
                            @CreatedAt, @UpdatedAt, @GOT,
                            @MnemonicDeviceId, @OutcoilNumber
                        )",
                        parameters, transaction);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Device={device.ID} 保存失敗 → {ex.Message}");
                return false;
            }

            transaction.Commit();
            Debug.WriteLine($"[確認] Memory テーブルのレコード数: {count}");
            return true;

        }


        // MnemonicDeviceのリストからメモリを登録する
        public bool SaveMnemonicTimerMemoriesZR(MnemonicTimerDevice device)
        {
            using var connection = new OleDbConnection(_connectionString);
            var count = 0;

            connection.Open();
            using var transaction = connection.BeginTransaction();
            var allExisting = connection.Query<Memory>("SELECT * FROM Memory", transaction: transaction).ToList();
            try
            {
                // MnemonicDeviceの情報をMemoryに変換
                int category = 0;
                var memoryCategory = "";
                switch (device.MnemonicId)
                {
                    case 1:
                        memoryCategory = "工程";
                        break;
                    case 2:
                        memoryCategory = "工程詳細";
                        break;
                    case 3:
                        memoryCategory = "操作";
                        break;
                    case 4:
                        memoryCategory = "出力";
                        break;
                    default:
                        // 誰かエラー処理書いて
                        memoryCategory = "なし";
                        break;
                }

                // "T" を削除してから int に変換
                var TdeviceNum = int.Parse(device.TimerDevice.Replace("ZR", ""));
                var DdeviceNum = int.Parse(device.ProcessTimerDevice.Replace("T", ""));

                var memoryZR = new Memory
                {
                    PlcId = device.PlcId,
                    MemoryCategory = category,
                    DeviceNumber = TdeviceNum,
                    DeviceNumber1 = device.TimerDevice,
                    DeviceNumber2 = "",
                    Device = device.TimerDevice,
                    Category = memoryCategory,
                    Row_1 = memoryCategory,
                    Row_2 = "",
                    Row_3 = "",
                    Row_4 = "",
                    Direct_Input = "",
                    Confirm = "",
                    Note = "",
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    GOT = false,
                    MnemonicTimerDeviceId = (int?)device.ID,
                    OutcoilNumber = count
                };

                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var existing = allExisting.FirstOrDefault(m =>
                    m.PlcId == memoryZR.PlcId && m.Device == memoryZR.Device);

                var parameters = new DynamicParameters();
                parameters.Add("PlcId", memoryZR.PlcId ?? 0, DbType.Int32);
                parameters.Add("MemoryCategory", memoryZR.MemoryCategory ?? 0, DbType.Int32);
                parameters.Add("DeviceNumber", memoryZR.DeviceNumber ?? 0, DbType.Int32);
                parameters.Add("DeviceNumber1", memoryZR.DeviceNumber1 ?? "", DbType.String);
                parameters.Add("DeviceNumber2", memoryZR.DeviceNumber2 ?? "", DbType.String);
                parameters.Add("Device", memoryZR.Device ?? "", DbType.String);
                parameters.Add("Category", memoryZR.Category ?? "", DbType.String);
                parameters.Add("Row_1", memoryZR.Row_1 ?? "", DbType.String);
                parameters.Add("Row_2", memoryZR.Row_2 ?? "", DbType.String);
                parameters.Add("Row_3", memoryZR.Row_3 ?? "", DbType.String);
                parameters.Add("Row_4", memoryZR.Row_4 ?? "", DbType.String);
                parameters.Add("Direct_Input", memoryZR.Direct_Input ?? "", DbType.String);
                parameters.Add("Confirm", memoryZR.Confirm ?? "", DbType.String);
                parameters.Add("Note", memoryZR.Note ?? "", DbType.String);
                parameters.Add("CreatedAt", memoryZR.CreatedAt ?? now, DbType.String);
                parameters.Add("UpdatedAt", now, DbType.String);
                parameters.Add("GOT", memoryZR.GOT ?? false, DbType.Boolean);
                parameters.Add("MnemonicDeviceId", memoryZR.MnemonicDeviceId ?? 0, DbType.Int32);
                parameters.Add("OutcoilNumber", memoryZR.OutcoilNumber ?? 0, DbType.Int32);

                if (existing != null)
                {
                    parameters.Add("ID", existing.ID, DbType.Int32);
                    connection.Execute(@"
                        UPDATE [Memory] SET
                            [MemoryCategory] = @MemoryCategory,
                            [DeviceNumber] = @DeviceNumber,
                            [DeviceNumber1] = @DeviceNumber1,
                            [DeviceNumber2] = @DeviceNumber2,
                            [Category] = @Category,
                            [Row_1] = @Row_1,
                            [Row_2] = @Row_2,
                            [Row_3] = @Row_3,
                            [Row_4] = @Row_4,
                            [Direct_Input] = @Direct_Input,
                            [Confirm] = @Confirm,
                            [Note] = @Note,
                            [UpdatedAt] = @UpdatedAt,
                            [GOT] = @GOT,
                            [MnemonicDeviceId] = @MnemonicDeviceId,
                            [OutcoilNumber] = @OutcoilNumber
                        WHERE [ID] = @ID",
                    parameters, transaction);
                }
                else
                {
                    connection.Execute(@"
                        INSERT INTO [Memory] (
                            [PlcId], [MemoryCategory], [DeviceNumber],
                            [DeviceNumber1], [DeviceNumber2], [Device],
                            [Category], [Row_1], [Row_2], [Row_3], [Row_4],
                            [Direct_Input], [Confirm], [Note],
                            [CreatedAt], [UpdatedAt], [GOT],
                            [MnemonicDeviceId], [OutcoilNumber]
                        ) VALUES (
                            @PlcId, @MemoryCategory, @DeviceNumber,
                            @DeviceNumber1, @DeviceNumber2, @Device,
                            @Category, @Row_1, @Row_2, @Row_3, @Row_4,
                            @Direct_Input, @Confirm, @Note,
                            @CreatedAt, @UpdatedAt, @GOT,
                            @MnemonicDeviceId, @OutcoilNumber
                        )",
                    parameters, transaction);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Device={device.ID} 保存失敗 → {ex.Message}");
                return false;
            }

            transaction.Commit();
            Debug.WriteLine($"[確認] Memory テーブルのレコード数: {count}");
            return true;

        }


        // MnemonicDeviceのリストからメモリを登録する
        public bool SaveMnemonicTimerMemoriesT(MnemonicTimerDevice device)
        {
            using var connection = new OleDbConnection(_connectionString);
            var count = 0;

            connection.Open();
            using var transaction = connection.BeginTransaction();
            var allExisting = connection.Query<Memory>("SELECT * FROM Memory", transaction: transaction).ToList();
            try
            {
                // MnemonicDeviceの情報をMemoryに変換
                int category = 0;
                var memoryCategory = "";
                switch (device.MnemonicId)
                {
                    case 1:
                        memoryCategory = "工程";
                        break;
                    case 2:
                        memoryCategory = "工程詳細";
                        break;
                    case 3:
                        memoryCategory = "操作";
                        break;
                    case 4:
                        memoryCategory = "出力";
                        break;
                    default:
                        // 誰かエラー処理書いて
                        memoryCategory = "なし";
                        break;
                }

                // "T" を削除してから int に変換
                var DdeviceNum = int.Parse(device.ProcessTimerDevice.Replace("T", ""));

                var memoryZR = new Memory
                {
                    PlcId = device.PlcId,
                    MemoryCategory = category,
                    DeviceNumber = DdeviceNum,
                    DeviceNumber1 = device.ProcessTimerDevice,
                    DeviceNumber2 = "",
                    Device = device.ProcessTimerDevice,
                    Category = memoryCategory,
                    Row_1 = memoryCategory,
                    Row_2 = "",
                    Row_3 = "",
                    Row_4 = "",
                    Direct_Input = "",
                    Confirm = "",
                    Note = "",
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    GOT = false,
                    MnemonicTimerDeviceId = (int?)device.ID,
                    OutcoilNumber = count
                };

                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var existing = allExisting.FirstOrDefault(m =>
                    m.PlcId == memoryZR.PlcId && m.Device == memoryZR.Device);

                var parameters = new DynamicParameters();
                parameters.Add("PlcId", memoryZR.PlcId ?? 0, DbType.Int32);
                parameters.Add("MemoryCategory", memoryZR.MemoryCategory ?? 0, DbType.Int32);
                parameters.Add("DeviceNumber", memoryZR.DeviceNumber ?? 0, DbType.Int32);
                parameters.Add("DeviceNumber1", memoryZR.DeviceNumber1 ?? "", DbType.String);
                parameters.Add("DeviceNumber2", memoryZR.DeviceNumber2 ?? "", DbType.String);
                parameters.Add("Device", memoryZR.Device ?? "", DbType.String);
                parameters.Add("Category", memoryZR.Category ?? "", DbType.String);
                parameters.Add("Row_1", memoryZR.Row_1 ?? "", DbType.String);
                parameters.Add("Row_2", memoryZR.Row_2 ?? "", DbType.String);
                parameters.Add("Row_3", memoryZR.Row_3 ?? "", DbType.String);
                parameters.Add("Row_4", memoryZR.Row_4 ?? "", DbType.String);
                parameters.Add("Direct_Input", memoryZR.Direct_Input ?? "", DbType.String);
                parameters.Add("Confirm", memoryZR.Confirm ?? "", DbType.String);
                parameters.Add("Note", memoryZR.Note ?? "", DbType.String);
                parameters.Add("CreatedAt", memoryZR.CreatedAt ?? now, DbType.String);
                parameters.Add("UpdatedAt", now, DbType.String);
                parameters.Add("GOT", memoryZR.GOT ?? false, DbType.Boolean);
                parameters.Add("MnemonicDeviceId", memoryZR.MnemonicDeviceId ?? 0, DbType.Int32);
                parameters.Add("OutcoilNumber", memoryZR.OutcoilNumber ?? 0, DbType.Int32);

                if (existing != null)
                {
                    parameters.Add("ID", existing.ID, DbType.Int32);
                    connection.Execute(@"
                        UPDATE [Memory] SET
                            [MemoryCategory] = @MemoryCategory,
                            [DeviceNumber] = @DeviceNumber,
                            [DeviceNumber1] = @DeviceNumber1,
                            [DeviceNumber2] = @DeviceNumber2,
                            [Category] = @Category,
                            [Row_1] = @Row_1,
                            [Row_2] = @Row_2,
                            [Row_3] = @Row_3,
                            [Row_4] = @Row_4,
                            [Direct_Input] = @Direct_Input,
                            [Confirm] = @Confirm,
                            [Note] = @Note,
                            [UpdatedAt] = @UpdatedAt,
                            [GOT] = @GOT,
                            [MnemonicDeviceId] = @MnemonicDeviceId,
                            [OutcoilNumber] = @OutcoilNumber
                        WHERE [ID] = @ID",
                    parameters, transaction);
                }
                else
                {
                    connection.Execute(@"
                        INSERT INTO [Memory] (
                            [PlcId], [MemoryCategory], [DeviceNumber],
                            [DeviceNumber1], [DeviceNumber2], [Device],
                            [Category], [Row_1], [Row_2], [Row_3], [Row_4],
                            [Direct_Input], [Confirm], [Note],
                            [CreatedAt], [UpdatedAt], [GOT],
                            [MnemonicDeviceId], [OutcoilNumber]
                        ) VALUES (
                            @PlcId, @MemoryCategory, @DeviceNumber,
                            @DeviceNumber1, @DeviceNumber2, @Device,
                            @Category, @Row_1, @Row_2, @Row_3, @Row_4,
                            @Direct_Input, @Confirm, @Note,
                            @CreatedAt, @UpdatedAt, @GOT,
                            @MnemonicDeviceId, @OutcoilNumber
                        )",
                    parameters, transaction);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Device={device.ID} 保存失敗 → {ex.Message}");
                return false;
            }

            transaction.Commit();
            Debug.WriteLine($"[確認] Memory テーブルのレコード数: {count}");
            return true;

        }


    }
}
