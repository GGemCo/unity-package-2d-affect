using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 플레이어의 <see cref="AffectComponent"/>를 관찰하여 <see cref="UIWindowPlayerBuffInfo"/>에 표시를 위임한다.
    /// </summary>
    /// <remarks>
    /// - UI는 '표시'만 담당하고, 실제 적용/만료/스택 정책은 AffectComponent가 단일 진실 소스이다.
    /// - 남은 시간은 매 프레임 변하므로, 구조 변경 이벤트(Changed) + 주기 동기화 방식을 사용한다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerAffectUiPresenter : MonoBehaviour
    {
        private const float DefaultSyncInterval = 0.10f;

        private AffectComponent _affectComponent;
        private UIWindowPlayerBuffInfo _view;

        private readonly List<AffectInstance> _instancesBuffer = new(64);
        private readonly List<AffectUiItem> _itemsBuffer = new(64);
        private readonly Dictionary<int, Aggregate> _aggregateByAffectUid = new(64);

        private float _syncInterval = DefaultSyncInterval;
        private float _syncTimer;
        private bool _dirty;

        private struct Aggregate
        {
            public int Stacks;
            public float RemainingMax;
            public float TotalDurationMax;
            public string IconKey;
        }

        /// <summary>
        /// 바인딩.
        /// </summary>
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

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnAffectChanged()
        {
            _dirty = true;
        }

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

        private void RenderSnapshot()
        {
            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();

            _affectComponent.CollectActiveInstances(_instancesBuffer);

            // UI는 "AffectUid" 단위로 집계하여 1개 아이콘으로 표현한다.
            // (StackPolicy.Independent로 여러 인스턴스가 존재할 수 있어도 UX는 보통 1개로 합친다.)
            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                var inst = _instancesBuffer[i];
                if (inst == null || inst.Definition == null) continue;

                int uid = inst.Definition.Uid;
                if (!_aggregateByAffectUid.TryGetValue(uid, out var agg))
                {
                    agg = new Aggregate
                    {
                        Stacks = 0,
                        RemainingMax = 0f,
                        TotalDurationMax = 0f,
                        IconKey = inst.Definition.IconKey
                    };
                }

                agg.Stacks += Mathf.Max(1, inst.Stacks);
                if (inst.RemainingTime > agg.RemainingMax) agg.RemainingMax = inst.RemainingTime;
                if (inst.TotalDuration > agg.TotalDurationMax) agg.TotalDurationMax = inst.TotalDuration;
                if (string.IsNullOrWhiteSpace(agg.IconKey)) agg.IconKey = inst.Definition.IconKey;

                _aggregateByAffectUid[uid] = agg;
            }

            foreach (var kv in _aggregateByAffectUid)
            {
                int uid = kv.Key;
                var agg = kv.Value;
                _itemsBuffer.Add(new AffectUiItem(uid, agg.Stacks, agg.RemainingMax, agg.TotalDurationMax, agg.IconKey));
            }

            _view.Render(_itemsBuffer);
        }
    }
}
