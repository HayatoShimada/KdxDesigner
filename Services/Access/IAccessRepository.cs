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
        /// </summary
        List<Models.Timer> GetTimers();

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
        /// 指定された工程詳細情報を更新します。
        /// </summary>
        /// <param name="processDetail">更新するProcessDetailオブジェクト。</param>
        void UpdateProcessDetail(ProcessDetail processDetail);

        /// <summary>
        /// 全ての工程詳細カテゴリを取得します。
        /// </summary>
        List<ProcessDetailCategory> GetProcessDetailCategories();

        /// <summary>
        /// idで指定されたの工程詳細カテゴリを取得します。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ProcessDetailCategory? GetProcessDetailCategoryById(int id);

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

        /// <summary>
        /// IOレコードのリストを更新し、同時に変更履歴を保存します。
        /// これらの一連の処理は単一のトランザクション内で実行されます。
        /// </summary>
        /// <param name="iosToUpdate">更新対象のIOオブジェクトのリスト。</param>
        /// <param name="histories">保存する変更履歴のリスト。</param>
        void UpdateAndLogIoChanges(List<IO> iosToUpdate, List<IOHistory> histories);

        /// <summary>
        /// 指定されたサイクルIDに紐づく工程詳細接続情報を取得します。
        /// </summary>
        /// <param name="cycleId">取得対象のサイクルID。</param>
        List<ProcessDetailConnection> GetProcessDetailConnections(int cycleId);

        /// <summary>
        /// 指定されたToProcessDetailIdに紐づく接続情報を取得します。
        /// </summary>
        /// <param name="toProcessDetailId">終点の工程詳細ID。</param>
        List<ProcessDetailConnection> GetConnectionsByToId(int toProcessDetailId);

        /// <summary>
        /// 指定されたFromProcessDetailIdに紐づく接続情報を取得します。
        /// </summary>
        /// <param name="fromProcessDetailId">始点の工程詳細ID。</param>
        List<ProcessDetailConnection> GetConnectionsByFromId(int fromProcessDetailId);

        /// <summary>
        /// 新しい工程詳細接続情報を追加します。
        /// </summary>
        /// <param name="connection">追加するProcessDetailConnectionオブジェクト。</param>
        void AddProcessDetailConnection(ProcessDetailConnection connection);

        /// <summary>
        /// 指定されたIDの工程詳細接続情報を削除します。
        /// </summary>
        /// <param name="id">削除対象の接続ID。</param>
        void DeleteProcessDetailConnection(int id);

        /// <summary>
        /// 指定されたFromIdとToIdの組み合わせの接続情報を削除します。
        /// </summary>
        /// <param name="fromId">始点の工程詳細ID。</param>
        /// <param name="toId">終点の工程詳細ID。</param>
        void DeleteConnectionsByFromAndTo(int fromId, int toId);

        /// <summary>
        /// 指定されたサイクルIDに紐づく工程詳細終了情報を取得します。
        /// </summary>
        /// <param name="cycleId">取得対象のサイクルID。</param>
        List<ProcessDetailFinish> GetProcessDetailFinishes(int cycleId);

        /// <summary>
        /// 指定されたProcessDetailIdに紐づく終了情報を取得します。
        /// </summary>
        /// <param name="processDetailId">工程詳細ID。</param>
        List<ProcessDetailFinish> GetFinishesByProcessDetailId(int processDetailId);

        /// <summary>
        /// 指定されたFinishProcessDetailIdに紐づく終了情報を取得します。
        /// </summary>
        /// <param name="finishProcessDetailId">終了先の工程詳細ID。</param>
        List<ProcessDetailFinish> GetFinishesByFinishId(int finishProcessDetailId);

        /// <summary>
        /// 新しい工程詳細終了情報を追加します。
        /// </summary>
        /// <param name="finish">追加するProcessDetailFinishオブジェクト。</param>
        void AddProcessDetailFinish(ProcessDetailFinish finish);

        /// <summary>
        /// 指定されたIDの工程詳細終了情報を削除します。
        /// </summary>
        /// <param name="id">削除対象の終了情報ID。</param>
        void DeleteProcessDetailFinish(int id);

        /// <summary>
        /// 指定されたProcessDetailIdとFinishProcessDetailIdの組み合わせの終了情報を削除します。
        /// </summary>
        /// <param name="processDetailId">工程詳細ID。</param>
        /// <param name="finishProcessDetailId">終了先の工程詳細ID。</param>
        void DeleteFinishesByProcessAndFinish(int processDetailId, int finishProcessDetailId);

    }
}