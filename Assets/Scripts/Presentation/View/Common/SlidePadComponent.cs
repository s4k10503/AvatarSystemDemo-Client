using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Presentation.View
{
    /// <summary>
    /// スライドパッドUIのロジックハンドラー。
    /// VisualElementを継承しない。
    /// </summary>
    public sealed class SlidePadComponent
    {
        // 外部で設定されるUI要素への参照
        private VisualElement _background;
        private VisualElement _handle;

        private Vector2 _startPosition;
        private Vector2 _currentDirection = Vector2.zero;
        private float _backgroundRadius;

        /// <summary>
        /// スライドパッド入力の正規化された方向ベクトル。
        /// </summary>
        public Vector2 Direction => _currentDirection;

        // 所有者にドラッグ試行を通知するイベント
        public event Action<VisualElement> OnRequestDragStart; // パラメータは背景要素
        public event Action OnRequestDragEnd;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SlidePadComponent()
        {
            // 初期化ロジックはSetElementsまたは要素が設定された後に発生します
        }

        /// <summary>
        /// このハンドラーが操作するVisualElementsを設定します。
        /// </summary>
        /// <param name="background">背景要素</param>
        /// <param name="handle">ハンドル要素</param>
        public void SetElements(VisualElement background, VisualElement handle)
        {
            _background = background;
            _handle = handle;

            if (_background == null)
            {
                Debug.LogError("[SlidePadLogic] Background element is null!");
                return;
            }
            if (_handle == null)
            {
                Debug.LogError("[SlidePadLogic] Handle element is null!");
            }

            _background.RegisterCallback<GeometryChangedEvent>(OnBackgroundGeometryChanged);
        }

        /// <summary>
        /// 背景要素のジオメトリ変更を処理します。
        /// </summary>
        /// <param name="evt">GeometryChangedEvent</param>
        private void OnBackgroundGeometryChanged(GeometryChangedEvent evt)
        {
            if (_background == null) return;
            _backgroundRadius = _background.resolvedStyle.width / 2f;

            // 半径が有効な場合のみリセット。ドラッグ状態は外部で管理されています。
            if (_backgroundRadius > 0 /* && external check for not dragging needed if resetting only when idle */)
            {
                ResetHandlePosition();
            }
        }

        /// <summary>
        /// イベントハンドラーは外部からRegisterCallbackを介して呼び出されるようになりました。
        /// </summary>
        /// <param name="evt">PointerDownEvent</param>
        public void OnPointerDown(PointerDownEvent evt)
        {
            if (_background == null)
            {
                return;
            }

            // 所有者にドラッグ試行が開始されたことを通知します。
            OnRequestDragStart?.Invoke(_background);

            // 所有者はInputHandlerの状態に基づいてポインターをキャプチャするかどうかを決定する必要があります。

            _startPosition = _background.WorldToLocal(evt.position);
            UpdateHandlePosition(evt.position); // ダウンイベントに基づいて初期位置を計算

            evt.StopPropagation();
        }

        public void OnPointerMove(PointerMoveEvent evt)
        {
            if (_background == null) return;

            // ポインターがキャプチャされている場合は、常にムーブイベントに基づいて位置を更新します。
            // 所有者（AvatarSystemPage）は、ドラッグがアクティブな場合のみこのメソッドを呼び出します。
            UpdateHandlePosition(evt.position);

            evt.StopPropagation();
        }

        private void UpdateHandlePosition(Vector2 pointerPosition)
        {
            if (_background == null || _handle == null) return;

            Vector2 localPosition = _background.WorldToLocal(pointerPosition);

            Vector2 center = new(_backgroundRadius, _backgroundRadius);

            Vector2 direction = localPosition - center;

            float distance = direction.magnitude;

            Vector2 clampedDirection = Vector2.ClampMagnitude(direction, _backgroundRadius);

            // 位置をtranslateで設定
            _handle.style.translate = new StyleTranslate(new Translate(clampedDirection.x, clampedDirection.y, 0));

            if (distance > 0.1f)
            {
                _currentDirection = clampedDirection / _backgroundRadius;
            }
            else
            {
                _currentDirection = Vector2.zero;
            }
        }

        public void OnPointerUp(PointerUpEvent evt)
        {
            if (_background == null)
            {
                return;
            }

            // 所有者にポインターが背景要素にアップされたことを通知します。
            OnRequestDragEnd?.Invoke();

            // 所有者はポインターを解放し、状態をリセットするかどうかを決定する必要があります。

            evt.StopPropagation();
        }

        /// <summary>
        /// このイベントは必要に応じて外部で登録する必要があるかもしれません。
        /// </summary>
        /// <param name="evt">PointerCaptureOutEvent</param>
        public void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_background == null)
            {
                return;
            }

            // 所有者にポインターのキャプチャが失われたことを通知します。
            OnRequestDragEnd?.Invoke(); // キャプチャアウトはポインターアップと同じようにドラッグを終了するため
        }

        /// <summary>
        /// ResetStateは必要に応じて外部でトリガーされるようになりました。
        /// </summary>
        public void ResetState()
        {
            _currentDirection = Vector2.zero;
            ResetHandlePosition();
        }

        private void ResetHandlePosition()
        {
            if (_handle == null || _background == null) return;

            // Guard: 背景の半径が計算されている場合のみ続行
            if (_backgroundRadius <= 0)
            {
                return;
            }

            float handleRadius = _handle.resolvedStyle.width / 2f;
            if (handleRadius <= 0)
            {
                handleRadius = _handle.resolvedStyle.width / 2f;
                if (handleRadius <= 0)
                {
                }
            }

            // 位置をtranslateでリセット（Flexboxが中央揃えを処理）
            _handle.style.translate = new StyleTranslate(Translate.None());
        }
    }
}