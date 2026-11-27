using UnityEngine;

namespace MergeBlast
{
    [CreateAssetMenu(menuName = "Merge Blast/Feedback")]
    public sealed class FeedbackConfig : ScriptableObject
    {
        [field: SerializeField] public float OverloadTimeScale { get; private set; } = 0.35f;
        [field: SerializeField] public float OverloadSlowdownDuration { get; private set; } = 0.3f;
        [field: SerializeField] public float OverloadShakeDuration { get; private set; } = 0.2f;
        [field: SerializeField] public float OverloadShakeAmplitude { get; private set; } = 0.12f;
        [field: SerializeField] public Vector2 ShotPitchRange { get; private set; } = new(0.95f, 1.05f);
        [field: SerializeField] public Vector4 MergePitches { get; private set; } = new(1f, 1.12f, 1.25f, 1.4f);
        [field: SerializeField] public float ButtonAnimationDuration { get; private set; } = 0.15f;
        [field: SerializeField] public float ScreenAnimationDuration { get; private set; } = 0.25f;
    }
}
