using System.Collections.Generic;
using UnityEngine;

namespace MergeBlast
{
    public sealed class MergeSystem : MonoBehaviour
    {
        [SerializeField] private BallConfig _config;
        [SerializeField] private BallSpawner _spawner;
        [SerializeField] private OverloadSystem _overload;
        [SerializeField] private MergeFeedback _feedback;

        private readonly Queue<Request> _pending = new();

        private readonly struct Request
        {
            public readonly Ball First;
            public readonly Ball Second;
            public readonly BallConfig.Tier Tier;
            public readonly Vector2 Position;
            public readonly Vector2 Velocity;
            public readonly int HitPoints;

            public Request(Ball first, Ball second, BallConfig.Tier tier, float upwardImpulse)
            {
                First = first;
                Second = second;
                Tier = tier;
                Position = (first.Position + second.Position) * 0.5f;
                Velocity = (first.Velocity + second.Velocity) * 0.5f + Vector2.up * upwardImpulse;
                HitPoints = first.HitPoints + second.HitPoints;
            }
        }

        internal void TryQueue(Ball first, Ball second)
        {
            BattleSession session = _spawner.Session;
            if (!isActiveAndEnabled || session?.IsActive != true || first == second ||
                first.Session != session || second.Session != session || !first.CanMerge || !second.CanMerge ||
                first.Nominal != second.Nominal || !_config.TryGetTier(first.Nominal * 2, out var tier))
                return;

            // Both transitions occur on the main thread before any other collision callback can claim either ball.
            first.ReserveForMerge();
            second.ReserveForMerge();
            _pending.Enqueue(new Request(first, second, tier, _config.MergeUpwardImpulse));
        }

        internal void ResolvePending(BattleSession session)
        {
            // Update runs after the physics callbacks, so all collider and object changes happen here.
            while (session.IsActive && session == _spawner.Session && _pending.TryDequeue(out var request))
            {
                var first = request.First;
                var second = request.Second;
                if (!first || !second || !first.IsReserved || !second.IsReserved ||
                    first.Session != session || second.Session != session)
                    continue;

                first.Despawn();
                second.Despawn();
                if (request.Tier.Nominal == 32)
                    _overload.Detonate(session, request.Position, request.Tier);
                else
                {
                    _spawner.SpawnMerged(request.Tier, request.Position, request.Velocity, request.HitPoints);
                    _feedback.PlayMerge(request.Position, request.Tier);
                }
            }
        }

        internal void Clear() => _pending.Clear();
    }
}
