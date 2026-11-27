using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeBlast
{
    [CreateAssetMenu(menuName = "Merge Blast/Wave")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Serializable]
        public struct Phase
        {
            public float StartsAt;
            public int[] Nominals;

            public Phase(float startsAt, params int[] nominals)
            {
                StartsAt = startsAt;
                Nominals = nominals;
            }
        }

        [SerializeField] private Phase[] _phases =
        {
            new(0f, 2, 4),
            new(10f, 2, 4, 8, 16),
            new(25f, 2, 4, 8),
            new(45f, 4, 8, 16),
            new(60f, 4, 8, 16)
        };
        [SerializeField] private AnimationCurve _spawnRate = CreateSpawnRate();

        public IReadOnlyList<Phase> Phases => _phases;
        [field: SerializeField] public int MaxActive { get; private set; } = 8;
        [field: SerializeField] public int FirstPairNominal { get; private set; } = 16;
        [field: SerializeField] public Vector2 FirstPairSpawnWindow { get; private set; } = new(12f, 18f);

        public float SpawnRateAt(float elapsedTime) => _spawnRate.Evaluate(elapsedTime);

        private static AnimationCurve CreateSpawnRate()
        {
            // Shared tangents keep the curve smooth, including the transition to a constant rate.
            const float earlySlope = 0.1f / 15f;
            const float lateSlope = 0.2f / 15f;
            const float transitionSlope = (earlySlope + lateSlope) * 0.5f;
            return new AnimationCurve(
                new Keyframe(0f, 0.3f, 0f, 0f),
                new Keyframe(15f, 0.4f, earlySlope, earlySlope),
                new Keyframe(30f, 0.5f, earlySlope, earlySlope),
                new Keyframe(45f, 0.6f, transitionSlope, transitionSlope),
                new Keyframe(60f, 0.8f, lateSlope, lateSlope),
                new Keyframe(75f, 1.0f, 0f, 0f))
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
        }
    }
}
