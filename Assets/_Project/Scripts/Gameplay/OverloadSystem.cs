using System.Collections.Generic;
using UnityEngine;

namespace MergeBlast
{
    public sealed class OverloadSystem : MonoBehaviour
    {
        [SerializeField] private BallConfig _config;
        [SerializeField] private BallSpawner _spawner;
        [SerializeField] private BattleController _battle;
        [SerializeField] private MergeFeedback _feedback;

        private readonly List<Ball> _snapshot = new();

        internal void Detonate(BattleSession session, Vector2 center, BallConfig.Tier overload)
        {
            if (session != _spawner.Session || !session.IsActive)
                return;

            _spawner.CopyActiveTo(_snapshot);
            float radius = _config.OverloadRadius;
            float radiusSquared = radius * radius;
            _battle.AddReward(session, overload.Reward);
            foreach (var ball in _snapshot)
            {
                if (ball && ball.IsActive && ball.Session == session && (ball.Position - center).sqrMagnitude <= radiusSquared)
                    ball.TryDamage(_config.OverloadDamage, Vector2.zero);
            }

            _snapshot.Clear();
            if (session.IsActive)
                _feedback.PlayOverload(center, radius, overload);
        }
    }
}
