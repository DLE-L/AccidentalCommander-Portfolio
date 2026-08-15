namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionPresentationModule
    {
        public CompanionRunEvent CreateSquadRecruitedEvent(
            long order,
            CompanionSquadModule squad,
            string cueId)
        {
            return Create(
                order,
                CompanionRunEventKind.SquadRecruited,
                squad,
                null,
                cueId,
                squad.FormationAnchor);
        }

        public CompanionRunEvent CreateSquadReinforcedEvent(
            long order,
            CompanionSquadModule squad,
            string cueId)
        {
            return Create(
                order,
                CompanionRunEventKind.SquadReinforced,
                squad,
                null,
                cueId,
                squad.FormationAnchor);
        }

        public CompanionRunEvent CreateSquadPromotedEvent(
            long order,
            CompanionSquadModule squad,
            string cueId)
        {
            return Create(
                order,
                CompanionRunEventKind.SquadPromoted,
                squad,
                null,
                cueId,
                squad.FormationAnchor);
        }

        public CompanionRunEvent CreateEffectResolvedEvent(
            long order,
            in EffectIntent intent,
            in EffectResolution resolution)
        {
            return new CompanionRunEvent(
                order,
                CompanionRunEventKind.EffectResolved,
                intent.SquadId,
                intent.SourceCompanionId,
                resolution,
                new PresentationCue(intent.PresentationCueId, intent.SquadId, intent.TargetPosition));
        }

        private static CompanionRunEvent Create(
            long order,
            CompanionRunEventKind kind,
            CompanionSquadModule squad,
            EffectResolution? resolution,
            string cueId,
            CompanionPoint position)
        {
            return new CompanionRunEvent(
                order,
                kind,
                squad.SquadId,
                squad.CompanionId,
                resolution,
                new PresentationCue(cueId, squad.SquadId, position));
        }
    }
}
