using System;
using System.Collections.Generic;
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
    public sealed class AffectComponent : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour targetBehaviour;

        private IAffectTarget _target;
        private IAffectDefinitionRepository _affectRepo;
        private IStatusDefinitionRepository _statusRepo;
        private IAffectVfxService _vfx;

        private readonly Dictionary<int, AffectInstance> _byRuntimeId = new();
        private readonly Dictionary<int, List<int>> _runtimeIdsByAffectUid = new();
        private readonly Dictionary<string, int> _groupIndex = new(StringComparer.Ordinal);

        private int _nextRuntimeId = 1;

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
        private readonly StateExecutor _stateExecutor = new();

        /// <summary>
        /// 현재 활성 어펙트가 1개 이상 존재하는지 여부.
        /// </summary>
        private bool HasAny => _byRuntimeId.Count > 0;

        /// <summary>
        /// 타겟(IAffectTarget)을 초기화하고, 런타임 리포지토리/서비스를 바인딩한다.
        /// </summary>
        /// <remarks>
        /// - targetBehaviour가 지정되어 있으면 우선 사용한다.
        /// - 미지정이면 동일 GameObject에서 IAffectTarget을 자동 탐색한다.
        /// - 타겟을 찾지 못하면 컴포넌트를 비활성화한다.
        /// </remarks>
        private void Awake()
        {
            // 1) 인스펙터로 지정된 경우 우선
            if (targetBehaviour != null)
                _target = targetBehaviour as IAffectTarget;

            // 2) 미지정이면 같은 GO에서 자동 탐색 (Unity는 인터페이스 GetComponent 지원)
            if (_target == null)
                _target = GetComponent<IAffectTarget>();

            if (_target == null)
            {
                Debug.LogError($"[AffectComponent] IAffectTarget not found. go={name}");
                enabled = false;
                return;
            }

            _affectRepo = AffectRuntime.AffectRepository;
            _statusRepo = AffectRuntime.StatusRepository;
            _vfx = AffectRuntime.VfxService;

            // 활성 인스턴스가 있을 때만 Update를 돌린다.
            enabled = HasAny;
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
            if (!HasAny || _target == null || !_target.IsAlive)
            {
                enabled = false;
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 삭제 큐(순회 중 컬렉션 변경 방지)
            SPendingRemoveIds.Clear();

            foreach (var kv in _byRuntimeId)
            {
                int runtimeId = kv.Key;
                var instance = kv.Value;

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
                    SPendingRemoveIds.Add(runtimeId);
            }

            // 만료 정리
            for (int i = 0; i < SPendingRemoveIds.Count; i++)
                RemoveByRuntimeId(SPendingRemoveIds[i]);

            FlushChangedIfNeeded();

            // 활성 인스턴스가 남아있을 때만 계속 Update
            enabled = HasAny;
        }

        /// <summary>
        /// 순회 중 삭제가 필요한 runtimeId를 임시로 담는 정적 리스트(재사용).
        /// </summary>
        private static readonly List<int> SPendingRemoveIds = new(32);

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
        /// 현재 활성화된 어펙트 인스턴스를 <paramref name="buffer"/>에 채운다.
        /// UI 등 외부 시스템이 현재 상태를 스냅샷으로 가져오기 위한 용도이다.
        /// </summary>
        /// <param name="buffer">활성 인스턴스를 받을 리스트(기존 내용은 Clear된다).</param>
        public void CollectActiveInstances(List<AffectInstance> buffer)
        {
            if (buffer == null) return;
            buffer.Clear();
            foreach (var kv in _byRuntimeId)
                buffer.Add(kv.Value);
        }

        /// <summary>
        /// 어펙트를 적용한다. 그룹 단일성/스택 정책에 따라 기존 인스턴스를 대체하거나 갱신할 수 있다.
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

            if (_target == null || _affectRepo == null) return;
            if (!_affectRepo.TryGetAffect(affectUid, out var def) || def == null)
            {
                Debug.LogError($"[AffectComponent] AffectDefinition not found. uid={affectUid}");
                return;
            }

            // 1) 그룹 단일성
            if (!def.IsNoneGroup && _groupIndex.TryGetValue(def.groupId ?? string.Empty, out var existingRuntimeId))
            {
                RemoveByRuntimeId(existingRuntimeId);
            }

            // 2) 동일 UID 처리(스택 정책)
            if (def.stackPolicy != StackPolicy.Independent && TryGetFirstRuntimeId(affectUid, out var existingId))
            {
                HandleReapply(def, existingId, context);
                return;
            }

            // 3) 신규 인스턴스
            float duration = context.DurationOverride > 0f ? context.DurationOverride : Mathf.Max(0f, def.baseDuration);
            var instance = new AffectInstance(def, context, duration);

            int runtimeId = _nextRuntimeId++;
            _byRuntimeId[runtimeId] = instance;
            AddIndex(affectUid, runtimeId);
            if (!def.IsNoneGroup)
                _groupIndex[def.groupId ?? string.Empty] = runtimeId;

            // 4) OnApply 실행
            ExecutePhase(AffectPhase.OnApply, instance);

            MarkChanged();
            FlushChangedIfNeeded();

            enabled = true;
        }

        /// <summary>
        /// 지정한 어펙트 UID의 "첫 번째" 활성 인스턴스를 제거한다.
        /// </summary>
        /// <param name="affectUid">제거할 어펙트 UID.</param>
        public void RemoveAffect(int affectUid)
        {
            if (!TryGetFirstRuntimeId(affectUid, out var runtimeId))
                return;

            RemoveByRuntimeId(runtimeId);
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
                RemoveByRuntimeId(SPendingRemoveIds[i]);

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
                RemoveByRuntimeId(SPendingRemoveIds[i]);

            enabled = false;
            FlushChangedIfNeeded();
        }

        // ----------------------
        // Internal
        // ----------------------

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

            float duration = ctx.DurationOverride > 0f ? ctx.DurationOverride : Mathf.Max(0f, def.baseDuration);

            switch (def.stackPolicy)
            {
                case StackPolicy.None:
                    return;

                case StackPolicy.Refresh:
                    instance.Refresh(duration);
                    if (def.refreshPolicy == RefreshPolicy.ValueAndDuration)
                    {
                        // 값도 다시 계산해야 하므로, Stat 토큰을 재적용한다.
                        CleanupTokens(instance);
                        instance.AccumulateTick(-instance.TickElapsed); // tick reset
                        ExecutePhase(AffectPhase.OnApply, instance);
                    }
                    MarkChanged();
                    FlushChangedIfNeeded();
                    return;

                case StackPolicy.Add:
                    instance.AddStack(def.maxStacks);
                    if (def.refreshPolicy != RefreshPolicy.None)
                        instance.Refresh(duration);
                    MarkChanged();
                    FlushChangedIfNeeded();
                    return;

                default:
                    instance.Refresh(duration);
                    MarkChanged();
                    FlushChangedIfNeeded();
                    return;
            }
        }

        /// <summary>
        /// 지정한 페이즈에 해당하는 Modifier들을 실행한다.
        /// </summary>
        /// <param name="phase">실행할 어펙트 페이즈(OnApply/OnTick/OnExpire).</param>
        /// <param name="instance">대상 어펙트 인스턴스.</param>
        /// <remarks>
        /// - Stat: OnApply에서만 실행
        /// - Damage: OnTick에서만 실행
        /// - State: OnApply/OnTick에서 실행 가능
        /// - VFX는 OnApply 시 1회 재생한다.
        /// </remarks>
        private void ExecutePhase(AffectPhase phase, AffectInstance instance)
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
                            _damageExecutor.ExecuteOnTick(_target, instance, mod, _affectRepo, _statusRepo);
                        break;

                    case ModifierKind.State:
                        if (phase == AffectPhase.OnApply)
                            _stateExecutor.ExecuteOnApply(_target, instance, mod, _affectRepo, _statusRepo);
                        else if (phase == AffectPhase.OnTick)
                            _stateExecutor.ExecuteOnTick(_target, instance, mod, _affectRepo, _statusRepo);
                        break;

                    default:
                        break;
                }
            }

            // VFX: OnApply 시 1회 재생
            if (phase == AffectPhase.OnApply && instance.Definition.vfxUid > 0)
            {
                float duration = instance.Definition.baseDuration;
                var token = _vfx?.Play(
                    instance.Definition.vfxUid,
                    _target,
                    instance.Definition.vfxScale,
                    instance.Definition.vfxOffsetY,
                    duration);

                // token 저장이 필요하다면 AffectInstance에 VfxToken 필드를 추가해 관리한다.
            }
        }

        /// <summary>
        /// runtimeId로 인스턴스를 제거한다.
        /// </summary>
        /// <param name="runtimeId">제거할 인스턴스의 runtimeId.</param>
        /// <remarks>
        /// - OnExpire 페이즈를 실행한다.
        /// - Stat/State 토큰을 회수한다.
        /// - UID/그룹 인덱스를 정리한다.
        /// - Changed 이벤트는 지연 플래그로 마킹한다(즉시 Flush는 호출자가 결정).
        /// </remarks>
        private void RemoveByRuntimeId(int runtimeId)
        {
            if (!_byRuntimeId.TryGetValue(runtimeId, out var instance))
                return;

            // OnExpire
            ExecutePhase(AffectPhase.OnExpire, instance);

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
        /// 인스턴스가 적용한 Stat/State 토큰을 타겟에서 제거한다.
        /// </summary>
        /// <param name="instance">정리할 어펙트 인스턴스.</param>
        /// <remarks>
        /// Stats는 제거 후 Recalculate를 호출하여 최종 능력치를 재계산한다.
        /// </remarks>
        private void CleanupTokens(AffectInstance instance)
        {
            if (_target == null) return;

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
