using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Presentation
{
    public enum CombatPresentationOrientation
    {
        World,
        FaceDirection,
        Attached,
    }

    public readonly struct CombatPresentationContext
    {
        public CombatPresentationContext(
            Vector3 position,
            Vector3 direction = default,
            float scaleMultiplier = 1.0f,
            Transform parent = null,
            Vector3 localPosition = default,
            CombatPresentationOrientation orientation = CombatPresentationOrientation.World,
            float intensityMultiplier = 1.0f)
        {
            Position = position;
            Direction = direction;
            ScaleMultiplier = scaleMultiplier;
            Parent = parent;
            LocalPosition = localPosition;
            Orientation = parent != null ? CombatPresentationOrientation.Attached : orientation;
            IntensityMultiplier = Mathf.Max(0.0f, intensityMultiplier);
        }

        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float ScaleMultiplier { get; }
        public Transform Parent { get; }
        public Vector3 LocalPosition { get; }
        public CombatPresentationOrientation Orientation { get; }
        public float IntensityMultiplier { get; }
        public bool IsAttached => Orientation == CombatPresentationOrientation.Attached;
    }

    public sealed class CombatPresentationModule
    {
        public static bool Present(string presentationId, in CombatPresentationContext context)
        {
            if (string.IsNullOrWhiteSpace(presentationId))
                return false;

            bool hasSfxCue = false;
            bool hasVfx = false;
            try { hasSfxCue = TryPlaySfx(presentationId, context.Position); }
            catch (System.Exception exception) { Debug.LogException(exception); }
            try { hasVfx = RetroVfx.Present(presentationId, context); }
            catch (System.Exception exception) { Debug.LogException(exception); }

            if (RunDiagnostics.IsHitFeedbackPresentation(presentationId))
            {
                RunDiagnostics.RecordHitFeedback(
                    presentationId,
                    hasFx: hasVfx,
                    hasSfx: hasSfxCue,
                    hasHitStop: false,
                    hasRewardCue: RunDiagnostics.HasRewardCuePresentation(presentationId));
            }

            return hasSfxCue || hasVfx;
        }

        public static bool TryPlaySfx(string presentationId, Vector3 position)
        {
            if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false
                || catalog.Feedback == null
                || catalog.Feedback.TryResolve(presentationId, out FeedbackPresentationCatalog.Definition cue) == false)
                return false;

            RetroSfx.Play(cue.Sfx, cue.Sfx.name, position, catalog.Feedback.MasterVolume * cue.SfxVolumeScale);
            return true;
        }
    }
}
