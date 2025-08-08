using Dapper;

using KdxDesigner.Models;
using KdxDesigner.Services.Access;

using System.Data;
using System.Data.OleDb;
using System.Diagnostics;


namespace KdxDesigner.Services.IDifinitions
{
    /// <summary>
    /// 定義情報のデータ操作を行うサービス実装
    /// </summary>
    internal class DifinitionsService : IDifinitionsService
    {

        private readonly string _connectionString;

        public DifinitionsService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // AccessRepository.cs に以下を追加:
        public List<Difinitions> GetDifinitions(string category)
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT * FROM Difinitions WHERE Category = @Category";
            return connection.Query<Difinitions>(sql, new { Category = category }).ToList();
        }

    }
}
