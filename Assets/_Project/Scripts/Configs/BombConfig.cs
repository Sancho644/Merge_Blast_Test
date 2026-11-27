using System.Collections.Generic;
using UnityEngine;

namespace MergeBlast
{
    [CreateAssetMenu(menuName = "Merge Blast/Bomb")]
    public sealed class BombConfig : ScriptableObject
    {
        [SerializeField] private int[] _nominals = { 4, 8, 16 };

        public IReadOnlyList<int> Nominals => _nominals;
        [field: SerializeField, Range(0f, 1f)] public float SpawnChance { get; private set; } = 0.05f;
        [field: SerializeField] public float FirstSpawnTime { get; private set; } = 10f;
        [field: SerializeField] public float SpawnCooldown { get; private set; } = 8f;
        [field: SerializeField] public float Duration { get; private set; } = 5f;
    }
}
