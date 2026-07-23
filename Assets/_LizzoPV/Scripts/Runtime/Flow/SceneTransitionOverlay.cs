using UnityEngine;

namespace Lizzo.PV.Flow
{
    public sealed class SceneTransitionOverlay : MonoBehaviour
    {
        static SceneTransitionOverlay s_instance;

        [SerializeField] GameObject _visualRoot;

        public static bool IsVisible => s_instance != null && s_instance._visualRoot != null && s_instance._visualRoot.activeSelf;

        void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
            if (_visualRoot == null)
            {
                Debug.LogError("[SceneTransitionOverlay] Authored transition visual is required.", this);
                return;
            }

            _visualRoot.SetActive(true);
        }

        void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        public static void Show()
        {
            if (s_instance?._visualRoot != null)
                s_instance._visualRoot.SetActive(true);
        }

        public static void Hide()
        {
            if (s_instance?._visualRoot != null)
                s_instance._visualRoot.SetActive(false);
        }
    }
}
