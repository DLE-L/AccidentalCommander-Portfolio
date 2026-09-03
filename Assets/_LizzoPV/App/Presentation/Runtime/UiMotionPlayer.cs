using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [DisallowMultipleComponent]
    public sealed class UiMotionPlayer : MonoBehaviour
    {
        [SerializeField] private Animation _animation;

        private AnimationState _activeState;
        private float _activeTime;
        private bool _isPlaying;

        public bool TryPlay(AnimationClip clip, out string issue)
        {
            if (_animation == null)
            {
                issue = "Authored Animation component is required.";
                return false;
            }

            if (clip == null)
            {
                issue = "AnimationClip is required.";
                return false;
            }

            if (!clip.legacy)
            {
                issue = $"UI motion clip '{clip.name}' must be Legacy for Animation playback.";
                return false;
            }

            AnimationClip existing = _animation.GetClip(clip.name);
            if (existing != clip)
            {
                if (existing != null)
                    _animation.RemoveClip(clip.name);

                _animation.AddClip(clip, clip.name);
            }

            _animation.Stop();
            AnimationState state = _animation[clip.name];
            if (state == null)
            {
                issue = $"Failed to prepare UI motion clip '{clip.name}'.";
                return false;
            }

            state.wrapMode = WrapMode.ClampForever;
            state.speed = 0f;
            state.time = 0f;
            state.weight = 1f;
            state.enabled = true;
            _activeState = state;
            _activeTime = 0f;
            _isPlaying = true;
            _animation.Sample();
            issue = string.Empty;
            return true;
        }

        private void Update()
        {
            if (!_isPlaying || _activeState == null)
                return;

            float duration = Mathf.Max(0f, _activeState.length);
            _activeTime = Mathf.Min(duration, _activeTime + Time.unscaledDeltaTime);
            _activeState.time = _activeTime;
            _animation.Sample();
            if (_activeTime >= duration)
                _isPlaying = false;
        }

        public void Stop()
        {
            _isPlaying = false;
            _activeState = null;
            _activeTime = 0f;
            if (_animation != null)
                _animation.Stop();
        }

        public void StopAndRewind()
        {
            if (_animation != null && _activeState != null)
            {
                _activeState.enabled = true;
                _activeState.weight = 1f;
                _activeState.time = 0f;
                _animation.Sample();
            }

            Stop();
        }

#if UNITY_EDITOR
        public void SetForEditor(Animation animation)
        {
            _animation = animation;
        }
#endif
    }
}
