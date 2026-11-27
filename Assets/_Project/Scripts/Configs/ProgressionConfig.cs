using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeBlast
{
    [Serializable]
    public struct CannonLevelData
    {
        public int Level;
        public int UpgradeCost;
        public int Damage;
        public float FireRate;
        public int MaxAvailableBounces;
        public Sprite Visual;

        public CannonLevelData(int level, int upgradeCost, int damage, float fireRate, int maxAvailableBounces)
        {
            Level = level;
            UpgradeCost = upgradeCost;
            Damage = damage;
            FireRate = fireRate;
            MaxAvailableBounces = maxAvailableBounces;
            Visual = null;
        }
    }

    [Serializable]
    public struct RicochetData
    {
        public int Level;
        public int RequiredCannonLevel;
        public int Price;

        public RicochetData(int level, int requiredCannonLevel, int price)
        {
            Level = level;
            RequiredCannonLevel = requiredCannonLevel;
            Price = price;
        }
    }

    [CreateAssetMenu(menuName = "Merge Blast/Progression")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        [SerializeField] private CannonLevelData[] _cannonLevels =
        {
            new(1, 0, 1, 4f, 0),
            new(2, 50, 2, 4f, 0),
            new(3, 75, 2, 4.5f, 1),
            new(4, 100, 3, 4.5f, 1),
            new(5, 125, 3, 5f, 1),
            new(6, 150, 4, 5f, 2),
            new(7, 175, 4, 5.5f, 2),
            new(8, 200, 5, 5.5f, 2),
            new(9, 250, 5, 6f, 3),
            new(10, 300, 6, 6f, 3)
        };
        [SerializeField] private RicochetData[] _ricochetLevels =
        {
            new(1, 3, 50),
            new(2, 6, 100),
            new(3, 9, 150)
        };

        public IReadOnlyList<CannonLevelData> CannonLevels => _cannonLevels;
        public IReadOnlyList<RicochetData> RicochetLevels => _ricochetLevels;
    }
}
