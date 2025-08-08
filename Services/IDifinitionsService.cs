using KdxDesigner.Models;
using System.Collections.Generic;

namespace KdxDesigner.Services
{
    /// <summary>
    /// 定義情報のデータ操作を行うサービスインターフェース
    /// </summary>
    public interface IDifinitionsService
    {
        /// <summary>
        /// 指定されたカテゴリーの定義情報を取得する
        /// </summary>
        /// <param name="category">カテゴリー名</param>
        /// <returns>定義情報のリスト</returns>
        List<Difinitions> GetDifinitions(string category);
    }
}