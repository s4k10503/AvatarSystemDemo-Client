using UnityEngine;
using Application.DTO;

namespace Presentation.Interfaces
{
    /// <summary>
    /// カメラビューのインターフェース
    /// </summary>
    public interface ICameraView
    {
        /// <summary>
        /// ターゲットを設定
        /// </summary>
        /// <param name="target">ターゲット</param>
        void SetTarget(Transform target);

        /// <summary>
        /// 回転角度を設定
        /// </summary>
        /// <param name="rotationAngle">水平方向の回転角度（度数法）</param>
        void SetRotationAngle(float rotationAngle);

        /// <summary>
        /// 回転角度を変化量で更新
        /// </summary>
        /// <param name="delta">移動量</param>
        void UpdateRotationByDelta(Vector2 delta);

        /// <summary>
        /// 現在の回転角度を取得
        /// </summary>
        /// <returns>現在の回転角度</returns>
        float GetCurrentRotationAngle();

        /// <summary>
        /// 顔モードを設定
        /// </summary>
        /// <param name="isFaceMode">顔モードかどうか</param>
        void SetFaceMode(bool isFaceMode);

        /// <summary>
        /// アバター設定値を更新する
        /// </summary>
        /// <param name="settings">アバターカスタマイズ設定</param>
        void UpdateAvatarSettings(AvatarSettingsViewModel settings);

        /// <summary>
        /// 顔の位置情報を設定する
        /// </summary>
        /// <param name="headOffset">アバターのルートからの相対的な頭の位置</param>
        /// <param name="headPosition">頭の世界座標位置</param>
        void SetHeadPosition(Vector3 headOffset, Vector3 headPosition);
    }
}
