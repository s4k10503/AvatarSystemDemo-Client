using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

using Domain.ValueObjects;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    /// <summary>
    /// アバターの色操作サービスの基本クラスです。
    /// </summary>
    public abstract class AvatarColorServiceBase : IAnimatorSettable, IDisposable
    {
        protected object _animator;
        protected readonly List<Renderer> _renderers = new();

        /// <summary>
        /// 基本サービスのコンストラクタです。
        /// </summary>
        protected AvatarColorServiceBase()
        {
        }

        public virtual void Dispose()
        {
            _animator = null;
            _renderers.Clear();
        }

        /// <summary>
        /// Animator を設定し、関連するレンダラーをキャッシュします。
        /// IAnimatorSettable インターフェースの実装。
        /// </summary>
        /// <param name="animatorInstance">アバターのアニメーターコンポーネント(object型)。</param>
        public virtual void SetAnimator(object animatorInstance)
        {
            _animator = animatorInstance;
            var unityAnimator = _animator as Animator;
            if (unityAnimator == null && animatorInstance != null)
            {
                Debug.LogWarning("AvatarColorServiceBase: Animator のキャストに失敗しました。");
            }
            CacheRenderers(unityAnimator);
        }

        /// <summary>
        /// 実装基準に基づいて、関連するSkinnedMeshRendererをキャッシュします。
        /// </summary>
        protected void CacheRenderers(Animator unityAnimator)
        {
            _renderers.Clear();
            if (unityAnimator == null) return;

            var allSkinnedRenderers = unityAnimator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in allSkinnedRenderers)
            {
                if (IsTargetRenderer(renderer))
                {
                    _renderers.Add(renderer);
                }
            }
            var allMeshRenderers = unityAnimator.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in allMeshRenderers)
            {
                if (IsTargetRenderer(renderer))
                {
                    if (!_renderers.Contains(renderer))
                    {
                        _renderers.Add(renderer);
                    }
                }
            }
        }

        /// <summary>
        /// レンダラーが特定の色サービス（例：髪、肌）に関連しているかどうかを判断します。
        /// 派生クラスで実装する必要があります。
        /// </summary>
        /// <param name="renderer">チェックするレンダラー。</param>
        /// <returns>レンダラーが関連している場合はtrue、それ以外はfalse。</returns>
        protected abstract bool IsTargetRenderer(Renderer renderer);

        /// <summary>
        /// ベースカラーに基づいてシェードカラーを計算します。
        /// 派生クラスで実装する必要があります。
        /// </summary>
        /// <param name="baseColor">ベースカラー。</param>
        /// <returns>最初のシェードカラーと2番目のシェードカラーを含むタプル。</returns>
        protected abstract (Color firstShade, Color secondShade) CalculateShadeColors(Color baseColor);

        /// <summary>
        /// ベースカラーと計算されたシェードをキャッシュされたレンダラーに適用します。
        /// </summary>
        /// <param name="baseUnityColor">適用するベースカラー。</param>
        protected virtual void ApplyColorInternal(Color baseUnityColor)
        {
            var unityAnimator = _animator as Animator;
            if (unityAnimator == null) return;
            var (firstShadeUnityColor, secondShadeUnityColor) = CalculateShadeColors(baseUnityColor);
            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;
                // renderer.materials は SkinnedMeshRenderer と MeshRenderer の両方で使用できる
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    // 一般的なシェーダープロパティとトゥーンシェーダープロパティに色を適用します
                    if (material.HasProperty("_Color")) material.SetColor("_Color", baseUnityColor);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseUnityColor);
                    if (material.HasProperty("_1st_ShadeColor")) material.SetColor("_1st_ShadeColor", firstShadeUnityColor);
                    if (material.HasProperty("_2nd_ShadeColor")) material.SetColor("_2nd_ShadeColor", secondShadeUnityColor);

                    // フォールバック/標準プロパティとしてmaterial.colorに適用します
                    material.color = baseUnityColor;
                }
            }
        }

        /// <summary>
        /// 関連する共有マテリアルからデフォルトのベースカラーを取得します。
        /// </summary>
        /// <returns>マテリアル名をキー、デフォルトのベースカラーを値とする辞書。</returns>
        public virtual Dictionary<string, Color> GetDefaultBaseColors()
        {
            var defaultColors = new Dictionary<string, Color>();
            var unityAnimator = _animator as Animator;
            if (unityAnimator == null)
            {
                return defaultColors;
            }
            if (_renderers.Count == 0) CacheRenderers(unityAnimator);

            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;

                // renderer.sharedMaterials は SkinnedMeshRenderer と MeshRenderer の両方で使用できる
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    bool considerMaterial = true;
                    if (considerMaterial && material.HasProperty("_BaseColor"))
                    {
                        defaultColors[material.name] = material.GetColor("_BaseColor");
                    }
                    else if (considerMaterial && material.HasProperty("_Color") && !defaultColors.ContainsKey(material.name))
                    {
                        defaultColors[material.name] = material.GetColor("_Color");
                    }
                }
            }
            return defaultColors;
        }

        /// <summary>
        /// 特定のレンダラーセットに非同期で色を適用します。
        /// </summary>
        /// <param name="color">適用する色の値。</param>
        /// <param name="renderersToUpdate">更新する特定のレンダラー。</param>
        /// <returns>非同期操作を表すUniTask。</returns>
        public virtual async UniTask ApplyColorAsyncInternal(ColorValue color, IEnumerable<Renderer> renderersToUpdate)
        {
            var unityAnimator = _animator as Animator;
            if (unityAnimator == null || renderersToUpdate == null) return;

            Color baseUnityColor = new Color(color.R, color.G, color.B, color.A);
            var (firstShadeUnityColor, secondShadeUnityColor) = CalculateShadeColors(baseUnityColor);
            foreach (var renderer in renderersToUpdate)
            {
                // レンダラーがnullであるか、ターゲットでない場合はスキップ
                if (renderer == null || !IsTargetRenderer(renderer))
                {
                    continue;
                }

                // renderer.materials は SkinnedMeshRenderer と MeshRenderer の両方で使用できる
                foreach (var material in renderer.materials) // インスタンスマテリアルに適用
                {
                    if (material == null) continue;

                    // 色を適用
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseUnityColor);
                    if (material.HasProperty("_1st_ShadeColor")) material.SetColor("_1st_ShadeColor", firstShadeUnityColor);
                    if (material.HasProperty("_2nd_ShadeColor")) material.SetColor("_2nd_ShadeColor", secondShadeUnityColor);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", baseUnityColor);
                    material.color = baseUnityColor;
                }
            }
            await UniTask.Yield();
        }
    }
}