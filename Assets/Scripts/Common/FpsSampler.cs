namespace SoftGames.Common
{
    /// <summary>
    /// Averages frame times over a refresh window. A per-frame reciprocal jitters far too much to
    /// read, so the reading only changes once a window closes.
    /// </summary>
    public sealed class FpsSampler
    {
        private readonly float _refreshInterval;
        private float _elapsed;
        private int _frames;

        public FpsSampler(float refreshInterval)
        {
            _refreshInterval = refreshInterval > 0f ? refreshInterval : 0.5f;
        }

        public float Fps { get; private set; }

        // True on the frames that close a window, where Fps has just changed.
        public bool AddFrame(float deltaTime)
        {
            _elapsed += deltaTime;
            _frames++;

            if (_elapsed < _refreshInterval)
            {
                return false;
            }

            Fps = _frames / _elapsed;
            _elapsed = 0f;
            _frames = 0;
            return true;
        }
    }
}
