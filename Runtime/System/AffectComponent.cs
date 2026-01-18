using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 캐릭터(Actor) 단위 어펙트 컴포넌트.
    /// - 중앙 스케줄러(Update)로 Duration/Tick을 관리한다.
    /// - Player/Monster/NPC 공통으로 사용 가능.
    /// </summary>
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
        /// 어펙트 구성(추가/삭제/스택/리프레시)이 변경되었을 때 호출된다.
        /// UI 등 외부 시스템은 이 이벤트를 통해 '구조 변경'을 즉시 반영할 수 있다.
        /// (RemainingTime은 Update에서 계속 변하므로, 별도 동기화 주기가 필요하다.)
        /// </summary>
        public event Action Changed;

        private bool _changedDirty;

        private void MarkChanged()
        {
            _changedDirty = true;
        }

        private void FlushChangedIfNeeded()
        {
            if (!_changedDirty) return;
            _changedDirty = false;
            Changed?.Invoke();
        }

        private readonly StatModifierExecutor _statExecutor = new();
        private readonly DamageExecutor _damageExecutor = new();
        private readonly StateExecutor _stateExecutor = new();

        private bool HasAny => _byRuntimeId.Count > 0;

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

            enabled = HasAny;
        }

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
                    while (instance.TryConsumeTick(instance.Definition.TickInterval))
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

            enabled = HasAny;
        }

        private static readonly List<int> SPendingRemoveIds = new(32);

        // ----------------------
        // Public API
        // ----------------------

        public bool HasAffect(int affectUid)
        {
            return _runtimeIdsByAffectUid.TryGetValue(affectUid, out var ids) && ids.Count > 0;
        }

        /// <summary>
        /// 현재 활성화된 어펙트 인스턴스를 buffer에 채운다.
        /// UI 등 외부 시스템이 현재 상태를 스냅샷으로 가져오기 위함이다.
        /// </summary>
        public void CollectActiveInstances(List<AffectInstance> buffer)
        {
            if (buffer == null) return;
            buffer.Clear();
            foreach (var kv in _byRuntimeId)
                buffer.Add(kv.Value);
        }

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
            if (!def.IsNoneGroup && _groupIndex.TryGetValue(def.GroupId ?? string.Empty, out var existingRuntimeId))
            {
                RemoveByRuntimeId(existingRuntimeId);
            }

            // 2) 동일 UID 처리(스택 정책)
            if (def.StackPolicy != StackPolicy.Independent && TryGetFirstRuntimeId(affectUid, out var existingId))
            {
                HandleReapply(def, existingId, context);
                return;
            }

            // 3) 신규 인스턴스
            float duration = context.DurationOverride > 0f ? context.DurationOverride : Mathf.Max(0f, def.BaseDuration);
            var instance = new AffectInstance(def, context, duration);

            int runtimeId = _nextRuntimeId++;
            _byRuntimeId[runtimeId] = instance;
            AddIndex(affectUid, runtimeId);
            if (!def.IsNoneGroup)
                _groupIndex[def.GroupId ?? string.Empty] = runtimeId;

            // 4) OnApply 실행
            ExecutePhase(AffectPhase.OnApply, instance);

            MarkChanged();
            FlushChangedIfNeeded();

            enabled = true;
        }

        public void RemoveAffect(int affectUid)
        {
            if (!TryGetFirstRuntimeId(affectUid, out var runtimeId))
                return;

            RemoveByRuntimeId(runtimeId);
            enabled = HasAny;
            FlushChangedIfNeeded();
        }

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

        private void HandleReapply(AffectDefinition def, int runtimeId, AffectApplyContext ctx)
        {
            if (!_byRuntimeId.TryGetValue(runtimeId, out var instance)) return;

            float duration = ctx.DurationOverride > 0f ? ctx.DurationOverride : Mathf.Max(0f, def.BaseDuration);

            switch (def.StackPolicy)
            {
                case StackPolicy.None:
                    return;

                case StackPolicy.Refresh:
                    instance.Refresh(duration);
                    if (def.RefreshPolicy == RefreshPolicy.ValueAndDuration)
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
                    instance.AddStack(def.MaxStacks);
                    if (def.RefreshPolicy != RefreshPolicy.None)
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

        private void ExecutePhase(AffectPhase phase, AffectInstance instance)
        {
            var mods = _affectRepo.GetModifiers(instance.Definition.Uid);
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
            if (phase == AffectPhase.OnApply && instance.Definition.VfxUid > 0)
            {
                float duration = instance.Definition.BaseDuration;
                var token = _vfx?.Play(instance.Definition.VfxUid, _target, instance.Definition.VfxScale, instance.Definition.VfxOffsetY, duration);
                // token 저장이 필요하다면 AffectInstance에 VfxToken 필드를 추가해 관리한다.
            }
        }

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
            RemoveIndex(instance.Definition.Uid, runtimeId);

            if (!instance.Definition.IsNoneGroup
                && _groupIndex.TryGetValue(instance.Definition.GroupId ?? string.Empty, out var mapped)
                && mapped == runtimeId)
            {
                _groupIndex.Remove(instance.Definition.GroupId ?? string.Empty);
            }

            MarkChanged();
        }

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

        private void AddIndex(int affectUid, int runtimeId)
        {
            if (!_runtimeIdsByAffectUid.TryGetValue(affectUid, out var list))
            {
                list = new List<int>(1);
                _runtimeIdsByAffectUid[affectUid] = list;
            }
            list.Add(runtimeId);
        }

        private void RemoveIndex(int affectUid, int runtimeId)
        {
            if (!_runtimeIdsByAffectUid.TryGetValue(affectUid, out var list)) return;
            list.Remove(runtimeId);
            if (list.Count == 0)
                _runtimeIdsByAffectUid.Remove(affectUid);
        }

        private bool TryGetFirstRuntimeId(int affectUid, out int runtimeId)
        {
            runtimeId = 0;
            if (!_runtimeIdsByAffectUid.TryGetValue(affectUid, out var list)) return false;
            if (list == null || list.Count == 0) return false;
            runtimeId = list[0];
            return true;
        }

        private void OnValidate()
        {
            if (targetBehaviour != null && targetBehaviour is not IAffectTarget)
                Debug.LogWarning("[AffectComponent] targetBehaviour does not implement IAffectTarget.");
        }
    }
}
