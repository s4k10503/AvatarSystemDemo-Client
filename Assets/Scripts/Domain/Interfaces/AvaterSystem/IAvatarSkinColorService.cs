using Domain.ValueObjects;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Domain.Interfaces
{
    /// <summary>
    /// アバター肌色カスタマイズサービスのインターフェース
    /// </summary>
    public interface IAvatarSkinColorService : IAnimatorSettable
    {
        /// <summary>
        /// 肌の色を適用する
        /// </summary>
        /// <param name="skinColor">適用する肌色</param>
        void ApplyColor(SkinColor skinColor);

        /// <summary>
        /// 現在の肌の色を取得する
        /// </summary>
        /// <returns>現在の肌色</returns>
        SkinColor GetCurrentColor();

        /// <summary>
        /// デフォルトの肌色に戻す
        /// </summary>
        void ResetColor();

        UniTask ApplyColorAsync(ColorValue color, IEnumerable<Renderer> renderers);

        /// <summary>
        /// アバターに紐づくデフォルトの肌のベースカラーを取得します。
        /// </summary>
        /// <returns>マテリアル名をキー、デフォルトのベースカラーを値とする辞書。</returns>
        Dictionary<string, Color> GetDefaultBaseColors();
    }
}