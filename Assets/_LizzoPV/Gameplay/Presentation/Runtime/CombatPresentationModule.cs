using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
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
            CombatPresentationOrientation orientation = CombatPresentationOrientation.World)
        {
            Position = position;
            Direction = direction;
            ScaleMultiplier = scaleMultiplier;
            Parent = parent;
            LocalPosition = localPosition;
            Orientation = parent != null ? CombatPresentationOrientation.Attached : orientation;
        }

        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float ScaleMultiplier { get; }
        public Transform Parent { get; }
        public Vector3 LocalPosition { get; }
        public CombatPresentationOrientation Orientation { get; }
        public bool IsAttached => Orientation == CombatPresentationOrientation.Attached;
    }

    public sealed class CombatPresentationModule
    {
        public static bool Present(string presentationId, in CombatPresentationContext context)
        {
            if (string.IsNullOrWhiteSpace(presentationId))
                return false;

            bool hasSfxCue = TryPlaySfx(presentationId, context.Position);
            bool hasVfx = RetroVfx.Present(presentationId, context);

            if (P0PlaytestDiagnostics.IsHitFeedbackPresentation(presentationId))
            {
                P0PlaytestDiagnostics.RecordHitFeedback(
                    presentationId,
                    hasFx: hasVfx,
                    hasSfx: hasSfxCue,
                    hasHitStop: false,
                    hasRewardCue: P0PlaytestDiagnostics.HasRewardCuePresentation(presentationId));
            }

            return hasSfxCue || hasVfx;
        }

        public static void PreloadDefaults()
        {
            if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false || catalog.Feedback == null)
                return;

            catalog.Feedback.ForEachDefinition(definition => RetroSfx.Preload(definition.Sfx.name));
        }

        private static bool TryPlaySfx(string presentationId, Vector3 position)
        {
            if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false
                || catalog.Feedback == null
                || catalog.Feedback.TryResolve(presentationId, out FeedbackPresentationCatalog.Definition cue) == false)
                return false;

            RetroSfx.Play(cue.Sfx, cue.Sfx.name, position, cue.SfxVolumeScale);
            return true;
        }
    }
}
