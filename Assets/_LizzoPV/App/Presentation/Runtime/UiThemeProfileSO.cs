using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/UI Theme Profile", fileName = "UiThemeProfile")]
    public sealed class UiThemeProfileSO : ScriptableObject
    {
        [SerializeField] private TypographyProfileSO _typographyProfile;
        [SerializeField] private ColorPaletteSO _colorPalette;
        [SerializeField] private ControlStyleProfileSetSO _controlStyleProfileSet;

        public TypographyProfileSO TypographyProfile => _typographyProfile;
        public ColorPaletteSO ColorPalette => _colorPalette;
        public ControlStyleProfileSetSO ControlStyleProfileSet => _controlStyleProfileSet;

        public bool TryValidate(out string issue)
        {
            if (_typographyProfile == null || _colorPalette == null || _controlStyleProfileSet == null)
            {
                issue = "UiThemeProfile requires TypographyProfile, ColorPalette, and ControlStyleProfileSet.";
                return false;
            }

            return _typographyProfile.TryValidate(out issue)
                   && _colorPalette.TryValidate(out issue)
                   && _controlStyleProfileSet.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            TypographyProfileSO typographyProfile,
            ColorPaletteSO colorPalette,
            ControlStyleProfileSetSO controlStyleProfileSet)
        {
            _typographyProfile = typographyProfile;
            _colorPalette = colorPalette;
            _controlStyleProfileSet = controlStyleProfileSet;
        }
#endif
    }
}
