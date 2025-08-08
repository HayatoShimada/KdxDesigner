using Dapper;
using KdxDesigner.Models;
using KdxDesigner.Models.Define; // MnemonicType を含むと仮定
using System.Data;
using System.Data.OleDb;
using System.Linq; // ToList, GroupBy, Any を使用するために追加
using System.Collections.Generic;
using KdxDesigner.Services.Access;

namespace KdxDesigner.Services.ProsTimeDevice
{
    /// <summary>
    /// ProsTime（工程時間）デバイスの管理サービス実装
    /// </summary>
    internal class ProsTimeDeviceService : IProsTimeDeviceService
    {
        private readonly string _connectionString;
        private readonly Dictionary<int, OperationProsTimeConfig> _loadedOperationConfigs; // ★変更: staticでなくインスタンスフィールドに

        public class OperationProsTimeConfig
        {
            public int TotalProsTimeCount { get; set; }
            public Dictionary<int, int> SortIdToCategoryIdMap { get; set; }
            public OperationProsTimeConfig()
            {
                SortIdToCategoryIdMap = new Dictionary<int, int>();
            }
        }

        // デフォルト設定は引き続き静的メンバーとして保持可能
        private static readonly OperationProsTimeConfig DefaultOperationConfig =
            new() { TotalProsTimeCount = 0, SortIdToCategoryIdMap = new Dictionary<int, int>() };

        public ProsTimeDeviceService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
            _loadedOperationConfigs = LoadOperationProsTimeConfigsFromDb(); // ★コンストラクタで読み込み
        }

        // MnemonicDeviceテーブルからPlcIdに基づいてデータを取得する

