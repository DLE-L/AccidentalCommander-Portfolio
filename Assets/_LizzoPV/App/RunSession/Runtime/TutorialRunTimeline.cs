namespace Lizzo.PV.Flow
{
    public enum TutorialRunPhase
    {
        MeleeFoundation,
        RangedExpansion,
        FinalAssembly,
        Showcase,
        BossWindow,
        Complete,
    }

    public static class TutorialRunTimeline
    {
        public const float RangedExpansionStartSeconds = 30.0f;
        public const float FinalAssemblyStartSeconds = 90.0f;
        public const float ShowcaseStartSeconds = 135.0f;
        public const float BossTargetSeconds = 150.0f;
        public const float CompletionTargetSeconds = 180.0f;

        public static TutorialRunPhase Resolve(float elapsedSeconds)
        {
            if (elapsedSeconds >= CompletionTargetSeconds)
                return TutorialRunPhase.Complete;
            if (elapsedSeconds >= BossTargetSeconds)
                return TutorialRunPhase.BossWindow;
            if (elapsedSeconds >= ShowcaseStartSeconds)
                return TutorialRunPhase.Showcase;
            if (elapsedSeconds >= FinalAssemblyStartSeconds)
                return TutorialRunPhase.FinalAssembly;
            if (elapsedSeconds >= RangedExpansionStartSeconds)
                return TutorialRunPhase.RangedExpansion;

            return TutorialRunPhase.MeleeFoundation;
        }
    }
}
