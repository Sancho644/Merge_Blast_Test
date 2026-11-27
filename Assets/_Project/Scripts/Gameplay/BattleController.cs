using System;
using UnityEngine;

namespace MergeBlast
{
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private BattleConfig _config;
        [SerializeField] private ProgressionConfig _progression;
        [SerializeField] private GameplayInput _input;
        [SerializeField] private CannonController _cannon;
        [SerializeField] private CannonShooter _shooter;
        [SerializeField] private BallSpawner _spawner;

        private float _startedAt;
        private float _baseFixedDeltaTime;

        public BattleSession Session { get; private set; }
        public bool IsInvulnerable => Session?.IsInvulnerableAt(Time.time) == true;
        public event Action Changed;

        private void Awake() => _baseFixedDeltaTime = Time.fixedDeltaTime;

        private void OnEnable() => _spawner.BallDestroyed += OnBallDestroyed;

        private void Start() => StartBattle();

        private void Update()
        {
            if (Session?.IsActive != true)
                return;

            _spawner.Tick(Time.time - _startedAt, Time.deltaTime);
        }

        public void StartBattle()
        {
            StopRuntime();
            Session = new BattleSession(_config);
            _startedAt = Time.time;
            _cannon.BeginBattle();
            _input.SetAcceptInput(true);
            _shooter.BeginBattle(_progression.CannonLevels[0]);
            Changed?.Invoke();
        }

        public void HandleCannonContact(Ball ball, Vector2 cannonPosition)
        {
            if (!ball.IsActive || Session?.TryLoseHeart(Time.time) != true)
                return;

            if (Session.IsActive)
                ball.BounceFromCannon(cannonPosition);
            else
                StopRuntime();

            Changed?.Invoke();
        }

        private void OnBallDestroyed(int reward)
        {
            if (Session?.AddReward(reward) == true)
                Changed?.Invoke();
        }

        private void OnDisable()
        {
            if (_spawner != null)
                _spawner.BallDestroyed -= OnBallDestroyed;
            StopRuntime();
        }

        private void StopRuntime()
        {
            Session?.End();
            if (_input != null)
                _input.SetAcceptInput(false);
            if (_cannon != null)
                _cannon.EndBattle();
            if (_shooter != null)
                _shooter.EndBattle();
            if (_spawner != null)
                _spawner.Clear();
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDeltaTime;
        }
    }
}
