using UnityEngine;

namespace Lizzo.PV.Prototypes.Vfx
{
    /// <summary>
    /// Disposable timing prototype for the externally supplied Epic recruit reveal concept.
    /// It deliberately owns no gameplay, recruit, persistence, audio, or Addressables behavior.
    /// </summary>
    public sealed class RecruitRevealEpicPrototypeController : MonoBehaviour
    {
        private const float FrameDuration = 1.0f / 60.0f;
        private const float PixelToWorld = 10.4f / 1920.0f;
        private const int RevealEndFrame = 162;

        [Header("Stage")]
        [SerializeField] private Transform _stageRoot;
        [SerializeField] private Transform _chestRoot;
        [SerializeField] private Transform _chestLid;
        [SerializeField] private Transform _characterRoot;

        [Header("Core Renderers")]
        [SerializeField] private SpriteRenderer _chestClosedRenderer;
        [SerializeField] private SpriteRenderer _chestBottomRenderer;
        [SerializeField] private SpriteRenderer _chestLidRenderer;
        [SerializeField] private SpriteRenderer _characterRenderer;
        [SerializeField] private SpriteRenderer _glowRenderer;
        [SerializeField] private SpriteRenderer _raysRenderer;
        [SerializeField] private SpriteRenderer _magicCircleRenderer;
        [SerializeField] private SpriteRenderer _energyStreakRenderer;
        [SerializeField] private SpriteRenderer _explosionRenderer;
        [SerializeField] private SpriteRenderer _flashRenderer;
        [SerializeField] private SpriteRenderer _shockwaveRenderer;
        [SerializeField] private SpriteRenderer _shockwaveSecondaryRenderer;
        [SerializeField] private SpriteRenderer _smokeRenderer;
        [SerializeField] private SpriteRenderer _debrisRenderer;
        [SerializeField] private SpriteRenderer _landingDustRenderer;

        [Header("Accent Renderers")]
        [SerializeField] private SpriteRenderer _sparkLeftRenderer;
        [SerializeField] private SpriteRenderer _sparkRightRenderer;
        [SerializeField] private SpriteRenderer _starLeftRenderer;
        [SerializeField] private SpriteRenderer _starRightRenderer;
        [SerializeField] private SpriteRenderer _gradeBannerRenderer;
        [SerializeField] private SpriteRenderer _nameBannerRenderer;

        [Header("Playback")]
        [SerializeField, Min(3.2f)] private float _loopDurationSeconds = 4.2f;
        [SerializeField] private bool _autoRepeat = true;
        [SerializeField, Range(-1.0f, 2.7f)] private float _debugFreezeTime = -1.0f;

        private Vector3 _stageBasePosition;
        private Vector3 _stageBaseScale;
        private Vector3 _chestBasePosition;
        private Vector3 _chestBaseScale;
        private Vector3 _lidBasePosition;
        private Vector3 _lidBaseScale;
        private Quaternion _lidBaseRotation;
        private Vector3 _characterBasePosition;
        private Vector3 _characterBaseScale;
        private Vector3 _glowBaseScale;
        private Vector3 _raysBaseScale;
        private Vector3 _magicCircleBaseScale;
        private Vector3 _energyBaseScale;
        private Vector3 _explosionBaseScale;
        private Vector3 _flashBaseScale;
        private Vector3 _shockwaveBaseScale;
        private Vector3 _shockwaveSecondaryBaseScale;
        private Vector3 _smokeBaseScale;
        private Vector3 _debrisBaseScale;
        private Vector3 _landingDustBaseScale;
        private Vector3 _sparkLeftBasePosition;
        private Vector3 _sparkRightBasePosition;
        private Vector3 _sparkLeftBaseScale;
        private Vector3 _sparkRightBaseScale;
        private Vector3 _starLeftBasePosition;
        private Vector3 _starRightBasePosition;
        private Vector3 _starLeftBaseScale;
        private Vector3 _starRightBaseScale;
        private Vector3 _gradeBannerBasePosition;
        private Vector3 _nameBannerBasePosition;
        private Vector3 _gradeBannerBaseScale;
        private Vector3 _nameBannerBaseScale;
        private float _time;
        private int _loopCount;
        private bool _skipped;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _epicStyle;
        private GUIStyle _nameStyle;
        private GUIStyle _confirmStyle;
        private Camera _mainCamera;
        private Color _cameraBaseColor;

