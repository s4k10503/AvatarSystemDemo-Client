using System.Collections.Generic;
using UnityEngine;
using Domain.ValueObjects;
using Cysharp.Threading.Tasks;

namespace Domain.Interfaces
{
    /// <summary>
    /// アバターの髪色を適用するサービスインターフェース
    /// </summary>
    public interface IAvatarHairColorService : IAnimatorSettable
    {
        /// <summary>
        /// 指定された髪色をアバターに適用します
        /// </summary>
        /// <param name="hairColor">適用する髪色</param>
        void ApplyColor(HairColor hairColor);

        /// <summary>
        /// 現在適用されている髪色を取得します
        /// </summary>
        /// <returns>現在の髪色</returns>
        HairColor GetCurrentColor();

        /// <summary>
        /// 髪色をデフォルトにリセットします
        /// </summary>
        void ResetColor();

        UniTask ApplyColorAsync(ColorValue color, IEnumerable<Renderer> renderers);

        /// <summary>
        /// アバターに紐づくデフォルトの髪のベースカラーを取得します。
        /// </summary>
        /// <returns>マテリアル名をキー、デフォルトのベースカラーを値とする辞書。</returns>
        Dictionary<string, Color> GetDefaultBaseColors();
    }
}