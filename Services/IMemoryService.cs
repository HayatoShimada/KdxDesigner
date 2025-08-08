using KdxDesigner.Models;
using System;
using System.Collections.Generic;

namespace KdxDesigner.Services
{
    /// <summary>
    /// メモリデータの操作を行うサービスインターフェース
    /// </summary>
    public interface IMemoryService
    {
        /// <summary>
        /// 指定されたPLC IDのメモリレコードを取得する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <returns>メモリレコードのリスト</returns>
        List<Memory> GetMemories(int plcId);

        /// <summary>
        /// メモリカテゴリーのリストを取得する
        /// </summary>
        /// <returns>メモリカテゴリーのリスト</returns>
        List<MemoryCategory> GetMemoryCategories();

        /// <summary>
        /// メモリレコードを保存または更新する
        /// </summary>
        /// <param name="plcId">PLC ID</param>
        /// <param name="memories">保存するメモリレコードのリスト</param>
        /// <param name="progressCallback">進捗コールバック（オプション）</param>
        void SaveMemories(int plcId, List<Memory> memories, Action<string>? progressCallback = null);

        /// <summary>
        /// ニーモニックデバイスのメモリ情報を保存する
        /// </summary>
        /// <param name="device">ニーモニックデバイス</param>
        /// <returns>保存成功の場合はtrue</returns>
        bool SaveMnemonicMemories(MnemonicDevice device);

        /// <summary>
        /// ニーモニックタイマーデバイスのZRメモリ情報を保存する
        /// </summary>
        /// <param name="device">ニーモニックタイマーデバイス</param>
        /// <returns>保存成功の場合はtrue</returns>
        bool SaveMnemonicTimerMemoriesZR(MnemonicTimerDevice device);

        /// <summary>
        /// ニーモニックタイマーデバイスのTメモリ情報を保存する
        /// </summary>
        /// <param name="device">ニーモニックタイマーデバイス</param>
        /// <returns>保存成功の場合はtrue</returns>
        bool SaveMnemonicTimerMemoriesT(MnemonicTimerDevice device);
    }
}