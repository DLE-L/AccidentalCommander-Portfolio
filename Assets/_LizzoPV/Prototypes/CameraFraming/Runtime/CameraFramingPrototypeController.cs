using UnityEngine;

namespace Lizzo.PV.Prototypes.CameraFraming
{
    [DisallowMultipleComponent]
    public sealed class CameraFramingPrototypeController : MonoBehaviour
    {
        private enum ComparisonPreset
        {
            CameraOnly,
            ReferenceMatch,
            WideWorldExpandedVisual,
        }

        private enum EnemyScalePreset
        {
            Current,
            PlusTenPercent,
            PlusTwentyPercent,
            RoleMatched,
        }

        private const int CompanionCapacity = 7;
        private const int EnemyCapacity = 5;
        private const float CameraOnlyInitialOrthographicSize = 5.5f;
        private const float ReferenceMatchInitialOrthographicSize = 5.0f;
        private const float FinalOrthographicSize = 7.2f;
        private const float ReferenceMatchCommanderVisualMultiplier = 1.25f;
        private const float ReferenceMatchCompanionVisualMultiplier = 1.10f;
        private const float WideWorldInitialOrthographicSize = 5.8f;
        private const float WideWorldFinalOrthographicSize = 7.5f;
        private const float WideWorldCommanderVisualMultiplier = 1.40f;
        private const float WideWorldCompanionVisualMultiplier = 1.20f;
        private const float WideWorldCommanderVisualTargetSize = 1.00f;
        private const float OrcToCompanionHeightRatio = 1.15f;
        private const float MidEnemyToCommanderHeightRatio = 1.15f;
        private const float BossToCommanderHeightRatio = 1.55f;
        private const float SmallGoblinSilhouetteAdjustment = 0.524f;
        private const float HungryWolfSilhouetteAdjustment = 0.488f;
        private const float ShieldOrcSilhouetteAdjustment = 0.675f;
        private const float BossSilhouetteAdjustment = 1.25f;
        private const float RecruitIntervalSeconds = 1.5f;
        private const float FullPartyHoldSeconds = 5.0f;
        private const float CameraSmoothTimeSeconds = 1.2f;

        private static readonly Vector3[] FormationPositions =
        {
            new Vector3(0.0f, 1.35f, 0.0f),
            new Vector3(-1.10f, 0.80f, 0.0f),
            new Vector3(1.10f, 0.80f, 0.0f),
            new Vector3(-1.42f, 0.05f, 0.0f),
            new Vector3(1.42f, 0.05f, 0.0f),
            new Vector3(-0.92f, -1.05f, 0.0f),
            new Vector3(0.92f, -1.05f, 0.0f),
        };

        private static readonly Vector3[] EnemyPositions =
        {
            new Vector3(-2.48f, 1.55f, 0.0f),
            new Vector3(2.48f, 1.55f, 0.0f),
            new Vector3(-2.58f, -0.35f, 0.0f),
            new Vector3(2.58f, -0.35f, 0.0f),
            new Vector3(0.0f, -2.65f, 0.0f),
        };

        [SerializeField] private Camera _camera;
        [SerializeField] private GameObject _commanderPrefab;
        [SerializeField] private GameObject[] _companionPrefabs = new GameObject[CompanionCapacity];
        [SerializeField] private GameObject[] _enemyPrefabs = new GameObject[EnemyCapacity];

        private readonly GameObject[] _companions = new GameObject[CompanionCapacity];
        private readonly Transform[] _companionVisuals = new Transform[CompanionCapacity];
        private readonly Vector3[] _companionBaseVisualScales = new Vector3[CompanionCapacity];
        private readonly GameObject[] _enemies = new GameObject[EnemyCapacity];
        private readonly Transform[] _enemyVisuals = new Transform[EnemyCapacity];
        private readonly Vector3[] _enemyBaseVisualScales = new Vector3[EnemyCapacity];
        private GameObject _commander;
        private Transform _commanderVisual;
        private Vector3 _commanderBaseVisualScale;
        private ComparisonPreset _activePreset;
        private EnemyScalePreset _activeEnemyScalePreset;
        private float _initialOrthographicSize;
        private float _finalOrthographicSize;
        private float _cameraVelocity;
        private float _cycleElapsed;
        private int _visibleCompanionCount;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _countStyle;

