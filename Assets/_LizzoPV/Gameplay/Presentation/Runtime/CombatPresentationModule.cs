using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public readonly struct CombatPresentationContext
    {
        public CombatPresentationContext(
            Vector3 position,
            Vector3 direction = default,
            float scaleMultiplier = 1.0f,
            Transform actor = null,
            Transform parent = null,
            Vector3 localPosition = default)
        {
            Position = position;
            Direction = direction;
            ScaleMultiplier = scaleMultiplier;
            Actor = actor;
            Parent = parent;
            LocalPosition = localPosition;
        }

        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float ScaleMultiplier { get; }
        public Transform Actor { get; }
        public Transform Parent { get; }
        public Vector3 LocalPosition { get; }
        public bool IsAttached => Parent != null;
    }

    public sealed class CombatPresentationModule
    {
        public static bool Present(string presentationId, in CombatPresentationContext context)
        {
            if (string.IsNullOrWhiteSpace(presentationId)
                || PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false
                || catalog.Feedback == null)
                return false;

            FeedbackPresentationSet feedback = catalog.Feedback;
            if (feedback.TryGetEntry(presentationId, out FeedbackPresentationSet.Entry general))
            {
                bool presented = context.IsAttached
                    ? RetroVfx.SpawnAttachedResolved(
                        general.Kind,
                        context.Parent,
                        context.LocalPosition,
                        context.Direction,
                        context.ScaleMultiplier)
                    : RetroVfx.SpawnResolved(
                        general.Kind,
                        context.Position,
                        context.Direction,
                        context.ScaleMultiplier);
                return ApplyActorFeedback(general.ActorFeedback, context.Actor) || presented;
            }

            if (feedback.TryGetCompanionAttackEntry(presentationId, out FeedbackPresentationSet.CompanionAttackEntry companion))
            {
                bool presented = RetroVfx.SpawnCompanionAttackResolved(
                    companion.EffectId,
                    context.Position,
                    context.Direction,
                    context.ScaleMultiplier);
                return ApplyActorFeedback(companion.ActorFeedback, context.Actor) || presented;
            }

            return false;
        }

        private static bool ApplyActorFeedback(ActorFeedbackProfile profile, Transform actor)
        {
            if (profile == ActorFeedbackProfile.None)
                return false;

            HitFlash hitFlash = actor == null ? null : actor.GetComponent<HitFlash>();
            if (hitFlash == null)
                return false;

            if (profile == ActorFeedbackProfile.Flash || profile == ActorFeedbackProfile.FlashAndShake)
                hitFlash.Play();
            if (profile == ActorFeedbackProfile.Shake || profile == ActorFeedbackProfile.FlashAndShake)
                hitFlash.PlayShake();
            return true;
        }
    }
}
