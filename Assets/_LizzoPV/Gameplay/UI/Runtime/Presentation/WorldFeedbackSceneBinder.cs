using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.PresentationRuntime
{
    [DefaultExecutionOrder(-850)]
    [DisallowMultipleComponent]
    public sealed class WorldFeedbackSceneBinder : MonoBehaviour
    {
        private const float LowHealthRatio = 0.3f;
        private const float WorldSfxDuplicateCooldownSeconds = 0.08f;
        private const int MaxWorldSfxStartsPerFrame = 3;
        private const float WorldSfxVolumeScale = 0.3f;

        [SerializeField] private RunBootstrap _runBootstrap;
        [SerializeField] private WorldFeedbackProfileSetSO _profiles;
        [SerializeField] private Transform _vfxRoot;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Image _screenOverlay;
        [SerializeField] private UiMotionPlayer _screenMotion;

        private readonly Dictionary<string, bool> _knownCompanions = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly HashSet<string> _activeCompanionIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _removedCompanionIds = new List<string>();
        private readonly WorldAudioConcurrencyGate _audioGate =
            new WorldAudioConcurrencyGate(WorldSfxDuplicateCooldownSeconds, MaxWorldSfxStartsPerFrame);

        private WorldFeedbackRuntimeSink _sink;
        private CompanionRunExternalAdapter _companionRoster;
        private AssetCatalogBundleRuntime _catalogs;
        private int _lastCommanderHp = int.MinValue;
        private bool _lowHealthActive;

        public bool IsBound => _sink != null;
        public WorldFeedbackProfileSetSO Profiles => _profiles;

        private void Awake()
        {
            if (_runBootstrap == null || _profiles == null || _vfxRoot == null || _audioSource == null
                || _screenOverlay == null || _screenMotion == null)
            {
                Debug.LogError("[WorldFeedbackSceneBinder] Authored RunBootstrap, Profile Set, VFX root, AudioSource, Screen Overlay, and Screen Motion are required.", this);
                enabled = false;
                return;
            }

            if (!_profiles.TryValidate(out string issue))
            {
                Debug.LogError($"[WorldFeedbackSceneBinder] Invalid World Feedback Profile Set: {issue}", this);
                enabled = false;
                return;
            }

            _screenOverlay.raycastTarget = false;
            _screenOverlay.gameObject.SetActive(false);
        }

        private void Start() => BindWhenReadyAsync().Forget();

        private async UniTaskVoid BindWhenReadyAsync()
        {
            try
            {
                await UniTask.WaitUntil(
                    () => _runBootstrap != null && _runBootstrap.IsReady,
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_runBootstrap.Services?.WorldFeedback == null)
            {
                Debug.LogError("[WorldFeedbackSceneBinder] RunServices has no WorldFeedback runtime.", this);
                return;
            }

            _catalogs = _runBootstrap.Services.App?.AssetCatalogs;
            if (_catalogs == null)
            {
                Debug.LogError("[WorldFeedbackSceneBinder] App Asset Catalog runtime is unavailable.", this);
                return;
            }

            Bind(_runBootstrap.Services.WorldFeedback.Sink);
            CaptureExistingCompanions(playJoinCue: false);
        }

        private void Update()
        {
            if (!IsBound || _runBootstrap.Services == null)
                return;

            UpdateCommanderFeedback();
        }

        private void OnDestroy()
        {
            _audioGate.Reset();
            Unbind();
        }

        private void Bind(WorldFeedbackRuntimeSink sink)
        {
            Unbind();
            _sink = sink;
            _sink.CombatImpactPresented += OnCombatImpact;
            _sink.StatusPresented += OnStatus;
            _sink.CompanionAttackPresented += OnCompanionAttack;
            _sink.EnemyAttackPresented += OnEnemyAttack;
            _sink.EnemySpawnPresented += OnEnemySpawn;
            _sink.EnemyDeathPresented += OnEnemyDeath;
            _sink.ExperiencePresented += OnExperience;
            _sink.RunOutcomePresented += OnRunOutcome;
            _companionRoster = _runBootstrap.Services.CompanionRuntimeHost.Adapter;
            _companionRoster.RosterChanged += OnCompanionRosterChanged;
        }

        private void Unbind()
        {
            if (_sink != null)
            {
                _sink.CombatImpactPresented -= OnCombatImpact;
                _sink.StatusPresented -= OnStatus;
                _sink.CompanionAttackPresented -= OnCompanionAttack;
                _sink.EnemyAttackPresented -= OnEnemyAttack;
                _sink.EnemySpawnPresented -= OnEnemySpawn;
                _sink.EnemyDeathPresented -= OnEnemyDeath;
                _sink.ExperiencePresented -= OnExperience;
                _sink.RunOutcomePresented -= OnRunOutcome;
                _sink = null;
            }

            if (_companionRoster != null)
            {
                _companionRoster.RosterChanged -= OnCompanionRosterChanged;
                _companionRoster = null;
            }
        }

        private void UpdateCommanderFeedback()
        {
            PlayerController commander = _runBootstrap.Services.Registry.Player;
            if (commander == null)
                return;

            if (_lastCommanderHp != int.MinValue && commander.Hp > _lastCommanderHp)
            {
                SpawnVfx(_profiles.CommanderProfile.HealVfxId, commander.transform.position);
                PlayAudio(_profiles.CommanderProfile.HealSfxId);
            }

            _lastCommanderHp = commander.Hp;
            bool isLow = commander.Hp > 0 && commander.MaxHp > 0
                         && (float)commander.Hp / commander.MaxHp <= LowHealthRatio;
            if (isLow == _lowHealthActive)
                return;

            _lowHealthActive = isLow;
            _screenOverlay.gameObject.SetActive(true);
            ApplyOverlaySprite(_profiles.CommanderProfile.LowHealthOverlaySpriteId);
            TryPlayMotion(isLow
                ? _profiles.CommanderProfile.LowHealthEnterMotionId
                : _profiles.CommanderProfile.LowHealthExitMotionId);
            PlayAudio(isLow
                ? _profiles.CommanderProfile.LowHealthEnterSfxId
                : _profiles.CommanderProfile.LowHealthExitSfxId);
        }

        private void CaptureExistingCompanions(bool playJoinCue)
        {
            CompanionRunSnapshot snapshot = _runBootstrap.Services.CompanionRuntimeHost.Module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index++)
            {
                SquadSnapshot companion = snapshot.Squads[index];
                _knownCompanions[companion.SquadId] = companion.Promoted;
                if (playJoinCue)
                    PresentCompanionLifecycle(companion, promoted: false);
            }
        }

        private void OnCompanionRosterChanged(CompanionRosterCommandKind commandKind)
        {
            UpdateCompanionLifecycleFeedback();
        }

        private void UpdateCompanionLifecycleFeedback()
        {
            CompanionRunSnapshot snapshot = _runBootstrap.Services.CompanionRuntimeHost.Module.CaptureSnapshot();
            _activeCompanionIds.Clear();
            for (int index = 0; index < snapshot.Squads.Count; index++)
            {
                SquadSnapshot companion = snapshot.Squads[index];
                _activeCompanionIds.Add(companion.SquadId);
                if (!_knownCompanions.TryGetValue(companion.SquadId, out bool wasPromoted))
                {
                    _knownCompanions.Add(companion.SquadId, companion.Promoted);
                    PresentCompanionLifecycle(companion, promoted: false);
                }
                else if (!wasPromoted && companion.Promoted)
                {
                    _knownCompanions[companion.SquadId] = true;
                    PresentCompanionLifecycle(companion, promoted: true);
                }
            }

            _removedCompanionIds.Clear();
            foreach (KeyValuePair<string, bool> pair in _knownCompanions)
            {
                if (!_activeCompanionIds.Contains(pair.Key))
                    _removedCompanionIds.Add(pair.Key);
            }

            for (int index = 0; index < _removedCompanionIds.Count; index++)
                _knownCompanions.Remove(_removedCompanionIds[index]);
        }

        private void PresentCompanionLifecycle(SquadSnapshot companion, bool promoted)
        {
            CompanionLifecycleFeedbackProfileSO profile = null;
            IReadOnlyList<CompanionLifecycleFeedbackBinding> bindings = _profiles.CompanionLifecycleBindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                if (bindings[index].CompanionId.Value == companion.CompanionId)
                {
                    profile = bindings[index].Profile;
                    break;
                }
            }

            if (profile == null)
                return;

            Vector3 position = new Vector3(companion.FormationAnchor.X, companion.FormationAnchor.Y, 0.0f);
            SpawnVfx(promoted ? profile.PromoteVfxId : profile.JoinVfxId, position);
            PlayAudio(promoted ? profile.PromoteSfxId : profile.JoinSfxId);
        }

        private void ApplyEnemyWorldUi(int instanceId)
        {
            foreach (MonsterController enemy in _runBootstrap.Services.Registry.Enemies)
            {
                if (enemy == null || enemy.GetInstanceID() != instanceId)
                    continue;

                Transform background = enemy.transform.Find("UI/HpBarAnchor/P0_HPBar/Back");
                Transform fill = enemy.transform.Find("UI/HpBarAnchor/P0_HPBar/Fill");
                if (background != null && TrySprite(_profiles.WorldUiProfile.EnemyHealthBarBackSpriteId, out Sprite backgroundSprite))
                {
                    SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                        renderer.sprite = backgroundSprite;
                }

                if (fill != null && TrySprite(_profiles.WorldUiProfile.EnemyHealthBarFillSpriteId, out Sprite fillSprite))
                {
                    SpriteRenderer renderer = fill.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                        renderer.sprite = fillSprite;
                }
                return;
            }
        }

        private void OnCombatImpact(CombatImpactPresentation presentation, CombatImpactFeedbackProfileSO profile)
        {
            PlayAudio(profile.ImpactSfxId);
            if (!profile.ScreenFeedbackMotionId.IsNone)
            {
                _screenOverlay.gameObject.SetActive(true);
                ApplyOverlaySprite(profile.ScreenOverlaySpriteId);
                TryPlayMotion(profile.ScreenFeedbackMotionId);
            }

            float seconds = profile.HitStopGrade switch
            {
                HitStopGrade.Light => 0.02f,
                HitStopGrade.Medium => 0.04f,
                HitStopGrade.Heavy => 0.08f,
                _ => 0f,
            };
            if (seconds > 0f)
                HitStop.Request(seconds, $"world_feedback:{presentation.ImpactKind.Value}");
        }

        private void OnStatus(StatusFeedbackPresentation presentation, StatusFeedbackProfileSO profile)
        {
            switch (presentation.EventKind)
            {
                case StatusFeedbackEventKind.Applied:
                    SpawnVfx(profile.ApplyVfxId, presentation.Position);
                    PlayAudio(profile.ApplySfxId);
                    break;
                case StatusFeedbackEventKind.Ended:
                    SpawnVfx(profile.EndVfxId, presentation.Position);
                    PlayAudio(profile.EndSfxId);
                    break;
                case StatusFeedbackEventKind.Reaction:
                    PresentStatusReaction(profile, presentation);
                    break;
            }
        }

        private void PresentStatusReaction(StatusFeedbackProfileSO profile, StatusFeedbackPresentation presentation)
        {
            IReadOnlyList<StatusReactionFeedback> reactions = profile.Reactions;
            for (int index = 0; index < reactions.Count; index++)
            {
                StatusReactionFeedback reaction = reactions[index];
                if (reaction.ReactionKind != presentation.ReactionKind)
                    continue;
                SpawnVfx(reaction.ReactionVfxId, presentation.Position);
                PlayAudio(reaction.ReactionSfxId);
                return;
            }
        }

        private void OnCompanionAttack(CompanionAttackPresentation presentation, AttackFeedbackProfileSO profile)
        {
            switch (presentation.EventKind)
            {
                case CompanionAttackFeedbackEventKind.Cast:
                    SpawnVfx(profile.CastFeedback.CastVfxId, presentation.Position);
                    PlayAudio(profile.CastFeedback.CastSfxId);
                    break;
                case CompanionAttackFeedbackEventKind.Release:
                    SpawnVfx(profile.CastFeedback.ReleaseVfxId, presentation.Position);
                    PlayAudio(profile.CastFeedback.ReleaseSfxId);
                    break;
                case CompanionAttackFeedbackEventKind.Impact:
                    SpawnVfx(profile.ImpactFeedback.ImpactVfxId, presentation.Position);
                    break;
                case CompanionAttackFeedbackEventKind.LifetimeEnd:
                    SpawnVfx(profile.ProjectileFeedback.LifetimeEndVfxId, presentation.Position);
                    PlayAudio(profile.ProjectileFeedback.LifetimeEndSfxId);
                    break;
            }
        }

        private void OnEnemyAttack(EnemyAttackPresentation presentation, EnemyAttackFeedbackProfileSO profile)
        {
            switch (presentation.EventKind)
            {
                case EnemyAttackFeedbackEventKind.Windup:
                    SpawnVfx(profile.WindupFeedback.WindupVfxId, presentation.Position);
                    PlayAudio(profile.WindupFeedback.WindupSfxId);
                    break;
                case EnemyAttackFeedbackEventKind.Impact:
                    SpawnVfx(profile.EnemyImpactFeedback.ImpactVfxId, presentation.Position);
                    break;
            }
        }

        private void OnEnemySpawn(EnemySpawnPresentation presentation, EnemySpawnFeedbackProfileSO profile)
        {
            ApplyEnemyWorldUi(presentation.EnemyInstanceId);
            SpawnVfx(profile.SpawnMarkerVfxId, presentation.Position);
            SpawnVfx(profile.SpawnVfxId, presentation.Position);
            PlayAudio(profile.SpawnSfxId);
        }

        private void OnEnemyDeath(EnemyDeathPresentation presentation, EnemyDeathFeedbackProfileSO profile)
        {
            SpawnVfx(profile.DeathVfxId, presentation.Position);
            PlayAudio(profile.DeathSfxId);
        }

        private void OnExperience(ExperienceFeedbackPresentation presentation, ExperienceOrbFeedbackProfileSO profile)
        {
            if (presentation.EventKind == ExperienceFeedbackEventKind.Spawn)
            {
                ApplyExperienceSprite(presentation.OrbInstanceId, profile.OrbSpriteId);
                SpawnVfx(profile.SpawnBurstVfxId, presentation.Position);
                return;
            }

            SpawnVfx(profile.AbsorbBurstVfxId, presentation.Position);
            PlayAudio(presentation.EventKind == ExperienceFeedbackEventKind.AbsorbComplete
                && !profile.AbsorbCompleteSfxId.IsNone
                    ? profile.AbsorbCompleteSfxId
                    : profile.AbsorbTickSfxId);
        }

        private void OnRunOutcome(RunOutcomeFeedbackPresentation presentation, RunOutcomeWorldFeedbackProfileSO profile)
        {
            _screenOverlay.gameObject.SetActive(true);
            switch (presentation.OutcomeKind)
            {
                case RunOutcomeFeedbackKind.Victory:
                    SpawnVfx(profile.VictoryWorldFeedback.RunCleanupVfxId, Vector3.zero);
                    TryPlayMotion(profile.VictoryWorldFeedback.VictoryTransitionMotionId);
                    break;
                case RunOutcomeFeedbackKind.Failure:
                    PlayerController commander = _runBootstrap.Services.Registry.Player;
                    SpawnVfx(profile.FailureWorldFeedback.CommanderDeathVfxId,
                        commander == null ? Vector3.zero : commander.transform.position);
                    PlayAudio(profile.FailureWorldFeedback.CommanderDeathSfxId);
                    TryPlayMotion(profile.FailureWorldFeedback.WorldDimMotionId);
                    break;
                case RunOutcomeFeedbackKind.Abandoned:
                    TryPlayMotion(profile.AbandonedWorldFeedback.WorldDimMotionId);
                    break;
            }
        }

        private void ApplyExperienceSprite(int instanceId, SpriteAssetId spriteId)
        {
            if (!TrySprite(spriteId, out Sprite sprite))
                return;

            foreach (GemController gem in _runBootstrap.Services.Registry.Gems)
            {
                if (gem == null || gem.GetInstanceID() != instanceId)
                    continue;
                SpriteRenderer renderer = gem.GetComponentInChildren<SpriteRenderer>(true);
                if (renderer != null)
                    renderer.sprite = sprite;
                return;
            }
        }

        private void SpawnVfx(VfxAssetId id, Vector3 position)
        {
            if (id.IsNone || _catalogs == null || !_catalogs.VfxCatalog.TryGet(id, out GameObject prefab))
                return;

            IPrefabFactory factory = _runBootstrap.Services?.Factory;
            if (factory == null)
            {
                Debug.LogError("[WorldFeedbackSceneBinder] PrefabFactory is unavailable for pooled VFX.", this);
                return;
            }

            GameObject instance = factory.Rent(prefab, $"WorldFeedback:{id.Value}:{prefab.GetInstanceID()}", _vfxRoot);
            if (instance == null)
                return;

            instance.name = $"WorldFeedback_{prefab.name}";
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            VfxWrapperInstance wrapper = instance.GetComponent<VfxWrapperInstance>();
            if (wrapper != null)
            {
                wrapper.ActivatePooled(factory);
                return;
            }

            Debug.LogError($"[WorldFeedbackSceneBinder] VFX Asset ID {id.Value} is missing {nameof(VfxWrapperInstance)}.", instance);
            factory.Release(instance);
        }

        private void PlayAudio(AudioAssetId id)
        {
            if (id.IsNone || _catalogs == null || !_catalogs.AudioCatalog.TryGet(id, out AudioClip clip))
                return;
            if (!_audioGate.TryAcquire(id, Time.realtimeSinceStartup, Time.frameCount))
                return;

            _audioSource.PlayOneShot(clip, WorldSfxVolumeScale);
        }

        private void ApplyOverlaySprite(SpriteAssetId id)
        {
            if (TrySprite(id, out Sprite sprite))
                _screenOverlay.sprite = sprite;
        }

        private bool TrySprite(SpriteAssetId id, out Sprite sprite)
        {
            sprite = null;
            return !id.IsNone && _catalogs != null && _catalogs.SpriteCatalog.TryGet(id, out sprite);
        }

        private void TryPlayMotion(MotionAssetId id)
        {
            if (id.IsNone || _catalogs == null || !_catalogs.MotionCatalog.TryGet(id, out AnimationClip clip))
                return;
            if (!_screenMotion.TryPlay(clip, out string issue))
                Debug.LogError($"[WorldFeedbackSceneBinder] {issue}", this);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            RunBootstrap runBootstrap,
            WorldFeedbackProfileSetSO profiles,
            Transform vfxRoot,
            AudioSource audioSource,
            Image screenOverlay,
            UiMotionPlayer screenMotion)
        {
            _runBootstrap = runBootstrap;
            _profiles = profiles;
            _vfxRoot = vfxRoot;
            _audioSource = audioSource;
            _screenOverlay = screenOverlay;
            _screenMotion = screenMotion;
        }
#endif
    }
}
