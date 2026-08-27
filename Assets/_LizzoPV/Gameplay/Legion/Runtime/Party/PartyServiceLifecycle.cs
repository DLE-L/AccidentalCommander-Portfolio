using Lizzo.PV.P0.Skills.Guard;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public void Dispose()
        {
            if (_passiveRoster != null)
                _passiveRoster.Changed -= RefreshAllCompanionCombat;
            this.ResetRunState();
            _rosterView = _legacyRosterView;
        }

        public void ResetRunState()
        {
            for (int i = Allies.Count - 1; i >= 0; i--)
            {
                if (Allies[i] != null)
                    _factory.Release(Allies[i].gameObject);
            }

            Allies.Clear();
            ShieldSoldiers.Clear();
            Companions.Clear();
            ShieldSoldierCountState = 0;
            ShieldCaptainCountState = 0;
            SwordsmanCountState = 0;
            ClericCountState = 0;
            ArcherCountState = 0;
            GuardSquadActivatedState = false;
            WasSlotFullState = false;
            _roster.Reset();
            _synergies?.Reset();
            _incomingDamage.Reset();
            _runTraitEffects?.ResetRunState();
            ResetCardModifiers();
            Formation.ResetRunState();
            GuardSquadSkillBehaviour.StopActive();
        }

    }
}
