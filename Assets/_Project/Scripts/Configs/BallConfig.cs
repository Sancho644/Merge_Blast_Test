using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeBlast
{
    [CreateAssetMenu(menuName = "Merge Blast/Ball")]
    public sealed class BallConfig : ScriptableObject
    {
        [Serializable]
        public struct Tier
        {
            public int Nominal;
            public int MaxHp;
            public float Radius;
            public int Reward;
            public Color32 Color;
            public Sprite Sprite;

            public Tier(int nominal, float radius, int reward, byte red, byte green, byte blue)
            {
                Nominal = nominal;
                MaxHp = nominal;
                Radius = radius;
                Reward = reward;
                Color = new Color32(red, green, blue, 255);
                Sprite = null;
            }
        }

        [SerializeField] private Tier[] _tiers =
        {
            new(2, 0.32f, 2, 0x32, 0xE6, 0xA1),
            new(4, 0.43f, 4, 0x43, 0x8E, 0xFF),
            new(8, 0.56f, 8, 0xAA, 0x60, 0xFF),
            new(16, 0.72f, 16, 0xFF, 0x96, 0x38),
            new(32, 0.92f, 64, 0xFF, 0xDA, 0x45)
        };

        public IReadOnlyList<Tier> Tiers => _tiers;
        [field: SerializeField] public float GravityScale { get; private set; } = 0.8f;
        [field: SerializeField] public float Bounciness { get; private set; } = 1f;
        [field: SerializeField] public float Friction { get; private set; }
        [field: SerializeField] public Vector2 SpawnHorizontalSpeed { get; private set; } = new(2f, 4f);
        [field: SerializeField] public float MinFloorBounceSpeed { get; private set; } = 6f;
        [field: SerializeField] public float MergeCooldown { get; private set; } = 0.25f;
        [field: SerializeField] public float MergeUpwardImpulse { get; private set; } = 1f;
        [field: SerializeField] public float OverloadRadius { get; private set; } = 3f;
        [field: SerializeField] public int OverloadDamage { get; private set; } = 16;
    }
}
