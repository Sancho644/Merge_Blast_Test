using UnityEngine;

namespace MergeBlast
{
    [CreateAssetMenu(menuName = "Merge Blast/Cannon")]
    public sealed class CannonConfig : ScriptableObject
    {
        [field: SerializeField] public float MoveSpeed { get; private set; } = 14f;
        [field: SerializeField] public float BulletSpeed { get; private set; } = 18f;
        [field: SerializeField] public float BulletRadius { get; private set; } = 0.08f;
        [field: SerializeField] public float BulletLifetime { get; private set; } = 6f;
    }
}
