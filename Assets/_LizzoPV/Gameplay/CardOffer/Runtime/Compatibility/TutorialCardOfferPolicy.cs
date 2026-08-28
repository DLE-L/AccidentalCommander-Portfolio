using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;

namespace Lizzo.PV.P0.Cards
{
    internal sealed class TutorialCardOfferPolicy
    {
        internal const int TargetProgression = 3;

        private static readonly CardKind[] TargetKinds =
        {
            CardKind.RecruitSwordsman,
            CardKind.AddShieldSoldier,
            CardKind.RecruitCleric,
            CardKind.RecruitArcher,
            CardKind.RecruitBombardier,
            CardKind.RecruitSkeletonBomber,
            CardKind.RecruitWolfTamer,
        };

        private readonly bool _enabled;
        private readonly float _completionSeconds;
        private readonly float _pauseStartSeconds;

        internal TutorialCardOfferPolicy(bool enabled)
        {
            _enabled = enabled;
            _completionSeconds = 0.0f;
            _pauseStartSeconds = 0.0f;
        }

        internal TutorialCardOfferPolicy(RunDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _enabled = definition.UsesGuidedCardOffers;
            _completionSeconds = definition.BossSpawnSeconds;
            _pauseStartSeconds = definition.ExperienceRewardCutoffSeconds;
        }

        internal bool TryBuildOffer(
            Func<CardKind, int> getProgression,
            int cardNumber,
            float elapsedSeconds,
            out CardKind[] offer)
        {
            offer = Array.Empty<CardKind>();
            if (_enabled == false)
                return false;

            if (getProgression == null)
                throw new ArgumentNullException(nameof(getProgression));

            int restoredProgression = 0;
            for (int index = 0; index < TargetKinds.Length; index++)
                restoredProgression += Math.Max(0, getProgression(TargetKinds[index]));
            cardNumber = Math.Max(cardNumber, restoredProgression + 1);

            bool completionCorrection = elapsedSeconds >= _completionSeconds;
            if (elapsedSeconds >= _pauseStartSeconds && completionCorrection == false)
                return true;

            int optionLimit = completionCorrection ? 1 : 2;

            if (completionCorrection)
            {
                offer = BuildCompletionDeficitOffer(getProgression);
                return true;
            }

            offer = cardNumber switch
            {
                1 => Fixed(CardKind.RecruitSwordsman, getProgression, 1),
                2 => Fixed(CardKind.AddShieldSoldier, getProgression, 1),
                3 => PairForTarget(
                    CardKind.AddShieldSoldier,
                    CardKind.RecruitCleric,
                    getProgression,
                    2,
                    1),
                4 => PairForTarget(
                    CardKind.AddShieldSoldier,
                    CardKind.RecruitCleric,
                    getProgression,
                    2,
                    1),
                5 => Fixed(CardKind.AddShieldSoldier, getProgression, 3),
                >= 6 and <= 8 => TrioForTarget(getProgression, 1, optionLimit),
                >= 9 and <= 11 => TrioForTarget(getProgression, 2, optionLimit),
                >= 12 and <= 14 => TrioForTarget(getProgression, 3, optionLimit),
                15 or 16 => PairForTarget(
                    CardKind.RecruitSwordsman,
                    CardKind.RecruitCleric,
                    getProgression,
                    2,
                    2),
                17 or 18 => PairForTarget(
                    CardKind.RecruitSwordsman,
                    CardKind.RecruitCleric,
                    getProgression,
                    3,
                    3),
                >= 19 and <= 21 => Fixed(
                    CardKind.RecruitWolfTamer,
                    getProgression,
                    cardNumber - 18),
                _ => Array.Empty<CardKind>(),
            };

            return true;
        }

        private static CardKind[] Fixed(
            CardKind kind,
            Func<CardKind, int> getProgression,
            int target)
        {
            return getProgression(kind) < target
                ? new[] { kind }
                : Array.Empty<CardKind>();
        }

        private static CardKind[] PairForTarget(
            CardKind first,
            CardKind second,
            Func<CardKind, int> getProgression,
            int firstTarget,
            int secondTarget)
        {
            return BuildDeficitOffer(
                2,
                first, getProgression(first), firstTarget,
                second, getProgression(second), secondTarget);
        }

        private static CardKind[] TrioForTarget(
            Func<CardKind, int> getProgression,
            int target,
            int optionLimit)
        {
            return BuildDeficitOffer(
                optionLimit,
                CardKind.RecruitArcher, getProgression(CardKind.RecruitArcher), target,
                CardKind.RecruitBombardier, getProgression(CardKind.RecruitBombardier), target,
                CardKind.RecruitSkeletonBomber, getProgression(CardKind.RecruitSkeletonBomber), target);
        }

        private static CardKind[] BuildCompletionDeficitOffer(Func<CardKind, int> getProgression)
        {
            for (int index = 0; index < TargetKinds.Length; index++)
            {
                CardKind kind = TargetKinds[index];
                if (getProgression(kind) < TargetProgression)
                    return new[] { kind };
            }

            return Array.Empty<CardKind>();
        }

        internal bool IsTarget(CardKind kind)
        {
            for (int index = 0; index < TargetKinds.Length; index++)
                if (TargetKinds[index] == kind)
                    return true;

            return false;
        }

        private static CardKind[] BuildDeficitOffer(
            int optionLimit,
            CardKind firstKind,
            int firstProgression,
            int firstTarget,
            CardKind secondKind,
            int secondProgression,
            int secondTarget,
            CardKind thirdKind = default,
            int thirdProgression = 0,
            int thirdTarget = 0)
        {
            List<CardKind> kinds = new List<CardKind>(optionLimit);
            AddDeficit(kinds, optionLimit, firstKind, firstProgression, firstTarget);
            AddDeficit(kinds, optionLimit, secondKind, secondProgression, secondTarget);
            if (thirdTarget > 0)
                AddDeficit(kinds, optionLimit, thirdKind, thirdProgression, thirdTarget);
            return kinds.ToArray();
        }

        private static void AddDeficit(
            List<CardKind> kinds,
            int optionLimit,
            CardKind kind,
            int progression,
            int target)
        {
            if (kinds.Count < optionLimit && progression < target)
                kinds.Add(kind);
        }
    }
}
