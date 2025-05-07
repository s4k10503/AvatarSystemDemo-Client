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
        /// <param name="animator">アバターのアニメーター</param>
        public AvatarSkinColorService(Animator animator)
            : base(animator)
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

                bool shaderMatch = (material.shader != null && material.shader.name.ToLower().Contains("skin"));

                if (nameMatch || shaderMatch)
                {
                    return true; // Found a skin material by name/shader
                }
            }

            // アバターモデルによっては基本的なShaderのみで構成されている場合があるため、
            // 肌色に近い色を持つマテリアルを検出する (Fallback)
            foreach (var material in materialsToCheck) // Check materials again for color fallback
            {
                if (material == null) continue;

                if (material.HasProperty("_Color"))
                {
                    Color materialColor = material.color; // Get default color from shared material
                    Color.RGBToHSV(materialColor, out float h, out float s, out float v);

                    // Check if color is within typical skin tone ranges
                    // 一般的な肌色は黄赤系（0-60度）で、彩度が低め～中程度
                    // (Adjusted range slightly: 0-60 degrees / 0.0-0.166f)
                    bool hueMatch = (h <= 0.166f || h >= 0.9f); // Allows reds and oranges
                    bool saturationMatch = s >= 0.1f && s <= 0.6f; // Broadened saturation slightly
                    bool valueMatch = v > 0.2f; // Avoid very dark colors being mistaken for skin

                    if (hueMatch && saturationMatch && valueMatch)
                    {
                        // To avoid conflicts, ensure it doesn't also seem like hair (less likely but possible)
                        if (!material.name.ToLower().Contains("hair"))
                        {
                            return true; // Found a likely skin material by color
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
            // Skin shades are typically subtle decreases in value
            Color firstShadeUnityColor = Color.HSVToRGB(h, s, Mathf.Clamp01(v * 0.98f));
            Color secondShadeUnityColor = Color.HSVToRGB(h, s, Mathf.Clamp01(v * 0.96f));
            return (firstShadeUnityColor, secondShadeUnityColor);
        }

        /// <summary>
        /// 肌の色を適用します。
        /// </summary>
        public void ApplyColor(SkinColor skinColor)
        {
            if (_animator == null) return; // Keep basic guard
            _currentSkinColor = skinColor;
            ColorValue baseSkinColorValue = skinColor.Value;

            // Convert Domain ColorValue to UnityEngine.Color for material application
            Color baseUnityColor = new(baseSkinColorValue.R, baseSkinColorValue.G, baseSkinColorValue.B, baseSkinColorValue.A);

            // Call the base class method to apply colors
            ApplyColorInternal(baseUnityColor);

            // Apply to specific _SkinColor property if it exists (add-on to base logic)
            foreach (var renderer in _renderers) // Use cached renderers from base
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
            _currentSkinColor = new SkinColor(color); // Update internal state if needed

            // Call the base class async method first
            await base.ApplyColorAsyncInternal(color, renderers);

            // Apply to specific _SkinColor property if it exists (add-on to base logic)
            Color baseUnityColor = new(color.R, color.G, color.B, color.A);
            if (renderers == null) return;

            foreach (var renderer in renderers) // renderer is already of type Renderer here
            {
                // IsTargetRenderer now accepts Renderer, so this check is fine.
                // The specific type casting for SkinnedMeshRenderer is no longer needed here
                // as IsTargetRenderer will be called by the base or this class's override.
                if (renderer == null || !IsTargetRenderer(renderer))
                {
                    continue;
                }

                // renderer.materials can be used for both SkinnedMeshRenderer and MeshRenderer
                foreach (var material in renderer.materials) // Apply to instance materials
                {
                    if (material == null) continue;
                    if (material.HasProperty("_SkinColor"))
                    {
                        material.SetColor("_SkinColor", baseUnityColor);
                    }
                }
            }
            // Base method already yields
        }
    }
}
