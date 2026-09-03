using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Status Feedback Profile", fileName = "StatusFeedbackProfile")]
    public sealed class StatusFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private VfxAssetId _applyVfxId, _loopVfxId, _endVfxId;
        [SerializeField] private SpriteAssetId _statusIconSpriteId;
        [SerializeField] private AudioAssetId _applySfxId, _loopSfxId, _endSfxId;
        [SerializeField] private List<StatusReactionFeedback> _reactions = new List<StatusReactionFeedback>();
        public VfxAssetId ApplyVfxId=>_applyVfxId; public VfxAssetId LoopVfxId=>_loopVfxId; public VfxAssetId EndVfxId=>_endVfxId;
        public SpriteAssetId StatusIconSpriteId=>_statusIconSpriteId; public AudioAssetId ApplySfxId=>_applySfxId; public AudioAssetId LoopSfxId=>_loopSfxId; public AudioAssetId EndSfxId=>_endSfxId;
        public IReadOnlyList<StatusReactionFeedback> Reactions=>_reactions;
        public bool TryValidate(out string issue)
        {
            if(!WorldFeedbackCoreValidation.Require(_applyVfxId,nameof(ApplyVfxId),out issue)||!WorldFeedbackCoreValidation.Require(_loopVfxId,nameof(LoopVfxId),out issue)
              ||!WorldFeedbackCoreValidation.Require(_endVfxId,nameof(EndVfxId),out issue)||!WorldFeedbackCoreValidation.Require(_statusIconSpriteId,nameof(StatusIconSpriteId),out issue)
              ||!WorldFeedbackCoreValidation.Require(_applySfxId,nameof(ApplySfxId),out issue)||!WorldFeedbackCoreValidation.Require(_loopSfxId,nameof(LoopSfxId),out issue)
              ||!WorldFeedbackCoreValidation.Require(_endSfxId,nameof(EndSfxId),out issue)) return false;
            bool consumed=false, death=false;
            if(_reactions!=null) for(int i=0;i<_reactions.Count;i++)
            { StatusReactionFeedback reaction=_reactions[i]; if(!reaction.TryValidate(out issue)){issue=$"Status reaction {reaction.ReactionKind}: {issue}";return false;}
              bool duplicate; switch(reaction.ReactionKind){case StatusReactionKind.Consumed:duplicate=consumed;consumed=true;break;case StatusReactionKind.TargetDeath:duplicate=death;death=true;break;default:issue=$"Unsupported status reaction kind {reaction.ReactionKind}.";return false;}
              if(duplicate){issue=$"Duplicate status reaction kind {reaction.ReactionKind}.";return false;} }
            issue=string.Empty;return true;
        }
#if UNITY_EDITOR
        public void SetForEditor(VfxAssetId apply,VfxAssetId loop,VfxAssetId end,SpriteAssetId icon,AudioAssetId applySfx,AudioAssetId loopSfx,AudioAssetId endSfx,IEnumerable<StatusReactionFeedback> reactions)
        { _applyVfxId=apply;_loopVfxId=loop;_endVfxId=end;_statusIconSpriteId=icon;_applySfxId=applySfx;_loopSfxId=loopSfx;_endSfxId=endSfx;_reactions=reactions==null?new List<StatusReactionFeedback>():new List<StatusReactionFeedback>(reactions); }
#endif
    }
}
