using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using Domain.Interfaces;
using Domain.ValueObjects;

namespace Infrastructure.Services
{
    /// <summary>
    /// アバターの髪色を管理するサービス。
    /// </summary>
    public sealed class AvatarHairColorService : AvatarColorServiceBase, IAvatarHairColorService
    {
        private HairColor _currentHairColor;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AvatarHairColorService()
            : base()
        {
            _currentHairColor = HairColor.Default;
        }

        /// <summary>
        /// レンダラーが髪用かどうかを判定します。
        /// </summary>
        protected override bool IsTargetRenderer(Renderer renderer)
        {
            if (renderer == null) return false;

            Material[] materialsToCheck = null;
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                materialsToCheck = skinnedMeshRenderer.sharedMaterials;
            }
            else if (renderer is MeshRenderer meshRenderer)
            {
                materialsToCheck = meshRenderer.sharedMaterials;
            }

            if (materialsToCheck == null) return false;

            // マテリアル名やシェーダー名で判定
            foreach (var material in materialsToCheck) // Use materialsToCheck
            {
                if (material == null) continue;
                string materialNameLower = material.name.ToLower();
                string shaderNameLower = material.shader != null ? material.shader.name.ToLower() : "";

                // 一般的な髪関連の名前を含むかチェック
                if (materialNameLower.Contains("hair") || materialNameLower.Contains("head")) // "head" も髪を含むことがある
                {
                    // ただし、"face" や "skin" が含まれる場合は除外 (肌と区別するため)
                    if (!materialNameLower.Contains("face") && !materialNameLower.Contains("skin"))
                    {
                        return true; // Found a hair material on this renderer
                    }
                }
                // Check shader name as well
                if (shaderNameLower.Contains("hair"))
                {
                    // Exclude if it also contains face/skin (less common for shaders, but for safety)
                    if (!materialNameLower.Contains("face") && !materialNameLower.Contains("skin"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 髪色用のシェードカラーを計算します。
        /// 白(#FFFFFF)を基準とした際の影色(#E5CFBF, #CCCCFF)との関係性を
        /// HSV空間での変化量として捉え、任意のベースカラーに適用します。
        /// </summary>
        protected override (Color firstShade, Color secondShade) CalculateShadeColors(Color baseColor)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);

            // 白(S=0, V=1)から目標の影色へのHSV変化量を定義
            // Shadow 1 (#E5CFBF): H≈0.071, S≈0.166, V≈0.898
            //   Hue Shift: 0.071 (白のHを0と仮定した場合の相対シフト)
            //   Saturation Add: +0.166 (0 -> 0.166)
            //   Value Add: -0.102 (1 -> 0.898)
            const float hueShift1 = 0.071f;
            const float satAdd1 = 0.166f;
            const float valAdd1 = -0.102f;

            // Shadow 2 (#CCCCFF): H≈0.667, S≈0.200, V=1.000
            //   Hue Shift: 0.667 (白のHを0と仮定した場合の相対シフト)
            //   Saturation Add: +0.200 (0 -> 0.200)
            //   Value Add: +0.0   (1 -> 1.0)
            const float hueShift2 = 0.667f;
            const float satAdd2 = 0.200f;
            const float valAdd2 = 0.0f;

            // ベースカラーのHSVに変化量を適用して影色1のHSVを計算
            float h1 = (h + hueShift1) % 1.0f; // 色相は循環
            float s1 = Mathf.Clamp01(s + satAdd1); // 彩度は0-1の範囲にクランプ
            float v1 = Mathf.Clamp01(v + valAdd1); // 明度も0-1の範囲にクランプ

            // ベースカラーのHSVに変化量を適用して影色2のHSVを計算
            float h2 = (h + hueShift2) % 1.0f; // 色相は循環
            float s2 = Mathf.Clamp01(s + satAdd2); // 彩度は0-1の範囲にクランプ
            float v2 = Mathf.Clamp01(v + valAdd2); // 明度も0-1の範囲にクランプ

            // HSVからRGBに変換
            Color firstShadeUnityColor = Color.HSVToRGB(h1, s1, v1);
            Color secondShadeUnityColor = Color.HSVToRGB(h2, s2, v2);

            return (firstShadeUnityColor, secondShadeUnityColor);
        }

        /// <summary>
        /// 髪の色を適用します。 (彩度制限はHairColor定義で行う)
        /// </summary>
        public void ApplyColor(HairColor hairColor)
        {
            if (_animator == null) return;
            _currentHairColor = hairColor;
            ColorValue baseHairColorValue = hairColor.Value; // 既に彩度が調整された値

            // Convert Domain ColorValue to UnityEngine.Color
            Color baseUnityColor = new(baseHairColorValue.R, baseHairColorValue.G, baseHairColorValue.B, baseHairColorValue.A);

            // Call the base class method to apply the adjusted color
            ApplyColorInternal(baseUnityColor);
        }

        /// <summary>
        /// 現在の髪の色を取得します。
        /// </summary>
        public HairColor GetCurrentColor()
        {
            return _currentHairColor;
        }

        /// <summary>
        /// 髪色をデフォルトにリセットします。
        /// </summary>
        public void ResetColor()
        {
            ApplyColor(HairColor.Default);
        }

        /// <summary>
        /// デフォルトの髪のベースカラーを取得します。
        /// (注意：このメソッドはマテリアルから直接色を取得するため、定義時の彩度制限は反映されません)
        /// </summary>
        public Dictionary<string, Color> GetDefaultBaseColors()
        {
            return base.GetDefaultBaseColors();
        }

        /// <summary>
        /// 非同期で髪色を適用します。 (彩度制限は ColorValue/HairColor 定義で行う)
        /// </summary>
        public async UniTask ApplyColorAsync(ColorValue colorValue, IEnumerable<Renderer> renderersToUpdate)
        {
            _currentHairColor = new HairColor(colorValue); // colorValue は既に調整済みと想定

            // Call the base class async method with the color value
            // The base class's ApplyColorAsyncInternal now handles iterating through renderersToUpdate
            // and calling the overridden IsTargetRenderer(Renderer) for each one.
            await base.ApplyColorAsyncInternal(colorValue, renderersToUpdate);
        }
    }
}
