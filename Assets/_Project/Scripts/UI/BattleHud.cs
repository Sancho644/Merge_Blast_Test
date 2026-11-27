using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeBlast
{
    public sealed class BattleHud : MonoBehaviour
    {
        [SerializeField] private BattleController _battle;
        [SerializeField] private TextMeshProUGUI _lives;
        [SerializeField] private TextMeshProUGUI _coins;
        [SerializeField] private GameObject _defeat;
        [SerializeField] private Button _restart;
        [SerializeField] private Image _damageFlash;
        [SerializeField] private Color _damageColor = new Color32(255, 64, 93, 255);
        [SerializeField, Range(0f, 1f)] private float _flashOpacity = 0.20f;
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float _lifePulseDuration = 0.28f;
        [SerializeField, Min(1f)] private float _lifePulseScale = 1.22f;

        private BattleSession _session;
        private int _shownHearts;
        private Color _idleLifeColor;
        private Vector3 _idleLifeScale;
        private float _damageElapsed;
        private bool _damageFeedbackActive;

        private void Awake()
        {
            _idleLifeColor = _lives.color;
            _idleLifeScale = _lives.rectTransform.localScale;
        }

        private void OnEnable()
        {
            _battle.Changed += Refresh;
            _restart.onClick.AddListener(_battle.StartBattle);
            Refresh();
        }

        private void OnDisable()
        {
            StopDamageFeedback();
            _session = null;

            if (_battle != null)
                _battle.Changed -= Refresh;
            if (_battle != null && _restart != null)
                _restart.onClick.RemoveListener(_battle.StartBattle);
        }

        private void Update()
        {
            if (!_damageFeedbackActive)
                return;

            _damageElapsed += Time.unscaledDeltaTime;
            if (_damageElapsed >= Mathf.Max(_flashDuration, _lifePulseDuration))
                StopDamageFeedback();
            else
                DrawDamageFeedback();
        }

        private void Refresh()
        {
            var session = _battle.Session;
            if (session == null)
                return;

            if (session != _session)
            {
                _session = session;
                StopDamageFeedback();
            }
            else if (session.Hearts < _shownHearts)
            {
                _damageElapsed = 0f;
                _damageFeedbackActive = true;
                DrawDamageFeedback();
            }

            _shownHearts = session.Hearts;
            _lives.SetText("{0}", session.Hearts);
            _coins.SetText("{0}", session.EarnedCoins);
            _defeat.SetActive(!session.IsActive);
        }

        private void DrawDamageFeedback()
        {
            float flash = 1f - Mathf.Clamp01(_damageElapsed / _flashDuration);
            Color color = _damageColor;
            color.a *= _flashOpacity * flash;
            _damageFlash.color = color;
            _damageFlash.enabled = flash > 0f;

            float pulse = 1f - Mathf.Clamp01(_damageElapsed / _lifePulseDuration);
            _lives.color = Color.Lerp(_idleLifeColor, _damageColor, pulse);
            _lives.rectTransform.localScale = _idleLifeScale * Mathf.Lerp(1f, _lifePulseScale, pulse);
        }

        private void StopDamageFeedback()
        {
            _damageFeedbackActive = false;
            _damageElapsed = 0f;
            if (_damageFlash != null)
            {
                Color color = _damageColor;
                color.a = 0f;
                _damageFlash.color = color;
                _damageFlash.enabled = false;
            }
            if (_lives != null)
            {
                _lives.color = _idleLifeColor;
                _lives.rectTransform.localScale = _idleLifeScale;
            }
        }
    }
}
