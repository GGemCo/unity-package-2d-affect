using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 캐릭터(Actor) 단위의 어펙트(Affect) 관리 컴포넌트.
    /// 중앙 스케줄러(Update)에서 Duration/Tick을 갱신하며, Player/Monster/NPC에 공통 적용할 수 있다.
    /// </summary>
    /// <remarks>
    /// - 내부적으로 "런타임 인스턴스 ID(runtimeId)"로 활성 인스턴스를 관리한다.
    /// - 동일 그룹(groupId) 단일성, 동일 UID 재적용(stack/refresh) 정책을 지원한다.
    /// - 컬렉션 변경(삭제) 시에는 삭제 큐를 사용하여 순회 중 예외를 방지한다.
    /// </remarks>
    public sealed class AffectComponent : MonoBehaviour, IGameInitializable, IGameActivatable, IGameDeinitializable
    {
        [SerializeField] private MonoBehaviour targetBehaviour;

        private IAffectTarget _target;
        private IAffectDefinitionRepository _affectRepo;
        private IStatusDefinitionRepository _statusRepo;
        private IAffectVfxService _vfx;
        private IAffectOutlineService _outline;
        private IAffectAnimationService _animation;

        private readonly Dictionary<int, AffectInstance> _byRuntimeId = new();
        private readonly Dictionary<int, List<int>> _runtimeIdsByAffectUid = new();
        private readonly Dictionary<string, int> _groupIndex = new(StringComparer.Ordinal);

        private int _nextRuntimeId = 1;
        private static readonly List<AffectComponent> SRegisteredComponents = new(64);
        private static readonly List<AffectComponent> SComponentSnapshot = new(64);

        /// <summary>
        /// 어펙트 구성(추가/삭제/스택/리프레시 등)이 변경되었을 때 발생한다.
        /// UI 등 외부 시스템은 이 이벤트를 통해 "구조 변경"을 즉시 반영할 수 있다.
        /// </summary>
        /// <remarks>
        /// RemainingTime 같은 값은 Update에서 지속적으로 변하므로, 값 동기화는 별도 주기가 필요하다.
        /// </remarks>
        public event Action Changed;

        private bool _changedDirty;

        /// <summary>
        /// "구조 변경" 이벤트(Changed) 발생이 필요함을 표시한다.
        /// </summary>
        private void MarkChanged()
        {
            _changedDirty = true;
        }

        /// <summary>
        /// 변경 플래그가 켜져 있으면 Changed 이벤트를 1회 발생시키고 플래그를 초기화한다.
        /// </summary>
        private void FlushChangedIfNeeded()
        {
            if (!_changedDirty) return;
            _changedDirty = false;
            Changed?.Invoke();
        }

        private readonly StatModifierExecutor _statExecutor = new();
        private readonly DamageExecutor _damageExecutor = new();
        private readonly HealExecutor _healExecutor = new();
        private readonly StateExecutor _stateExecutor = new();
        private readonly CrowdControlExecutor _crowdControlExecutor = new();
        private readonly ApplyAffectToTargetExecutor _applyAffectExecutor = new();
        private readonly FormulaVariableModifierExecutor _formulaVariableExecutor = new();

        private bool _isInitialized;
        private bool _isActivated;

        /// <summary>
        /// 캐릭터 단위 컴포넌트이므로 기본 초기화 단계에서 실행합니다.
        /// </summary>
        public int InitializeOrder => 0;

        /// <summary>
        /// 현재 활성 어펙트가 1개 이상 존재하는지 여부.
        /// </summary>
        private bool HasAny => _byRuntimeId.Count > 0;

        /// <summary>
        /// 타겟(IAffectTarget)을 캐싱하고, 실제 런타임 서비스 바인딩은 명시적 Initialize 단계로 미룹니다.
        /// </summary>
        /// <remarks>
        /// - targetBehaviour가 지정되어 있으면 우선 사용한다.
        /// - 미지정이면 동일 GameObject에서 IAffectTarget을 자동 탐색한다.
        /// - 타겟을 찾지 못하면 컴포넌트를 비활성화한다.
        /// </remarks>
        private void Awake()
        {
            ResolveTarget();
            RegisterComponent(this);
        }

        /// <summary>
        /// 컴포넌트가 파괴될 때 source 사망 알림 레지스트리에서 제거합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnregisterComponent(this);
        }

        /// <summary>
        /// 레거시 테스트 씬처럼 별도 부트스트랩이 없는 환경에서 최소 동작을 보장합니다.
        /// </summary>
        private void Start()
        {
            if (_isInitialized)
                return;

            Initialize(null);
            Activate(null);
        }

        /// <summary>
        /// Affect 런타임 저장소와 표시 서비스를 명시적으로 바인딩합니다.
        /// </summary>
        /// <param name="context">초기화 컨텍스트입니다. Affect는 전역 런타임 저장소를 사용하므로 null을 허용합니다.</param>
        public void Initialize(GameInitContext context)
        {
            if (_isInitialized)
                return;

            if (!ResolveTarget())
            {
                enabled = false;
                return;
            }

            BindRuntimeServices();
            _isInitialized = true;
            enabled = _isActivated && HasAny;
        }

        /// <summary>
        /// 모든 캐릭터 초기화가 끝난 뒤 Affect 시간 갱신을 허용합니다.
        /// </summary>
        /// <param name="context">초기화 컨텍스트입니다.</param>
        public void Activate(GameInitContext context)
        {
            if (!_isInitialized)
                Initialize(context);

            if (!_isInitialized)
                return;

            _isActivated = true;
            enabled = HasAny;
        }

        /// <summary>
        /// Affect 갱신을 중지하고 다음 활성화 전까지 Update를 차단합니다.
        /// </summary>
        public void Deinitialize()
        {
            _isActivated = false;
            enabled = false;
        }

        /// <summary>
        /// 인스펙터 지정 대상 또는 동일 GameObject의 IAffectTarget 구현체를 캐싱합니다.
        /// </summary>
        /// <returns>유효한 타겟을 찾았으면 true를 반환합니다.</returns>
        private bool ResolveTarget()
        {
            if (_target != null)
                return true;

            if (targetBehaviour != null)
                _target = targetBehaviour as IAffectTarget;

            if (_target == null)
                _target = GetComponent<IAffectTarget>();

            if (_target != null)
                return true;

            Debug.LogError($"[AffectComponent] IAffectTarget not found. go={name}");
            return false;
        }

        /// <summary>
        /// AffectRuntime에 등록된 저장소와 표시 서비스를 현재 컴포넌트에 연결합니다.
        /// </summary>
        private void BindRuntimeServices()
        {
            _affectRepo = AffectRuntime.AffectRepository;
            _statusRepo = AffectRuntime.StatusRepository;
            _vfx = AffectRuntime.VfxService;
            _outline = AffectRuntime.OutlineService;
            _animation = AffectRuntime.AnimationService;
        }

        /// <summary>
        /// Source 사망 알림을 받을 수 있도록 AffectComponent를 전역 레지스트리에 등록합니다.
        /// </summary>
        /// <param name="component">등록할 AffectComponent입니다.</param>
        private static void RegisterComponent(AffectComponent component)
        {
            if (component == null || SRegisteredComponents.Contains(component))
            {
                return;
            }

            SRegisteredComponents.Add(component);
        }

        /// <summary>
        /// 파괴된 AffectComponent가 source 사망 알림 순회 대상에 남지 않도록 제거합니다.
        /// </summary>
        /// <param name="component">제거할 AffectComponent입니다.</param>
        private static void UnregisterComponent(AffectComponent component)
        {
            if (component == null)
            {
                return;
            }

            SRegisteredComponents.Remove(component);
        }

        /// <summary>
        /// 프레임마다 활성 어펙트의 시간/틱을 갱신하고, 만료된 인스턴스를 정리한다.
        /// </summary>
        /// <remarks>
        /// - 타겟이 없거나 사망 상태면 즉시 비활성화한다.
        /// - tickInterval 단위로 OnTick 페이즈를 실행한다.
        /// - 만료 처리는 삭제 큐를 통해 일괄 수행한다.
        /// </remarks>
        private void Update()
        {
            if (!_isInitialized || !_isActivated)
            {
                enabled = false;
                return;
            }

            if (!HasAny || _target == null || !_target.IsAlive)
            {
                enabled = false;
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // NOTE:
            // Update 중(OnTick/OnExpire 등 실행 중) 외부에서 RemoveAll/RemoveAffect/ApplyAffect 등이 호출될 수 있다.
            // (예: Tick 데미지 -> 캐릭터 사망 -> Core에서 AffectRuntimeBridge.RemoveAll 호출)
            // Dictionary는 열거 중 수정되면 InvalidOperationException이 발생하므로,
            // 런타임Id 스냅샷을 만든 뒤 TryGetValue 기반으로 안전하게 진행한다.

            // 삭제 큐(만료 정리)
            SPendingRemoveIds.Clear();
            SPendingRemoveReasons.Clear();

            // 순회 스냅샷(재사용)
            SRuntimeIdSnapshot.Clear();
            foreach (var kv in _byRuntimeId)
                SRuntimeIdSnapshot.Add(kv.Key);

            for (int s = 0; s < SRuntimeIdSnapshot.Count; s++)
            {
                if (_target == null || !_target.IsAlive)
                    break;

                int runtimeId = SRuntimeIdSnapshot[s];
                if (!_byRuntimeId.TryGetValue(runtimeId, out var instance) || instance == null)
                    continue;

                if (ShouldRemoveBySourceLifePolicy(instance))
                {
                    SPendingRemoveIds.Add(runtimeId);
                    SPendingRemoveReasons.Add(AffectExpireReason.SourceDead);
                    continue;
                }

                // 시간 감소
                instance.UpdateTime(dt);

                // Tick
                if (instance.Definition.HasTick)
                {
                    instance.AccumulateTick(dt);
                    while (instance.TryConsumeTick(instance.Definition.tickInterval))
                    {
                        ExecutePhase(AffectPhase.OnTick, instance);
                    }
                }

                if (instance.IsExpired)
                {
                    SPendingRemoveIds.Add(runtimeId);
                    SPendingRemoveReasons.Add(AffectExpireReason.NaturalExpire);
                }
            }

            // 만료 정리
            for (int i = 0; i < SPendingRemoveIds.Count; i++)
                RemoveByRuntimeId(SPendingRemoveIds[i], SPendingRemoveReasons[i]);

            FlushChangedIfNeeded();

            // 활성 인스턴스가 남아있을 때만 계속 Update
            enabled = HasAny;
        }

        /// <summary>
        /// 순회 중 삭제가 필요한 runtimeId를 임시로 담는 정적 리스트(재사용).
        /// </summary>
        private static readonly List<int> SPendingRemoveIds = new(32);

        /// <summary>
        /// <see cref="SPendingRemoveIds"/>와 동일 인덱스로 관리되는 종료 사유 목록입니다.
        /// </summary>
        private static readonly List<AffectExpireReason> SPendingRemoveReasons = new(32);

        /// <summary>
        /// Update/NotifyHit 등에서 안전한 순회를 위해 사용하는 runtimeId 스냅샷 버퍼.
        /// (Dictionary 열거 중 수정 예외 방지)
        /// </summary>
        private static readonly List<int> SRuntimeIdSnapshot = new(64);

        // ----------------------
        // Public API
        // ----------------------

        /// <summary>
        /// 지정한 어펙트 UID의 인스턴스가 현재 1개 이상 활성화되어 있는지 확인한다.
        /// </summary>
        /// <param name="affectUid">조회할 어펙트 정의 UID.</param>
        /// <returns>활성 인스턴스가 있으면 true, 없으면 false.</returns>
        public bool HasAffect(int affectUid)
        {
            return _runtimeIdsByAffectUid.TryGetValue(affectUid, out var ids) && ids.Count > 0;
        }

        /// <summary>
        /// 지정한 Affect UID의 첫 번째 활성 런타임 인스턴스를 조회합니다.
        /// </summary>
        /// <param name="affectUid">조회할 Affect 정의 UID입니다.</param>
        /// <param name="instance">조회된 활성 Affect 인스턴스입니다.</param>
        /// <returns>유효한 활성 인스턴스를 찾으면 <see langword="true"/>입니다.</returns>
        public bool TryGetActiveInstance(int affectUid, out AffectInstance instance)
        {
            instance = null;
            if (!TryGetFirstRuntimeId(affectUid, out int runtimeId))
                return false;

            return _byRuntimeId.TryGetValue(runtimeId, out instance) && instance != null;
        }

        /// <summary>
        /// 지정한 Affect에 남아 있는 OnTick Damage를 합산하여 대상에게 즉시 적용합니다.
        /// </summary>
        /// <param name="affectUid">남은 Tick 피해를 정산할 Affect 정의 UID입니다.</param>
        /// <param name="remainingTickCount">정산 대상으로 계산된 남은 Tick 횟수입니다.</param>
        /// <param name="appliedDamage">DamageReceiver에 전달한 합산 피해량입니다.</param>
        /// <returns>하나 이상의 OnTick Damage Modifier를 합산 적용했으면 <see langword="true"/>입니다.</returns>
        /// <remarks>
        /// 이 메서드는 피해만 즉시 정산하며 Affect를 제거하지 않습니다.
        /// 호출자는 정산 성공 여부와 무관하게 프로젝트 규칙에 따라 Affect 제거 시점을 결정해야 합니다.
        /// </remarks>
        public bool TryApplyRemainingTickDamage(
            int affectUid,
            out int remainingTickCount,
            out float appliedDamage)
        {
            remainingTickCount = 0;
            appliedDamage = 0f;

            if (_target == null || _affectRepo == null || !TryGetActiveInstance(affectUid, out AffectInstance instance))
                return false;

            AffectDefinition definition = instance.Definition;
            if (definition == null || definition.tickInterval <= 0f)
                return false;

            remainingTickCount = instance.GetRemainingTickCount(definition.tickInterval);
            if (remainingTickCount <= 0)
                return false;

            IReadOnlyList<AffectModifierDefinition> modifiers = _affectRepo.GetModifiers(affectUid);
            bool applied = false;

            for (int i = 0; i < modifiers.Count; i++)
            {
                AffectModifierDefinition modifier = modifiers[i];
                if (modifier == null ||
                    modifier.kind != ModifierKind.Damage ||
                    modifier.phase != AffectPhase.OnTick)
                {
                    continue;
                }

                float modifierDamage = _damageExecutor.ExecuteRemainingTicksImmediately(
                    _target,
                    instance,
                    modifier,
                    remainingTickCount,
                    _affectRepo,
                    _statusRepo);
                if (modifierDamage <= 0f)
                    continue;

                appliedDamage += modifierDamage;
                applied = true;
            }

            return applied;
        }

        /// <summary>
        /// 현재 활성화된 어펙트 인스턴스를 <paramref name="buffer"/>에 채운다.
        /// UI 등 외부 시스템이 현재 상태를 스냅샷으로 가져오기 위한 용도이다.
        /// </summary>
        /// <param name="buffer">활성 인스턴스를 받을 리스트(기존 내용은 Clear된다).</param>
        public void CollectActiveInstances(List<AffectInstance> buffer)
        {
            if (buffer == null) return;
            buffer.Clear();

            // UI가 LateUpdate/Update 타이밍에 호출할 수 있어, 안전하게 스냅샷 기반으로 수집한다.
            SRuntimeIdSnapshot.Clear();
            foreach (var kv in _byRuntimeId)
                SRuntimeIdSnapshot.Add(kv.Key);

            for (int i = 0; i < SRuntimeIdSnapshot.Count; i++)
            {
                if (_byRuntimeId.TryGetValue(SRuntimeIdSnapshot[i], out var instance) && instance != null)
                    buffer.Add(instance);
            }
        }

        /// <summary>
        /// 공격자(이 컴포넌트를 보유한 대상)가 타격에 성공했음을 알린다.
        /// - 공격자에게 활성화된 Affect 중, Modifier Phase가 OnHit인 항목만 실행된다.
        /// - 예: PoisonCoating(버프) -> OnHit 시 피격자에게 POISON_DOT 부여
        /// </summary>
        /// <param name="hitTargetGo">피격자 GameObject.</param>
        public void NotifyHit(GameObject hitTargetGo)
        {
            if (hitTargetGo == null) return;
            if (!HasAny || _target == null || !_target.IsAlive) return;

            var hitTarget = hitTargetGo.GetComponent<IAffectTarget>();
            if (hitTarget == null) return;

            // OnHit 실행 도중에도 구조 변경(RemoveAll 등)이 발생할 수 있으므로 스냅샷 기반으로 순회한다.
            SRuntimeIdSnapshot.Clear();
            foreach (var kv in _byRuntimeId)
                SRuntimeIdSnapshot.Add(kv.Key);

            for (int i = 0; i < SRuntimeIdSnapshot.Count; i++)
            {
                if (_target == null || !_target.IsAlive) break;
                if (!_byRuntimeId.TryGetValue(SRuntimeIdSnapshot[i], out var instance) || instance == null) continue;
                ExecuteHitPhase(instance, hitTarget);
            }
        }

        /// <summary>
        /// 특정 인스턴스의 OnHit 페이즈를 실행한다.
        /// </summary>
        private void ExecuteHitPhase(AffectInstance instance, IAffectTarget hitTarget)
        {
            if (instance == null || hitTarget == null) return;

            var mods = _affectRepo.GetModifiers(instance.Definition.uid);
            for (int i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                if (mod == null || mod.phase != AffectPhase.OnHit) continue;

                switch (mod.kind)
                {
                    case ModifierKind.ApplyAffectToTarget:
                        _applyAffectExecutor.ExecuteOnHit(_target, hitTarget, instance, mod, _affectRepo, _statusRepo);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 어펙트를 적용한다.
        /// 그룹 단일성/스택 정책에 따라 기존 인스턴스를 대체하거나 갱신할 수 있다.
        /// </summary>
        /// <param name="affectUid">적용할 어펙트 정의 UID.</param>
        /// <param name="context">적용 컨텍스트(지속시간 오버라이드 등). null이면 기본 컨텍스트를 생성한다.</param>
        /// <remarks>
        /// - 그룹 단일성: groupId가 None이 아니면 같은 그룹의 기존 인스턴스 1개를 제거 후 적용한다.
        /// - 스택 정책: Independent가 아니고 동일 UID가 이미 있으면 신규 생성 대신 재적용 정책을 수행한다.
        /// - OnApply 페이즈 실행 후 Changed 이벤트를 발생시킨다.
        /// </remarks>
        public void ApplyAffect(int affectUid, AffectApplyContext context = null)
        {
            context ??= new AffectApplyContext();

            if (!_isInitialized)
                Initialize(null);
            if (!_isActivated)
                Activate(null);

            if (_target == null || _affectRepo == null) return;
            RegisterComponent(this);
            if (!_affectRepo.TryGetAffect(affectUid, out var def) || def == null)
            {
                Debug.LogError($"[AffectComponent] AffectDefinition not found. uid={affectUid}");
                return;
            }

            // 1) 동일 UID 처리(스택 정책)
            // 같은 Affect를 다시 실행한 경우에는 그룹 교체보다 재적용 정책을 먼저 평가합니다.
            // StackPolicy.None이면 세션형 BASE_HP/BASE_HP_TEMP 보정처럼 중복 누적 없이 기존 효과를 유지합니다.
            if (def.stackPolicy != StackPolicy.Independent && TryGetFirstRuntimeId(affectUid, out var existingId))
            {
                HandleReapply(def, existingId, context);
                return;
            }

            // 2) 그룹 단일성
            if (!def.IsNoneGroup && _groupIndex.TryGetValue(def.groupId ?? string.Empty, out var existingRuntimeId))
            {
                RemoveByRuntimeId(existingRuntimeId, AffectExpireReason.ReplacedByGroup);
            }

            // 3) 신규 인스턴스
            float duration = ResolveApplyDuration(def, context);
            var instance = new AffectInstance(def, context, duration);

            int runtimeId = _nextRuntimeId++;
            _byRuntimeId[runtimeId] = instance;
            AddIndex(affectUid, runtimeId);
            if (!def.IsNoneGroup)
                _groupIndex[def.groupId ?? string.Empty] = runtimeId;

            // 스킬 취소 과정에서 대기 애니메이션으로 복귀할 수 있으므로,
            // Affect의 OnApply 상태 및 애니메이션보다 먼저 취소하여 최종 연출이 Affect 기준으로 유지되게 합니다.
            RequestCancelRunningSkillOnApply(def);
            if (!IsRegisteredInstance(runtimeId, instance))
            {
                // 스킬 취소 콜백이 Affect를 제거한 경우 제거된 인스턴스의 OnApply를 실행하지 않습니다.
                return;
            }

            // 4) OnApply 실행
            ExecutePhase(AffectPhase.OnApply, instance);
            ExecuteAnimationOnApply(instance);

            MarkChanged();
            FlushChangedIfNeeded();

            enabled = true;
            AffectTimerUiPresenter.TryEnsure(this);
        }

        /// <summary>
        /// 지정한 Source에서 발생한 어펙트를 모든 활성 AffectComponent에서 Source 사망 사유로 제거합니다.
        /// </summary>
        /// <param name="source">사망한 Source GameObject입니다.</param>
        /// <param name="affectUid">0보다 크면 해당 UID만 제거하고, 0이면 Source 사망 정책 어펙트를 모두 제거합니다.</param>
        public static void RemoveBySource(GameObject source, int affectUid = 0)
        {
            if (source == null)
            {
                return;
            }

            SComponentSnapshot.Clear();
            for (int i = 0; i < SRegisteredComponents.Count; i++)
            {
                AffectComponent component = SRegisteredComponents[i];
                if (component != null)
                {
                    SComponentSnapshot.Add(component);
                }
            }

            for (int i = 0; i < SComponentSnapshot.Count; i++)
            {
                SComponentSnapshot[i].RemoveInstancesBySource(source, affectUid);
            }
        }

        /// <summary>
        /// 지정한 어펙트 UID의 "첫 번째" 활성 인스턴스를 제거한다.
        /// </summary>
        /// <param name="affectUid">제거할 어펙트 UID.</param>
        public void RemoveAffect(int affectUid)
        {
            if (!TryGetFirstRuntimeId(affectUid, out var runtimeId))
                return;

            RemoveByRuntimeId(runtimeId, AffectExpireReason.ManualRemove);
            enabled = HasAny;
            FlushChangedIfNeeded();
        }

        /// <summary>
        /// 조건에 맞는 어펙트를 해제(Dispel)한다.
        /// </summary>
        /// <param name="query">해제 조건(매칭 로직/최대 제거 개수 등)을 포함한 쿼리.</param>
        /// <returns>제거된 인스턴스 개수.</returns>
        /// <remarks>
        /// 내부 순회 중 컬렉션 변경을 피하기 위해 삭제 큐에 담아 일괄 제거한다.
        /// </remarks>
        public int Dispel(DispelQuery query)
        {
            if (query == null) return 0;

            int removed = 0;
            SPendingRemoveIds.Clear();

            foreach (var kv in _byRuntimeId)
            {
                if (removed >= query.MaxRemoveCount) break;
                var def = kv.Value.Definition;
                if (!query.Match(def)) continue;

                SPendingRemoveIds.Add(kv.Key);
                removed++;
            }

            for (int i = 0; i < SPendingRemoveIds.Count; i++)
                RemoveByRuntimeId(SPendingRemoveIds[i], AffectExpireReason.Dispel);

            enabled = HasAny;
            FlushChangedIfNeeded();
            return removed;
        }

        /// <summary>
        /// 활성화된 모든 어펙트 인스턴스를 제거한다.
        /// </summary>
        public void RemoveAll()
        {
            SPendingRemoveIds.Clear();
            foreach (var kv in _byRuntimeId)
                SPendingRemoveIds.Add(kv.Key);

            for (int i = 0; i < SPendingRemoveIds.Count; i++)
                RemoveByRuntimeId(SPendingRemoveIds[i], AffectExpireReason.RemoveAll);

            enabled = false;
            FlushChangedIfNeeded();
        }

        /// <summary>
        /// 지정한 Source에서 발생했고 Source 사망 정책을 가진 어펙트 인스턴스를 제거합니다.
        /// </summary>
        /// <param name="source">사망한 Source GameObject입니다.</param>
        /// <param name="affectUid">0보다 크면 해당 UID만 제거합니다.</param>
        private void RemoveInstancesBySource(GameObject source, int affectUid)
        {
            if (source == null || !HasAny)
            {
                return;
            }

            SPendingRemoveIds.Clear();
            foreach (var pair in _byRuntimeId)
            {
                AffectInstance instance = pair.Value;
                if (!ShouldRemoveByDeadSourceNotification(instance, source, affectUid))
                {
                    continue;
                }

                SPendingRemoveIds.Add(pair.Key);
            }

            for (int i = 0; i < SPendingRemoveIds.Count; i++)
            {
                RemoveByRuntimeId(SPendingRemoveIds[i], AffectExpireReason.SourceDead);
            }

            enabled = HasAny;
            FlushChangedIfNeeded();
        }

        // ----------------------
        // Internal
        // ----------------------

        /// <summary>
        /// Affect 정의와 적용 컨텍스트를 기준으로 최종 지속시간을 계산합니다.
        /// </summary>
        /// <param name="def">적용할 Affect 정의입니다.</param>
        /// <param name="ctx">적용 시 전달된 런타임 컨텍스트입니다.</param>
        /// <returns>보너스 지속시간까지 반영한 최종 지속시간입니다.</returns>
        /// <remarks>
        /// Session 정책도 표시/디버그용 지속시간 값은 계산하지만,
        /// 실제 자연 만료 여부는 <see cref="AffectInstance.IsExpired"/>에서 정책별로 판정합니다.
        /// </remarks>
        private static float ResolveApplyDuration(AffectDefinition def, AffectApplyContext ctx)
        {
            float baseDuration = ctx != null && ctx.DurationOverride > 0f
                ? ctx.DurationOverride
                : Mathf.Max(0f, def != null ? def.baseDuration : 0f);
            float bonusSeconds = ctx != null ? Mathf.Max(0f, ctx.DurationBonusSeconds) : 0f;
            return Mathf.Max(0f, baseDuration + bonusSeconds);
        }

        /// <summary>
        /// 동일 UID를 재적용했을 때의 스택/리프레시 정책을 처리한다.
        /// </summary>
        /// <param name="def">대상 어펙트 정의.</param>
        /// <param name="runtimeId">이미 존재하는 인스턴스의 runtimeId.</param>
        /// <param name="ctx">적용 컨텍스트.</param>
        /// <remarks>
        /// RefreshPolicy가 ValueAndDuration인 경우, 값 재계산을 위해 토큰을 정리한 뒤 OnApply를 재실행한다.
        /// </remarks>
        private void HandleReapply(AffectDefinition def, int runtimeId, AffectApplyContext ctx)
        {
            if (!_byRuntimeId.TryGetValue(runtimeId, out var instance)) return;

            float duration = ResolveApplyDuration(def, ctx);

            switch (def.stackPolicy)
            {
                case StackPolicy.None:
                    return;

                case StackPolicy.Refresh:
                    RequestCancelRunningSkillOnApply(def);
                    if (!IsRegisteredInstance(runtimeId, instance))
                        return;
                    instance.Refresh(duration);
                    if (def.refreshPolicy == RefreshPolicy.ValueAndDuration)
                    {
                        // 값도 다시 계산해야 하므로, Stat 토큰을 재적용한다.
                        CleanupTokens(instance);
                        CleanupActiveVisuals(instance);
                        CleanupOutline(instance);
                        CleanupAnimation(instance);
                        instance.AccumulateTick(-instance.TickElapsed); // tick reset
                        ExecutePhase(AffectPhase.OnApply, instance);
                        ExecuteAnimationOnApply(instance);
                    }
                    MarkChanged();
                    FlushChangedIfNeeded();
                    AffectTimerUiPresenter.TryEnsure(this);
                    return;

                case StackPolicy.Add:
                    RequestCancelRunningSkillOnApply(def);
                    if (!IsRegisteredInstance(runtimeId, instance))
                        return;
                    instance.AddStack(def.maxStacks);
                    if (def.refreshPolicy != RefreshPolicy.None)
                        instance.Refresh(duration);
                    MarkChanged();
                    FlushChangedIfNeeded();
                    AffectTimerUiPresenter.TryEnsure(this);
                    return;

                default:
                    RequestCancelRunningSkillOnApply(def);
                    if (!IsRegisteredInstance(runtimeId, instance))
                        return;
                    instance.Refresh(duration);
                    MarkChanged();
                    FlushChangedIfNeeded();
                    AffectTimerUiPresenter.TryEnsure(this);
                    return;
            }
        }

        /// <summary>
        /// Affect 정의의 정책에 따라 대상 캐릭터의 실행 중인 스킬 취소를 요청합니다.
        /// </summary>
        /// <param name="definition">현재 신규 적용 또는 재적용이 인정된 Affect 정의입니다.</param>
        /// <remarks>
        /// 스킬 시스템이 없는 일반 Affect 대상도 지원해야 하므로 선택적 기능 계약을 구현한 대상에만 요청합니다.
        /// 취소 실패는 적용 실패가 아니며 Affect의 OnApply 실행은 그대로 계속됩니다.
        /// </remarks>
        private void RequestCancelRunningSkillOnApply(AffectDefinition definition)
        {
            if (definition == null || !definition.cancelRunningSkillOnApply)
                return;

            if (_target is IAffectRunningSkillCanceler skillCanceler)
            {
                skillCanceler.RequestCancelRunningSkill();
            }
        }

        /// <summary>
        /// 지정한 Affect 인스턴스가 현재 runtimeId에 계속 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="runtimeId">확인할 런타임 인스턴스 식별자입니다.</param>
        /// <param name="instance">등록 상태를 비교할 Affect 인스턴스입니다.</param>
        /// <returns>동일 인스턴스가 현재 등록되어 있으면 <see langword="true"/>입니다.</returns>
        /// <remarks>
        /// 스킬 취소 알림에서 Affect 제거와 같은 외부 콜백이 동기적으로 실행될 수 있으므로
        /// 재적용 처리를 계속하기 전에 컬렉션 상태를 다시 검증합니다.
        /// </remarks>
        private bool IsRegisteredInstance(int runtimeId, AffectInstance instance)
        {
            return instance != null &&
                   _byRuntimeId.TryGetValue(runtimeId, out AffectInstance registeredInstance) &&
                   ReferenceEquals(registeredInstance, instance);
        }

        /// <summary>
        /// 지정한 페이즈에 해당하는 Modifier를 실행합니다.
        /// </summary>
        /// <param name="phase">실행할 Affect 페이즈입니다.</param>
        /// <param name="instance">실행 대상 Affect 인스턴스입니다.</param>
        /// <param name="expireReason">
        /// OnExpire 실행 시 종료 원인입니다. OnApply/OnTick에서는 사용하지 않습니다.
        /// </param>
        /// <remarks>
        /// Damage의 OnExpire 실행은 종료 원인 기반 정책(<see cref="ShouldExecuteExpireDamage"/>)으로 제어합니다.
        /// </remarks>
        private void ExecutePhase(
            AffectPhase phase,
            AffectInstance instance,
            AffectExpireReason expireReason = AffectExpireReason.NaturalExpire)
        {
            var mods = _affectRepo.GetModifiers(instance.Definition.uid);
            for (int i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                if (mod == null || mod.phase != phase) continue;

                switch (mod.kind)
                {
                    case ModifierKind.Stat:
                        if (phase == AffectPhase.OnApply)
                            _statExecutor.ExecuteOnApply(_target, instance, mod, _affectRepo, _statusRepo);
                        break;

                    case ModifierKind.Damage:
                        if (phase == AffectPhase.OnTick)
                        {
                            _damageExecutor.ExecuteOnTick(_target, instance, mod, _affectRepo, _statusRepo);
                        }
                        else if (phase == AffectPhase.OnExpire && ShouldExecuteExpireDamage(expireReason))
                        {
                            _damageExecutor.ExecuteOnExpire(_target, instance, mod, _affectRepo, _statusRepo);
                        }
                        break;


                    case ModifierKind.Heal:
                        if (phase == AffectPhase.OnApply)
                            _healExecutor.ExecuteOnApply(_target, instance, mod, _affectRepo, _statusRepo);
                        else if (phase == AffectPhase.OnTick)
                            _healExecutor.ExecuteOnTick(_target, instance, mod, _affectRepo, _statusRepo);
                        break;

                    case ModifierKind.State:
                        if (phase == AffectPhase.OnApply)
                            _stateExecutor.ExecuteOnApply(_target, instance, mod, _affectRepo, _statusRepo);
                        else if (phase == AffectPhase.OnTick)
                            _stateExecutor.ExecuteOnTick(_target, instance, mod, _affectRepo, _statusRepo);
                        break;

                    case ModifierKind.CrowdControl:
                        if (phase == AffectPhase.OnApply)
                            _crowdControlExecutor.ExecuteOnApply(_target, instance, mod, _affectRepo, _statusRepo);
                        break;

                    case ModifierKind.FormulaVariable:
                        if (phase == AffectPhase.OnApply)
                            _formulaVariableExecutor.ExecuteOnApply(_target, instance, mod, _affectRepo, _statusRepo);
                        break;

                    default:
                        break;
                }
            }

            ExecuteVisualPhase(phase, instance);

            // Outline: OnApply 시 적용 (적용 중 갱신될 수 있으므로 기존 토큰은 안전하게 제거 후 재적용)
            if (phase == AffectPhase.OnApply && instance.Definition.useOutline)
            {
                if (instance.OutlineToken != null)
                {
                    _outline?.Remove(instance.OutlineToken);
                    instance.OutlineToken = null;
                }

                int px = instance.Definition.outlinePixelSize;
                if (px <= 0) px = 1;
                Color color = instance.Definition.outlineColor;
                instance.OutlineToken = _outline?.Apply(_target, px, color);
            }
        }

        /// <summary>
        /// Source 생존 정책을 평가하여, Source 사망으로 Affect를 제거해야 하는지 확인합니다.
        /// </summary>
        /// <param name="instance">평가할 Affect 인스턴스입니다.</param>
        /// <returns>
        /// 정책이 <see cref="SourceLifePolicy.RemoveOnSourceDeath"/>이고 Source가 사망/소멸 상태이면 <c>true</c>입니다.
        /// </returns>
        /// <remarks>
        /// Source가 아예 기록되지 않은 경우(null)는 기존 호환을 위해 제거하지 않습니다.
        /// </remarks>
        private static bool ShouldRemoveBySourceLifePolicy(AffectInstance instance)
        {
            if (instance == null || instance.Definition == null)
                return false;

            if (instance.Definition.sourceLifePolicy != SourceLifePolicy.RemoveOnSourceDeath)
                return false;

            object source = instance.Context?.Source;
            if (source == null)
                return false;

            // UnityEngine.Object가 파괴되면 null 비교만 true가 되므로 소멸로 간주한다.
            if (source is UnityEngine.Object unityObject && unityObject == null)
                return true;

            GameObject sourceGameObject = ResolveSourceGameObject(source);
            if (sourceGameObject == null)
                return false;

            return !IsSourceAlive(sourceGameObject);
        }

        /// <summary>
        /// source 사망 알림으로 제거할 어펙트 인스턴스인지 확인합니다.
        /// </summary>
        /// <param name="instance">검사할 어펙트 인스턴스입니다.</param>
        /// <param name="deadSource">사망한 Source GameObject입니다.</param>
        /// <param name="affectUid">0보다 크면 일치하는 UID만 허용합니다.</param>
        /// <returns>Source 사망 사유로 제거해야 하면 true를 반환합니다.</returns>
        private static bool ShouldRemoveByDeadSourceNotification(
            AffectInstance instance,
            GameObject deadSource,
            int affectUid)
        {
            if (instance == null || instance.Definition == null || deadSource == null)
            {
                return false;
            }

            if (instance.Definition.sourceLifePolicy != SourceLifePolicy.RemoveOnSourceDeath)
            {
                return false;
            }

            if (affectUid > 0 && instance.Definition.uid != affectUid)
            {
                return false;
            }

            GameObject sourceGameObject = ResolveSourceGameObject(instance.Context?.Source);
            return IsSameSource(sourceGameObject, deadSource);
        }

        /// <summary>
        /// 두 Source GameObject가 같은 런타임 주체인지 확인합니다.
        /// </summary>
        /// <param name="candidate">어펙트 인스턴스에 기록된 Source입니다.</param>
        /// <param name="source">사망 알림으로 전달된 Source입니다.</param>
        /// <returns>같은 Source이면 true를 반환합니다.</returns>
        private static bool IsSameSource(GameObject candidate, GameObject source)
        {
            return candidate != null && source != null && candidate == source;
        }

        /// <summary>
        /// Affect Context에 기록된 Source 객체를 <see cref="GameObject"/>로 변환합니다.
        /// </summary>
        /// <param name="source">AffectApplyContext.Source로 전달된 원본 객체입니다.</param>
        /// <returns>변환 가능한 경우 Source의 <see cref="GameObject"/>, 아니면 <c>null</c>입니다.</returns>
        private static GameObject ResolveSourceGameObject(object source)
        {
            if (source is GameObject sourceGameObject)
                return sourceGameObject;

            if (source is Component sourceComponent)
                return sourceComponent.gameObject;

            return null;
        }

        /// <summary>
        /// Source GameObject의 생존 여부를 평가합니다.
        /// </summary>
        /// <param name="sourceGameObject">생존 여부를 확인할 Source GameObject입니다.</param>
        /// <returns>생존 상태면 <c>true</c>, 사망 상태면 <c>false</c>입니다.</returns>
        /// <remarks>
        /// <see cref="IAffectTarget"/>가 있으면 해당 <c>IsAlive</c>를 우선 사용하고,
        /// 없으면 <see cref="CharacterBase"/>의 사망 상태를 확인합니다.
        /// 두 타입 모두 없으면 생존으로 간주합니다.
        /// </remarks>
        private static bool IsSourceAlive(GameObject sourceGameObject)
        {
            if (sourceGameObject == null)
                return false;

            var sourceAffectTarget = sourceGameObject.GetComponent<IAffectTarget>();
            if (sourceAffectTarget != null)
                return sourceAffectTarget.IsAlive;

            var sourceCharacter = sourceGameObject.GetComponent<CharacterBase>();
            if (sourceCharacter != null)
                return !sourceCharacter.IsStatusDead() && !sourceCharacter.IsDeathPending;

            return true;
        }

        /// <summary>
        /// 종료 사유를 기준으로 OnExpire Damage Modifier 실행 여부를 판정합니다.
        /// </summary>
        /// <param name="reason">Affect 종료 사유입니다.</param>
        /// <returns>
        /// 자연 만료(<see cref="AffectExpireReason.NaturalExpire"/>)인 경우에만 <c>true</c>를 반환합니다.
        /// </returns>
        private static bool ShouldExecuteExpireDamage(AffectExpireReason reason)
        {
            return reason == AffectExpireReason.NaturalExpire;
        }


        private void ExecuteVisualPhase(AffectPhase phase, AffectInstance instance)
        {
            if (_vfx == null || instance == null)
                return;

            var actions = instance.Definition.visualActions;
            if (actions == null || actions.Count == 0)
                return;

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action == null || !action.IsValid || action.phase != phase)
                    continue;

                float duration = action.ResolveDuration(instance.TotalDuration);
                var token = _vfx.Play(
                    action.vfxUid,
                    _target,
                    action.vfxScale,
                    action.vfxOffsetY,
                    duration,
                    action.vfxPlayMode,
                    action.vfxPositionType,
                    action.vfxFollowType,
                    action.vfxSortingLayerKey);

                if (token != null && action.vfxPlayMode != AffectVfxPlayMode.Once)
                    instance.AddVisualToken(token);
            }
        }

        private void CleanupActiveVisuals(AffectInstance instance)
        {
            if (instance == null)
                return;

            var tokens = instance.VisualTokens;
            for (int i = 0; i < tokens.Count; i++)
                _vfx?.Stop(tokens[i]);

            instance.ClearVisualTokens();
        }

        private void CleanupOutline(AffectInstance instance)
        {
            if (instance == null || instance.OutlineToken == null)
                return;

            _outline?.Remove(instance.OutlineToken);
            instance.OutlineToken = null;
        }

        private void ExecuteAnimationOnApply(AffectInstance instance)
        {
            if (_animation == null || instance == null)
                return;

            AffectAnimationDefinition definition = instance.Definition?.animation;
            if (definition == null || !definition.IsConfigured)
                return;

            CleanupAnimation(instance);
            instance.AnimationToken = _animation.Play(_target, definition);
        }

        private void CleanupAnimation(AffectInstance instance)
        {
            if (instance == null || instance.AnimationToken == null)
                return;

            _animation?.Stop(instance.AnimationToken);
            instance.AnimationToken = null;
        }

        /// <summary>
        /// runtimeId에 해당하는 Affect 인스턴스를 제거합니다.
        /// </summary>
        /// <param name="runtimeId">제거할 인스턴스의 runtimeId입니다.</param>
        /// <param name="expireReason">OnExpire 정책 판정에 사용할 종료 사유입니다.</param>
        /// <remarks>
        /// - OnExpire 페이즈를 실행합니다.
        /// - Stat/State 토큰과 시각 효과 토큰을 정리합니다.
        /// - UID/그룹 인덱스를 정리하고 Changed 플래그를 마킹합니다.
        /// </remarks>
        private void RemoveByRuntimeId(int runtimeId, AffectExpireReason expireReason)
        {
            if (!_byRuntimeId.TryGetValue(runtimeId, out var instance))
                return;

            // OnExpire
            ExecutePhase(AffectPhase.OnExpire, instance, expireReason);

            CleanupAnimation(instance);
            CleanupActiveVisuals(instance);
            CleanupOutline(instance);

            // 토큰 회수(Stat/State)
            CleanupTokens(instance);

            // 인덱스 정리
            _byRuntimeId.Remove(runtimeId);
            RemoveIndex(instance.Definition.uid, runtimeId);

            if (!instance.Definition.IsNoneGroup
                && _groupIndex.TryGetValue(instance.Definition.groupId ?? string.Empty, out var mapped)
                && mapped == runtimeId)
            {
                _groupIndex.Remove(instance.Definition.groupId ?? string.Empty);
            }

            MarkChanged();
        }

        /// <summary>
        /// 인스턴스가 적용한 Stat/State/공식 변수 토큰을 타겟에서 제거한다.
        /// </summary>
        /// <param name="instance">정리할 어펙트 인스턴스.</param>
        /// <remarks>
        /// Stats는 제거 후 Recalculate를 호출하여 최종 능력치를 재계산한다.
        /// 공식 변수는 Base*/Stat* 값을 변경하지 않으므로 별도 스탯 재계산 없이 제공자에서만 제거한다.
        /// </remarks>
        private void CleanupTokens(AffectInstance instance)
        {
            if (_target == null || instance == null) return;

            if (_target.Stats != null)
            {
                var tokens = instance.StatTokens;
                for (int i = 0; i < tokens.Count; i++)
                    _target.Stats.RemoveModifier(tokens[i]);
                _target.Stats.Recalculate();
            }

            if (_target.States != null)
            {
                var tokens = instance.StateTokens;
                for (int i = 0; i < tokens.Count; i++)
                    _target.States.RemoveState(tokens[i]);
            }

            if (_target.Transform != null)
            {
                AffectFormulaVariableProvider provider = _target.Transform.GetComponent<AffectFormulaVariableProvider>();
                if (provider != null)
                {
                    var tokens = instance.FormulaVariableTokens;
                    for (int i = 0; i < tokens.Count; i++)
                        provider.RemoveVariable(tokens[i]);
                }
            }
        }

        /// <summary>
        /// affectUid → runtimeId 목록 인덱스에 runtimeId를 추가한다.
        /// </summary>
        /// <param name="affectUid">어펙트 UID.</param>
        /// <param name="runtimeId">추가할 런타임 인스턴스 ID.</param>
        private void AddIndex(int affectUid, int runtimeId)
        {
            if (!_runtimeIdsByAffectUid.TryGetValue(affectUid, out var list))
            {
                list = new List<int>(1);
                _runtimeIdsByAffectUid[affectUid] = list;
            }
            list.Add(runtimeId);
        }

        /// <summary>
        /// affectUid → runtimeId 목록 인덱스에서 runtimeId를 제거한다.
        /// </summary>
        /// <param name="affectUid">어펙트 UID.</param>
        /// <param name="runtimeId">제거할 런타임 인스턴스 ID.</param>
        private void RemoveIndex(int affectUid, int runtimeId)
        {
            if (!_runtimeIdsByAffectUid.TryGetValue(affectUid, out var list)) return;
            list.Remove(runtimeId);
            if (list.Count == 0)
                _runtimeIdsByAffectUid.Remove(affectUid);
        }

        /// <summary>
        /// 지정한 affectUid에 대해 첫 번째 runtimeId를 가져온다.
        /// </summary>
        /// <param name="affectUid">조회할 어펙트 UID.</param>
        /// <param name="runtimeId">성공 시 첫 번째 runtimeId.</param>
        /// <returns>조회 성공 여부.</returns>
        /// <remarks>
        /// 동일 UID가 Independent로 여러 개 존재할 수 있으므로 "첫 번째"만 반환한다.
        /// </remarks>
        private bool TryGetFirstRuntimeId(int affectUid, out int runtimeId)
        {
            runtimeId = 0;
            if (!_runtimeIdsByAffectUid.TryGetValue(affectUid, out var list)) return false;
            if (list == null || list.Count == 0) return false;
            runtimeId = list[0];
            return true;
        }

        /// <summary>
        /// 인스펙터에서 targetBehaviour가 IAffectTarget 구현체인지 검사한다.
        /// </summary>
        private void OnValidate()
        {
            if (targetBehaviour != null && targetBehaviour is not IAffectTarget)
                Debug.LogWarning("[AffectComponent] targetBehaviour does not implement IAffectTarget.");
        }
    }
}
