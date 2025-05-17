using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using Domain.Interfaces;
using Domain.ValueObjects;

namespace Infrastructure.Services
{
    /// <summary>
    /// アバターの肌色を管理するサービス。
    /// </summary>
    public sealed class AvatarSkinColorService : AvatarColorServiceBase, IAvatarSkinColorService
    {
        private SkinColor _currentSkinColor;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AvatarSkinColorService()
            : base()
        {
            _currentSkinColor = SkinColor.Default;
        }

        /// <summary>
        /// レンダラーが肌用かどうかを判定します。
        /// </summary>
        protected override bool IsTargetRenderer(Renderer renderer)
        {
            if (renderer == null) return false;

            // Exclude specific mesh names
            if (renderer.gameObject.name.ToLower().Contains("eye_base_old"))
            {
                return false; // Do not target meshes named "eye_base_old"
            }

            // Determine materials based on renderer type
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

            // マテリアル名やシェーダー名で判定する（例: "Skin", "Avatar/Skin" など）
            foreach (var material in materialsToCheck)
            {
                if (material == null) continue;

                string materialNameLower = material.name.ToLower();
                bool nameMatch = materialNameLower.Contains("skin") ||
                                 materialNameLower.Contains("face") ||
                                 materialNameLower.Contains("eyebase") ||
                                 materialNameLower.Contains("eyeline");

                bool shaderMatch = material.shader != null && material.shader.name.ToLower().Contains("skin");

                if (nameMatch || shaderMatch)
                {
                    return true; // 肌色用のマテリアルを見つけた
                }
            }

            // アバターモデルによっては基本的なShaderのみで構成されている場合があるため、
            // 肌色に近い色を持つマテリアルを検出する (Fallback)
            foreach (var material in materialsToCheck) // Check materials again for color fallback
            {
                if (material == null) continue;

                if (material.HasProperty("_Color"))
                {
                    Color materialColor = material.color; // Get the color from the material
                    Color.RGBToHSV(materialColor, out float h, out float s, out float v);

                    // コメントは日本語にして
                    // Check if color is within typical skin tone ranges　
                    // 一般的な肌色は黄赤系（0-60度）で、彩度が低め～中程度
                    // (Adjusted range slightly: 0-60 degrees / 0.0-0.166f)
                    bool hueMatch = h <= 0.166f || h >= 0.9f; // 赤色とオレンジ色を許可
                    bool saturationMatch = s >= 0.1f && s <= 0.6f; // 広げた彩度
                    bool valueMatch = v > 0.2f; // 非常に暗い色を肌色と間違えないように

                    if (hueMatch && saturationMatch && valueMatch)
                    {
                        // 競合を避けるため、髪のように見えないことを確認
                        if (!material.name.ToLower().Contains("hair"))
                        {
                            return true; // 色で肌色のマテリアルを見つけた
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 肌色用のシェードカラーを計算します。
        /// </summary>
        protected override (Color firstShade, Color secondShade) CalculateShadeColors(Color baseColor)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            // 肌のシェードは典型的には値のわずかな減少
            Color firstShadeUnityColor = Color.HSVToRGB(h, s, Mathf.Clamp01(v * 0.98f));
            Color secondShadeUnityColor = Color.HSVToRGB(h, s, Mathf.Clamp01(v * 0.96f));
            return (firstShadeUnityColor, secondShadeUnityColor);
        }

        /// <summary>
        /// 肌の色を適用します。
        /// </summary>
        public void ApplyColor(SkinColor skinColor)
        {
            if (_animator == null) return; // 基本的なガードを維持
            _currentSkinColor = skinColor;
            ColorValue baseSkinColorValue = skinColor.Value;

            // ドメインのColorValueをUnityEngine.Colorに変換してマテリアルに適用
            Color baseUnityColor = new(baseSkinColorValue.R, baseSkinColorValue.G, baseSkinColorValue.B, baseSkinColorValue.A);

            // ベースクラスのメソッドを呼び出して色を適用
            ApplyColorInternal(baseUnityColor);

            // 特定の_SkinColorプロパティが存在する場合は適用
            foreach (var renderer in _renderers) // ベースからキャッシュされたレンダラーを使用
            {
                if (renderer == null) continue;
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_SkinColor"))
                    {
                        material.SetColor("_SkinColor", baseUnityColor);
                    }
                }
            }
        }

        /// <summary>
        /// 現在の肌の色を取得します。
        /// </summary>
        public SkinColor GetCurrentColor()
        {
            return _currentSkinColor;
        }

        /// <summary>
        /// 肌色をデフォルトにリセットします。
        /// </summary>
        public void ResetColor()
        {
            ApplyColor(SkinColor.Default);
        }

        /// <summary>
        /// デフォルトの肌のベースカラーを取得します。
        /// </summary>
        public Dictionary<string, Color> GetDefaultBaseColors()
        {
            // Call the base class method
            return base.GetDefaultBaseColors();
        }

        /// <summary>
        /// 非同期で肌色を適用します。
        /// </summary>
        public async UniTask ApplyColorAsync(ColorValue color, IEnumerable<Renderer> renderers)
        {
            _currentSkinColor = new SkinColor(color); // 必要な場合は内部状態を更新

            // ベースクラスの非同期メソッドを最初に呼び出す
            await base.ApplyColorAsyncInternal(color, renderers);

            // 特定の_SkinColorプロパティが存在する場合は適用
            Color baseUnityColor = new(color.R, color.G, color.B, color.A);
            if (renderers == null) return;

            foreach (var renderer in renderers) // rendererはすでにRenderer型です
            {
                // IsTargetRendererはRendererを受け取るようになったので、このチェックは問題ありません。
                // 特定の型のキャストはもう必要ありません。
                // IsTargetRendererはベースクラスかこのクラスのオーバーライドで呼び出されます。
                if (renderer == null || !IsTargetRenderer(renderer))
                {
                    continue;
                }

                // renderer.materialsはSkinnedMeshRendererとMeshRendererの両方で使用できます
                foreach (var material in renderer.materials) // インスタンスのマテリアルに適用
                {
                    if (material == null) continue;
                    if (material.HasProperty("_SkinColor"))
                    {
                        material.SetColor("_SkinColor", baseUnityColor);
                    }
                }
            }
            // ベースメソッドはすでにyieldを返しています
        }
    }
}
