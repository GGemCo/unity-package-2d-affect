using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// AffectComponent의 활성 어펙트 스냅샷을 캐릭터 추적 타이머 UI 데이터로 변환합니다.
    /// </summary>
    /// <remarks>
    /// 실제 어펙트 적용/만료/스택 정책은 <see cref="AffectComponent"/>가 담당하고,
    /// 이 Presenter는 표시 가능한 스냅샷을 일정 주기로 동기화합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AffectTimerUiPresenter : MonoBehaviour
    {
        private const float DefaultRefreshInterval = 0.10f;

        private readonly List<AffectInstance> _instancesBuffer = new(16);
        private readonly List<AffectTimerUiItem> _itemsBuffer = new(16);
        private readonly Dictionary<int, Aggregate> _aggregateByAffectUid = new(16);

        private AffectComponent _affectComponent;
        private AffectTimerAnchor _anchor;
        private int _ownerId;
        private float _refreshTimer;
        private bool _dirty = true;

        /// <summary>
        /// 대상 오브젝트에 타이머 UI Presenter를 자동 부착합니다.
        /// </summary>
        /// <param name="affectComponent">표시 원본 AffectComponent입니다.</param>
        public static void TryEnsure(AffectComponent affectComponent)
        {
            if (affectComponent == null)
                return;

            var settings = GGemCoAffectSettingsRuntime.GetOrLoad();
            if (settings != null && (!settings.enableAffectTimerUi || !settings.timerAutoAddPresenter))
                return;

            var presenter = affectComponent.GetComponent<AffectTimerUiPresenter>();
            if (presenter == null)
                presenter = affectComponent.gameObject.AddComponent<AffectTimerUiPresenter>();

            presenter.Bind(affectComponent);
        }

        /// <summary>
        /// 표시 원본 AffectComponent를 바인딩합니다.
        /// </summary>
        /// <param name="affectComponent">표시할 어펙트 컴포넌트입니다.</param>
        public void Bind(AffectComponent affectComponent)
        {
            if (_affectComponent == affectComponent)
            {
                _dirty = true;
                enabled = true;
                return;
            }

            Unbind();
            _affectComponent = affectComponent;
            if (_affectComponent != null)
            {
                _affectComponent.Changed += OnAffectChanged;
                _dirty = true;
                enabled = true;
            }
        }

        /// <summary>
        /// 컴포넌트 초기화 시 같은 GameObject의 AffectComponent를 자동 바인딩합니다.
        /// </summary>
        private void Awake()
        {
            _ownerId = GetInstanceID();
            _anchor = GetComponent<AffectTimerAnchor>();

            if (_affectComponent == null)
                Bind(GetComponent<AffectComponent>());
        }

        /// <summary>
        /// 비활성화될 때 현재 표시 중인 UI를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            AffectTimerUiManager.Current?.HideOwner(_ownerId);
        }

        /// <summary>
        /// 파괴 시 이벤트 구독과 UI 표시를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            Unbind();
            AffectTimerUiManager.Current?.HideOwner(_ownerId);
        }

        /// <summary>
        /// 구조 변경 발생 시 다음 Update에서 즉시 다시 렌더링하도록 표시합니다.
        /// </summary>
        private void OnAffectChanged()
        {
            _dirty = true;
        }

        /// <summary>
        /// 표시 주기마다 활성 어펙트 스냅샷을 렌더링합니다.
        /// </summary>
        private void Update()
        {
            if (_affectComponent == null)
                return;

            var settings = GGemCoAffectSettingsRuntime.GetOrLoad();
            if (settings == null)
                return;

            if (!settings.enableAffectTimerUi)
            {
                AffectTimerUiManager.Current?.HideOwner(_ownerId);
                return;
            }

            float interval = Mathf.Max(0.02f, settings.timerRefreshInterval > 0f ? settings.timerRefreshInterval : DefaultRefreshInterval);
            float deltaTime = settings.timerUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _refreshTimer += Mathf.Max(0f, deltaTime);

            if (!_dirty && _refreshTimer < interval)
                return;

            _refreshTimer = 0f;
            _dirty = false;
            RenderSnapshot(settings);
        }

        /// <summary>
        /// 이벤트 구독을 해제하고 내부 버퍼를 초기화합니다.
        /// </summary>
        private void Unbind()
        {
            if (_affectComponent != null)
                _affectComponent.Changed -= OnAffectChanged;

            _affectComponent = null;
            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();
            _dirty = false;
            _refreshTimer = 0f;
        }

        /// <summary>
        /// AffectComponent 스냅샷을 수집하여 표시 가능한 타이머 UI 목록을 생성합니다.
        /// </summary>
        private void RenderSnapshot(GGemCoAffectSettings settings)
        {
            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();

            _affectComponent.CollectActiveInstances(_instancesBuffer);
            if (_instancesBuffer.Count == 0)
            {
                AffectTimerUiManager.Current?.HideOwner(_ownerId);
                return;
            }

            if (settings.timerAggregateSameAffectUid)
                BuildAggregatedItems(settings);
            else
                BuildInstanceItems(settings);

            SortItems(settings);

            if (_itemsBuffer.Count == 0)
            {
                AffectTimerUiManager.Current?.HideOwner(_ownerId);
                return;
            }

            AffectTimerUiManager.GetOrCreate().RenderOwner(
                _ownerId,
                transform,
                _anchor,
                _itemsBuffer,
                settings);
        }

        /// <summary>
        /// 같은 AffectUid를 하나의 표시 항목으로 집계합니다.
        /// </summary>
        private void BuildAggregatedItems(GGemCoAffectSettings settings)
        {
            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                AffectInstance instance = _instancesBuffer[i];
                if (!IsDisplayable(instance, settings))
                    continue;

                int uid = instance.Definition.uid;
                if (!_aggregateByAffectUid.TryGetValue(uid, out var aggregate))
                {
                    aggregate = new Aggregate
                    {
                        AffectUid = uid,
                        DisplayName = ResolveDisplayName(instance.Definition),
                        DispelType = instance.Definition.dispelType,
                        Stacks = 0,
                        RemainingTime = 0f,
                        TotalDuration = 0f
                    };
                }

                aggregate.Stacks += Mathf.Max(1, instance.Stacks);
                if (instance.RemainingTime > aggregate.RemainingTime)
                    aggregate.RemainingTime = instance.RemainingTime;
                if (instance.TotalDuration > aggregate.TotalDuration)
                    aggregate.TotalDuration = instance.TotalDuration;

                _aggregateByAffectUid[uid] = aggregate;
            }

            foreach (var kv in _aggregateByAffectUid)
            {
                Aggregate aggregate = kv.Value;
                _itemsBuffer.Add(new AffectTimerUiItem(
                    aggregate.AffectUid,
                    aggregate.AffectUid,
                    aggregate.Stacks,
                    aggregate.RemainingTime,
                    aggregate.TotalDuration,
                    aggregate.DisplayName,
                    aggregate.DispelType));
            }
        }

        /// <summary>
        /// 활성 인스턴스 각각을 별도 표시 항목으로 변환합니다.
        /// </summary>
        private void BuildInstanceItems(GGemCoAffectSettings settings)
        {
            int displayIndex = 0;
            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                AffectInstance instance = _instancesBuffer[i];
                if (!IsDisplayable(instance, settings))
                    continue;

                int uid = instance.Definition.uid;
                int displayKey = unchecked((uid * 397) ^ displayIndex);
                _itemsBuffer.Add(new AffectTimerUiItem(
                    displayKey,
                    uid,
                    Mathf.Max(1, instance.Stacks),
                    instance.RemainingTime,
                    instance.TotalDuration,
                    ResolveDisplayName(instance.Definition),
                    instance.Definition.dispelType));
                displayIndex++;
            }
        }

        /// <summary>
        /// 설정과 인스턴스 상태를 기준으로 타이머 UI 표시 가능 여부를 반환합니다.
        /// </summary>
        private static bool IsDisplayable(AffectInstance instance, GGemCoAffectSettings settings)
        {
            if (instance == null || instance.Definition == null || settings == null)
                return false;

            if (instance.RemainingTime <= Mathf.Max(0f, settings.timerHideBelowSeconds))
                return false;

            if (instance.Definition.hasUseTimerUiOverride)
                return instance.Definition.useTimerUi;

            return IsDisplayableByLegacyTypeFilter(instance.Definition.dispelType, settings);
        }

        /// <summary>
        /// 레거시 전역 타입 필터 정책으로 타이머 표시 여부를 판단합니다.
        /// </summary>
        /// <param name="dispelType">어펙트의 디스펠 분류 타입입니다.</param>
        /// <param name="settings">Affect 타이머 UI 설정입니다.</param>
        /// <returns>해당 타입의 타이머를 표시해야 하면 true를 반환합니다.</returns>
        /// <remarks>
        /// UseTimerUi 컬럼이 비어 있는 기존 테이블 데이터와의 호환을 위한 폴백 경로입니다.
        /// </remarks>
        private static bool IsDisplayableByLegacyTypeFilter(DispelType dispelType, GGemCoAffectSettings settings)
        {
            return dispelType switch
            {
                DispelType.Buff => settings.timerShowBuff,
                DispelType.Debuff => settings.timerShowDebuff,
                _ => settings.timerShowNoneType
            };
        }

        /// <summary>
        /// 설정된 정렬 정책에 따라 표시 항목 순서를 정렬합니다.
        /// </summary>
        private void SortItems(GGemCoAffectSettings settings)
        {
            switch (settings.timerSortMode)
            {
                case AffectDebugSortMode.RemainingTimeDesc:
                    _itemsBuffer.Sort((a, b) => b.RemainingTime.CompareTo(a.RemainingTime));
                    break;
                case AffectDebugSortMode.Name:
                    _itemsBuffer.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
                    break;
                case AffectDebugSortMode.ApplyOrder:
                    break;
                case AffectDebugSortMode.RemainingTimeAsc:
                default:
                    _itemsBuffer.Sort((a, b) => a.RemainingTime.CompareTo(b.RemainingTime));
                    break;
            }
        }

        /// <summary>
        /// Affect 이름 표시 문자열을 해석합니다.
        /// </summary>
        private static string ResolveDisplayName(AffectDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            string key = definition.nameKey;
            if (string.IsNullOrWhiteSpace(key))
                return definition.uid.ToString();

            var localization = LocalizationManagerAffect.Instance;
            if (localization == null)
                return key;

            string localized = localization.GetAffectNameByKey(key);
            return string.IsNullOrWhiteSpace(localized) ? key : localized;
        }

        /// <summary>
        /// UID 단위 집계 결과입니다.
        /// </summary>
        private struct Aggregate
        {
            public int AffectUid;
            public int Stacks;
            public float RemainingTime;
            public float TotalDuration;
            public string DisplayName;
            public DispelType DispelType;
        }
    }
}
