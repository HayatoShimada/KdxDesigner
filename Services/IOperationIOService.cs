using KdxDesigner.Models;
using System.Collections.Generic;

namespace KdxDesigner.Services
{
    /// <summary>
    /// OperationIOテーブルのデータ操作を行うサービスインターフェース
    /// </summary>
    public interface IOperationIOService
    {
        /// <summary>
        /// 指定されたOperationに関連付けられたIOのリストを取得
        /// </summary>
        /// <param name="operationId">操作ID</param>
        /// <returns>OperationIOレコードのリスト</returns>
        List<OperationIO> GetOperationIOs(int operationId);

        /// <summary>
        /// 指定されたIOに関連付けられたOperationのリストを取得
        /// </summary>
        /// <param name="ioAddress">IOアドレス</param>
        /// <param name="plcId">PLC ID</param>
        /// <returns>OperationIOレコードのリスト</returns>
        List<OperationIO> GetIOOperations(string ioAddress, int plcId);

        /// <summary>
        /// OperationとIOの関連付けを追加
        /// </summary>
        /// <param name="operationId">操作ID</param>
        /// <param name="ioAddress">IOアドレス</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="ioUsage">IO使用方法</param>
        /// <param name="comment">コメント（オプション）</param>
        void AddAssociation(int operationId, string ioAddress, int plcId, string ioUsage, string? comment = null);

        /// <summary>
        /// OperationとIOの関連付けを削除
        /// </summary>
        /// <param name="operationId">操作ID</param>
        /// <param name="ioAddress">IOアドレス</param>
        /// <param name="plcId">PLC ID</param>
        void RemoveAssociation(int operationId, string ioAddress, int plcId);

        /// <summary>
        /// 指定されたPLCのすべての関連付けを取得
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <returns>OperationIOレコードのリスト</returns>
        List<OperationIO> GetAllAssociations(int plcId);
    }
}