        private void Awake()
        {
            if (!ValidateAuthoring())
            {
                enabled = false;
                return;
            }

            ConfigureCamera();
            CreateCharacters();
            CreateEnemyComparisons();
            ApplyPreset(ComparisonPreset.WideWorldExpandedVisual);
            ApplyEnemyScale(EnemyScalePreset.Current);

            Debug.Log("[CameraFramingPrototype] Ready. Character and enemy roots stay at one; default=C character size + current enemy scale.", this);
        }

        private void Update()
        {
            _cycleElapsed += Time.deltaTime;

            if (_visibleCompanionCount < CompanionCapacity)
            {
                if (_cycleElapsed >= RecruitIntervalSeconds)
                {
                    _cycleElapsed = 0.0f;
                    SetVisibleCompanionCount(_visibleCompanionCount + 1);
                }
            }
            else if (_cycleElapsed >= FullPartyHoldSeconds)
            {
                ResetCycle();
            }

            float targetSize = ResolveTargetOrthographicSize(_visibleCompanionCount);
            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize,
                targetSize,
                ref _cameraVelocity,
                CameraSmoothTimeSeconds);
        }

        private void OnGUI()
        {
            EnsureGuiStyles();

            float scale = Mathf.Max(0.75f, Screen.width / 1080.0f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1.0f));

            GUI.Box(new Rect(24.0f, 24.0f, 520.0f, 512.0f), GUIContent.none);
            GUI.Label(new Rect(48.0f, 42.0f, 430.0f, 38.0f), "CAMERA FRAMING PROTOTYPE", _headerStyle);
            GUI.Label(
                new Rect(48.0f, 82.0f, 430.0f, 48.0f),
                $"Companions  {_visibleCompanionCount} / {CompanionCapacity}",
                _countStyle);
            GUI.Label(
                new Rect(48.0f, 132.0f, 430.0f, 30.0f),
                $"Camera  {_camera.orthographicSize:0.00}  ->  {ResolveTargetOrthographicSize(_visibleCompanionCount):0.00}",
                _bodyStyle);
            GUI.Label(
                new Rect(48.0f, 166.0f, 460.0f, 28.0f),
                _activePreset switch
                {
                    ComparisonPreset.CameraOnly => "A  Cam 5.5 -> 7.2 / Visual 1.00x",
                    ComparisonPreset.ReferenceMatch => "B  Cam 5.0 -> 7.2 / Cmd 1.25x / Allies 1.10x",
                    _ => "C  Cam 5.8 -> 7.5 / Cmd Visual 1.00 / Relative Allies",
                },
                _bodyStyle);

            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = _activePreset == ComparisonPreset.CameraOnly
                ? new Color(1.0f, 0.84f, 0.30f, 1.0f)
                : Color.white;
            if (GUI.Button(new Rect(48.0f, 212.0f, 146.0f, 70.0f), "A  CAMERA\n5.5 / 1.00x"))
                ApplyPreset(ComparisonPreset.CameraOnly);

            GUI.backgroundColor = _activePreset == ComparisonPreset.ReferenceMatch
                ? new Color(1.0f, 0.84f, 0.30f, 1.0f)
                : Color.white;
            if (GUI.Button(new Rect(204.0f, 212.0f, 146.0f, 70.0f), "B  CLOSE\n5.0 / 1.25x"))
                ApplyPreset(ComparisonPreset.ReferenceMatch);

            GUI.backgroundColor = _activePreset == ComparisonPreset.WideWorldExpandedVisual
                ? new Color(1.0f, 0.84f, 0.30f, 1.0f)
                : Color.white;
            if (GUI.Button(new Rect(360.0f, 212.0f, 160.0f, 70.0f), "C  CMD VISUAL\nSIZE 1.00"))
                ApplyPreset(ComparisonPreset.WideWorldExpandedVisual);

