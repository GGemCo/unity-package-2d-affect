using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 플레이어의 <see cref="AffectComponent"/>를 관찰하여 버프 UI를 갱신하는 프리젠터입니다.
    /// </summary>
    /// <remarks>
    /// 어펙트 적용, 만료, 스택 규칙은 <see cref="AffectComponent"/>가 단일 진실 소스로 관리합니다.
    /// 이 프리젠터는 현재 활성 인스턴스를 UI 표시 단위로 집계하고,
    /// <see cref="UIWindowPlayerBuffInfo"/>에 렌더링 스냅샷을 전달합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerAffectUiPresenter : MonoBehaviour
    {
        /// <summary>
        /// 구조 변경 이벤트가 없어도 남은 시간을 갱신하기 위한 기본 동기화 주기입니다.
        /// </summary>
        private const float DefaultSyncInterval = 0.10f;

        private readonly List<AffectInstance> _instancesBuffer = new(64);
        private readonly List<AffectUiItem> _itemsBuffer = new(64);
        private readonly Dictionary<int, Aggregate> _aggregateByAffectUid = new(64);

        private AffectComponent _affectComponent;
        private UIWindowPlayerBuffInfo _view;
        private float _syncInterval = DefaultSyncInterval;
        private float _syncTimer;
        private bool _dirty;

        /// <summary>
        /// 동일 어펙트 UID를 하나의 UI 아이콘으로 표시하기 위해 집계한 값입니다.
        /// </summary>
        private struct Aggregate
        {
            /// <summary>동일 UID 인스턴스들의 합산 스택 수입니다.</summary>
            public int Stacks;

            /// <summary>동일 UID 그룹 중 가장 긴 남은 시간입니다.</summary>
            public float RemainingMax;

            /// <summary>동일 UID 그룹 중 가장 긴 전체 지속 시간입니다.</summary>
            public float TotalDurationMax;

            /// <summary>아이콘에 쿨타임 게이지를 표시할지 여부입니다.</summary>
            public bool ShowCoolTimeGauge;

            /// <summary>표시할 아이콘 키입니다.</summary>
            public string IconKey;

            /// <summary>표시할 보조 데코레이터 데이터입니다.</summary>
            public AffectUiDecoratorData Decorator;
        }

        /// <summary>
        /// 어펙트 소스와 버프 UI 윈도우를 연결합니다.
        /// </summary>
        /// <param name="affectComponent">관찰할 어펙트 컴포넌트입니다.</param>
        /// <param name="view">렌더링을 담당할 PlayerBuffInfo 윈도우입니다.</param>
        /// <param name="syncIntervalSeconds">남은 시간 동기화 주기입니다. 최소 0.02초로 보정합니다.</param>
        public void Bind(AffectComponent affectComponent, UIWindowPlayerBuffInfo view, float syncIntervalSeconds = DefaultSyncInterval)
        {
            Unbind();

            _affectComponent = affectComponent;
            _view = view;
            _syncInterval = Mathf.Max(0.02f, syncIntervalSeconds);

            if (_affectComponent != null)
            {
                _affectComponent.Changed += OnAffectChanged;
                _dirty = true;
            }
        }

        /// <summary>
        /// 현재 바인딩을 해제하고 내부 버퍼와 상태를 초기화합니다.
        /// </summary>
        public void Unbind()
        {
            if (_affectComponent != null)
            {
                _affectComponent.Changed -= OnAffectChanged;
            }

            _affectComponent = null;
            _view = null;

            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();

            _syncTimer = 0f;
            _dirty = false;
        }

        /// <summary>
        /// 오브젝트 파괴 시 이벤트 구독을 해제해 누수와 중복 호출을 방지합니다.
        /// </summary>
        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// 어펙트 구조가 변경되었음을 표시합니다.
        /// </summary>
        private void OnAffectChanged()
        {
            _dirty = true;
        }

        /// <summary>
        /// 변경 이벤트 또는 동기화 주기에 맞춰 버프 UI 스냅샷을 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (_view == null || _affectComponent == null)
            {
                return;
            }

            _syncTimer += Time.unscaledDeltaTime;
            if (!_dirty && _syncTimer < _syncInterval)
            {
                return;
            }

            _syncTimer = 0f;
            _dirty = false;

            RenderSnapshot();
        }

        /// <summary>
        /// 현재 활성 어펙트 인스턴스를 UID 기준으로 집계한 뒤 UI 스냅샷으로 전달합니다.
        /// </summary>
        /// <remarks>
        /// 같은 어펙트 UID가 여러 인스턴스로 존재하더라도 UI에서는 하나의 아이콘으로 표시합니다.
        /// 스택은 합산하고, 남은 시간과 전체 지속 시간은 가장 긴 값을 대표값으로 사용합니다.
        /// </remarks>
        private void RenderSnapshot()
        {
            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();

            _affectComponent.CollectActiveInstances(_instancesBuffer);

            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                AffectInstance instance = _instancesBuffer[i];
                if (instance == null || instance.Definition == null)
                {
                    continue;
                }

                int uid = instance.Definition.uid;
                if (!_aggregateByAffectUid.TryGetValue(uid, out Aggregate aggregate))
                {
                    aggregate = new Aggregate
                    {
                        Stacks = 0,
                        RemainingMax = 0f,
                        TotalDurationMax = 0f,
                        // 정의 정책뿐 아니라 적용 컨텍스트에서 강제된 Session 수명도 동일하게 처리합니다.
                        ShowCoolTimeGauge = !instance.IsSessionLifetime,
                        IconKey = instance.Definition.iconKey,
                        Decorator = ResolveDecorator(instance.Definition)
                    };
                }

                aggregate.Stacks += Mathf.Max(1, instance.Stacks);

                if (instance.RemainingTime > aggregate.RemainingMax)
                {
                    aggregate.RemainingMax = instance.RemainingTime;
                }

                if (instance.TotalDuration > aggregate.TotalDurationMax)
                {
                    aggregate.TotalDurationMax = instance.TotalDuration;
                }

                if (string.IsNullOrWhiteSpace(aggregate.IconKey))
                {
                    aggregate.IconKey = instance.Definition.iconKey;
                }

                _aggregateByAffectUid[uid] = aggregate;
            }

            foreach (KeyValuePair<int, Aggregate> pair in _aggregateByAffectUid)
            {
                Aggregate aggregate = pair.Value;
                _itemsBuffer.Add(new AffectUiItem(
                    pair.Key,
                    aggregate.Stacks,
                    aggregate.RemainingMax,
                    aggregate.TotalDurationMax,
                    aggregate.ShowCoolTimeGauge,
                    aggregate.IconKey,
                    aggregate.Decorator));
            }

            _view.Render(_itemsBuffer);
        }

        /// <summary>
        /// 어펙트 정의와 설정값을 기준으로 보조 데코레이터 표시 데이터를 해석합니다.
        /// </summary>
        /// <param name="definition">데코레이터를 해석할 어펙트 정의입니다.</param>
        /// <returns>표시 가능한 데코레이터 데이터입니다. 없으면 Hidden을 반환합니다.</returns>
        private static AffectUiDecoratorData ResolveDecorator(AffectDefinition definition)
        {
            if (definition == null)
            {
                return AffectUiDecoratorData.Hidden;
            }

            GGemCoAffectSettings settings = GGemCoAffectSettingsRuntime.GetOrLoad();
            if (settings == null)
            {
                return AffectUiDecoratorData.Hidden;
            }

            if (!settings.TryGetTypeIconStyle(definition.dispelType, out AffectTypeIconStyle style) || style == null)
            {
                return AffectUiDecoratorData.Hidden;
            }

            return new AffectUiDecoratorData(
                true,
                style.sprite,
                style.size,
                style.anchor,
                style.offset);
        }
    }
}
