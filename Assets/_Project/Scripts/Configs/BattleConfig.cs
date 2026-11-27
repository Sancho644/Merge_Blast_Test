using UnityEngine;

namespace MergeBlast
{
    [CreateAssetMenu(menuName = "Merge Blast/Battle")]
    public sealed class BattleConfig : ScriptableObject
    {
        [field: SerializeField] public float Duration { get; private set; } = 75f;
        [field: SerializeField] public int Hearts { get; private set; } = 3;
        [field: SerializeField] public int VictoryBonus { get; private set; } = 50;
        [field: SerializeField] public float InvulnerabilityDuration { get; private set; } = 1.5f;
    }
}