        private void Awake()
        {
            if (!HasRequiredAuthoring())
            {
                Debug.LogError("[RecruitRevealEpicPrototype] Required prototype authoring is missing.", this);
                enabled = false;
                return;
            }

            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("[RecruitRevealEpicPrototype] Main Camera is missing.", this);
                enabled = false;
                return;
            }

            _cameraBaseColor = _mainCamera.backgroundColor;
            CacheBaseState();
            Restart();
        }

        private void Update()
        {
            if (_skipped)
                return;

            if (_debugFreezeTime >= 0.0f)
            {
                _time = _debugFreezeTime;
                Evaluate(_time);
                return;
            }

            _time += Time.unscaledDeltaTime;
            if (_autoRepeat && _time >= _loopDurationSeconds)
            {
                _loopCount += 1;
                _time = 0.0f;
            }

            Evaluate(Mathf.Min(_time, FrameTime(RevealEndFrame)));
        }

        private void OnDisable()
        {
            if (_stageRoot == null)
                return;

            RestoreBaseTransforms();
            SetAllAlpha(1.0f);
            if (_mainCamera != null)
                _mainCamera.backgroundColor = _cameraBaseColor;
        }

        private void OnGUI()
        {
            EnsureGuiStyles();

            float scale = Mathf.Max(0.65f, Screen.width / 1080.0f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1.0f));
            float logicalWidth = Screen.width / scale;
            float logicalHeight = Screen.height / scale;

            GUI.Label(new Rect(36.0f, 34.0f, logicalWidth - 72.0f, 48.0f), "PROTOTYPE · EPIC RECRUIT REVEAL v1.0", _titleStyle);
            GUI.Label(
                new Rect(38.0f, 84.0f, logicalWidth - 76.0f, 88.0f),
                $"60fps authored rhythm · 2.70s reveal · loop {_loopCount + 1}\nauto replay · production gameplay/audio not connected",
                _bodyStyle);

