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

        public int Nominal => _tier.Nominal;
        public int HitPoints { get; private set; }
        public bool IsActive { get; private set; }
        public event Action<Ball, int> Removed;

        public void Initialize(BallConfig config, BallConfig.Tier tier, Collider2D floor,
            PhysicsMaterial2D material, Vector2 velocity)
        {
            _config = config;
            _tier = tier;
            _floor = floor;
            HitPoints = tier.MaxHp;
            IsActive = true;
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
        }

        public bool TryDamage(int damage, Vector2 direction)
        {
            if (!IsActive || damage <= 0)
                return false;

            HitPoints = Mathf.Max(0, HitPoints - damage);
            if (HitPoints == 0)
                Remove(_tier.Reward);
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
            if (IsActive)
                Remove(0);
        }

        private void Remove(int reward)
        {
            IsActive = false;
            if (_collider != null)
                _collider.enabled = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
            Removed?.Invoke(this, reward);
        }

        private void OnCollisionEnter2D(Collision2D collision) => BounceFromFloor(collision);
        private void OnCollisionStay2D(Collision2D collision) => BounceFromFloor(collision);

        private void BounceFromFloor(Collision2D collision)
        {
            if (IsActive && collision.collider == _floor && _body.linearVelocityY < _config.MinFloorBounceSpeed)
                _body.linearVelocityY = _config.MinFloorBounceSpeed;
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
