using System;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    internal sealed class BossChargePresentation : IDisposable
    {
        private readonly EnemyBossController _source;
        private readonly ChargePathWarning _path;
        private readonly SpriteRenderer _sprite;
        private readonly Color _baseColor;

        internal BossChargePresentation(EnemyBossController source, ChargePathWarning path, SpriteRenderer sprite, Color baseColor)
        {
            _source = source; _path = path; _sprite = sprite; _baseColor = baseColor;
            source.ChargeWarningChanged += OnWarning;
            source.ChargeWarningHidden += OnHidden;
            source.FrameChanged += OnFrame;
        }

        private void OnWarning(Vector2 origin, Vector2 direction, float length)
        {
            try { _path.Show(_source.transform, origin, direction, length, 1.25f, new Color(1f, .18f, .04f, .34f), "HungryGiantChargePath"); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        private void OnHidden() => _path.Hide();
        private void OnFrame(EnemyActionFrame frame)
        {
            if (_sprite == null) return;
            if (frame.Kind == EnemyAttackKind.Charge && frame.Phase == EnemyActionPhase.Warning)
                _sprite.color = new Color(.95f, .28f, .12f, 1f);
            else if (frame.Kind == EnemyAttackKind.Charge && frame.Phase == EnemyActionPhase.Executing)
                _sprite.color = new Color(.85f, .02f, .02f, 1f);
            else if (frame.Phase == EnemyActionPhase.Idle && !_source.IsStaggered)
                _sprite.color = _baseColor;
        }
        public void Dispose()
        {
            _source.ChargeWarningChanged -= OnWarning;
            _source.ChargeWarningHidden -= OnHidden;
            _source.FrameChanged -= OnFrame;
            _path.Hide();
        }
    }
}
