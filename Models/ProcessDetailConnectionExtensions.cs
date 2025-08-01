using System;
using System.Collections.Generic;
using System.Linq;

namespace KdxDesigner.Models
{
    /// <summary>
    /// ProcessDetailConnection用の拡張メソッドクラス
    /// </summary>
    public static class ProcessDetailConnectionExtensions
    {
        /// <summary>
        /// StartIds文字列から接続リストを作成します
        /// </summary>
        /// <param name="toProcessDetailId">接続先の工程詳細ID</param>
        /// <param name="startIds">セミコロン区切りのStartIds文字列</param>
        /// <returns>ProcessDetailConnectionのリスト</returns>
        public static List<ProcessDetailConnection> CreateFromStartIds(int toProcessDetailId, string startIds)
        {
            var connections = new List<ProcessDetailConnection>();
            
            if (string.IsNullOrWhiteSpace(startIds))
                return connections;
            
            var ids = startIds.Split(';')
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct();
            
            foreach (var fromId in ids)
            {
                connections.Add(new ProcessDetailConnection
                {
                    FromProcessDetailId = fromId,
                    ToProcessDetailId = toProcessDetailId
                });
            }
            
            return connections;
        }
        
        /// <summary>
        /// 接続リストからStartIds文字列を作成します
        /// </summary>
        /// <param name="connections">ProcessDetailConnectionのリスト</param>
        /// <returns>セミコロン区切りのStartIds文字列</returns>
        public static string ToStartIds(this IEnumerable<ProcessDetailConnection> connections)
        {
            var fromIds = connections
                .Select(c => c.FromProcessDetailId.ToString())
                .Distinct()
                .OrderBy(id => int.Parse(id));
            
            return string.Join(";", fromIds);
        }
        
        /// <summary>
        /// 接続が同じかどうかを比較します（IDを除く）
        /// </summary>
        public static bool IsSameConnection(this ProcessDetailConnection conn1, ProcessDetailConnection conn2)
        {
            return conn1.FromProcessDetailId == conn2.FromProcessDetailId &&
                   conn1.ToProcessDetailId == conn2.ToProcessDetailId;
        }
        
        /// <summary>
        /// 接続リストから重複を除去します
        /// </summary>
        public static List<ProcessDetailConnection> RemoveDuplicates(this IEnumerable<ProcessDetailConnection> connections)
        {
            var uniqueConnections = new List<ProcessDetailConnection>();
            var seen = new HashSet<(int from, int to)>();
            
            foreach (var conn in connections)
            {
                var key = (conn.FromProcessDetailId, conn.ToProcessDetailId);
                if (seen.Add(key))
                {
                    uniqueConnections.Add(conn);
                }
            }
            
            return uniqueConnections;
        }
    }
}