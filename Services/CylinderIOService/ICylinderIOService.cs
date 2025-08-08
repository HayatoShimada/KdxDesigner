using KdxDesigner.Models;

using System.Collections.Generic;

namespace KdxDesigner.Services.CylinderIOService
{
    /// <summary>
    /// CylinderIOテーブルのデータ操作を行うサービスインターフェース
    /// </summary>
    public interface ICylinderIOService
    {
        /// <summary>
        /// 指定されたCYに関連付けられたIOのリストを取得
        /// </summary>
        /// <param name="cylinderId">シリンダーID</param>
        /// <param name="plcId">PLC ID</param>
        /// <returns>CylinderIOレコードのリスト</returns>
        List<CylinderIO> GetCylinderIOs(int cylinderId, int plcId);

        /// <summary>
        /// 指定されたIOに関連付けられたCYのリストを取得
        /// </summary>
        /// <param name="ioAddress">IOアドレス</param>
        /// <param name="plcId">PLC ID</param>
        /// <returns>CylinderIOレコードのリスト</returns>
        List<CylinderIO> GetIOCylinders(string ioAddress, int plcId);

        /// <summary>
        /// CYとIOの関連付けを追加
        /// </summary>
        /// <param name="cylinderId">シリンダーID</param>
        /// <param name="ioAddress">IOアドレス</param>
        /// <param name="plcId">PLC ID</param>
        /// <param name="ioType">IOタイプ</param>
        /// <param name="comment">コメント（オプション）</param>
        void AddAssociation(int cylinderId, string ioAddress, int plcId, string ioType, string? comment = null);

        /// <summary>
        /// CYとIOの関連付けを削除
        /// </summary>
        /// <param name="cylinderId">シリンダーID</param>
        /// <param name="ioAddress">IOアドレス</param>
        /// <param name="plcId">PLC ID</param>
        void RemoveAssociation(int cylinderId, string ioAddress, int plcId);

        /// <summary>
        /// 指定されたPLCのすべての関連付けを取得
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <returns>CylinderIOレコードのリスト</returns>
        List<CylinderIO> GetAllAssociations(int plcId);
    }
}