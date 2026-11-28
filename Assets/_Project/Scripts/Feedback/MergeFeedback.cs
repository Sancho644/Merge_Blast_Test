using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MergeBlast
{
    public sealed class MergeFeedback : MonoBehaviour
    {
        private const int RingSegments = 64;
        private const float MergeDuration = 0.27f;
        private const float OverloadDuration = 0.85f;
        private const float OverloadLabelWidth = 3.8f;
        private const float DestroyDuration = 0.35f;
        private const int DestroyParticleCount = 10;

        private enum Effect { Merge, Overload, BallDestroy }

        [SerializeField] private FeedbackConfig _config;
        [SerializeField] private PortraitLayout _layout;
        [SerializeField] private Camera _camera;
        [SerializeField] private Material _ringMaterial;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Material _textMaterial;
        [SerializeField] private Sprite _destroySpark;

        private readonly List<Pulse> _pulses = new();
        private bool _slowing;
        private bool _shaking;
        private float _slowUntil;
        private float _shakeStartedAt;
        private float _savedScale;
        private float _savedFixedStep;

        private sealed class Pulse
        {
            public LineRenderer Ring;
            public TextMeshPro Label;
            public Color Color;
            public float Radius;
            public float StartedAt;
            public Effect Kind;
            public SpriteRenderer Flash;
            public SpriteRenderer[] Particles;
            public Vector3 LabelOrigin;
        }

        internal void PlayMerge(Vector2 center, BallConfig.Tier tier)
        {
            if (isActiveAndEnabled)
                CreatePulse(center, tier.Radius * 2.25f, tier.Color, Effect.Merge);
        }

        internal void PlayBallDestroy(Vector2 center, BallConfig.Tier tier)
        {
            if (!isActiveAndEnabled || !_destroySpark)
                return;

            Pulse pulse = CreatePulse(center, tier.Radius, tier.Color, Effect.BallDestroy);
            Transform root = pulse.Ring.transform.parent;
            root.name = $"Ball Destroy {tier.Nominal}";
            pulse.Flash = CreateSpark(root, "Flash", 18);
            pulse.Particles = new SpriteRenderer[DestroyParticleCount];
            for (int i = 0; i < pulse.Particles.Length; i++)
            {
                var particle = CreateSpark(root, "Spark", 19);
                particle.transform.localRotation = Quaternion.Euler(0f, 0f, 360f * i / DestroyParticleCount);
                pulse.Particles[i] = particle;
            }

            Rect arena = _layout.ArenaBounds;
            float x = Mathf.Clamp(center.x, arena.xMin + 0.6f, arena.xMax - 0.6f);
            float y = Mathf.Clamp(center.y + tier.Radius * 0.35f, arena.yMin + 0.35f, arena.yMax - 0.95f);
            pulse.LabelOrigin = new Vector3(x, y);
            pulse.Label = CreateLabel(root, "Ball reward", pulse.LabelOrigin, $"+{tier.Reward}", tier.Color,
                3.1f + tier.Radius * 2f, new Vector2(1.4f, 0.7f));
            AnimatePulse(pulse, 0f);
        }

        private SpriteRenderer CreateSpark(Transform root, string name, int order)
        {
            var sprite = new GameObject(name).AddComponent<SpriteRenderer>();
            sprite.transform.SetParent(root, false);
            sprite.sprite = _destroySpark;
            sprite.sortingOrder = order;
            return sprite;
        }

        internal void PlayOverload(Vector2 center, float radius, BallConfig.Tier tier)
        {
            if (!isActiveAndEnabled)
                return;

            Pulse pulse = CreatePulse(center, radius, tier.Color, Effect.Overload);
            Rect arena = _layout.ArenaBounds;
            float halfLabel = OverloadLabelWidth * 0.5f;
            float x = Mathf.Clamp(center.x, arena.xMin + halfLabel, arena.xMax - halfLabel);
            float y = Mathf.Clamp(center.y + 1.05f, arena.yMin + 0.8f, arena.yMax - 0.7f);
            pulse.Label = CreateLabel(pulse.Ring.transform.parent, "Overload reward", new Vector3(x, y),
                $"OVERLOAD\n<size=70%>+{tier.Reward}</size>", tier.Color, 5.8f, new Vector2(OverloadLabelWidth, 1.3f));

            float now = Time.unscaledTime;
            if (!_slowing)
            {
                _savedScale = Time.timeScale;
                _savedFixedStep = Time.fixedDeltaTime;
                _slowing = true;
                Time.timeScale = _config.OverloadTimeScale;
                float originalScale = _savedScale > 0f ? _savedScale : 1f;
                Time.fixedDeltaTime = _savedFixedStep * (_config.OverloadTimeScale / originalScale);
            }

            // A second blast refreshes the deadline while preserving the original clock.
            _slowUntil = now + _config.OverloadSlowdownDuration;
            _shakeStartedAt = now;
            _shaking = true;
        }

        private TextMeshPro CreateLabel(Transform root, string name, Vector3 position, string text,
            Color color, float fontSize, Vector2 size)
        {
            var label = new GameObject(name).AddComponent<TextMeshPro>();
            label.transform.SetParent(root, false);
            label.transform.position = position;
            label.font = _font;
            label.fontSharedMaterial = _textMaterial;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = size;
            label.sortingOrder = 21;
            label.color = color;
            label.text = text;
            return label;
        }

        private Pulse CreatePulse(Vector2 center, float radius, Color color, Effect kind)
        {
            var root = new GameObject(kind == Effect.Overload ? "Overload feedback" : "Merge feedback");
            root.transform.SetParent(transform, false);
            root.transform.position = center;
            var ring = new GameObject("Shockwave").AddComponent<LineRenderer>();
            ring.transform.SetParent(root.transform, false);
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.sharedMaterial = _ringMaterial;
            ring.sortingOrder = 20;
            ring.positionCount = RingSegments;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = 2f * Mathf.PI * i / RingSegments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)));
            }

            var pulse = new Pulse
            {
                Ring = ring,
                Color = color,
                Radius = radius,
                StartedAt = Time.unscaledTime,
                Kind = kind
            };
            if (kind != Effect.BallDestroy)
                AnimatePulse(pulse, 0f);
            _pulses.Add(pulse);
            return pulse;
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            if (_slowing && now >= _slowUntil)
                RestoreClock();

            if (_shaking)
            {
                float elapsed = now - _shakeStartedAt;
                if (elapsed >= _config.OverloadShakeDuration)
                    RestoreCamera();
                else
                {
                    float envelope = 1f - elapsed / _config.OverloadShakeDuration;
                    Vector3 offset = new Vector3(Mathf.Sin(elapsed * 110f), Mathf.Sin(elapsed * 137f), 0f);
                    _camera.ResetWorldToCameraMatrix();
                    _camera.worldToCameraMatrix *= Matrix4x4.Translate(-offset * (_config.OverloadShakeAmplitude * envelope));
                }
            }

            for (int i = _pulses.Count - 1; i >= 0; i--)
            {
                Pulse pulse = _pulses[i];
                float age = now - pulse.StartedAt;
                float duration = pulse.Kind switch
                {
                    Effect.Overload => OverloadDuration,
                    Effect.BallDestroy => DestroyDuration,
                    _ => MergeDuration
                };
                if (age < duration)
                    AnimatePulse(pulse, age / duration);
                else
                {
                    RemovePulse(pulse);
                    _pulses.RemoveAt(i);
                }
            }
        }

        private static void AnimatePulse(Pulse pulse, float progress)
        {
            if (pulse.Kind == Effect.BallDestroy)
            {
                AnimateBallDestroy(pulse, progress);
                return;
            }

            bool overload = pulse.Kind == Effect.Overload;
            float expansion = Mathf.Clamp01(progress * (overload ? 2.7f : 1.8f));
            float radius = pulse.Radius * Mathf.Lerp(overload ? 0.08f : 0.7f, 1f, expansion);
            pulse.Ring.transform.localScale = new Vector3(radius, radius, 1f);
            pulse.Ring.widthMultiplier = Mathf.Lerp(overload ? 0.16f : 0.12f, 0.025f, progress);
            Color color = Color.Lerp(Color.white, pulse.Color, Mathf.Clamp01(0.35f + progress * 4f));
            color.a = 1f - Mathf.SmoothStep(0f, 1f, progress);
            pulse.Ring.startColor = pulse.Ring.endColor = color;
            if (pulse.Label != null)
                pulse.Label.alpha = 1f - Mathf.InverseLerp(0.6f, 1f, progress);
        }

        private static void AnimateBallDestroy(Pulse pulse, float progress)
        {
            float outward = 1f - (1f - progress) * (1f - progress);
            float ringRadius = pulse.Radius * Mathf.Lerp(0.72f, 2.25f, outward);
            pulse.Ring.transform.localScale = new Vector3(ringRadius, ringRadius, 1f);
            pulse.Ring.widthMultiplier = pulse.Radius * Mathf.Lerp(0.21f, 0.05f, progress);
            Color color = Color.Lerp(Color.white, pulse.Color, 0.75f);
            color.a = 1f - progress;
            pulse.Ring.startColor = pulse.Ring.endColor = color;

            Color flash = Color.Lerp(Color.white, pulse.Color, 0.2f);
            flash.a = 1f - Mathf.Clamp01(progress * 3.8f);
            pulse.Flash.color = flash;
            float radius = pulse.Radius;
            float spriteScale = radius / pulse.Flash.sprite.bounds.size.x;
            pulse.Flash.transform.localScale = Vector3.one * (spriteScale * Mathf.Lerp(2.6f, 1.6f, progress));
            for (int i = 0; i < pulse.Particles.Length; i++)
            {
                var particle = pulse.Particles[i];
                float travel = i % 2 == 0 ? 2.4f : 3.2f;
                var direction = particle.transform.localRotation * Vector3.right;
                particle.transform.localPosition = direction * (radius * Mathf.Lerp(0.45f, travel, outward));
                particle.transform.localScale = Vector3.one * (spriteScale * Mathf.Lerp(i % 2 == 0 ? 0.6f : 0.45f, 0.1f, progress));
                particle.color = color;
            }

            pulse.Label.transform.position = pulse.LabelOrigin + Vector3.up * (outward * (0.45f + radius * 0.2f));
            pulse.Label.alpha = 1f - Mathf.InverseLerp(0.48f, 1f, progress);
        }

        private void RestoreClock()
        {
            if (!_slowing)
                return;

            Time.timeScale = _savedScale;
            Time.fixedDeltaTime = _savedFixedStep;
            _slowing = false;
        }

        private void RestoreCamera()
        {
            if (!_shaking)
                return;

            if (_camera != null)
                _camera.ResetWorldToCameraMatrix();
            _shaking = false;
        }

        internal void Clear()
        {
            RestoreClock();
            RestoreCamera();
            foreach (Pulse pulse in _pulses)
                RemovePulse(pulse);
            _pulses.Clear();
        }

        private static void RemovePulse(Pulse pulse)
        {
            // During scene unload Unity may already have destroyed the child renderers.
            if (!pulse.Ring)
                return;

            var root = pulse.Ring.transform.parent.gameObject;
            root.SetActive(false);
            Destroy(root);
        }

        private void OnDisable() => Clear();
    }
}
