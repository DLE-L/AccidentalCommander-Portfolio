using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public sealed partial class UnitVisualDriver
    {
        private void InitializeStateHashes()
        {
            _idleHash = Animator.StringToHash(STATE_PATH_PREFIX + IDLE_STATE);
            _runHash = Animator.StringToHash(STATE_PATH_PREFIX + RUN_STATE);
            _attackHash = Animator.StringToHash(STATE_PATH_PREFIX + ATTACK_STATE);
            _deathHash = Animator.StringToHash(STATE_PATH_PREFIX + DEATH_STATE);
        }

        private void ResetState()
        {
            _currentStateHash = 0;
            _isMoving = false;
            _isDead = false;
            _attackUntil = 0.0f;
            _attackDuration = Mathf.Max(0.05f, _attackHoldSeconds);
            _lastSpriteCategory = null;
            _lastSpriteFrame = -1;
            if (_animator != null)
                _animator.speed = 1.0f;
        }

        private void UpdateSpriteResolver(int stateHash)
        {
            if (_spriteResolver == null || _animator == null)
                return;

            string category = ResolveSpriteCategory(stateHash);
            if (category == null)
                return;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateHash == _deathHash
                ? Mathf.Clamp01(state.normalizedTime)
                : state.normalizedTime - Mathf.Floor(state.normalizedTime);
            int frameCount = ResolveFrameCount(stateHash);
            int frame = Mathf.Clamp(Mathf.FloorToInt(normalizedTime * frameCount), 0, frameCount - 1);
            if (_lastSpriteCategory == category && _lastSpriteFrame == frame)
                return;

            _spriteResolver.SetCategoryAndLabel(category, SpriteLabels[frame]);
            _lastSpriteCategory = category;
            _lastSpriteFrame = frame;
        }

        private string ResolveSpriteCategory(int stateHash)
        {
            if (stateHash == _idleHash) return _idleCategory;
            if (stateHash == _runHash) return _runCategory;
            if (stateHash == _attackHash) return _attackCategory;
            if (stateHash == _deathHash) return _deathCategory;
            return null;
        }

        private int ResolveFrameCount(int stateHash)
        {
            if (stateHash == _idleHash) return IdleFrameCount;
            if (stateHash == _runHash) return RunFrameCount;
            if (stateHash == _attackHash) return AttackFrameCount;
            if (stateHash == _deathHash) return DeathFrameCount;
            return 1;
        }

        private void ValidateFrameCounts()
        {
            ValidateFrameCount(_idleFrameCount, IDLE_STATE);
            ValidateFrameCount(_runFrameCount, RUN_STATE);
            ValidateFrameCount(_attackFrameCount, ATTACK_STATE);
            ValidateFrameCount(_deathFrameCount, DEATH_STATE);
        }

        private void ValidateFrameCount(int frameCount, string stateName)
        {
            if (frameCount == ClampFrameCount(frameCount))
                return;

            Debug.LogError($"UnitVisualDriver frame count must be between 1 and {SpriteLabels.Length} on '{gameObject.name}': {stateName}={frameCount}", this);
        }

        private static int ClampFrameCount(int frameCount) => Mathf.Clamp(frameCount, 1, SpriteLabels.Length);

        private int ResolveStateHash()
        {
            if (_isDead)
                return _deathHash;

            if (Time.time < _attackUntil)
                return _attackHash;

            return _isMoving ? _runHash : _idleHash;
        }

        private bool HasRequiredState(int stateHash)
        {
            return _animator != null
                && _animator.runtimeAnimatorController != null
                && _animator.HasState(0, stateHash);
        }

        private string ResolveStateName(int stateHash)
        {
            if (stateHash == _idleHash) return IDLE_STATE;
            if (stateHash == _runHash) return RUN_STATE;
            if (stateHash == _attackHash) return ATTACK_STATE;
            if (stateHash == _deathHash) return DEATH_STATE;
            return "<unknown>";
        }
    }
}
