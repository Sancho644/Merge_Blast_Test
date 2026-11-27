using UnityEngine;

namespace MergeBlast
{
    public sealed class CannonController : MonoBehaviour
    {
        [SerializeField] private GameplayInput _input;
        [SerializeField] private PortraitLayout _layout;
        [SerializeField] private Camera _camera;
        [SerializeField] private CannonConfig _config;
        [SerializeField] private BattleController _battle;
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private BoxCollider2D _hitbox;
        [SerializeField] private GameObject _visual;

        private float _targetX;
        private bool _active;

        private void OnEnable() => _input.Dragged += OnDragged;

        private void OnDisable()
        {
            if (_input != null)
                _input.Dragged -= OnDragged;
            EndBattle();
        }

        public void BeginBattle()
        {
            _body.simulated = true;
            _active = true;
            _targetX = _layout.ArenaBounds.center.x;
            _body.position = new Vector2(_targetX, _layout.CannonY);
            _visual.SetActive(true);
        }

        public void EndBattle()
        {
            _active = false;
            if (_body != null)
                _body.simulated = false;
            if (_visual != null)
                _visual.SetActive(true);
        }

        private void OnDragged(float pixels)
        {
            if (_active && _camera.pixelWidth > 0)
            {
                float worldWidth = 2f * _camera.orthographicSize * _camera.aspect;
                _targetX = ClampToArena(_targetX + pixels * worldWidth / _camera.pixelWidth);
            }
        }

        private void FixedUpdate()
        {
            if (!_active)
                return;

            _targetX = ClampToArena(_targetX);
            float x = Mathf.MoveTowards(_body.position.x, _targetX, _config.MoveSpeed * Time.fixedDeltaTime);
            _body.MovePosition(new Vector2(ClampToArena(x), _layout.CannonY));
        }

        private void Update()
        {
            if (_active)
            {
                bool visible = !_battle.IsInvulnerable || Mathf.Repeat(Time.time * 8f, 1f) >= 0.35f;
                if (_visual.activeSelf != visible)
                    _visual.SetActive(visible);
            }
        }

        private float ClampToArena(float x)
        {
            var bounds = _layout.ArenaBounds;
            float halfWidth = _hitbox.size.x * 0.5f;
            return Mathf.Clamp(x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_active && other.TryGetComponent<Ball>(out var ball))
                _battle.HandleCannonContact(ball, _body.position);
        }
    }
}
