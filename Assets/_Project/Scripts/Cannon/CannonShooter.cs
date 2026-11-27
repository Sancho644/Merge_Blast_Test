using UnityEngine;

namespace MergeBlast
{
    public sealed class CannonShooter : MonoBehaviour
    {
        [SerializeField] private Transform _shootPoint;
        [SerializeField] private Transform _projectiles;
        [SerializeField] private Projectile _prefab;
        [SerializeField] private CannonConfig _config;
        [SerializeField] private LayerMask _hitMask;

        private int _damage;
        private float _shotInterval;
        private float _cooldown;
        private bool _firing;

        public void BeginBattle(CannonLevelData level)
        {
            _damage = level.Damage;
            _firing = level.FireRate > 0f;
            _shotInterval = _firing ? 1f / level.FireRate : 0f;
            _cooldown = 0f;
        }

        public void EndBattle()
        {
            _firing = false;
            if (_projectiles == null)
                return;

            foreach (Transform shot in _projectiles)
            {
                shot.gameObject.SetActive(false);
                Destroy(shot.gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (!_firing)
                return;

            _cooldown -= Time.fixedDeltaTime;
            if (_cooldown > 0f)
                return;

            _cooldown += _shotInterval;
            var shot = Instantiate(_prefab, _shootPoint.position, Quaternion.identity, _projectiles);
            shot.Initialize(_config, _damage, _hitMask);
        }
    }
}
