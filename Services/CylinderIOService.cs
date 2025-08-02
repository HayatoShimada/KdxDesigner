using Dapper;
using KdxDesigner.Models;
using KdxDesigner.Services.Access;
using System.Data.OleDb;

namespace KdxDesigner.Services
{
    /// <summary>
    /// CylinderIOテーブルのデータ操作を行うサービスクラス
    /// </summary>
    public class CylinderIOService
    {
        private readonly string _connectionString;

        public CylinderIOService(IAccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
        }

        /// <summary>
        /// 指定されたCYに関連付けられたIOのリストを取得
        /// </summary>
        public List<CylinderIO> GetCylinderIOs(int cylinderId, int plcId)
        {
            try
            {
                using var connection = new OleDbConnection(_connectionString);
                var sql = @"
                    SELECT * FROM CylinderIO 
                    WHERE CylinderId = @CylinderId AND PlcId = @PlcId
                    ORDER BY SortOrder, IOType";
                return connection.Query<CylinderIO>(sql, new { CylinderId = cylinderId, PlcId = plcId }).ToList();
            }
            catch (OleDbException ex) when (ex.Message.Contains("CylinderIO") && ex.Message.Contains("見つかりませんでした"))
            {
                // テーブルが存在しない場合は空のリストを返す
                return new List<CylinderIO>();
            }
        }

        /// <summary>
        /// 指定されたIOに関連付けられたCYのリストを取得
        /// </summary>
        public List<CylinderIO> GetIOCylinders(string ioAddress, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = @"
                SELECT * FROM CylinderIO 
                WHERE IOAddress = @IOAddress AND PlcId = @PlcId
                ORDER BY CylinderId";
            return connection.Query<CylinderIO>(sql, new { IOAddress = ioAddress, PlcId = plcId }).ToList();
        }

        /// <summary>
        /// CYとIOの関連付けを追加
        /// </summary>
        public void AddAssociation(int cylinderId, string ioAddress, int plcId, string ioType, string? comment = null)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 既存の関連付けをチェック
                var checkSql = @"
                    SELECT COUNT(*) FROM CylinderIO 
                    WHERE CylinderId = @CylinderId 
                      AND IOAddress = @IOAddress 
                      AND PlcId = @PlcId";
                
                var count = connection.ExecuteScalar<int>(checkSql, 
                    new { CylinderId = cylinderId, IOAddress = ioAddress, PlcId = plcId }, 
                    transaction);

                if (count > 0)
                {
                    throw new InvalidOperationException("この関連付けは既に存在します。");
                }

                // 新規追加
                var insertSql = @"
                    INSERT INTO CylinderIO (CylinderId, IOAddress, PlcId, IOType, Comment)
                    VALUES (@CylinderId, @IOAddress, @PlcId, @IOType, @Comment)";
                
                connection.Execute(insertSql, 
                    new { CylinderId = cylinderId, IOAddress = ioAddress, PlcId = plcId, 
                          IOType = ioType, Comment = comment }, 
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
        /// CYとIOの関連付けを削除
        /// </summary>
        public void RemoveAssociation(int cylinderId, string ioAddress, int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = @"
                DELETE FROM CylinderIO 
                WHERE CylinderId = @CylinderId 
                  AND IOAddress = @IOAddress 
                  AND PlcId = @PlcId";
            
            connection.Execute(sql, new { CylinderId = cylinderId, IOAddress = ioAddress, PlcId = plcId });
        }

        /// <summary>
        /// 指定されたPLCのすべての関連付けを取得
        /// </summary>
        public List<CylinderIO> GetAllAssociations(int plcId)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM CylinderIO WHERE PlcId = @PlcId ORDER BY CylinderId, SortOrder";
            return connection.Query<CylinderIO>(sql, new { PlcId = plcId }).ToList();
        }
    }
}