            Color previousColor = GUI.color;
            float gradeAlpha = Smooth01(Frames01(_time, 144, 11));
            float nameAlpha = Smooth01(Frames01(_time, 150, 10));
            float confirmAlpha = Smooth01(Frames01(_time, 156, 6));
            GUI.color = new Color(1.0f, 1.0f, 1.0f, gradeAlpha);
            GUI.Label(new Rect(0.0f, logicalHeight * 0.238f, logicalWidth, 82.0f), "EPIC", _epicStyle);
            GUI.color = new Color(1.0f, 1.0f, 1.0f, nameAlpha);
            GUI.Label(new Rect(0.0f, logicalHeight * 0.308f, logicalWidth, 62.0f), "ROYAL SWORDSMAN", _nameStyle);
            GUI.color = new Color(1.0f, 1.0f, 1.0f, confirmAlpha);
            GUI.Box(new Rect(logicalWidth * 0.31f, logicalHeight * 0.805f, logicalWidth * 0.38f, 72.0f), "CONFIRM", _confirmStyle);
            GUI.color = previousColor;
            GUI.matrix = Matrix4x4.identity;
        }

        public void Restart()
        {
            _skipped = false;
            _time = 0.0f;
            Evaluate(0.0f);
        }

        public void Skip()
        {
            _skipped = true;
            _time = FrameTime(RevealEndFrame);
            Evaluate(_time);
        }

        private void Evaluate(float time)
        {
            RestoreBaseTransforms();
            SetAllAlpha(0.0f);
            _mainCamera.backgroundColor = _cameraBaseColor;

            AnimateChest(time);
            AnimateCharge(time);
            AnimateExplosion(time);
            AnimateCharacter(time);
            AnimateResult(time);
            AnimateContentShake(time);
        }

        private void AnimateChest(float time)
        {
            if (time < FrameTime(82))
            {
                SetAlpha(_chestClosedRenderer, 1.0f);
                if (time <= FrameTime(7))
                {
                    float t = OutCubic(Frames01(time, 0, 7));
                    _chestRoot.localPosition = _chestBasePosition + Vector3.up * Mathf.Lerp(60.0f, -15.0f, t) * PixelToWorld;
                    _chestRoot.localScale = Vector3.Scale(_chestBaseScale, Vector3.one * Mathf.Lerp(0.68f, 1.08f, t));
                }
                else if (time <= FrameTime(10))
                {
                    float t = OutQuad(Frames01(time, 7, 3));
                    _chestRoot.localPosition = _chestBasePosition + Vector3.up * Mathf.Lerp(-15.0f, 0.0f, t) * PixelToWorld;
                    _chestRoot.localScale = Vector3.Scale(_chestBaseScale, Vector3.one * Mathf.Lerp(1.08f, 1.0f, t));
                }
                else if (time < FrameTime(31))
                {
                    ApplyDirectedShake(Frames01(time, 11, 20), 4.0f, 2.0f, 0.7f, 1.55f);
                }
                else if (time < FrameTime(59))
                {
                    float t = Smooth01(Frames01(time, 31, 28));
                    ApplyDirectedShake(t, Mathf.Lerp(4.0f, 10.0f, t), Mathf.Lerp(2.0f, 5.0f, t), Mathf.Lerp(0.7f, 3.5f, t), Mathf.Lerp(2.3f, 4.8f, t));
                }
                else if (time < FrameTime(73))
                {
                    float t = InCubic(Frames01(time, 59, 13));
                    float vibration = Mathf.Sin(Frames01(time, 59, 14) * Mathf.PI * 8.0f) * 3.5f * PixelToWorld;
                    _chestRoot.localPosition = _chestBasePosition + new Vector3(vibration, Mathf.Lerp(0.0f, -12.0f, t) * PixelToWorld, 0.0f);
                    _chestRoot.localScale = Vector3.Scale(_chestBaseScale, new Vector3(Mathf.Lerp(1.0f, 1.06f, t), Mathf.Lerp(1.0f, 0.94f, t), 1.0f));
                }
                else
                {
                    // Frames 73-78: the important six-frame complete stop.
                    _chestRoot.localPosition = _chestBasePosition + Vector3.down * 12.0f * PixelToWorld;
                    _chestRoot.localScale = Vector3.Scale(_chestBaseScale, new Vector3(1.06f, 0.94f, 1.0f));
                }
                return;
            }

            float openLife = 1.0f - Smooth01(Frames01(time, 88, 16));
            SetAlpha(_chestBottomRenderer, openLife);
            SetAlpha(_chestLidRenderer, openLife);
            float bottomSettle = OutQuad(Frames01(time, 82, 5));
            _chestRoot.localScale = Vector3.Scale(_chestBaseScale, new Vector3(Mathf.Lerp(1.09f, 1.0f, bottomSettle), Mathf.Lerp(0.90f, 1.0f, bottomSettle), 1.0f));
            float lidOpen = OutExpo(Frames01(time, 82, 8));
            _chestLid.localPosition = _lidBasePosition + new Vector3(25.0f, 160.0f, 0.0f) * PixelToWorld * lidOpen;
            _chestLid.localRotation = _lidBaseRotation * Quaternion.Euler(0.0f, 0.0f, -18.0f * lidOpen);
            _chestLid.localScale = Vector3.Scale(_lidBaseScale, Vector3.one * Mathf.Lerp(1.0f, 1.04f, lidOpen));
        }

        private void AnimateCharge(float time)
        {
            float charge = Mathf.Clamp01(Frames01(time, 11, 68));
            float curve = SampleChargeCurve(charge);
            float preBurst = time < FrameTime(79) ? 1.0f : 0.0f;
            SetAlpha(_glowRenderer, curve * preBurst);
            _glowRenderer.transform.localScale = _glowBaseScale * Mathf.Lerp(0.52f, 1.08f, curve);
            float rays = Smooth01(Frames01(time, 59, 13)) * 0.30f * preBurst;
            SetAlpha(_raysRenderer, rays);
            _raysRenderer.transform.localScale = _raysBaseScale * Mathf.Lerp(0.88f, 1.0f, Smooth01(Frames01(time, 59, 13)));
            float streakAlpha = time >= FrameTime(31) && time < FrameTime(73) ? curve * (0.25f + Pulse01(Frames01(time, 31, 42)) * 0.35f) : 0.0f;
            SetAlpha(_energyStreakRenderer, streakAlpha);
            _energyStreakRenderer.transform.localScale = _energyBaseScale * Mathf.Lerp(0.68f, 1.0f, curve);
            AnimateChargeSparks(time, Smooth01(Frames01(time, 31, 28)) * preBurst);
            float push = time < FrameTime(79) ? Smooth01(Frames01(time, 59, 20)) : 0.0f;
            _stageRoot.localScale = _stageBaseScale * Mathf.Lerp(1.0f, 1.025f, push);
        }

        private void AnimateChargeSparks(float time, float chargeAlpha)
        {
            float frame = time / FrameDuration;
            float pulseA = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(frame * 0.31f));
            float pulseB = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(frame * 0.37f + 1.2f));
            SetAlpha(_sparkLeftRenderer, chargeAlpha * pulseA * 0.40f);
            SetAlpha(_sparkRightRenderer, chargeAlpha * pulseB * 0.34f);
            _sparkLeftRenderer.transform.localPosition = _sparkLeftBasePosition + Vector3.up * Mathf.Repeat(frame * 0.018f, 0.32f);
            _sparkRightRenderer.transform.localPosition = _sparkRightBasePosition + Vector3.up * Mathf.Repeat(frame * 0.015f + 0.16f, 0.32f);
            _sparkLeftRenderer.transform.localScale = _sparkLeftBaseScale * Mathf.Lerp(0.70f, 0.96f, pulseA);
            _sparkRightRenderer.transform.localScale = _sparkRightBaseScale * Mathf.Lerp(0.68f, 0.92f, pulseB);
        }

        private void AnimateExplosion(float time)
        {
            if (time >= FrameTime(79) && time < FrameTime(88))
            {
                float coreFade = 1.0f - Smooth01(Frames01(time, 81, 7));
                SetAlpha(_flashRenderer, coreFade);
                _flashRenderer.transform.localScale = _flashBaseScale * Mathf.Lerp(0.25f, 1.0f, OutCubic(Frames01(time, 79, 2)));
            }

            _mainCamera.backgroundColor = Color.Lerp(_cameraBaseColor, new Color(1.0f, 0.957f, 1.0f, 1.0f), ScreenFlashAlpha(time));
            AnimateShockwave(_shockwaveRenderer, _shockwaveBaseScale, time, 80, 12, 0.22f, 1.55f, 0.85f, 0.0f);
            AnimateShockwave(_shockwaveSecondaryRenderer, _shockwaveSecondaryBaseScale, time, 84, 17, 0.35f, 1.85f, 0.50f, 12.0f);
            AnimateShockwave(_shockwaveSecondaryRenderer, _shockwaveSecondaryBaseScale, time, 127, 10, 0.20f, 0.80f, 0.35f, 0.0f);

            if (time >= FrameTime(79) && time < FrameTime(96))
            {
                float t = Frames01(time, 79, 17);
                SetAlpha(_explosionRenderer, (1.0f - Smooth01(t)) * 0.72f);
                SetAlpha(_energyStreakRenderer, (1.0f - Smooth01(t)) * 0.88f);
                _explosionRenderer.transform.localScale = _explosionBaseScale * Mathf.Lerp(0.30f, 1.12f, OutCubic(t));
                _energyStreakRenderer.transform.localScale = _energyBaseScale * Mathf.Lerp(0.76f, 1.18f, OutCubic(t));
            }

            if (time >= FrameTime(84) && time < FrameTime(126))
            {
                float t = Frames01(time, 84, 42);
                SetAlpha(_debrisRenderer, Pulse01(t) * 0.72f);
                _debrisRenderer.transform.localScale = _debrisBaseScale * Mathf.Lerp(0.62f, 1.18f, OutCubic(t));
            }

            if (time >= FrameTime(87) && time < FrameTime(113))
            {
                float t = Frames01(time, 87, 26);
                SetAlpha(_smokeRenderer, Pulse01(t) * 0.22f);
                _smokeRenderer.transform.localScale = _smokeBaseScale * Mathf.Lerp(0.62f, 1.08f, OutCubic(t));
            }

            if (time >= FrameTime(90))
            {
                float postReveal = Smooth01(Frames01(time, 90, 24));
                SetAlpha(_raysRenderer, postReveal * 0.16f);
                SetAlpha(_glowRenderer, postReveal * 0.22f);
                SetAlpha(_magicCircleRenderer, postReveal * 0.52f);
                _magicCircleRenderer.transform.localScale = _magicCircleBaseScale * Mathf.Lerp(0.72f, 1.0f, postReveal);
            }

            if (time >= FrameTime(105))
            {
                float accent = Smooth01(Frames01(time, 105, 20));
                SetAlpha(_starLeftRenderer, accent * 0.62f);
                SetAlpha(_starRightRenderer, accent * 0.54f);
                _starLeftRenderer.transform.localPosition = _starLeftBasePosition + new Vector3(-0.16f * accent, 0.16f * accent, 0.0f);
                _starRightRenderer.transform.localPosition = _starRightBasePosition + new Vector3(0.16f * accent, 0.10f * accent, 0.0f);
            }
        }

        private void AnimateCharacter(float time)
        {
            if (time < FrameTime(84))
                return;

            SetAlpha(_characterRenderer, 1.0f);
            Vector3 start = _characterBasePosition + Vector3.down * 40.0f * PixelToWorld;
            Vector3 apex = _characterBasePosition + Vector3.up * 410.0f * PixelToWorld;
            if (time < FrameTime(102))
            {
                float t = OutExpo(Frames01(time, 84, 17));
                _characterRoot.localPosition = Vector3.LerpUnclamped(start, apex, t);
                _characterRoot.localScale = Vector3.Scale(_characterBaseScale, Vector3.Lerp(new Vector3(0.90f, 1.13f, 1.0f), new Vector3(1.02f, 0.98f, 1.0f), OutCubic(Frames01(time, 84, 17))));
                return;
            }

            if (time < FrameTime(108))
            {
                float hang = Frames01(time, 102, 6);
                _characterRoot.localPosition = apex + Vector3.up * Mathf.Sin(hang * Mathf.PI) * 3.0f * PixelToWorld;
                _characterRoot.localScale = Vector3.Scale(_characterBaseScale, new Vector3(1.02f, 0.98f, 1.0f));
                return;
            }

            if (time < FrameTime(127))
            {
                float t = InCubic(Frames01(time, 108, 19));
                _characterRoot.localPosition = Vector3.LerpUnclamped(apex, _characterBasePosition, t);
                _characterRoot.localScale = Vector3.Scale(_characterBaseScale, Vector3.Lerp(new Vector3(1.02f, 0.98f, 1.0f), Vector3.one, t));
                return;
            }

            _characterRoot.localPosition = _characterBasePosition;
            if (time < FrameTime(132))
                _characterRoot.localScale = Vector3.Scale(_characterBaseScale, Vector3.Lerp(new Vector3(1.18f, 0.78f, 1.0f), new Vector3(1.08f, 0.91f, 1.0f), OutQuad(Frames01(time, 127, 4))));
            else if (time < FrameTime(137))
                _characterRoot.localScale = Vector3.Scale(_characterBaseScale, Vector3.Lerp(new Vector3(0.94f, 1.10f, 1.0f), new Vector3(1.02f, 0.99f, 1.0f), OutQuad(Frames01(time, 132, 5))));
            else if (time < FrameTime(144))
                _characterRoot.localScale = Vector3.Scale(_characterBaseScale, Vector3.LerpUnclamped(new Vector3(1.02f, 0.99f, 1.0f), Vector3.one, OutBack(Frames01(time, 137, 7), 1.10f)));

            if (time < FrameTime(145))
            {
                float dust = Frames01(time, 127, 18);
                SetAlpha(_landingDustRenderer, (1.0f - Smooth01(dust)) * 0.58f);
                _landingDustRenderer.transform.localScale = _landingDustBaseScale * Mathf.Lerp(0.55f, 1.0f, OutCubic(dust));
            }
        }

        private void AnimateResult(float time)
        {
            float grade = Smooth01(Frames01(time, 144, 11));
            float name = Smooth01(Frames01(time, 150, 10));
            SetAlpha(_gradeBannerRenderer, grade * 0.96f);
            SetAlpha(_nameBannerRenderer, name * 0.90f);
            _gradeBannerRenderer.transform.localPosition = _gradeBannerBasePosition + Vector3.up * Mathf.Lerp(28.0f * PixelToWorld, 0.0f, OutCubic(grade));
            _nameBannerRenderer.transform.localPosition = _nameBannerBasePosition + Vector3.up * Mathf.Lerp(18.0f * PixelToWorld, 0.0f, OutCubic(name));
            _gradeBannerRenderer.transform.localScale = _gradeBannerBaseScale * Mathf.Lerp(0.86f, 1.0f + Mathf.Sin(grade * Mathf.PI) * 0.08f, grade);
            _nameBannerRenderer.transform.localScale = _nameBannerBaseScale * Mathf.Lerp(0.90f, 1.0f + Mathf.Sin(name * Mathf.PI) * 0.06f, name);
        }

        private void AnimateContentShake(float time)
        {
            if (time >= FrameTime(79) && time < FrameTime(87))
                ApplyStageShake(time, 79, 8, 10.0f, 6.0f, 0.30f);
            else if (time >= FrameTime(127) && time < FrameTime(132))
                ApplyStageShake(time, 127, 5, 4.0f, 3.0f, 0.12f);
        }

        private void ApplyStageShake(float time, int startFrame, int durationFrames, float xPixels, float yPixels, float rotationDegrees)
        {
            float decay = 1.0f - OutCubic(Frames01(time, startFrame, durationFrames));
            float frame = time / FrameDuration;
            _stageRoot.localPosition += new Vector3(Mathf.Sin(frame * 3.1f) * xPixels, Mathf.Sin(frame * 2.7f + 0.8f) * yPixels, 0.0f) * PixelToWorld * decay;
            _stageRoot.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Sin(frame * 2.4f) * rotationDegrees * decay);
        }

        private void ApplyDirectedShake(float normalizedTime, float xPixels, float yPixels, float rotationDegrees, float cycles)
        {
            float phase = normalizedTime * Mathf.PI * 2.0f * cycles;
            float x = Mathf.Sin(phase) + Mathf.Sin(phase * 2.17f + 0.7f) * 0.12f;
            float y = Mathf.Sin(phase * 0.73f + 1.1f) * 0.65f;
            _chestRoot.localPosition = _chestBasePosition + new Vector3(x * xPixels, y * yPixels, 0.0f) * PixelToWorld;
            _chestRoot.localRotation = Quaternion.Euler(0.0f, 0.0f, -x * rotationDegrees);
        }

        private static void AnimateShockwave(SpriteRenderer renderer, Vector3 baseScale, float time, int startFrame, int durationFrames, float startScale, float endScale, float maxAlpha, float rotation)
        {
            if (time < FrameTime(startFrame) || time >= FrameTime(startFrame + durationFrames))
                return;
            float t = Frames01(time, startFrame, durationFrames);
            renderer.transform.localScale = baseScale * Mathf.Lerp(startScale, endScale, OutCubic(t));
            renderer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotation * t);
            SetAlpha(renderer, (1.0f - Smooth01(t)) * maxAlpha);
        }

        private static float ScreenFlashAlpha(float time)
        {
            if (time < FrameTime(81) || time >= FrameTime(88))
                return 0.0f;
            if (time < FrameTime(83))
                return Mathf.Lerp(0.0f, 0.72f, Frames01(time, 81, 2));
            if (time < FrameTime(84))
                return 0.72f;
            return Mathf.Lerp(0.72f, 0.0f, Frames01(time, 84, 4));
        }

        private bool HasRequiredAuthoring()
        {
            return _stageRoot != null
                && _chestRoot != null
                && _chestLid != null
                && _characterRoot != null
                && _chestClosedRenderer != null
                && _chestBottomRenderer != null
                && _chestLidRenderer != null
                && _characterRenderer != null
                && _glowRenderer != null
                && _raysRenderer != null
                && _magicCircleRenderer != null
                && _energyStreakRenderer != null
                && _explosionRenderer != null
                && _flashRenderer != null
                && _shockwaveRenderer != null
                && _shockwaveSecondaryRenderer != null
                && _smokeRenderer != null
                && _debrisRenderer != null
                && _landingDustRenderer != null
                && _sparkLeftRenderer != null
                && _sparkRightRenderer != null
                && _starLeftRenderer != null
                && _starRightRenderer != null
                && _gradeBannerRenderer != null
                && _nameBannerRenderer != null;
        }

        private void CacheBaseState()
        {
            _stageBasePosition = _stageRoot.localPosition;
            _stageBaseScale = _stageRoot.localScale;
            _chestBasePosition = _chestRoot.localPosition;
            _chestBaseScale = _chestRoot.localScale;
            _lidBasePosition = _chestLid.localPosition;
            _lidBaseScale = _chestLid.localScale;
            _lidBaseRotation = _chestLid.localRotation;
            _characterBasePosition = _characterRoot.localPosition;
            _characterBaseScale = _characterRoot.localScale;
            _glowBaseScale = _glowRenderer.transform.localScale;
            _raysBaseScale = _raysRenderer.transform.localScale;
            _magicCircleBaseScale = _magicCircleRenderer.transform.localScale;
            _energyBaseScale = _energyStreakRenderer.transform.localScale;
            _explosionBaseScale = _explosionRenderer.transform.localScale;
            _flashBaseScale = _flashRenderer.transform.localScale;
            _shockwaveBaseScale = _shockwaveRenderer.transform.localScale;
            _shockwaveSecondaryBaseScale = _shockwaveSecondaryRenderer.transform.localScale;
            _smokeBaseScale = _smokeRenderer.transform.localScale;
            _debrisBaseScale = _debrisRenderer.transform.localScale;
            _landingDustBaseScale = _landingDustRenderer.transform.localScale;
            _sparkLeftBasePosition = _sparkLeftRenderer.transform.localPosition;
            _sparkRightBasePosition = _sparkRightRenderer.transform.localPosition;
            _sparkLeftBaseScale = _sparkLeftRenderer.transform.localScale;
            _sparkRightBaseScale = _sparkRightRenderer.transform.localScale;
            _starLeftBasePosition = _starLeftRenderer.transform.localPosition;
            _starRightBasePosition = _starRightRenderer.transform.localPosition;
            _starLeftBaseScale = _starLeftRenderer.transform.localScale;
            _starRightBaseScale = _starRightRenderer.transform.localScale;
            _gradeBannerBasePosition = _gradeBannerRenderer.transform.localPosition;
            _nameBannerBasePosition = _nameBannerRenderer.transform.localPosition;
            _gradeBannerBaseScale = _gradeBannerRenderer.transform.localScale;
            _nameBannerBaseScale = _nameBannerRenderer.transform.localScale;
        }

        private void RestoreBaseTransforms()
        {
            _stageRoot.localPosition = _stageBasePosition;
            _stageRoot.localRotation = Quaternion.identity;
            _stageRoot.localScale = _stageBaseScale;
            _chestRoot.localPosition = _chestBasePosition;
            _chestRoot.localRotation = Quaternion.identity;
            _chestRoot.localScale = _chestBaseScale;
            _chestLid.localPosition = _lidBasePosition;
            _chestLid.localRotation = _lidBaseRotation;
            _chestLid.localScale = _lidBaseScale;
            _characterRoot.localPosition = _characterBasePosition;
            _characterRoot.localRotation = Quaternion.identity;
            _characterRoot.localScale = _characterBaseScale;
            _glowRenderer.transform.localScale = _glowBaseScale;
            _glowRenderer.transform.localRotation = Quaternion.identity;
            _raysRenderer.transform.localScale = _raysBaseScale;
            _raysRenderer.transform.localRotation = Quaternion.identity;
            _magicCircleRenderer.transform.localScale = _magicCircleBaseScale;
            _magicCircleRenderer.transform.localRotation = Quaternion.identity;
            _energyStreakRenderer.transform.localScale = _energyBaseScale;
            _explosionRenderer.transform.localScale = _explosionBaseScale;
            _flashRenderer.transform.localScale = _flashBaseScale;
            _shockwaveRenderer.transform.localScale = _shockwaveBaseScale;
            _shockwaveRenderer.transform.localRotation = Quaternion.identity;
            _shockwaveSecondaryRenderer.transform.localScale = _shockwaveSecondaryBaseScale;
            _shockwaveSecondaryRenderer.transform.localRotation = Quaternion.identity;
            _smokeRenderer.transform.localScale = _smokeBaseScale;
            _debrisRenderer.transform.localScale = _debrisBaseScale;
            _landingDustRenderer.transform.localScale = _landingDustBaseScale;
            _sparkLeftRenderer.transform.localPosition = _sparkLeftBasePosition;
            _sparkRightRenderer.transform.localPosition = _sparkRightBasePosition;
            _sparkLeftRenderer.transform.localScale = _sparkLeftBaseScale;
            _sparkRightRenderer.transform.localScale = _sparkRightBaseScale;
            _starLeftRenderer.transform.localPosition = _starLeftBasePosition;
            _starRightRenderer.transform.localPosition = _starRightBasePosition;
            _starLeftRenderer.transform.localScale = _starLeftBaseScale;
            _starRightRenderer.transform.localScale = _starRightBaseScale;
            _gradeBannerRenderer.transform.localPosition = _gradeBannerBasePosition;
            _nameBannerRenderer.transform.localPosition = _nameBannerBasePosition;
            _gradeBannerRenderer.transform.localScale = _gradeBannerBaseScale;
            _nameBannerRenderer.transform.localScale = _nameBannerBaseScale;
        }

        private void SetAllAlpha(float alpha)
        {
            SetAlpha(_chestClosedRenderer, alpha);
            SetAlpha(_chestBottomRenderer, alpha);
            SetAlpha(_chestLidRenderer, alpha);
            SetAlpha(_characterRenderer, alpha);
            SetAlpha(_glowRenderer, alpha);
            SetAlpha(_raysRenderer, alpha);
            SetAlpha(_magicCircleRenderer, alpha);
            SetAlpha(_energyStreakRenderer, alpha);
            SetAlpha(_explosionRenderer, alpha);
            SetAlpha(_flashRenderer, alpha);
            SetAlpha(_shockwaveRenderer, alpha);
            SetAlpha(_shockwaveSecondaryRenderer, alpha);
            SetAlpha(_smokeRenderer, alpha);
            SetAlpha(_debrisRenderer, alpha);
            SetAlpha(_landingDustRenderer, alpha);
            SetAlpha(_sparkLeftRenderer, alpha);
            SetAlpha(_sparkRightRenderer, alpha);
            SetAlpha(_starLeftRenderer, alpha);
            SetAlpha(_starRightRenderer, alpha);
            SetAlpha(_gradeBannerRenderer, alpha);
            SetAlpha(_nameBannerRenderer, alpha);
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private static float FrameTime(int frame) => frame * FrameDuration;

        private static float Frames01(float time, int startFrame, int durationFrames)
        {
            return Mathf.Clamp01((time - FrameTime(startFrame)) / Mathf.Max(FrameDuration, FrameTime(durationFrames)));
        }

        private static float Smooth01(float t) => t * t * (3.0f - 2.0f * t);
        private static float Pulse01(float t) => Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        private static float OutQuad(float t) => 1.0f - (1.0f - t) * (1.0f - t);
        private static float InCubic(float t) => t * t * t;
        private static float OutCubic(float t) => 1.0f - Mathf.Pow(1.0f - t, 3.0f);

        private static float OutExpo(float t)
        {
            return t >= 1.0f ? 1.0f : 1.0f - Mathf.Pow(2.0f, -10.0f * t);
        }

        private static float OutBack(float t, float overshoot)
        {
            float shifted = t - 1.0f;
            return 1.0f + (overshoot + 1.0f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        private static float SampleChargeCurve(float t)
        {
            if (t <= 0.20f)
                return Mathf.Lerp(0.0f, 0.08f, t / 0.20f);
            if (t <= 0.40f)
                return Mathf.Lerp(0.08f, 0.15f, (t - 0.20f) / 0.20f);
            if (t <= 0.65f)
                return Mathf.Lerp(0.15f, 0.35f, (t - 0.40f) / 0.25f);
            if (t <= 0.82f)
                return Mathf.Lerp(0.35f, 0.60f, (t - 0.65f) / 0.17f);
            if (t <= 0.92f)
                return Mathf.Lerp(0.60f, 0.82f, (t - 0.82f) / 0.10f);
            return Mathf.Lerp(0.82f, 1.0f, (t - 0.92f) / 0.08f);
        }

        private void EnsureGuiStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.97f, 0.86f, 1.0f, 1.0f) },
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.78f, 0.72f, 0.86f, 1.0f) },
            };
            _epicStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 58,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.84f, 0.30f, 1.0f) },
            };
            _nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            _confirmStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
        }
    }
}
