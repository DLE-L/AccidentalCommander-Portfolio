using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI.Theming.Draft
{
    public interface ISkillSelectDraftThemeTarget
    {
        void Apply(SkillSelectStyleSet styleSet);
    }

    public sealed class SkillSelectDraftThemeBinder : MonoBehaviour, ISkillSelectDraftThemeTarget
    {
        [SerializeField] SkillSelectStyleSet _styleSet;
        [SerializeField] Image _headerImage;
        [SerializeField] Image _refreshActionImage;

        void Awake()
        {
            Apply(_styleSet);
        }

        public void Apply(SkillSelectStyleSet styleSet)
        {
            if (styleSet == null)
            {
                Debug.LogError("[SkillSelectDraftThemeBinder] SkillSelectStyleSet is required.", this);
                return;
            }

            if (_headerImage == null || _refreshActionImage == null)
            {
                Debug.LogError("[SkillSelectDraftThemeBinder] All authored shell Image references are required.", this);
                return;
            }

            if (styleSet.Header.Sprite == null
                || styleSet.RefreshAction.Sprite == null)
            {
                Debug.LogError("[SkillSelectDraftThemeBinder] All SkillSelect shell Sprite slots are required.", styleSet);
                return;
            }

            _headerImage.sprite = styleSet.Header.Sprite;
            _refreshActionImage.sprite = styleSet.RefreshAction.Sprite;
        }
    }
}
