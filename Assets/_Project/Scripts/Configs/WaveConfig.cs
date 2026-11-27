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
        [field: SerializeField] public int MaxActive { get; private set; } = 16;
        [field: SerializeField] public int FirstPairNominal { get; private set; } = 16;
        [field: SerializeField] public Vector2 FirstPairSpawnWindow { get; private set; } = new(12f, 18f);

        public float SpawnRateAt(float elapsedTime) => _spawnRate.Evaluate(elapsedTime);

        private static AnimationCurve CreateSpawnRate()
        {
            // Each key uses the slope of its adjacent segments for linear interpolation.
            return new AnimationCurve(
                new Keyframe(0f, 0.5f, 0f, 0.2f / 15f),
                new Keyframe(15f, 0.7f, 0.2f / 15f, 0.2f / 15f),
                new Keyframe(30f, 0.9f, 0.2f / 15f, 0.2f / 15f),
                new Keyframe(45f, 1.1f, 0.2f / 15f, 0.3f / 15f),
                new Keyframe(60f, 1.4f, 0.3f / 15f, 0.2f / 15f),
                new Keyframe(75f, 1.6f, 0.2f / 15f, 0f));
        }
    }
}
