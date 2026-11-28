using UnityEngine;

namespace MergeBlast
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private Transform _visual;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[1];
        private ContactFilter2D _filter;
        private float _speed;
        private float _radius;
        private float _remaining;
        private int _damage;

        public void Initialize(CannonConfig config, int damage, LayerMask hitMask)
        {
            _speed = config.BulletSpeed;
            _radius = config.BulletRadius;
            _remaining = config.BulletLifetime;
            _damage = damage;
            _filter.SetLayerMask(hitMask);
            _filter.useTriggers = false;
            _visual.localScale = new Vector3(_radius, 2f * _radius, 1f);
        }

        private void FixedUpdate()
        {
            float step = Mathf.Min(Time.fixedDeltaTime, _remaining);
            if (step <= 0f)
            {
                Despawn();
                return;
            }

            float distance = _speed * step;
            Vector2 origin = transform.position;
            if (Physics2D.CircleCast(origin, _radius, Vector2.up, _filter, _hits, distance) > 0)
            {
                RaycastHit2D hit = _hits[0];
                transform.position = hit.centroid;
                if (hit.collider.TryGetComponent<Ball>(out var ball))
                    ball.TryDamage(_damage, Vector2.up, fromProjectile: true);

                Despawn();
                return;
            }

            transform.position = origin + Vector2.up * distance;
            _remaining -= step;
            if (_remaining <= 0f)
                Despawn();
        }

        private void Despawn()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
