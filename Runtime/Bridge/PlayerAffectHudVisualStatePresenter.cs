using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 플레이어의 Affect 상태를 HUD 시각 상태 키로 해석하여 수신자에 전달하는 프리젠터입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAffectHudVisualStatePresenter : MonoBehaviour
    {
        private const float DefaultSyncInterval = 0.10f;

        private AffectComponent _affectComponent;
        private IAffectHudVisualStateReceiver _receiver;
        private readonly List<AffectInstance> _instancesBuffer = new(64);

        private float _syncInterval = DefaultSyncInterval;
        private float _syncTimer;
        private bool _dirty;
        private string _lastAppliedStateKey;

        /// <summary>
        /// 어펙트 컴포넌트와 HUD 상태 수신자를 바인딩합니다.
        /// </summary>
        public void Bind(AffectComponent affectComponent, IAffectHudVisualStateReceiver receiver, float syncIntervalSeconds = DefaultSyncInterval)
        {
            Unsubscribe();

            _affectComponent = affectComponent;
            _receiver = receiver;
            _syncInterval = Mathf.Max(0.02f, syncIntervalSeconds);
            _syncTimer = 0f;
            _dirty = true;
            _lastAppliedStateKey = null;

            if (_affectComponent != null)
                _affectComponent.Changed += OnAffectChanged;

            RefreshNow();
            enabled = _affectComponent != null && _receiver != null;
        }

        private void OnEnable()
        {
            if (_affectComponent != null && _receiver != null)
                _dirty = true;
        }

        private void OnDisable()
        {
            if (_receiver != null)
            {
                _receiver.ResetAffectVisualState();
                _lastAppliedStateKey = null;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (_affectComponent == null || _receiver == null)
            {
                enabled = false;
                return;
            }

            _syncTimer += Time.unscaledDeltaTime;
            if (_dirty || _syncTimer >= _syncInterval)
            {
                RefreshNow();
            }
        }

        private void OnAffectChanged()
        {
            _dirty = true;
        }

        private void RefreshNow()
        {
            _syncTimer = 0f;
            _dirty = false;

            string stateKey = ResolveHighestPriorityStateKey();
            if (string.Equals(_lastAppliedStateKey, stateKey, StringComparison.Ordinal))
                return;

            _lastAppliedStateKey = stateKey;

            if (string.IsNullOrWhiteSpace(stateKey))
                _receiver.ResetAffectVisualState();
            else
                _receiver.SetAffectVisualState(stateKey);
        }

        private string ResolveHighestPriorityStateKey()
        {
            _instancesBuffer.Clear();
            _affectComponent.CollectActiveInstances(_instancesBuffer);

            string bestKey = null;
            int bestPriority = int.MinValue;

            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                var instance = _instancesBuffer[i];
                if (instance?.Definition == null)
                    continue;

                string key = instance.Definition.uiHeartStateKey;
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                int priority = 0;
                if (_receiver is UIWindowHudResourceBase heartView)
                    priority = heartView.GetAffectVisualPriority(key);

                if (bestKey == null || priority > bestPriority)
                {
                    bestKey = key;
                    bestPriority = priority;
                }
            }

            return bestKey;
        }

        private void Unsubscribe()
        {
            if (_affectComponent != null)
                _affectComponent.Changed -= OnAffectChanged;
        }
    }
}