            GUI.backgroundColor = previousBackgroundColor;
            GUI.Label(
                new Rect(48.0f, 292.0f, 460.0f, 26.0f),
                "Changing preset resets the comparison to 0 companions.",
                _bodyStyle);
            GUI.Label(
                new Rect(48.0f, 322.0f, 460.0f, 26.0f),
                "Background: flowing highlights + small decor v8",
                _bodyStyle);
            GUI.Label(
                new Rect(48.0f, 360.0f, 460.0f, 28.0f),
                _activeEnemyScalePreset == EnemyScalePreset.RoleMatched
                    ? "Enemy Visual  ROLE v2: small down / orc slight / mid keep / boss up"
                    : $"Enemy Visual  x{ResolveEnemyScaleMultiplier(_activeEnemyScalePreset):0.00}",
                _bodyStyle);

            GUI.backgroundColor = _activeEnemyScalePreset == EnemyScalePreset.Current
                ? new Color(1.0f, 0.84f, 0.30f, 1.0f)
                : Color.white;
            if (GUI.Button(new Rect(48.0f, 402.0f, 108.0f, 70.0f), "ENEMY\nCURRENT"))
                ApplyEnemyScale(EnemyScalePreset.Current);

            GUI.backgroundColor = _activeEnemyScalePreset == EnemyScalePreset.PlusTenPercent
                ? new Color(1.0f, 0.84f, 0.30f, 1.0f)
                : Color.white;
            if (GUI.Button(new Rect(166.0f, 402.0f, 108.0f, 70.0f), "ENEMY\n+10%"))
                ApplyEnemyScale(EnemyScalePreset.PlusTenPercent);

            GUI.backgroundColor = _activeEnemyScalePreset == EnemyScalePreset.PlusTwentyPercent
                ? new Color(1.0f, 0.84f, 0.30f, 1.0f)
                : Color.white;
            if (GUI.Button(new Rect(284.0f, 402.0f, 108.0f, 70.0f), "ENEMY\n+20%"))
                ApplyEnemyScale(EnemyScalePreset.PlusTwentyPercent);

            GUI.backgroundColor = _activeEnemyScalePreset == EnemyScalePreset.RoleMatched
                ? new Color(1.0f, 0.84f, 0.30f, 1.0f)
                : Color.white;
            if (GUI.Button(new Rect(402.0f, 402.0f, 118.0f, 70.0f), "ENEMY\nROLE"))
                ApplyEnemyScale(EnemyScalePreset.RoleMatched);

