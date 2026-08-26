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
            CardKind.AddShieldSoldier,
            CardKind.RecruitSwordsman,
            CardKind.RecruitCleric,
            CardKind.RecruitArcher,
            CardKind.RecruitBombardier,
            CardKind.RecruitSkeletonBomber,
            CardKind.RecruitWolfTamer,
        };

        private readonly RunContext _context;

        internal TutorialCardOfferPolicy(RunContext context)
        {
            _context = context;
        }

        internal bool TryBuildOffer(
            Func<CardKind, int> getProgression,
            out CardKind[] offer)
        {
            offer = Array.Empty<CardKind>();
            if (_context.IsTutorial == false)
                return false;

            if (getProgression == null)
                throw new ArgumentNullException(nameof(getProgression));

            int shield = getProgression(CardKind.AddShieldSoldier);
            if (shield <= 0)
            {
                offer = new[] { CardKind.AddShieldSoldier };
                return true;
            }

            int swordsman = getProgression(CardKind.RecruitSwordsman);
            if (shield < TargetProgression || swordsman < 1)
            {
                offer = BuildDeficitOffer(
                    CardKind.AddShieldSoldier, shield, TargetProgression,
                    CardKind.RecruitSwordsman, swordsman, 1);
                return true;
            }

            int cleric = getProgression(CardKind.RecruitCleric);
            if (cleric < 1)
            {
                offer = new[] { CardKind.RecruitCleric };
                return true;
            }

            int archer = getProgression(CardKind.RecruitArcher);
            int bombardier = getProgression(CardKind.RecruitBombardier);
            if (archer < 1 || bombardier < 1)
            {
                offer = BuildDeficitOffer(
                    CardKind.RecruitArcher, archer, 1,
                    CardKind.RecruitBombardier, bombardier, 1);
                return true;
            }

            int skeleton = getProgression(CardKind.RecruitSkeletonBomber);
            if (skeleton < 1)
            {
                offer = new[] { CardKind.RecruitSkeletonBomber };
                return true;
            }

            if (archer < TargetProgression
                || bombardier < TargetProgression
                || skeleton < TargetProgression)
            {
                offer = BuildDeficitOffer(
                    CardKind.RecruitArcher, archer, TargetProgression,
                    CardKind.RecruitBombardier, bombardier, TargetProgression,
                    CardKind.RecruitSkeletonBomber, skeleton, TargetProgression);
                return true;
            }

            if (swordsman < TargetProgression || cleric < TargetProgression)
            {
                offer = BuildDeficitOffer(
                    CardKind.RecruitSwordsman, swordsman, TargetProgression,
                    CardKind.RecruitCleric, cleric, TargetProgression);
                return true;
            }

            int wolfTamer = getProgression(CardKind.RecruitWolfTamer);
            if (wolfTamer < TargetProgression)
                offer = new[] { CardKind.RecruitWolfTamer };

            return true;
        }

        internal bool IsTarget(CardKind kind)
        {
            for (int index = 0; index < TargetKinds.Length; index++)
                if (TargetKinds[index] == kind)
                    return true;

            return false;
        }

        private static CardKind[] BuildDeficitOffer(
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
            List<CardKind> kinds = new List<CardKind>(2);
            AddDeficit(kinds, firstKind, firstProgression, firstTarget);
            AddDeficit(kinds, secondKind, secondProgression, secondTarget);
            if (thirdTarget > 0)
                AddDeficit(kinds, thirdKind, thirdProgression, thirdTarget);
            return kinds.ToArray();
        }

        private static void AddDeficit(
            List<CardKind> kinds,
            CardKind kind,
            int progression,
            int target)
        {
            if (kinds.Count < 2 && progression < target)
                kinds.Add(kind);
        }
    }
}
