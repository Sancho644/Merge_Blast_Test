using System;
using TMPro;
using UnityEngine;

namespace MergeBlast
{
    public sealed class Ball : MonoBehaviour
    {
        private const int RingSegments = 40;
        private const float RingRadius = 1.12f;
        private const float TrackWidthToDiameter = 0.07f;
        private const float FillWidthToDiameter = 0.06f;
        private const float MergePunchDuration = 0.18f;

        private enum State { Removed, Active, Reserved }

        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private CircleCollider2D _collider;
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private TextMeshPro _number;
        [SerializeField] private LineRenderer _hpTrack;
        [SerializeField] private LineRenderer _hpFill;

        private BallConfig _config;
        private BallConfig.Tier _tier;
        private Collider2D _floor;
        private MergeSystem _merge;
        private MergeFeedback _feedback;
        private State _state;
        private float _mergeReadyAt;
        private float _punchStartedAt = float.NegativeInfinity;

        public int Nominal => _tier.Nominal;
        public int HitPoints { get; private set; }
        public bool IsActive => _state == State.Active;
        internal bool CanMerge => IsActive && Time.time >= _mergeReadyAt;
        internal bool IsReserved => _state == State.Reserved;
        internal BattleSession Session { get; private set; }
        internal Vector2 Position => _body.position;
        internal Vector2 Velocity => _body.linearVelocity;
        public event Action<Ball, int> Removed;

        internal void Initialize(BallConfig config, BallConfig.Tier tier, Collider2D floor,
            PhysicsMaterial2D material, Vector2 velocity, BattleSession session, MergeSystem merge,
            MergeFeedback feedback, int? mergedHp)
        {
            _config = config;
            _tier = tier;
            _floor = floor;
            Session = session;
            _merge = merge;
            _feedback = feedback;
            HitPoints = mergedHp ?? tier.MaxHp;
            _state = State.Active;
            _mergeReadyAt = Time.time + (mergedHp.HasValue ? config.MergeCooldown : config.SpawnMergeCooldown);
            if (mergedHp.HasValue)
                _punchStartedAt = Time.unscaledTime;
            _collider.radius = tier.Radius;
            _collider.sharedMaterial = material;
            _body.gravityScale = config.GravityScale;
            _body.linearVelocity = velocity;

            _visual.localScale = new Vector3(tier.Radius, tier.Radius, 1f);
            _visual.rotation = Quaternion.identity;
            _sprite.sprite = tier.Sprite;
            _number.SetText("{0}", tier.Nominal);
            float diameter = 2f * tier.Radius;
            _hpTrack.widthMultiplier = diameter * TrackWidthToDiameter;
            _hpFill.widthMultiplier = diameter * FillWidthToDiameter;
            _hpFill.startColor = _hpFill.endColor = tier.Color;
            SetRing(_hpTrack, 1f);
            RefreshHealth();
        }

        private void LateUpdate()
        {
            if (_visual.rotation != Quaternion.identity)
                _visual.rotation = Quaternion.identity;

            float elapsed = Time.unscaledTime - _punchStartedAt;
            if (elapsed < MergePunchDuration)
            {
                float multiplier = 1f + 0.26f * Mathf.Sin(Mathf.PI * elapsed / MergePunchDuration);
                float scale = _tier.Radius * multiplier;
                _visual.localScale = new Vector3(scale, scale, 1f);
            }
            else if (_punchStartedAt != float.NegativeInfinity)
            {
                _visual.localScale = new Vector3(_tier.Radius, _tier.Radius, 1f);
                _punchStartedAt = float.NegativeInfinity;
            }
        }

        // MergeSystem checks both candidates before reserving either one.
        internal void ReserveForMerge() => _state = State.Reserved;

        public bool TryDamage(int damage, Vector2 direction) => TryDamage(damage, direction, false);

        internal bool TryDamage(int damage, Vector2 direction, bool fromProjectile)
        {
            if (!IsActive || damage <= 0)
                return false;

            HitPoints = Mathf.Max(0, HitPoints - damage);
            if (HitPoints == 0)
                Remove(_tier.Reward, fromProjectile);
            else
            {
                RefreshHealth();
                _body.AddForce(direction * _config.ProjectileHitImpulse, ForceMode2D.Impulse);
            }

            return true;
        }

        public void BounceFromCannon(Vector2 cannonPosition)
        {
            var velocity = _body.linearVelocity;
            float offset = _body.position.x - cannonPosition.x;
            float side = Mathf.Abs(offset) > 0.01f ? Mathf.Sign(offset) : Mathf.Sign(velocity.x);
            velocity.x = side * Mathf.Max(Mathf.Abs(velocity.x), _config.SpawnHorizontalSpeed.x);
            velocity.y = Mathf.Max(velocity.y, _config.MinFloorBounceSpeed);
            _body.linearVelocity = velocity;
        }

        public void Despawn()
        {
            if (_state != State.Removed)
                Remove(0);
        }

        private void Remove(int reward, bool fromProjectile = false)
        {
            Vector2 destroyCenter = fromProjectile ? Position : default;
            _state = State.Removed;
            if (_collider != null)
                _collider.enabled = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
            Removed?.Invoke(this, reward);
            if (fromProjectile && Session?.IsActive == true && _feedback != null)
                _feedback.PlayBallDestroy(destroyCenter, _tier);
        }

        private void OnCollisionEnter2D(Collision2D collision) => HandleCollision(collision);
        private void OnCollisionStay2D(Collision2D collision) => HandleCollision(collision);

        private void HandleCollision(Collision2D collision)
        {
            if (!IsActive)
                return;

            if (collision.collider == _floor)
            {
                if (_body.linearVelocityY < _config.MinFloorBounceSpeed)
                    _body.linearVelocityY = _config.MinFloorBounceSpeed;
                return;
            }

            if (CanMerge && collision.collider.TryGetComponent<Ball>(out var other))
                _merge.TryQueue(this, other);
        }

        private void RefreshHealth() => SetRing(_hpFill, (float)HitPoints / _tier.MaxHp);

        private static void SetRing(LineRenderer ring, float fraction)
        {
            int segments = Mathf.CeilToInt(RingSegments * fraction);
            ring.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float radians = Mathf.Min((float)i / RingSegments, fraction) * (2f * Mathf.PI);
                ring.SetPosition(i, new Vector3(Mathf.Sin(radians), Mathf.Cos(radians)) * RingRadius);
            }
        }
    }
}
