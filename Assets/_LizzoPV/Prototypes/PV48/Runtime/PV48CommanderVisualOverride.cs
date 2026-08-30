using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Prototypes.PV48.Runtime
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Lizzo.PV.P0.Visuals.UnitVisualDriver))]
    public sealed class PV48CommanderVisualOverride : MonoBehaviour
    {
        private const int FrameCount = 8;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteLibraryAsset _spriteLibrary;
        [SerializeField] private Lizzo.PV.P0.Visuals.UnitVisualDriver _visualDriver;

        public SpriteLibraryAsset SpriteLibrary => _spriteLibrary;
        public float CycleSeconds => Lizzo.PV.P0.Visuals.UnitVisualDriver.IdleCycleSeconds;

        private void LateUpdate()
        {
            if (!_spriteRenderer || !_spriteLibrary)
                return;

            _visualDriver ??= GetComponent<Lizzo.PV.P0.Visuals.UnitVisualDriver>();
            float phase = _visualDriver ? _visualDriver.IdlePhaseOffset : 0.0f;
            float progress = Mathf.Repeat((Time.time / CycleSeconds) + phase, 1.0f);
            int frame = Mathf.Clamp(Mathf.FloorToInt(progress * FrameCount), 0, FrameCount - 1);
            Sprite sprite = _spriteLibrary.GetSprite("Idle", frame.ToString());
            if (sprite)
                _spriteRenderer.sprite = sprite;
        }
    }
}
