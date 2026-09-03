using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/Typography Profile", fileName = "TypographyProfile")]
    public sealed class TypographyProfileSO : ScriptableObject
    {
        [SerializeField] private List<TypographyProfileBinding> _bindings = new List<TypographyProfileBinding>();

        public IReadOnlyList<TypographyProfileBinding> Bindings => _bindings;

        public bool TryGet(TypographyRole role, out TypographyProfileBinding binding)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].Role == role)
                {
                    binding = _bindings[i];
                    return true;
                }
            }

            binding = default;
            return false;
        }

        public bool TryValidate(out string issue)
        {
            var seen = new HashSet<TypographyRole>();
            for (int i = 0; i < _bindings.Count; i++)
            {
                TypographyProfileBinding binding = _bindings[i];
                if (!seen.Add(binding.Role))
                {
                    issue = $"Duplicate TypographyRole binding: {binding.Role}.";
                    return false;
                }

                if (binding.FontAsset == null)
                {
                    issue = $"TypographyRole {binding.Role} requires a FontAsset.";
                    return false;
                }
            }

            foreach (TypographyRole role in Enum.GetValues(typeof(TypographyRole)))
            {
                if (!seen.Contains(role))
                {
                    issue = $"Missing required TypographyRole binding: {role}.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(IEnumerable<TypographyProfileBinding> bindings)
        {
            _bindings = bindings == null
                ? new List<TypographyProfileBinding>()
                : new List<TypographyProfileBinding>(bindings);
        }
#endif
    }
}
