using Dapper;
using KdxDesigner.Models;
using KdxDesigner.Services.Access;

using System.Data;
using System.Data.OleDb;
using System.Diagnostics;


namespace KdxDesigner.Services
{
    internal class DifinitionsService
    {

        private readonly string _connectionString;

        public DifinitionsService(AccessRepository repository)
        {
            _connectionString = repository.ConnectionString;
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
