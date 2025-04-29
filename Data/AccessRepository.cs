using KdxDesigner.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using Dapper;

namespace KdxDesigner.Data
{
    public class AccessRepository
    {
        private readonly string _connectionString;

        public AccessRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Company> GetIoInfoList()
        {
            using var connection = new OleDbConnection(_connectionString);
            var sql = "SELECT Id, PlcId, PUCO, CYNum, OilNum, MacineId, DriveSub, PlaceId, CYNameSub, SensorId FROM CY";
            return connection.Query<Company>(sql).ToList();
        }
    }
}
