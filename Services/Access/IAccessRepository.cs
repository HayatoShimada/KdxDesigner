using KdxDesigner.Models;
using KdxDesigner.Models.Define;

using System.Collections.Generic;

namespace KdxDesigner.Services.Access
{
    /// <summary>
    /// データベースへのアクセス機能を提供するリポジトリのインターフェース。
    /// </summary>
    public interface IAccessRepository
    {
        /// <summary>
        /// データベースへの接続文字列を取得します。
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// 全ての会社情報を取得します。
        /// </summary>
        List<Company> GetCompanies();

        /// <summary>
        /// 全ての機種情報を取得します。
        /// </summary>
        List<Model> GetModels();

        /// <summary>
        /// 全てのPLC情報を取得します。
        /// </summary>
        List<PLC> GetPLCs();

        /// <summary>
        /// 全てのサイクル情報を取得します。
        /// </summary>
        List<Cycle> GetCycles();

        /// <summary>
        /// 全ての工程情報を取得します。
        /// </summary>
        List<Models.Process> GetProcesses();

        /// <summary>
        /// 全ての機械情報を取得します。
        /// </summary>
        List<Models.Machine> GetMachines();

        /// <summary>
        /// idで指定された機械情報を取得します。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Models.Machine? GetMachineById(int id);

        /// <summary>
        /// 全ての駆動部(主)情報を取得します。
        /// </summary>
        List<DriveMain> GetDriveMains();

        /// <summary>
        /// 全ての駆動部(副)情報を取得します。
        /// </summary>
        List<DriveSub> GetDriveSubs();

        /// <summary>
        /// 駆動部(副)情報をidで指定して取得します。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        DriveSub? GetDriveSubById(int id);

        /// <summary>
        /// 全てのシリンダー(CY)情報を取得します。
        /// </summary>
        List<CY> GetCYs();

        /// <summary>
        /// idで指定されたシリンダー(CY)情報を取得します。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        CY? GetCYById(int id);

        /// <summary>
        /// 指定されたサイクルIDに紐づくタイマー情報を取得します。
        /// </summary>
        /// <param name="cycleId">取得対象のサイクルID。</param>
        List<Models.Timer> GetTimersByCycleId(int cycleId);

        /// <summary>
        /// 全ての操作情報を取得します。
        /// </summary>
        List<Operation> GetOperations();

        /// <summary>
        /// 指定されたIDの操作情報を取得します。
        /// </summary>
        /// <param name="id">取得対象の操作ID。</param>
        /// <returns>見つかった場合はOperationオブジェクト、見つからない場合はnull。</returns>
        Operation? GetOperationById(int id);

        /// <summary>
        /// 指定されたPLC IDに紐づくLength情報を取得します。
        /// </summary>
        /// <param name="plcId">取得対象のPLC ID。</param>
        List<Length>? GetLengthByPlcId(int plcId);

        /// <summary>
        /// 指定された操作情報を更新します。
        /// </summary>
        /// <param name="operation">更新するOperationオブジェクト。</param>
        void UpdateOperation(Operation operation);

        /// <summary>
        /// 全ての工程詳細情報を取得します。
        /// </summary>
        List<ProcessDetail> GetProcessDetails();

        /// <summary>
        /// 全てのIOリスト情報を取得します。
        /// </summary>
        List<IO> GetIoList();

        /// <summary>
        /// 全てのタイマーカテゴリ情報を取得します。
        /// </summary>
        List<TimerCategory> GetTimerCategory();

        /// <summary>
        /// サーボ情報を取得します。
        /// </summary>
        List<Servo> GetServos(int? plcId, int? cylinderId);

        /// <summary>
        /// IOレコードのリストを受け取り、LinkDeviceカラムをバッチ更新します。
        /// </summary>
        /// <param name="ioRecordsToUpdate">LinkDeviceが設定されたIOオブジェクトのリスト。</param>
        void UpdateIoLinkDevices(IEnumerable<IO> ioRecordsToUpdate);

    }
}