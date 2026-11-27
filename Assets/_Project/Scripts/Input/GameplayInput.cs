using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MergeBlast
{
    public sealed class GameplayInput : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _dragThresholdDp = 20f;
        [SerializeField, Min(1f)] private float _referenceWidth = 1080f;

        private readonly List<RaycastResult> _uiHits = new();
        private TouchControl _touch;
        private Mouse _mouse;
        private int _touchId;
        private Vector2 _lastPosition;
        private bool _dragging;
        private bool _acceptInput;

        public event Action<float> Dragged;

        public void SetAcceptInput(bool acceptInput)
        {
            _acceptInput = acceptInput;
            ResetPointer();
        }

        private void Update()
        {
            if (!_acceptInput)
                return;

            if (_touch != null && _touch.device.added && _touch.press.isPressed &&
                _touch.touchId.ReadValue() == _touchId)
            {
                ReadDrag(_touch.position.ReadValue());
                return;
            }

            if (_mouse != null && _mouse.added && _mouse.leftButton.isPressed)
            {
                ReadDrag(_mouse.position.ReadValue());
                return;
            }

            ResetPointer();

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (!touch.press.wasPressedThisFrame || !touch.press.isPressed)
                        continue;

                    if (BeginPointer(touch.position.ReadValue()))
                    {
                        _touch = touch;
                        _touchId = touch.touchId.ReadValue();
                        return;
                    }
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && mouse.leftButton.isPressed &&
                BeginPointer(mouse.position.ReadValue()))
                _mouse = mouse;
        }

        private bool BeginPointer(Vector2 position)
        {
            var events = EventSystem.current;
            if (events != null)
            {
                events.RaycastAll(new PointerEventData(events) { position = position }, _uiHits);
                bool overUi = _uiHits.Count > 0;
                _uiHits.Clear();
                if (overUi)
                    return false;
            }

            _lastPosition = position;
            return true;
        }

        private void ReadDrag(Vector2 position)
        {
            Vector2 delta = position - _lastPosition;
            if (!_dragging)
            {
                float pixelsPerDp = Screen.dpi > 0f ? Screen.dpi / 160f : Screen.width / _referenceWidth;
                float threshold = _dragThresholdDp * pixelsPerDp;
                if (delta.sqrMagnitude < threshold * threshold)
                    return;

                _dragging = true;
            }

            _lastPosition = position;
            if (delta.x != 0f)
                Dragged?.Invoke(delta.x);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                ResetPointer();
        }

        private void OnDisable() => ResetPointer();

        private void ResetPointer()
        {
            _touch = null;
            _mouse = null;
            _dragging = false;
        }
    }
}
