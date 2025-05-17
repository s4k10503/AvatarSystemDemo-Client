using UnityEngine;
using System.Collections.Generic;
using System;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Infrastructure.Services
{
    public sealed class AvatarCustomizationService : IAvatarCustomizationService, IAnimatorSettable, IDisposable
    {
        private object _animator;
        private BodyScale _currentScale;

        // ボーンキャッシュ（すべてのHumanBodyBonesをまとめる）
        private readonly Dictionary<HumanBodyBones, Transform> _boneCache = new();

        // 各部位のボーン群
        private Transform[] _torsoBones;      // Hips, Spine, Chest, UpperChest
        private Transform[] _headBones;       // Head
        private Transform[] _shoulderBones;   // LeftShoulder, RightShoulder
        private Transform[] _armBones;        // LeftUpperArm, RightUpperArm, LeftLowerArm, RightLowerArm
        private Transform[] _upperLegBones;   // LeftUpperLeg, RightUpperLeg
        private Transform[] _lowerLegBones;   // LeftLowerLeg, RightLowerLeg
        private Transform[] _footBones;       // LeftFoot, RightFoot

        public AvatarCustomizationService(IAvatarParameterRepository repository)
        {
            _currentScale = new BodyScale();
        }

        /// <summary>
        /// Animator (object型) を設定し、ボーンキャッシュと関連ボーン群を初期化します。
        /// </summary>
        public void SetAnimator(object animatorInstance)
        {
            _animator = animatorInstance;
            var unityAnimator = _animator as Animator;
            if (unityAnimator != null)
            {
                CacheBones(unityAnimator);
                InitializeBoneGroups(unityAnimator);
            }
            else
            {
                if (animatorInstance != null)
                {
                    Debug.LogWarning("AvatarCustomizationService: Animator のキャストに失敗しました。");
                }
                _boneCache.Clear();
                _torsoBones = null;
                _headBones = null;
                _shoulderBones = null;
                _armBones = null;
                _upperLegBones = null;
                _lowerLegBones = null;
                _footBones = null;
            }
        }

        private void InitializeBoneGroups(Animator unityAnimator)
        {
            if (unityAnimator == null) return;

            // キャッシュを用いて各部位のボーン群を初期化
            _torsoBones = new Transform[]
            {
                GetBone(HumanBodyBones.Hips),
                GetBone(HumanBodyBones.Spine),
                GetBone(HumanBodyBones.Chest),
                GetBone(HumanBodyBones.UpperChest)
            };

            _headBones = new Transform[]
            {
                GetBone(HumanBodyBones.Head)
            };

            _shoulderBones = new Transform[]
            {
                GetBone(HumanBodyBones.LeftShoulder),
                GetBone(HumanBodyBones.RightShoulder)
            };

            _armBones = new Transform[]
            {
                GetBone(HumanBodyBones.LeftUpperArm),
                GetBone(HumanBodyBones.RightUpperArm),
                GetBone(HumanBodyBones.LeftLowerArm),
                GetBone(HumanBodyBones.RightLowerArm)
            };

            _upperLegBones = new Transform[]
            {
                GetBone(HumanBodyBones.LeftUpperLeg),
                GetBone(HumanBodyBones.RightUpperLeg)
            };

            _lowerLegBones = new Transform[]
            {
                GetBone(HumanBodyBones.LeftLowerLeg),
                GetBone(HumanBodyBones.RightLowerLeg)
            };

            _footBones = new Transform[]
            {
                GetBone(HumanBodyBones.LeftFoot),
                GetBone(HumanBodyBones.RightFoot)
            };
        }

        /// <summary>
        /// Humanoidボーンをキャッシュします。
        /// </summary>
        private void CacheBones(Animator unityAnimator)
        {
            if (unityAnimator == null) return;
            _boneCache.Clear();

            foreach (HumanBodyBones boneType in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (boneType == HumanBodyBones.LastBone) continue;
                var bone = unityAnimator.GetBoneTransform(boneType);
                if (bone != null)
                {
                    _boneCache[boneType] = bone;
                }
            }
        }

        /// <summary>
        /// キャッシュから指定のボーンを取得します。
        /// </summary>
        private Transform GetBone(HumanBodyBones bone)
        {
            _boneCache.TryGetValue(bone, out Transform transform);
            return transform;
        }

        /// <summary>
        /// 体全体のスケール（身長、横幅、肩幅、頭サイズ）を適用します。
        /// </summary>
        public void ApplyBodyScale(BodyScale scale)
        {
            var unityAnimator = _animator as Animator;
            if (unityAnimator == null) return;
            _currentScale = scale;

            ApplyHeight(scale.Height);
            ApplyBodyWidth(scale.BodyWidth);
            ApplyShoulderWidth(scale.ShoulderWidth);
            ApplyArmScale(scale.ShoulderWidth); // 肩幅に基づいて腕のスケールも調整
            ApplyHeadScale(scale.HeadSize);
        }

        /// <summary>
        /// 身長スケールを適用し、足の位置を調整します。
        /// </summary>
        public void ApplyHeight(float height)
        {
            var unityAnimator = _animator as Animator;
            if (unityAnimator == null) return;
            var hips = GetBone(HumanBodyBones.Hips);
            if (hips == null) return;

            // 現在の足の最低Y座標を保存
            float originalFootY = GetMinimumFootY();

            // 胴体・脚部（上半身＋下半身）のX,Y軸に一様スケールを適用（Z軸はそのまま）
            ApplyUniformXYScale(_torsoBones, height);
            ApplyUniformXYScale(_upperLegBones, height);
            ApplyUniformXYScale(_lowerLegBones, height);
            ApplyUniformXYScale(_footBones, height);

            // 腕のボーンには親のローカルスケールを考慮してスケール調整
            // (身長変更時に腕の太さが変わらないようにするため)
            foreach (var armBone in _armBones)
            {
                if (armBone?.parent != null)
                {
                    var parentScale = armBone.parent.lossyScale;
                    // 肩 (Shoulder) は長さ(X)を維持し、他軸を親に合わせて調整
                    if (armBone.name.Contains("Shoulder", StringComparison.OrdinalIgnoreCase))
                    {
                        armBone.localScale = new Vector3(
                            armBone.localScale.x, // 現在のX（長さ）を維持
                            height / parentScale.y, // 親のYスケールに合わせる
                            height / parentScale.z // 親のZスケールに合わせる
                        );
                    }
                    else // 上腕・下腕はXYZ均等にスケール
                    {
                        armBone.localScale = new Vector3(
                            height / parentScale.x,
                            height / parentScale.y,
                            height / parentScale.z
                        );
                    }
                }
            }

            // スケール適用後に足の最低Y座標の変化分を計算し、hipsの位置を調整
            float newFootY = GetMinimumFootY();
            float deltaY = newFootY - originalFootY;
            hips.position = new Vector3(hips.position.x, hips.position.y - deltaY, hips.position.z);
        }

        /// <summary>
        /// 指定ボーン配列のX,Y軸ローカルスケールを一様に変更します。
        /// </summary>
        private void ApplyUniformXYScale(Transform[] bones, float scale)
        {
            if (bones == null) return;
            foreach (var bone in bones)
            {
                if (bone == null) continue;
                Vector3 current = bone.localScale;
                bone.localScale = new Vector3(scale, scale, current.z);
            }
        }

        /// <summary>
        /// 両足の最低Y座標を取得します。
        /// </summary>
        private float GetMinimumFootY()
        {
            if (_footBones == null || _footBones.Length < 2) return float.MaxValue;
            float leftY = _footBones[0] != null ? _footBones[0].position.y : float.MaxValue;
            float rightY = _footBones[1] != null ? _footBones[1].position.y : float.MaxValue;
            return Mathf.Min(leftY, rightY);
        }

        /// <summary>
        /// 腕の根本（肩）のZ軸スケール（幅）を調整します。
        /// </summary>
        private void ApplyArmScale(float shoulderWidth)
        {
            if (_shoulderBones == null) return;
            foreach (var shoulderBone in _shoulderBones)
            {
                if (shoulderBone?.parent != null)
                {
                    var parentScale = shoulderBone.parent.lossyScale;
                    Vector3 current = shoulderBone.localScale;
                    // X(長さ), Y(太さ) は変更せず、Z(幅)のみ調整
                    shoulderBone.localScale = new Vector3(
                        current.x,
                        current.y, // Yは変更しない（身長で調整されるため）
                        shoulderWidth / parentScale.z
                    );
                }
            }
        }

        /// <summary>
        /// 頭のXYZスケールを均一に適用します。
        /// </summary>
        public void ApplyHeadScale(float headSize)
        {
            if (_headBones == null) return;
            foreach (var headBone in _headBones)
            {
                if (headBone?.parent != null)
                {
                    var parentScale = headBone.parent.lossyScale;
                    headBone.localScale = new Vector3(
                        headSize / parentScale.x,
                        headSize / parentScale.y,
                        headSize / parentScale.z
                    );
                }
            }
        }

        /// <summary>
        /// 体の横幅（下半身中心）のZ軸スケールを適用します。
        /// </summary>
        public void ApplyBodyWidth(float width)
        {
            var unityAnimator = _animator as Animator;
            if (unityAnimator == null) return;

            // 対象ボーンは hips と脚部の上部・下部
            var bonesToScale = new List<Transform>(
                _upperLegBones.Length + _lowerLegBones.Length + 1 // Initial capacity
            );
            var hipsBone = GetBone(HumanBodyBones.Hips);
            if (hipsBone != null) bonesToScale.Add(hipsBone);
            bonesToScale.AddRange(_upperLegBones);
            bonesToScale.AddRange(_lowerLegBones);

            foreach (var bone in bonesToScale)
            {
                if (bone == null || bone.parent == null) continue;
                var parentScale = bone.parent.lossyScale;
                Vector3 current = bone.localScale;
                // X, Y は変更せず Z のみ適用
                bone.localScale = new Vector3(
                    current.x,
                    current.y,
                    width / parentScale.z
                );
            }
        }

        /// <summary>
        /// 肩幅（上半身中心）のZ軸スケールを適用します。
        /// </summary>
        public void ApplyShoulderWidth(float width)
        {
            var unityAnimator = _animator as Animator;
            if (unityAnimator == null) return;

            Transform[] upperBodyBones = new Transform[]
            {
                GetBone(HumanBodyBones.Spine),
                GetBone(HumanBodyBones.Chest),
                GetBone(HumanBodyBones.UpperChest)
            };

            foreach (var bone in upperBodyBones)
            {
                if (bone == null || bone.parent == null) continue;
                var parentScale = bone.parent.lossyScale;
                Vector3 current = bone.localScale;
                // X, Y は変更せず Z のみ適用
                bone.localScale = new Vector3(
                    current.x,
                    current.y,
                    width / parentScale.z
                );
            }
        }

        /// <summary>
        /// 現在のボディスケールを取得します。
        /// </summary>
        public BodyScale GetCurrentBodyScale()
        {
            return _currentScale;
        }

        /// <summary>
        /// ボディスケールをデフォルト値にリセットします。
        /// </summary>
        public void ResetBodyScale()
        {
            ApplyBodyScale(new BodyScale());
        }

        /// <summary>
        /// リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            _animator = null;
            _boneCache.Clear();
            _torsoBones = null;
            _headBones = null;
            _shoulderBones = null;
            _armBones = null;
            _upperLegBones = null;
            _lowerLegBones = null;
            _footBones = null;
        }
    }
}
