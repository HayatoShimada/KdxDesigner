using KdxDesigner.Models;
using System.Collections.Generic;
using static KdxDesigner.Services.ProsTimeDeviceService;

namespace KdxDesigner.Services
{
    /// <summary>
    /// ProsTime（工程時間）デバイスの管理サービスインターフェース
    /// </summary>
    public interface IProsTimeDeviceService
    {
        /// <summary>
        /// ProsTimeテーブルのすべてのレコードを削除する
        /// </summary>
        void DeleteProsTimeTable();

        /// <summary>
        /// 指定されたPLC IDに関連するProsTimeレコードを取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <returns>ProsTimeレコードのリスト</returns>
        List<ProsTime> GetProsTimeByPlcId(int plcId);

        /// <summary>
        /// 指定されたPLC IDとニーモニックIDに関連するProsTimeレコードを取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="mnemonicId">ニーモニックID</param>
        /// <returns>ProsTimeレコードのリスト</returns>
        List<ProsTime> GetProsTimeByMnemonicId(int plcId, int mnemonicId);

        /// <summary>
        /// 操作リストに基づいてProsTimeレコードを保存する
        /// </summary>
        /// <param name="operations">操作リスト</param>
        /// <param name="startCurrent">現在のデバイス開始番号</param>
        /// <param name="startPrevious">前回のデバイス開始番号</param>
        /// <param name="startCylinder">シリンダーデバイス開始番号</param>
        /// <param name="plcId">PLC ID</param>
        void SaveProsTime(List<Operation> operations, int startCurrent, int startPrevious, int startCylinder, int plcId);
    }
}