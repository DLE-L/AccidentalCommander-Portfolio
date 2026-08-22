namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CombatResolutionModule
    {
        private readonly ICompanionCombatWorld _combatWorld;

        public CombatResolutionModule(ICompanionCombatWorld combatWorld)
        {
            _combatWorld = combatWorld;
        }

        public EffectResolution ResolveEffect(in EffectIntent intent)
        {
            return _combatWorld.Resolve(intent);
        }
    }
}
