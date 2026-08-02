using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Numeria.Game
{
    /// <summary>
    /// 数字水晶:点按即提交;移动超过阈值进入拖拽,松手落在空格上也提交。
    /// 阈值规避手指点按的 1-2px 抖动(Web 原型验证过的交互)。
    /// </summary>
    public class Crystal : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public int Value;
        public RectTransform SlotRect;            // 为 null 时只支持点按
        public Action<int, Crystal> OnSubmit;

        private const float DragThreshold = 12f;  // 屏幕像素
        private RectTransform _rt;
        private Vector2 _homePos;
        private Vector2 _pointerStart;
        private bool _dragging;

        private void Awake()
        {
            _rt = (RectTransform)transform;
        }

        private void Start()
        {
            _homePos = _rt.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData e)
        {
            _pointerStart = e.position;
            _dragging = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (SlotRect == null) return;
            if (!_dragging && (e.position - _pointerStart).magnitude < DragThreshold) return;
            _dragging = true;
            _rt.position = e.position;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_dragging)
            {
                OnSubmit?.Invoke(Value, this);
                return;
            }
            _dragging = false;
            bool overSlot = SlotRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(SlotRect, e.position);
            _rt.anchoredPosition = _homePos;
            if (overSlot) OnSubmit?.Invoke(Value, this);
        }
    }
}
