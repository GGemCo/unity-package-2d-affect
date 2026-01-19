using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 플레이어의 <see cref="AffectComponent"/>를 관찰하여 버프 UI(<see cref="UIWindowPlayerBuffInfo"/>) 갱신을 위임하는 프리젠터.
    /// </summary>
    /// <remarks>
    /// - UI는 '표시'만 담당하고, 실제 적용/만료/스택 정책은 <see cref="AffectComponent"/>가 단일 진실 소스(SSOT)이다.
    /// - 남은 시간은 매 프레임 변하므로, 구조 변경 이벤트(Changed) + 주기 동기화 방식을 함께 사용한다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerAffectUiPresenter : MonoBehaviour
    {
        /// <summary>
        /// 구조 변경이 없더라도 남은 시간을 갱신하기 위한 기본 동기화 주기(초).
        /// </summary>
        private const float DefaultSyncInterval = 0.10f;

        private AffectComponent _affectComponent;
        private UIWindowPlayerBuffInfo _view;

        // GC 최소화를 위해 버퍼를 재사용한다.
        private readonly List<AffectInstance> _instancesBuffer = new(64);
        private readonly List<AffectUiItem> _itemsBuffer = new(64);
        private readonly Dictionary<int, Aggregate> _aggregateByAffectUid = new(64);

        private float _syncInterval = DefaultSyncInterval;
        private float _syncTimer;
        private bool _dirty;

        /// <summary>
        /// 동일 Affect(Definition.Uid) 기준으로 UI 표현에 필요한 값을 집계한 결과.
        /// </summary>
        private struct Aggregate
        {
            /// <summary>표시용 총 스택 수(여러 인스턴스의 스택을 합산).</summary>
            public int Stacks;

            /// <summary>동일 UID 그룹 중 가장 긴 남은 시간(아이콘 1개 표현 기준).</summary>
            public float RemainingMax;

            /// <summary>동일 UID 그룹 중 가장 긴 총 지속 시간(게이지/퍼센트 계산용).</summary>
            public float TotalDurationMax;

            /// <summary>표시할 아이콘 키(가능하면 Definition.IconKey 사용).</summary>
            public string IconKey;
        }

        /// <summary>
        /// 버프 UI 갱신을 위해 Affect 소스와 뷰를 바인딩한다.
        /// </summary>
        /// <param name="affectComponent">관찰할 Affect 컴포넌트.</param>
        /// <param name="view">렌더링 대상 UI 뷰.</param>
        /// <param name="syncIntervalSeconds">
        /// 구조 변경 이벤트가 없어도 남은 시간을 갱신하기 위한 동기화 주기(초).
        /// 너무 작은 값은 비용이 커질 수 있어 최소 0.02초로 클램프한다.
        /// </param>
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
        /// 바인딩을 해제하고 내부 버퍼/상태를 초기화한다.
        /// </summary>
        public void Unbind()
        {
            if (_affectComponent != null)
                _affectComponent.Changed -= OnAffectChanged;

            _affectComponent = null;
            _view = null;

            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();

            _syncTimer = 0f;
            _dirty = false;
        }

        /// <summary>
        /// 오브젝트 파괴 시 이벤트 구독을 해제하여 누수/중복 호출을 방지한다.
        /// </summary>
        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// Affect 구조(추가/제거/스택 변경 등)가 변했음을 표시한다.
        /// </summary>
        private void OnAffectChanged()
        {
            _dirty = true;
        }

        /// <summary>
        /// 주기적으로(또는 구조 변경 시 즉시) 버프 UI 스냅샷을 렌더링한다.
        /// </summary>
        private void Update()
        {
            if (_view == null || _affectComponent == null)
                return;

            // 구조 변경이 없더라도 남은 시간은 주기적으로 동기화한다.
            _syncTimer += Time.unscaledDeltaTime;
            if (!_dirty && _syncTimer < _syncInterval)
                return;

            _syncTimer = 0f;
            _dirty = false;

            RenderSnapshot();
        }

        /// <summary>
        /// 현재 활성 Affect 인스턴스를 수집하고, UID 단위로 집계하여 뷰에 전달한다.
        /// </summary>
        /// <remarks>
        /// UI는 "AffectUid" 단위로 집계하여 1개 아이콘으로 표현한다.
        /// (StackPolicy.Independent로 여러 인스턴스가 존재할 수 있어도 UX는 보통 1개로 합친다.)
        /// </remarks>
        private void RenderSnapshot()
        {
            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();

            _affectComponent.CollectActiveInstances(_instancesBuffer);

            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                var inst = _instancesBuffer[i];
                if (inst == null || inst.Definition == null) continue;

                int uid = inst.Definition.uid;
                if (!_aggregateByAffectUid.TryGetValue(uid, out var agg))
                {
                    agg = new Aggregate
                    {
                        Stacks = 0,
                        RemainingMax = 0f,
                        TotalDurationMax = 0f,
                        IconKey = inst.Definition.iconKey
                    };
                }

                // 표시는 "합산 스택"으로 처리한다(최소 1).
                agg.Stacks += Mathf.Max(1, inst.Stacks);

                // 아이콘 1개로 표현할 때 일반적으로 "가장 오래 남은 것"을 대표로 잡는다.
                if (inst.RemainingTime > agg.RemainingMax) agg.RemainingMax = inst.RemainingTime;
                if (inst.TotalDuration > agg.TotalDurationMax) agg.TotalDurationMax = inst.TotalDuration;

                // 최초 정의가 비어있을 수 있으므로, 비어 있으면 갱신한다.
                if (string.IsNullOrWhiteSpace(agg.IconKey)) agg.IconKey = inst.Definition.iconKey;

                _aggregateByAffectUid[uid] = agg;
            }

            foreach (var kv in _aggregateByAffectUid)
            {
                int uid = kv.Key;
                var agg = kv.Value;

                _itemsBuffer.Add(new AffectUiItem(
                    uid,
                    agg.Stacks,
                    agg.RemainingMax,
                    agg.TotalDurationMax,
                    agg.IconKey));
            }

            _view.Render(_itemsBuffer);
        }
    }
}
