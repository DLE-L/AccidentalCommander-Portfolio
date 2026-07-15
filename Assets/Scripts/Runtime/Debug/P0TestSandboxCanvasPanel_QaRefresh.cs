using Lizzo.PV.P0.Telemetry;

namespace Lizzo.PV.P0.Debugging
{
    public sealed partial class P0TestSandboxCanvasPanel
    {
        private void RefreshQa()
        {
            int requiredPassed = CountRequiredGatePasses();
            _qaText.text =
                $"P0 QA Gate: {requiredPassed}/5\n" +
                BuildQaLine("First recruit", HasAnyEvent(P0Telemetry.FirstRecruit, P0Telemetry.CompanionRecruit), BuildEventDetail(P0Telemetry.FirstRecruit, P0Telemetry.CompanionRecruit)) +
                BuildQaLine("Shield Captain promotion", HasAnyEvent(P0Telemetry.FirstPromotion, P0Telemetry.PromotionComplete), BuildEventDetail(P0Telemetry.FirstPromotion, P0Telemetry.PromotionComplete)) +
                BuildQaLine("Guard Squad synergy", HasAnyEvent(P0Telemetry.FirstSynergy, P0Telemetry.SynergyActivate), BuildEventDetail(P0Telemetry.FirstSynergy, P0Telemetry.SynergyActivate)) +
                BuildQaLine("Hungry Giant seen", P0Telemetry.HasLogged(P0Telemetry.FirstBossSeen), BuildEventDetail(P0Telemetry.FirstBossSeen)) +
                BuildQaLine("Result or failure", P0Telemetry.HasLogged(P0Telemetry.ResultView), BuildEventDetail(P0Telemetry.ResultView)) +
                "\nFirst 5-Minute Loop\n" +
                BuildQaLine("Run start", P0Telemetry.HasLogged(P0Telemetry.RunStart), BuildEventDetail(P0Telemetry.RunStart)) +
                BuildQaLine("First level-up cards", P0Telemetry.HasLogged(P0Telemetry.CardOptionsShow), BuildFirstLevelUpDetail()) +
                BuildQaLine("Card selected", P0Telemetry.HasLogged(P0Telemetry.CardSelect), BuildEventDetail(P0Telemetry.CardSelect)) +
                BuildQaLine("Boss kill or run end", HasAnyEvent(P0Telemetry.FirstBossKill, P0Telemetry.RunEnd), BuildEventDetail(P0Telemetry.FirstBossKill, P0Telemetry.RunEnd)) +
                "\nSystem Checks\n" +
                BuildQaLine("Pause/resume", HasAnyEvent(P0Telemetry.PauseOpen, P0Telemetry.PauseResume), BuildEventDetail(P0Telemetry.PauseOpen, P0Telemetry.PauseResume)) +
                BuildQaLine("App resume recovery", HasAnyEvent(P0Telemetry.AppBackground, P0Telemetry.AppResume, P0Telemetry.SaveRecover), BuildEventDetail(P0Telemetry.AppBackground, P0Telemetry.AppResume, P0Telemetry.SaveRecover)) +
                BuildQaLine("Companion down/recover", HasAnyEvent(P0Telemetry.CompanionDown, P0Telemetry.CompanionRecover), BuildEventDetail(P0Telemetry.CompanionDown, P0Telemetry.CompanionRecover)) +
                BuildQaLine("Surrounded formation", HasAnyEvent(P0Telemetry.SurroundedModeEnter, P0Telemetry.SurroundedModeExit), BuildEventDetail(P0Telemetry.SurroundedModeEnter, P0Telemetry.SurroundedModeExit)) +
                BuildQaLine("Charge warning", P0Telemetry.HasLogged(P0Telemetry.ChargePathWarning), BuildEventDetail(P0Telemetry.ChargePathWarning)) +
                BuildQaLine("Boss pattern hit", P0Telemetry.HasLogged(P0Telemetry.BossPatternHit), BuildEventDetail(P0Telemetry.BossPatternHit));
        }

        private static int CountRequiredGatePasses()
        {
            int count = 0;
            if (HasAnyEvent(P0Telemetry.FirstRecruit, P0Telemetry.CompanionRecruit))
                count++;
            if (HasAnyEvent(P0Telemetry.FirstPromotion, P0Telemetry.PromotionComplete))
                count++;
            if (HasAnyEvent(P0Telemetry.FirstSynergy, P0Telemetry.SynergyActivate))
                count++;
            if (P0Telemetry.HasLogged(P0Telemetry.FirstBossSeen))
                count++;
            if (P0Telemetry.HasLogged(P0Telemetry.ResultView))
                count++;
            return count;
        }

        private static string BuildQaLine(string label, bool passed, string detail)
        {
            return $"{(passed ? "[OK]" : "[--]")} {label}: {detail}\n";
        }

        private static bool HasAnyEvent(params string[] eventNames)
        {
            if (eventNames == null)
                return false;

            for (int i = 0; i < eventNames.Length; i++)
            {
                if (P0Telemetry.HasLogged(eventNames[i]))
                    return true;
            }

            return false;
        }

        private static string BuildFirstLevelUpDetail()
        {
            if (P0Telemetry.TryGetEventSnapshot(P0Telemetry.CardOptionsShow, out P0Telemetry.EventSnapshot snapshot) == false)
                return "missing";

            string timing = snapshot.FirstRunSeconds <= 20.0f ? "target ok" : "late";
            return $"first={snapshot.FirstRunSeconds:0.0}s ({timing}) count={snapshot.Count} {TrimDetail(snapshot.LastParametersText)}";
        }

        private static string BuildEventDetail(params string[] eventNames)
        {
            if (eventNames == null)
                return "missing";

            for (int i = 0; i < eventNames.Length; i++)
            {
                string eventName = eventNames[i];
                if (P0Telemetry.TryGetEventSnapshot(eventName, out P0Telemetry.EventSnapshot snapshot) == false)
                    continue;

                string detail = TrimDetail(snapshot.LastParametersText);
                return detail.Length == 0
                    ? $"{eventName} t={snapshot.FirstRunSeconds:0.0}s count={snapshot.Count}"
                    : $"{eventName} t={snapshot.FirstRunSeconds:0.0}s count={snapshot.Count} {detail}";
            }

            return "missing";
        }

        private static string TrimDetail(string detail)
        {
            if (string.IsNullOrEmpty(detail))
                return string.Empty;

            const int maxLength = 78;
            return detail.Length <= maxLength ? detail : $"{detail.Substring(0, maxLength)}...";
        }
    }
}
