namespace MergeBlast
{
    public sealed class BattleSession
    {
        private readonly float _invulnerabilityDuration;
        private float _invulnerableUntil;

        public BattleSession(BattleConfig config)
        {
            Hearts = config.Hearts;
            _invulnerabilityDuration = config.InvulnerabilityDuration;
        }

        public int Hearts { get; private set; }
        public int EarnedCoins { get; private set; }
        public bool IsActive { get; private set; } = true;

        public bool IsInvulnerableAt(float time) => IsActive && time < _invulnerableUntil;

        public bool TryLoseHeart(float time)
        {
            if (!IsActive || IsInvulnerableAt(time))
                return false;

            Hearts--;
            _invulnerableUntil = time + _invulnerabilityDuration;
            if (Hearts == 0)
                End();

            return true;
        }

        public bool AddReward(int amount)
        {
            if (!IsActive || amount <= 0)
                return false;

            EarnedCoins += amount;
            return true;
        }

        public void End() => IsActive = false;
    }
}
