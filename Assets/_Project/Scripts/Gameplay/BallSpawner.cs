using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MergeBlast
{
    public sealed class BallSpawner : MonoBehaviour
    {
        [SerializeField] private Ball _prefab;
        [SerializeField] private Transform _balls;
        [SerializeField] private PortraitLayout _layout;
        [SerializeField] private Collider2D _floor;
        [SerializeField] private BallConfig _ballConfig;
        [SerializeField] private WaveConfig _wave;
        [SerializeField] private LayerMask _occupiedMask;

        private readonly HashSet<Ball> _active = new();
        private readonly Collider2D[] _overlaps = new Collider2D[1];
        private ContactFilter2D _occupancy;
        private PhysicsMaterial2D _material;
        private float _spawnProgress;

        public int ActiveCount => _active.Count;
        public event Action<int> BallDestroyed;

        private void Awake()
        {
            _occupancy.SetLayerMask(_occupiedMask);
            _occupancy.useTriggers = false;
            _material = new PhysicsMaterial2D("Ball bounce")
            {
                bounciness = _ballConfig.Bounciness,
                friction = _ballConfig.Friction
            };
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

        public void Tick(float elapsed, float step)
        {
            if (_active.Count >= _wave.MaxActive)
            {
                _spawnProgress = 0f;
                return;
            }

            _spawnProgress += _wave.SpawnRateAt(elapsed) * step;
            if (_spawnProgress < 1f)
                return;

            _spawnProgress %= 1f;

            var phase = _wave.Phases[0];
            foreach (var candidate in _wave.Phases)
            {
                if (candidate.StartsAt > elapsed)
                    break;
                phase = candidate;
            }

            int nominal = phase.Nominals[Random.Range(0, phase.Nominals.Length)];
            foreach (var tier in _ballConfig.Tiers)
            {
                if (tier.Nominal == nominal)
                {
                    TrySpawn(tier);
                    return;
                }
            }
        }

        public void Clear()
        {
            _spawnProgress = 0f;
            foreach (var ball in _active)
            {
                if (ball == null)
                    continue;

                ball.Removed -= OnBallRemoved;
                ball.Despawn();
            }

            _active.Clear();
        }

        private void TrySpawn(BallConfig.Tier tier)
        {
            Rect bounds = _layout.SpawnBounds;
            Rect arena = _layout.ArenaBounds;
            float minX = Mathf.Max(bounds.xMin, arena.xMin + tier.Radius);
            float maxX = Mathf.Min(bounds.xMax, arena.xMax - tier.Radius);
            float minY = Mathf.Max(bounds.yMin, arena.yMin + tier.Radius);
            float maxY = Mathf.Min(bounds.yMax, arena.yMax - tier.Radius);
            if (minX >= maxX || minY >= maxY)
                return;

            const int placementAttempts = 8;
            for (int i = 0; i < placementAttempts; i++)
            {
                var position = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                if (Physics2D.OverlapCircle(position, tier.Radius, _occupancy, _overlaps) != 0)
                    continue;

                float speed = Random.Range(_ballConfig.SpawnHorizontalSpeed.x, _ballConfig.SpawnHorizontalSpeed.y);
                float horizontal = Random.value < 0.5f ? -speed : speed;
                var ball = Instantiate(_prefab, position, Quaternion.identity, _balls);
                ball.Initialize(_ballConfig, tier, _floor, _material, new Vector2(horizontal, 0f));
                _active.Add(ball);
                if (_active.Count >= _wave.MaxActive)
                    _spawnProgress = 0f;
                ball.Removed += OnBallRemoved;
                return;
            }
        }

        private void OnBallRemoved(Ball ball, int reward)
        {
            ball.Removed -= OnBallRemoved;
            if (_active.Remove(ball) && reward > 0)
                BallDestroyed?.Invoke(reward);
        }
    }
}