        /// <summary>
        ///     
        /// </summary>
        public void DeleteProsTimeTable()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "DELETE FROM ProsTime";
            connection.Execute(sql);

        }

        public List<ProsTime> GetProsTimeByPlcId(int plcId)

        {

            using var connection = new OleDbConnection(_connectionString);

            var sql = "SELECT * FROM ProsTime WHERE PlcId = @PlcId";

            return connection.Query<ProsTime>(sql, new { PlcId = plcId }).ToList();

        }

        // MnemonicDeviceテーブルからPlcIdとMnemonicIdに基づいてデータを取得する

        public List<ProsTime> GetProsTimeByMnemonicId(int plcId, int mnemonicId)

        {

            using var connection = new OleDbConnection(_connectionString);

            var sql = "SELECT * FROM ProsTime WHERE PlcId = @PlcId AND MnemonicId = @MnemonicId";

            return connection.Query<ProsTime>(sql, new { PlcId = plcId, MnemonicId = mnemonicId }).ToList();

        }

        // Helper DTO for querying the ProsTimeCategoryDefinitions table
        private class ProsTimeDefinitionRow
        {
            public int OperationCategoryId { get; set; }
            public int TotalCount { get; set; }
            public int SortOrder { get; set; }
            public int OperationDefinitionsId { get; set; }
        }

        private Dictionary<int, OperationProsTimeConfig> LoadOperationProsTimeConfigsFromDb()
        {
            var configs = new Dictionary<int, OperationProsTimeConfig>();
            // テーブル名は実際の設計に合わせてください
            const string sql = "SELECT OperationCategoryId, TotalCount, SortOrder, OperationDefinitionsId FROM ProsTimeDefinitions ORDER BY OperationCategoryId, SortOrder";

            try
            {
                using var connection = new OleDbConnection(_connectionString);
                var rawConfigData = connection.Query<ProsTimeDefinitionRow>(sql).ToList();

                if (rawConfigData == null || !rawConfigData.Any())
                {
                    // TODO: 設定データが空の場合のログ記録やエラー処理を検討
                    System.Diagnostics.Debug.WriteLine("警告: ProsTimeCategoryDefinitions テーブルから設定を読み込めませんでした。");
                    return configs; // 空のコンフィグを返す
                }

                var groupedData = rawConfigData.GroupBy(r => r.OperationCategoryId);

                foreach (var group in groupedData)
                {
                    var operationCategoryKey = group.Key;
                    // グループ内の TotalCount は全て同じはずなので、最初の要素から取得
                    var totalCount = group.First().TotalCount;

                    var map = new Dictionary<int, int>();
                    foreach (var item in group)
                    {
                        // SortOrder と ResultingCategoryId のマッピングを追加
                        map[item.SortOrder] = item.OperationDefinitionsId;
                    }

                    configs[operationCategoryKey] = new OperationProsTimeConfig
                    {
                        TotalProsTimeCount = totalCount,
                        SortIdToCategoryIdMap = map
                    };
                }
            }
            catch (Exception ex)
            {
                // TODO: データベースアクセスエラーのログ記録やエラー処理
                System.Diagnostics.Debug.WriteLine($"エラー: ProsTimeCategoryDefinitions の読み込み中に例外が発生しました - {ex.Message}");
                // エラー発生時は空のコンフィグを返すか、例外をスローするか検討
            }
            return configs;
        }

        // GetProsTimeByPlcId, GetProsTimeByMnemonicId メソッドは変更なし

        public void SaveProsTime(List<Operation> operations, int startCurrent, int startPrevious, int startCylinder, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            operations.Sort((o1, o2) => o1.Id.CompareTo(o2.Id));

            var allExistingProsTimesRaw = GetProsTimeByMnemonicId(plcId, (int)MnemonicType.Operation);
            var existingProsTimeMap = allExistingProsTimesRaw
                .GroupBy(pt => pt.RecordId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(pt => pt.SortId, pt => pt)
                );

            int count = 0;
            foreach (Operation operation in operations)
            {
                if (operation == null || operation.CategoryId == null) continue;

                var operationCategoryValue = operation.CategoryId.Value; // Null許容型から値を取得

                // ★変更: _loadedOperationConfigs (インスタンスメンバー) を使用
                OperationProsTimeConfig currentConfig = _loadedOperationConfigs.TryGetValue(operationCategoryValue, out var specificConfig)
                                                        ? specificConfig
                                                        : DefaultOperationConfig;

                int prosTimeCount = currentConfig.TotalProsTimeCount;

                for (int i = 0; i < prosTimeCount; i++)
                {
                    string currentDevice = "ZR" + (startCurrent + count).ToString();
                    string previousDevice = "ZR" + (startPrevious + count).ToString();
                    string cylinderDevice = "ZR" + (startCylinder + count).ToString();

                    ProsTime? existing = null;
                    if (existingProsTimeMap.TryGetValue(operation.Id, out var opGroup) &&
                        opGroup.TryGetValue(i, out var foundProsTime))
                    {
                        existing = foundProsTime;
                    }

                    count++;
                    var parameters = new DynamicParameters();

                    parameters.Add("PlcId", plcId, DbType.Int32);
                    parameters.Add("MnemonicId", (int)MnemonicType.Operation, DbType.Int32);
                    parameters.Add("RecordId", operation.Id, DbType.Int32);
                    parameters.Add("SortId", i, DbType.Int32);
                    parameters.Add("CurrentDevice", currentDevice, DbType.String);
                    parameters.Add("PreviousDevice", previousDevice, DbType.String);
                    parameters.Add("CylinderDevice", cylinderDevice, DbType.String);

                    int finalCategoryId = currentConfig.SortIdToCategoryIdMap.TryGetValue(i, out var catId) ? catId : 0;
                    parameters.Add("CategoryId", finalCategoryId, DbType.Int32);

                    if (existing != null)
                    {
                        parameters.Add("ID", existing.ID, DbType.Int32);
                        connection.Execute(@"
                        UPDATE [ProsTime] SET
                            [PlcId] = @PlcId, [MnemonicId] = @MnemonicId, [RecordId] = @RecordId,
                            [SortId] = @SortId, [CurrentDevice] = @CurrentDevice,
                            [PreviousDevice] = @PreviousDevice, [CylinderDevice] = @CylinderDevice,
                            [CategoryId] = @CategoryId
                        WHERE [ID] = @ID",
                        parameters, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
                        INSERT INTO [ProsTime] (
                            [PlcId], [MnemonicId], [RecordId], [SortId],
                            [CurrentDevice], [PreviousDevice], [CylinderDevice], [CategoryId]
                        ) VALUES (
                            @PlcId, @MnemonicId, @RecordId, @SortId,
                            @CurrentDevice, @PreviousDevice, @CylinderDevice, @CategoryId
                        )",
                        parameters, transaction);
                    }
                }
            }
            transaction.Commit();
        }
    }
}