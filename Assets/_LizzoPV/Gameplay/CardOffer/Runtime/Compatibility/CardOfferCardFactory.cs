using Lizzo.PV.Data;
using Lizzo.PV.Legion;

namespace Lizzo.PV.P0.Cards
{
    internal sealed class CardOfferCardFactory
    {
        private readonly PartyService _party;
        private readonly CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        private readonly CanonicalPassiveCardService _canonicalPassiveCards;

        internal CardOfferCardFactory(
            PartyService party,
            CanonicalCompanionCardEligibility canonicalCompanionEligibility,
            CanonicalPassiveCardService canonicalPassiveCards)
        {
            _party = party;
            _canonicalCompanionEligibility = canonicalCompanionEligibility;
            _canonicalPassiveCards = canonicalPassiveCards;
        }

        internal CardData Create(CardKind kind, CardHighlight highlight = CardHighlight.None)
        {
            string canonicalBaseUnitId = null;
            string canonicalPassiveId = null;
            _canonicalCompanionEligibility?.TryGetBaseUnitId(kind, out canonicalBaseUnitId);
            CanonicalPassiveCardService.TryGetPassiveId(kind, out canonicalPassiveId);
            if (string.IsNullOrWhiteSpace(canonicalPassiveId) == false
                && _canonicalPassiveCards.TryGetCandidate(kind, out CanonicalPassiveCardCandidate passiveCandidate))
            {
                PassiveData passive = _party.Data.GetPassive(passiveCandidate.PassiveId);
                string title = passive == null ? string.Empty : passive.TitleKo;
                string description = PassiveCardPresentation.FormatCurrentToNext(
                    passive,
                    _canonicalPassiveCards.Roster.GetLevel(passiveCandidate.PassiveId));
                return new CardData(
                    kind,
                    title,
                    description,
                    CardHighlight.None,
                    canonicalBaseUnitId,
                    canonicalPassiveId,
                    ResolveAmount(kind, null));
            }

            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry))
            {
                string title = string.IsNullOrWhiteSpace(entry.Title) ? ResolveFallbackTitle(kind) : entry.Title;
                string description = string.IsNullOrWhiteSpace(entry.Description)
                    ? ResolveFallbackDescription(kind)
                    : entry.Description;
                return new CardData(
                    kind,
                    title,
                    description,
                    highlight,
                    canonicalBaseUnitId,
                    canonicalPassiveId,
                    ResolveAmount(kind, entry));
            }

            return new CardData(
                kind,
                ResolveFallbackTitle(kind),
                ResolveFallbackDescription(kind),
                highlight,
                canonicalBaseUnitId,
                canonicalPassiveId,
                ResolveAmount(kind, null));
        }

        private static int ResolveAmount(CardKind kind, CardDefinitionSet.Entry entry)
        {
            if (entry != null)
                return entry.IntValue;
            return kind == CardKind.SmallHeal ? 30 : 0;
        }

        private static string ResolveFallbackTitle(CardKind kind)
        {
            return kind switch
            {
                CardKind.SmallHeal => "작은 회복",
                CardKind.BasicAttackUp => "기본 공격 강화",
                CardKind.AddShieldSoldier => "방패병 합류",
                CardKind.RecruitArcher => "궁수 합류",
                CardKind.MoveSpeedUp => "이동속도 증가",
                CardKind.RecruitSwordsman => "검병 합류",
                CardKind.LegionBanner => "군단 깃발",
                CardKind.RecruitCleric => "성직자 합류",
                CardKind.GuardShockwaveCrest => "방패 충격문장",
                _ => kind.ToString(),
            };
        }

        private static string ResolveFallbackDescription(CardKind kind)
        {
            return kind switch
            {
                CardKind.SmallHeal => "군단장과 동료의 HP를 회복합니다.",
                CardKind.BasicAttackUp => "군단장의 공격력이 증가합니다.",
                CardKind.AddShieldSoldier => "방패병을 1명 합류시킵니다.",
                CardKind.RecruitArcher => "원거리 공격 병종을 합류시킵니다.",
                CardKind.MoveSpeedUp => "군단장의 이동속도가 증가합니다.",
                CardKind.RecruitSwordsman => "근접 공격 병종을 합류시킵니다.",
                CardKind.LegionBanner => "동료 공격력이 8% 증가합니다.",
                CardKind.RecruitCleric => "회복 지원 병종을 합류시킵니다.",
                CardKind.GuardShockwaveCrest => "Guard Squad 충격파 범위와 지속시간이 10% 증가합니다.",
                _ => "프로토타입 카드입니다.",
            };
        }
    }
}
