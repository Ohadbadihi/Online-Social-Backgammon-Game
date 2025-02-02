namespace FinalProjApi.Game
{
    public class PlayerTimer
    {
        public TimeSpan TimeRemaining { get; private set; }

        private DateTime _lastStartTime;

        private bool _isRunning;

        public PlayerTimer(TimeSpan initialTime)
        {
            TimeRemaining = initialTime;
            _isRunning = false;
        }

        public void Start()
        {
            if (!_isRunning)
            {
                _lastStartTime = DateTime.UtcNow;
                _isRunning = true;
            }
        }

        public void Stop()
        {
            if (_isRunning)
            {
                TimeRemaining -= DateTime.UtcNow - _lastStartTime;
                _isRunning = false;
            }
        }

        public bool HasExpired()
        {
            return TimeRemaining <= TimeSpan.Zero;
        }
    }
}
