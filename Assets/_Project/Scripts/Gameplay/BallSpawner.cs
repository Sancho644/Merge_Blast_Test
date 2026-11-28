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
        [SerializeField] private MergeSystem _merge;
        [SerializeField] private MergeFeedback _feedback;

        private readonly HashSet<Ball> _active = new();
        private readonly Collider2D[] _overlaps = new Collider2D[1];
        private readonly ContactFilter2D _firstPairOccupancy = new() { useTriggers = true };
        private ContactFilter2D _occupancy;
        private PhysicsMaterial2D _material;
        private float _spawnProgress;
        private double _lastWaveSpawnAt = double.NegativeInfinity;
        // Restart starts a new interval without reopening a frame that already spawned a ball.
        private int _lastWaveSpawnFrame = -1;
        private Ball _firstPairBall;
        private double _firstPairStartedAt = -1d;
        private bool _firstPairResolved;

        public int ActiveCount => _active.Count;
        internal BattleSession Session { get; private set; }
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

        public void Tick(double elapsed, float step)
        {
            if (Session?.IsActive != true)
                return;

            if (_active.Count >= _wave.MaxActive)
            {
                _spawnProgress = 0f;
                return;
            }

            _spawnProgress += _wave.SpawnRateAt((float)elapsed) * step;
            if (!CanSpawnWaveBall)
            {
                _spawnProgress = Mathf.Min(_spawnProgress, 1f);
                return;
            }

            if (TryFirstPair(elapsed))
            {
                _spawnProgress = 0f;
                return;
            }

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
            if (_ballConfig.TryGetTier(nominal, out var tier))
                TrySpawn(tier);
        }

        internal void BeginBattle(BattleSession session) => Session = session;

        public void Clear()
        {
            Session = null;
            _spawnProgress = 0f;
            _lastWaveSpawnAt = double.NegativeInfinity;
            _firstPairBall = null;
            _firstPairStartedAt = -1d;
            _firstPairResolved = false;
            foreach (var ball in _active)
            {
                if (ball == null)
                    continue;

                ball.Removed -= OnBallRemoved;
                ball.Despawn();
            }

            _active.Clear();
        }

        internal void CopyActiveTo(List<Ball> snapshot)
        {
            snapshot.Clear();
            snapshot.AddRange(_active);
        }

        internal void SpawnMerged(BallConfig.Tier tier, Vector2 position, Vector2 velocity, int hitPoints)
        {
            Rect arena = _layout.ArenaBounds;
            position.x = Mathf.Clamp(position.x, arena.xMin + tier.Radius, arena.xMax - tier.Radius);
            position.y = Mathf.Clamp(position.y, arena.yMin + tier.Radius, arena.yMax - tier.Radius);
            Spawn(tier, position, velocity, hitPoints);
        }

        private bool TryFirstPair(double elapsed)
        {
            Vector2 window = _wave.FirstPairSpawnWindow;
            if (_firstPairResolved || elapsed < window.x)
                return false;

            if (elapsed > window.y || (_firstPairStartedAt >= 0f && (!_firstPairBall || !_firstPairBall.IsActive)))
            {
                _firstPairResolved = true;
                _firstPairBall = null;
                return false;
            }

            if (!_ballConfig.TryGetTier(_wave.FirstPairNominal, out var tier))
                return false;

            Rect bounds = GetSpawnBounds(tier.Radius);
            if (bounds.width <= 0f || bounds.height <= 0f)
                return false;

            if (_firstPairBall)
            {
                if (!TrySpawnSecond(tier, bounds, (float)(elapsed - _firstPairStartedAt)))
                    return false;

                _firstPairResolved = true;
                _firstPairBall = null;
                return true;
            }

            if (_active.Count > _wave.MaxActive - 2 || elapsed + _wave.MinSpawnInterval > window.y)
                return false;

            for (int i = 0; i < 2; i++)
            {
                var position = new Vector2(i == 0 ? bounds.xMin : bounds.xMax, bounds.center.y);
                if (!IsFree(position, tier.Radius, _firstPairOccupancy))
                    continue;

                float speed = Random.Range(_ballConfig.SpawnHorizontalSpeed.x, _ballConfig.SpawnHorizontalSpeed.y);
                _firstPairBall = TrySpawnWaveBall(tier, position, Vector2.right * (i == 0 ? speed : -speed));
                if (!_firstPairBall)
                    return false;
                _firstPairStartedAt = elapsed;
                return true;
            }

            return false;
        }

        private bool TrySpawnSecond(BallConfig.Tier tier, Rect bounds, float delay)
        {
            Rect arena = _layout.ArenaBounds;
            float left = arena.xMin + tier.Radius;
            float width = arena.width - 2f * tier.Radius;
            float drop = bounds.center.y - (arena.yMin + tier.Radius);
            float gravity = -Physics2D.gravity.y * _ballConfig.GravityScale;
            // Estimate when a ball rising from the floor crosses the second ball on its way down.
            float fallTime = gravity > 0f ? Mathf.Sqrt(2f * drop / gravity) : 0f;
            float lead = Mathf.Max(_ballConfig.SpawnMergeCooldown, fallTime - delay * 0.5f);
            float targetX = ReflectedX(_firstPairBall.Position.x + _firstPairBall.Velocity.x * lead, left, width);
            float speed = (_ballConfig.SpawnHorizontalSpeed.x + _ballConfig.SpawnHorizontalSpeed.y) * 0.5f;
            float bestDistance = float.PositiveInfinity;
            Vector2 bestPosition = default;
            float bestVelocity = 0f;
            for (int i = 0; i < 7; i++)
            {
                var position = new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, i / 6f), bounds.center.y);
                if (!IsFree(position, tier.Radius, _firstPairOccupancy))
                    continue;

                for (int side = -1; side <= 1; side += 2)
                {
                    float velocity = speed * side;
                    float distance = Mathf.Abs(ReflectedX(position.x + velocity * lead, left, width) - targetX);
                    if (distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    bestPosition = position;
                    bestVelocity = velocity;
                }
            }

            if (float.IsPositiveInfinity(bestDistance))
                return false;

            return TrySpawnWaveBall(tier, bestPosition, Vector2.right * bestVelocity) != null;
        }

        private static float ReflectedX(float x, float min, float width) =>
            min + width - Mathf.Abs(Mathf.Repeat(x - min, 2f * width) - width);

        private void TrySpawn(BallConfig.Tier tier)
        {
            Rect bounds = GetSpawnBounds(tier.Radius);
            if (bounds.width <= 0f || bounds.height <= 0f)
                return;

            const int placementAttempts = 8;
            for (int i = 0; i < placementAttempts; i++)
            {
                var position = new Vector2(Random.Range(bounds.xMin, bounds.xMax), Random.Range(bounds.yMin, bounds.yMax));
                if (!IsFree(position, tier.Radius, _occupancy))
                    continue;

                float speed = Random.Range(_ballConfig.SpawnHorizontalSpeed.x, _ballConfig.SpawnHorizontalSpeed.y);
                float horizontal = Random.value < 0.5f ? -speed : speed;
                TrySpawnWaveBall(tier, position, new Vector2(horizontal, 0f));
                return;
            }
        }

        private Rect GetSpawnBounds(float radius)
        {
            Rect spawn = _layout.SpawnBounds;
            Rect arena = _layout.ArenaBounds;
            return Rect.MinMaxRect(Mathf.Max(spawn.xMin, arena.xMin + radius), Mathf.Max(spawn.yMin, arena.yMin + radius),
                Mathf.Min(spawn.xMax, arena.xMax - radius), Mathf.Min(spawn.yMax, arena.yMax - radius));
        }

        private bool IsFree(Vector2 position, float radius, ContactFilter2D filter) =>
            Physics2D.OverlapCircle(position, radius, filter, _overlaps) == 0;

        private bool CanSpawnWaveBall =>
            Time.frameCount != _lastWaveSpawnFrame && Time.timeAsDouble - _lastWaveSpawnAt >= _wave.MinSpawnInterval;

        private Ball TrySpawnWaveBall(BallConfig.Tier tier, Vector2 position, Vector2 velocity)
        {
            if (Session?.IsActive != true || _active.Count >= _wave.MaxActive || !CanSpawnWaveBall)
                return null;

            var ball = Spawn(tier, position, velocity);
            _lastWaveSpawnAt = Time.timeAsDouble;
            _lastWaveSpawnFrame = Time.frameCount;
            return ball;
        }

        private Ball Spawn(BallConfig.Tier tier, Vector2 position, Vector2 velocity, int? mergedHp = null)
        {
            var ball = Instantiate(_prefab, position, Quaternion.identity, _balls);
            ball.Initialize(_ballConfig, tier, _floor, _material, velocity, Session, _merge, _feedback, mergedHp);
            _active.Add(ball);
            if (_active.Count >= _wave.MaxActive)
                _spawnProgress = 0f;
            ball.Removed += OnBallRemoved;
            return ball;
        }

        private void OnBallRemoved(Ball ball, int reward)
        {
            ball.Removed -= OnBallRemoved;
            if (_active.Remove(ball) && reward > 0)
                BallDestroyed?.Invoke(reward);
        }
    }
}
