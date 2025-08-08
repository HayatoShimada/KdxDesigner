using Dapper;
using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using System.Data.OleDb;

namespace KdxDesigner.Services
{
    /// <summary>
    /// OperationIOテーブルのデータ操作を行うサービスクラス
    /// </summary>
    public class OperationIOService : IOperationIOService
    {
        private readonly string _connectionString;

        public OperationIOService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        /// <summary>
        /// 指定されたOperationに関連付けられたIOのリストを取得
        /// </summary>
        public List<OperationIO> GetOperationIOs(int operationId)
        {
            try
            {
                using var connection = new OleDbConnection(_connectionString);
                var sql = @"
                    SELECT * FROM OperationIO 
                    WHERE OperationId = @OperationId
                    ORDER BY SortOrder, IOUsage";
                return connection.Query<OperationIO>(sql, new { OperationId = operationId }).ToList();
            }
            catch (OleDbException ex) when (ex.Message.Contains("OperationIO") && ex.Message.Contains("見つかりませんでした"))
            {
                // テーブルが存在しない場合は空のリストを返す
                return new List<OperationIO>();
            }
        }

        /// <summary>
        /// 指定されたIOに関連付けられたOperationのリストを取得
        /// </summary>
        public List<OperationIO> GetIOOperations(string ioAddress, int plcId)
        {
            try
            {
                using var connection = new OleDbConnection(_connectionString);
                var sql = @"
                    SELECT * FROM OperationIO 
                    WHERE IOAddress = @IOAddress AND PlcId = @PlcId
                    ORDER BY OperationId";
                return connection.Query<OperationIO>(sql, new { IOAddress = ioAddress, PlcId = plcId }).ToList();
            }
            catch (OleDbException ex) when (ex.Message.Contains("OperationIO") && ex.Message.Contains("見つかりませんでした"))
            {
                return new List<OperationIO>();
            }
        }

        /// <summary>
        /// OperationとIOの関連付けを追加
        /// </summary>
        public void AddAssociation(int operationId, string ioAddress, int plcId, string ioUsage, string? comment = null)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 既存の関連付けをチェック
                var checkSql = @"
                    SELECT COUNT(*) FROM OperationIO 
                    WHERE OperationId = @OperationId 
                      AND IOAddress = @IOAddress 
                      AND PlcId = @PlcId";
                
                var count = connection.ExecuteScalar<int>(checkSql, 
                    new { OperationId = operationId, IOAddress = ioAddress, PlcId = plcId }, 
                    transaction);

                if (count > 0)
                {
                    throw new InvalidOperationException("この関連付けは既に存在します。");
                }

                // 新規追加
                var insertSql = @"
                    INSERT INTO OperationIO (OperationId, IOAddress, PlcId, IOUsage, Comment)
                    VALUES (@OperationId, @IOAddress, @PlcId, @IOUsage, @Comment)";
                
                connection.Execute(insertSql, 
                    new { OperationId = operationId, IOAddress = ioAddress, PlcId = plcId, 
                          IOUsage = ioUsage, Comment = comment }, 
                    transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// OperationとIOの関連付けを削除
        /// </summary>
        public void RemoveAssociation(int operationId, string ioAddress, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = @"
                DELETE FROM OperationIO 
                WHERE OperationId = @OperationId 
                  AND IOAddress = @IOAddress 
                  AND PlcId = @PlcId";
            
            connection.Execute(sql, new { OperationId = operationId, IOAddress = ioAddress, PlcId = plcId });
        }

        /// <summary>
        /// 指定されたPLCのすべての関連付けを取得
        /// </summary>
        public List<OperationIO> GetAllAssociations(int plcId)
        {
            try
            {
                using var connection = new OleDbConnection(_connectionString);
                var sql = "SELECT * FROM OperationIO WHERE PlcId = @PlcId ORDER BY OperationId, SortOrder";
                return connection.Query<OperationIO>(sql, new { PlcId = plcId }).ToList();
            }
            catch (OleDbException ex) when (ex.Message.Contains("OperationIO") && ex.Message.Contains("見つかりませんでした"))
            {
                return new List<OperationIO>();
            }
        }
    }
}