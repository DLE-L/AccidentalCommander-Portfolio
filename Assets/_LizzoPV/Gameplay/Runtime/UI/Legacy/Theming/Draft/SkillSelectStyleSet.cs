using UnityEngine;

namespace Lizzo.PV.UI.Theming.Draft
{
    [CreateAssetMenu(fileName = "SkillSelectStyleSet", menuName = "Lizzo/UI/Draft/Skill Select Style Set")]
    public sealed class SkillSelectStyleSet : ScriptableObject
    {
        [SerializeField] DraftImageStyle _header;
        [SerializeField] DraftImageStyle _refreshAction;

        public DraftImageStyle Header => _header;
        public DraftImageStyle RefreshAction => _refreshAction;
    }
}
