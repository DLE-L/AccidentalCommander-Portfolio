using System;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    internal sealed class EnemyChargePresentation : IDisposable
    {
        private readonly EnemyChargeController _source;
        private readonly Transform _owner;
        private readonly SpriteRenderer _path;
        private readonly SpriteRenderer _sprite;
        private readonly EnemyChargeProfile _profile;
        private readonly ChargePathWarning _warning = new ChargePathWarning();
        private readonly Color _baseColor;
        private readonly float _length;
        private readonly float _width;

        internal EnemyChargePresentation(EnemyChargeController source, SpriteRenderer path, SpriteRenderer sprite,
            EnemyChargeProfile profile, Color baseColor, float length, float width)
        {
            _source = source; _owner = source.transform; _path = path; _sprite = sprite;
            _profile = profile; _baseColor = baseColor; _length = length; _width = width;
            _warning.Bind(path);
            _source.FrameChanged += OnFrame;
        }

        private void OnFrame(EnemyActionFrame frame)
        {
            try
            {
                if ((frame.Signals & EnemyActionSignals.Started) != 0 && _profile.ChargeFeedback)
                    RetroVfx.Spawn(RetroVfxKind.RedChargerCharge, _owner.position, frame.Direction);
            }
            catch (Exception exception) { Debug.LogException(exception); }
            try
            {
                if (_path != null && frame.Phase == EnemyActionPhase.Warning)
                    _warning.Show(_owner, _owner.position, frame.Direction,
                        _length * (_profile.GrowWarning ? frame.WarningProgress : 1f), _width, _profile.PathColor, _path.name);
                else _warning.Hide();
                if (_sprite != null)
                    _sprite.color = frame.Phase == EnemyActionPhase.Warning ? _profile.WarningColor
                        : frame.Phase == EnemyActionPhase.Executing || frame.Phase == EnemyActionPhase.ImpactGrace ? _profile.ChargeColor : _baseColor;
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public void Dispose()
        {
            _source.FrameChanged -= OnFrame;
            _warning.Hide();
        }
    }
}
