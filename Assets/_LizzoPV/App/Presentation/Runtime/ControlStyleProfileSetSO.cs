using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Theme/Control Style Profile Set", fileName = "ControlStyleProfileSet")]
    public sealed class ControlStyleProfileSetSO : ScriptableObject
    {
        [SerializeField] private List<ControlStyleProfileBinding> _bindings = new List<ControlStyleProfileBinding>();

        public IReadOnlyList<ControlStyleProfileBinding> Bindings => _bindings;

        public bool TryGet(ControlStyleRole role, out ControlStyleProfileSO profile)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i].Role == role)
                {
                    profile = _bindings[i].Profile;
                    return profile != null;
                }
            }

            profile = null;
            return false;
        }

        public bool TryValidate(out string issue)
        {
            var seen = new HashSet<ControlStyleRole>();
            for (int i = 0; i < _bindings.Count; i++)
            {
                ControlStyleProfileBinding binding = _bindings[i];
                if (!seen.Add(binding.Role))
                {
                    issue = $"Duplicate ControlStyleRole binding: {binding.Role}.";
                    return false;
                }

                if (binding.Profile == null)
                {
                    issue = $"ControlStyleRole {binding.Role} requires a Profile.";
                    return false;
                }

                if (!binding.Profile.TryValidate(binding.Role, out issue))
                {
                    issue = $"ControlStyleRole {binding.Role}: {issue}";
                    return false;
                }
            }

            foreach (ControlStyleRole role in Enum.GetValues(typeof(ControlStyleRole)))
            {
                if (!seen.Contains(role))
                {
                    issue = $"Missing required ControlStyleRole binding: {role}.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(IEnumerable<ControlStyleProfileBinding> bindings)
        {
            _bindings = bindings == null
                ? new List<ControlStyleProfileBinding>()
                : new List<ControlStyleProfileBinding>(bindings);
        }
#endif
    }
}
