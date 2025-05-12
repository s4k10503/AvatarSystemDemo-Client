using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using R3;

namespace Presentation.Interfaces
{
    /// <summary>
    /// 入力インターフェース (R3ベース)
    /// </summary>
    public interface IInputHandler : IDisposable
    {
        // --- R3 Observables & Properties ---
        Observable<Vector2> PointerDown { get; }
        Observable<Vector2> PointerUp { get; }
        Observable<Vector2> PointerMove { get; }
        ReadOnlyReactiveProperty<Vector2> CurrentPointerPosition { get; }
        ReadOnlyReactiveProperty<Vector2> PreviousPointerPosition { get; }
        ReadOnlyReactiveProperty<bool> IsDragging { get; }
        ReadOnlyReactiveProperty<float> DragDistance { get; }
        Observable<Unit> GlobalDragStarted { get; }
        Observable<Unit> GlobalDragEnded { get; }
        Observable<VisualElement> ElementDragStarted { get; }
        Observable<VisualElement> ElementDragEnded { get; }

        /// <summary>
        /// ピンチジェスチャー中の2本の指の間の距離の変化を発行するObservable。
        /// 通常、正の値は指が離れることを意味し、負の値は指が近づくことを意味します。
        /// </summary>
        Observable<float> OnPinch { get; }

        // --- State & Actions ---

        /// <summary>
        /// ドラッグ中の要素を取得します。
        /// </summary>
        VisualElement DraggingElement { get; }

        /// <summary>
        /// ポインターがUI上にあるかどうかを確認します。
        /// </summary>
        /// <param name="position">確認する位置</param>
        /// <returns>UI上にある場合はtrue</returns>
        bool IsPointerOverUI(Vector2 position);

        /// <summary>
        /// 特定の要素に対するドラッグを開始します。
        /// </summary>
        /// <param name="element">ドラッグを開始する要素</param>
        void StartDraggingOnElement(VisualElement element);

        /// <summary>
        /// UI要素に紐付かないグローバルなドラッグを開始します。
        /// </summary>
        void StartGlobalDragging();

        /// <summary>
        /// 短いドラッグ（タップ）かどうかを確認します。
        /// </summary>
        bool IsShortDrag { get; }

        // --- Touch Utilities (Multi-touch support) ---

        /// <summary>
        /// 現在のタッチ数を取得します。
        /// </summary>
        /// <returns>タッチ数</returns>
        int GetTouchCount();

        /// <summary>
        /// 指定したインデックスのタッチ情報を取得します。
        /// </summary>
        /// <param name="index">タッチのインデックス</param>
        /// <returns>タッチ情報。存在しない場合はnull</returns>
        Touch? GetTouch(int index);

        /// <summary>
        /// 指定したフェーズに合致する最初のタッチを取得します。
        /// </summary>
        /// <param name="phase">検索するタッチフェーズ</param>
        /// <param name="touch">見つかったタッチ情報</param>
        /// <returns>合致するタッチが見つかった場合はtrue</returns>
        bool TryGetTouchByPhase(TouchPhase phase, out Touch touch);

        /// <summary>
        /// 指定したフェーズに合致するすべてのタッチを取得します。
        /// </summary>
        /// <param name="phase">検索するタッチフェーズ</param>
        /// <returns>合致するタッチ情報の列挙子</returns>
        IEnumerable<Touch> GetTouchesByPhase(TouchPhase phase);
    }
}