            GUI.backgroundColor = previousBackgroundColor;
            GUI.matrix = previousMatrix;
        }

        private bool ValidateAuthoring()
        {
            if (_camera == null)
            {
                Debug.LogError("[CameraFramingPrototype] Camera reference is required.", this);
                return false;
            }

            if (_commanderPrefab == null)
            {
                Debug.LogError("[CameraFramingPrototype] Commander visual Prefab is required.", this);
                return false;
            }

            if (_commanderPrefab.transform.Find("Visual") == null)
            {
                Debug.LogError("[CameraFramingPrototype] Commander Prefab requires a direct Visual child.", this);
                return false;
            }

            if (_companionPrefabs == null || _companionPrefabs.Length != CompanionCapacity)
            {
                Debug.LogError($"[CameraFramingPrototype] Exactly {CompanionCapacity} companion visual Prefabs are required.", this);
                return false;
            }

            for (int index = 0; index < _companionPrefabs.Length; index++)
            {
                GameObject companionPrefab = _companionPrefabs[index];
                if (companionPrefab == null)
                {
                    Debug.LogError($"[CameraFramingPrototype] Companion Prefab {index + 1} is missing.", this);
                    return false;
                }

                if (companionPrefab.transform.Find("Visual") != null)
                    continue;

                Debug.LogError($"[CameraFramingPrototype] Companion Prefab {index + 1} requires a direct Visual child.", this);
                return false;
            }

            if (_enemyPrefabs == null || _enemyPrefabs.Length != EnemyCapacity)
            {
                Debug.LogError($"[CameraFramingPrototype] Exactly {EnemyCapacity} enemy Prefabs are required.", this);
                return false;
            }

            for (int index = 0; index < _enemyPrefabs.Length; index++)
            {
                GameObject enemyPrefab = _enemyPrefabs[index];
                if (enemyPrefab == null || enemyPrefab.transform.Find("Visual") == null)
                {
                    Debug.LogError($"[CameraFramingPrototype] Enemy Prefab {index + 1} requires a direct Visual child.", this);
                    return false;
                }
            }

            return true;
        }

        private void ConfigureCamera()
        {
            _camera.orthographic = true;
            _camera.orthographicSize = WideWorldInitialOrthographicSize;
            _camera.transform.SetPositionAndRotation(new Vector3(0.0f, 0.0f, -10.0f), Quaternion.identity);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.10f, 0.16f, 0.12f, 1.0f);
        }

        private void CreateCharacters()
        {
            _commander = Instantiate(_commanderPrefab, Vector3.zero, Quaternion.identity, transform);
            _commander.name = "Commander_Visual";
            _commander.transform.localScale = Vector3.one;
            _commanderVisual = _commander.transform.Find("Visual");
            _commanderBaseVisualScale = _commanderVisual.localScale;

            for (int index = 0; index < CompanionCapacity; index++)
            {
                GameObject companion = Instantiate(
                    _companionPrefabs[index],
                    FormationPositions[index],
                    Quaternion.identity,
                    transform);
                companion.name = $"Companion_{index + 1:00}";
                companion.transform.localScale = Vector3.one;
                Transform visual = companion.transform.Find("Visual");
                _companionVisuals[index] = visual;
                _companionBaseVisualScales[index] = visual.localScale;
                companion.SetActive(false);
                _companions[index] = companion;
            }
        }

        private void CreateEnemyComparisons()
        {
            GameObject enemyRoot = new GameObject("EnemyScaleComparison");
            enemyRoot.transform.SetParent(transform, false);
            enemyRoot.SetActive(false);

            for (int index = 0; index < EnemyCapacity; index++)
            {
                GameObject enemy = Instantiate(
                    _enemyPrefabs[index],
                    EnemyPositions[index],
                    Quaternion.identity,
                    enemyRoot.transform);
                enemy.name = $"Enemy_{index + 1:00}_{_enemyPrefabs[index].name}";
                enemy.transform.localScale = Vector3.one;

                MonoBehaviour[] behaviours = enemy.GetComponentsInChildren<MonoBehaviour>(true);
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                    behaviours[behaviourIndex].enabled = false;

                Animator[] animators = enemy.GetComponentsInChildren<Animator>(true);
                for (int animatorIndex = 0; animatorIndex < animators.Length; animatorIndex++)
                    animators[animatorIndex].enabled = false;

                Rigidbody2D[] bodies = enemy.GetComponentsInChildren<Rigidbody2D>(true);
                for (int bodyIndex = 0; bodyIndex < bodies.Length; bodyIndex++)
                    bodies[bodyIndex].simulated = false;

                Collider2D[] colliders = enemy.GetComponentsInChildren<Collider2D>(true);
                for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                    colliders[colliderIndex].enabled = false;

                Transform visual = enemy.transform.Find("Visual");
                _enemies[index] = enemy;
                _enemyVisuals[index] = visual;
                _enemyBaseVisualScales[index] = visual.localScale;
            }

            enemyRoot.SetActive(true);
        }

        private void ApplyEnemyScale(EnemyScalePreset preset)
        {
            _activeEnemyScalePreset = preset;

            for (int index = 0; index < _enemies.Length; index++)
            {
                _enemies[index].transform.localScale = Vector3.one;
                _enemyVisuals[index].localScale = _enemyBaseVisualScales[index];
            }

            if (preset == EnemyScalePreset.RoleMatched)
            {
                ApplyRoleMatchedEnemyScale();
                return;
            }

            float multiplier = ResolveEnemyScaleMultiplier(preset);
            for (int index = 0; index < _enemies.Length; index++)
                _enemyVisuals[index].localScale = ScaleVisual(_enemyBaseVisualScales[index], multiplier);

            Debug.Log($"[CameraFramingPrototype] enemy_scale_preset={preset}", this);
        }

        private void ApplyRoleMatchedEnemyScale()
        {
            float companionHeight = ResolveBaseCompanionReferenceHeight();
            float commanderHeight = ResolveRenderedSpriteHeight(_commanderVisual);
            float[] targetHeights =
            {
                companionHeight * SmallGoblinSilhouetteAdjustment,
                companionHeight * HungryWolfSilhouetteAdjustment,
                companionHeight * OrcToCompanionHeightRatio * ShieldOrcSilhouetteAdjustment,
                commanderHeight * MidEnemyToCommanderHeightRatio,
                commanderHeight * BossToCommanderHeightRatio * BossSilhouetteAdjustment,
            };

            for (int index = 0; index < _enemyVisuals.Length; index++)
            {
                float baseHeight = ResolveRenderedSpriteHeight(_enemyVisuals[index]);
                if (baseHeight <= Mathf.Epsilon)
                {
                    Debug.LogError($"[CameraFramingPrototype] Enemy {index} has no measurable SpriteRenderer height.", this);
                    continue;
                }

                float multiplier = targetHeights[index] / baseHeight;
                _enemyVisuals[index].localScale = ScaleVisual(_enemyBaseVisualScales[index], multiplier);
                float appliedHeight = ResolveRenderedSpriteHeight(_enemyVisuals[index]);
                Debug.Log(
                    $"[CameraFramingPrototype] enemy_role_index={index} target_height={targetHeights[index]:0.000} " +
                    $"applied_height={appliedHeight:0.000} visual_scale={_enemyVisuals[index].localScale.x:0.000}",
                    this);
            }

            Debug.Log(
                $"[CameraFramingPrototype] enemy_scale_preset=RoleMatched companion_height={companionHeight:0.000} " +
                $"commander_height={commanderHeight:0.000}",
                this);
        }

        private float ResolveBaseCompanionReferenceHeight()
        {
            const int BaseCompanionCount = 5;
            float totalHeight = 0.0f;
            for (int index = 0; index < BaseCompanionCount; index++)
                totalHeight += ResolveRenderedSpriteHeight(_companionVisuals[index]);

            return totalHeight / BaseCompanionCount;
        }

        private static float ResolveRenderedSpriteHeight(Transform visual)
        {
            SpriteRenderer[] renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
            float minimumY = float.PositiveInfinity;
            float maximumY = float.NegativeInfinity;

            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer.sprite == null)
                    continue;

                Bounds bounds = renderer.sprite.bounds;
                Vector3 minimum = bounds.min;
                Vector3 maximum = bounds.max;
                IncludeWorldY(renderer.transform, new Vector3(minimum.x, minimum.y, 0.0f), ref minimumY, ref maximumY);
                IncludeWorldY(renderer.transform, new Vector3(minimum.x, maximum.y, 0.0f), ref minimumY, ref maximumY);
                IncludeWorldY(renderer.transform, new Vector3(maximum.x, minimum.y, 0.0f), ref minimumY, ref maximumY);
                IncludeWorldY(renderer.transform, new Vector3(maximum.x, maximum.y, 0.0f), ref minimumY, ref maximumY);
            }

            return float.IsPositiveInfinity(minimumY) ? 0.0f : maximumY - minimumY;
        }

        private static void IncludeWorldY(
            Transform rendererTransform,
            Vector3 localPoint,
            ref float minimumY,
            ref float maximumY)
        {
            float worldY = rendererTransform.TransformPoint(localPoint).y;
            minimumY = Mathf.Min(minimumY, worldY);
            maximumY = Mathf.Max(maximumY, worldY);
        }

        private static float ResolveEnemyScaleMultiplier(EnemyScalePreset preset)
        {
            return preset switch
            {
                EnemyScalePreset.PlusTenPercent => 1.10f,
                EnemyScalePreset.PlusTwentyPercent => 1.20f,
                _ => 1.0f,
            };
        }

        private void ApplyPreset(ComparisonPreset preset)
        {
            _activePreset = preset;
            float commanderVisualMultiplier;
            float companionVisualMultiplier;
            switch (preset)
            {
                case ComparisonPreset.ReferenceMatch:
                    _initialOrthographicSize = ReferenceMatchInitialOrthographicSize;
                    _finalOrthographicSize = FinalOrthographicSize;
                    commanderVisualMultiplier = ReferenceMatchCommanderVisualMultiplier;
                    companionVisualMultiplier = ReferenceMatchCompanionVisualMultiplier;
                    break;
                case ComparisonPreset.WideWorldExpandedVisual:
                    _initialOrthographicSize = WideWorldInitialOrthographicSize;
                    _finalOrthographicSize = WideWorldFinalOrthographicSize;
                    float visualNormalization = WideWorldCommanderVisualTargetSize
                        / (_commanderBaseVisualScale.x * WideWorldCommanderVisualMultiplier);
                    commanderVisualMultiplier = WideWorldCommanderVisualMultiplier * visualNormalization;
                    companionVisualMultiplier = WideWorldCompanionVisualMultiplier * visualNormalization;
                    break;
                default:
                    _initialOrthographicSize = CameraOnlyInitialOrthographicSize;
                    _finalOrthographicSize = FinalOrthographicSize;
                    commanderVisualMultiplier = 1.0f;
                    companionVisualMultiplier = 1.0f;
                    break;
            }

            _commander.transform.localScale = Vector3.one;
            _commanderVisual.localScale = ScaleVisual(_commanderBaseVisualScale, commanderVisualMultiplier);
            for (int index = 0; index < _companions.Length; index++)
            {
                _companions[index].transform.localScale = Vector3.one;
                _companionVisuals[index].localScale = ScaleVisual(
                    _companionBaseVisualScales[index],
                    companionVisualMultiplier);
            }

            ResetCycle();
            Debug.Log(
                $"[CameraFramingPrototype] preset={_activePreset} camera={_initialOrthographicSize:0.00}->{_finalOrthographicSize:0.00} commander_visual_multiplier={commanderVisualMultiplier:0.00} companion_visual_multiplier={companionVisualMultiplier:0.00}",
                this);
        }

        private static Vector3 ScaleVisual(Vector3 authoredScale, float multiplier)
        {
            return new Vector3(
                authoredScale.x * multiplier,
                authoredScale.y * multiplier,
                authoredScale.z);
        }

        private void ResetCycle()
        {
            _cycleElapsed = 0.0f;
            _cameraVelocity = 0.0f;
            _camera.orthographicSize = _initialOrthographicSize;
            SetVisibleCompanionCount(0);
        }

        private void SetVisibleCompanionCount(int count)
        {
            _visibleCompanionCount = Mathf.Clamp(count, 0, CompanionCapacity);

            for (int index = 0; index < _companions.Length; index++)
                _companions[index].SetActive(index < _visibleCompanionCount);

            Debug.Log(
                $"[CameraFramingPrototype] companions={_visibleCompanionCount} target_size={ResolveTargetOrthographicSize(_visibleCompanionCount):0.00}",
                this);
        }

        private float ResolveTargetOrthographicSize(int companionCount)
        {
            float progress = Mathf.Clamp01(companionCount / (float)CompanionCapacity);
            return Mathf.Lerp(_initialOrthographicSize, _finalOrthographicSize, progress);
        }

        private void EnsureGuiStyles()
        {
            if (_headerStyle != null)
                return;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            _countStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.86f, 0.34f, 1.0f) },
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                normal = { textColor = new Color(0.92f, 0.96f, 0.90f, 1.0f) },
            };
        }

    }
}
