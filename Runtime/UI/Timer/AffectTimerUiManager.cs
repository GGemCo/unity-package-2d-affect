using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 캐릭터를 따라다니는 어펙트 타이머 UI의 Canvas, Pool, 위치 갱신을 관리합니다.
    /// </summary>
    /// <remarks>
    /// Presenter는 표시할 데이터만 만들고, 실제 TMP_Text 생성/재사용/위치 보정은 이 매니저가 담당합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AffectTimerUiManager : MonoBehaviour
    {
        private const string DefaultCanvasName = "AffectTimerUiCanvas";
        private const string DefaultTextName = "AffectTimerText";

        private static AffectTimerUiManager _instance;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Camera _cachedCamera;
        private Transform _poolRoot;

        private readonly Dictionary<AffectTimerViewKey, ViewState> _activeViews = new();
        private readonly Dictionary<int, List<AffectTimerViewKey>> _keysByOwner = new();
        private readonly Stack<AffectTimerTextView> _viewPool = new();
        private readonly List<AffectTimerViewKey> _keysToRemove = new(32);
        private readonly HashSet<AffectTimerViewKey> _currentRenderKeys = new();


        /// <summary>
        /// 현재 존재하는 매니저를 반환합니다. 없으면 새로 생성하지 않습니다.
        /// </summary>
        public static AffectTimerUiManager Current
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<AffectTimerUiManager>();
                return _instance;
            }
        }

        /// <summary>
        /// 현재 활성 매니저를 반환하거나 필요 시 생성합니다.
        /// </summary>
        public static AffectTimerUiManager GetOrCreate()
        {
            if (_instance != null)
                return _instance;

            _instance = FindFirstObjectByType<AffectTimerUiManager>();
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(AffectTimerUiManager));
            if (Application.isPlaying)
                DontDestroyOnLoad(go);

            _instance = go.AddComponent<AffectTimerUiManager>();
            return _instance;
        }

        /// <summary>
        /// 매니저 인스턴스 참조를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Presenter 1개가 담당하는 대상의 타이머 UI를 렌더링합니다.
        /// </summary>
        /// <param name="ownerId">Presenter 인스턴스 ID입니다.</param>
        /// <param name="target">UI가 따라다닐 대상 Transform입니다.</param>
        /// <param name="anchor">선택적 위치 앵커입니다.</param>
        /// <param name="items">표시할 어펙트 타이머 목록입니다.</param>
        /// <param name="settings">Affect 표시 설정입니다.</param>
        public void RenderOwner(
            int ownerId,
            Transform target,
            AffectTimerAnchor anchor,
            IReadOnlyList<AffectTimerUiItem> items,
            GGemCoAffectSettings settings)
        {
            if (ownerId == 0 || target == null || items == null || items.Count == 0 || settings == null)
            {
                HideOwner(ownerId);
                return;
            }

            EnsureCanvas(settings);
            if (_canvas == null || _canvasRect == null)
                return;

            _currentRenderKeys.Clear();
            List<AffectTimerViewKey> ownerKeys = GetOwnerKeyList(ownerId);

            int maxCount = Mathf.Max(1, settings.timerMaxVisiblePerTarget);
            int count = Mathf.Min(items.Count, maxCount);
            for (int i = 0; i < count; i++)
            {
                AffectTimerUiItem item = items[i];
                var key = new AffectTimerViewKey(ownerId, item.DisplayKey);
                _currentRenderKeys.Add(key);

                if (!_activeViews.TryGetValue(key, out var state) || state == null)
                {
                    state = new ViewState
                    {
                        View = GetOrCreateView(settings)
                    };
                    _activeViews[key] = state;
                    ownerKeys.Add(key);
                }

                state.OwnerId = ownerId;
                state.DisplayKey = item.DisplayKey;
                state.Target = target;
                state.Anchor = anchor;
                state.LineIndex = i;
                state.Text = AffectTimerTextFormatter.Format(
                    settings.timerTextFormatMode,
                    item.RemainingTime,
                    item.TotalDuration,
                    item.Stacks,
                    item.DisplayName);
                state.TextColor = ResolveTextColor(settings, item.DispelType);
                state.FontSize = Mathf.Max(1f, settings.timerFontSize);

                if (state.View != null)
                {
                    state.View.SetText(state.Text, state.TextColor, state.FontSize);
                    state.View.SetVisible(true);
                }
            }

            RemoveMissingOwnerKeys(ownerId, ownerKeys);
        }

        /// <summary>
        /// 특정 Presenter가 표시하던 모든 타이머 UI를 숨깁니다.
        /// </summary>
        /// <param name="ownerId">Presenter 인스턴스 ID입니다.</param>
        public void HideOwner(int ownerId)
        {
            if (ownerId == 0)
                return;

            if (!_keysByOwner.TryGetValue(ownerId, out var keys) || keys == null)
                return;

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (_activeViews.TryGetValue(key, out var state))
                {
                    RecycleView(state);
                    _activeViews.Remove(key);
                }
            }

            keys.Clear();
            _keysByOwner.Remove(ownerId);
        }

        /// <summary>
        /// 활성 View들의 월드 추적 위치를 매 프레임 갱신합니다.
        /// </summary>
        private void LateUpdate()
        {
            if (_activeViews.Count == 0)
                return;

            var settings = GGemCoAffectSettingsRuntime.GetOrLoad();
            if (settings == null || !settings.enableAffectTimerUi)
                return;

            EnsureCanvas(settings);
            if (_canvas == null || _canvasRect == null)
                return;

            _keysToRemove.Clear();
            foreach (var kv in _activeViews)
            {
                var state = kv.Value;
                if (state == null || state.View == null || state.Target == null)
                {
                    _keysToRemove.Add(kv.Key);
                    continue;
                }

                if (!TryUpdateViewPosition(state, settings))
                    state.View.SetVisible(false);
            }

            for (int i = 0; i < _keysToRemove.Count; i++)
            {
                var key = _keysToRemove[i];
                if (_activeViews.TryGetValue(key, out var state))
                    RecycleView(state);
                _activeViews.Remove(key);
                RemoveOwnerKey(key);
            }
        }

        /// <summary>
        /// Canvas와 Pool Root를 준비합니다.
        /// </summary>
        private void EnsureCanvas(GGemCoAffectSettings settings)
        {
            if (_canvas != null && _canvasRect != null)
                return;

            GameObject canvasObject = null;
            if (!string.IsNullOrWhiteSpace(settings.timerCanvasName))
                canvasObject = GameObject.Find(settings.timerCanvasName);

            if (canvasObject == null)
                canvasObject = GameObject.Find(DefaultCanvasName);

            if (canvasObject == null)
            {
                canvasObject = new GameObject(DefaultCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                if (Application.isPlaying)
                    DontDestroyOnLoad(canvasObject);
            }

            _canvas = canvasObject.GetComponent<Canvas>() ?? canvasObject.AddComponent<Canvas>();
            _canvasRect = canvasObject.transform as RectTransform;

            _canvas.renderMode = settings.timerCanvasRenderMode;
            _canvas.sortingOrder = settings.timerCanvasSortingOrder;
            if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _cachedCamera = ResolveCamera(settings);
                _canvas.worldCamera = _cachedCamera;
            }

            var scaler = canvasObject.GetComponent<CanvasScaler>() ?? canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = settings.timerCanvasReferenceResolution;
            scaler.matchWidthOrHeight = settings.timerCanvasMatchWidthOrHeight;

            var graphicRaycaster = canvasObject.GetComponent<GraphicRaycaster>() ?? canvasObject.AddComponent<GraphicRaycaster>();
            EnsurePoolRoot(canvasObject.transform);
        }

        /// <summary>
        /// 풀링 회수용 Root Transform을 준비합니다.
        /// </summary>
        private void EnsurePoolRoot(Transform canvasTransform)
        {
            if (_poolRoot != null)
                return;

            var poolGo = new GameObject("Pool", typeof(RectTransform));
            poolGo.transform.SetParent(canvasTransform, false);
            poolGo.SetActive(false);
            _poolRoot = poolGo.transform;
        }

        /// <summary>
        /// 설정에 맞는 카메라를 반환합니다.
        /// </summary>
        private Camera ResolveCamera(GGemCoAffectSettings settings)
        {
            if (settings.timerCanvasCamera != null)
                return settings.timerCanvasCamera;

            if (_cachedCamera != null)
                return _cachedCamera;

            return Camera.main;
        }

        /// <summary>
        /// View 위치를 월드 좌표에서 Canvas 로컬 좌표로 변환하여 적용합니다.
        /// </summary>
        private bool TryUpdateViewPosition(ViewState state, GGemCoAffectSettings settings)
        {
            Camera camera = ResolveCamera(settings);
            Vector3 worldPosition = state.Anchor != null
                ? state.Anchor.GetWorldPosition()
                : state.Target.position + settings.timerWorldOffset;

            Vector3 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            if (screenPosition.z < 0f)
                return false;

            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, uiCamera, out var localPoint))
                return false;

            Vector2 lineOffset = new(
                settings.timerScreenOffset.x,
                settings.timerScreenOffset.y + state.LineIndex * settings.timerLineSpacing);
            Vector2 anchoredPosition = localPoint + lineOffset;

            if (settings.timerClampToScreen)
                anchoredPosition = ClampToCanvas(anchoredPosition, settings.timerScreenPadding);

            state.View.SetAnchoredPosition(anchoredPosition);
            state.View.SetVisible(true);
            return true;
        }

        /// <summary>
        /// Canvas Rect 밖으로 UI가 과도하게 벗어나지 않도록 좌표를 보정합니다.
        /// </summary>
        private Vector2 ClampToCanvas(Vector2 position, Vector2 padding)
        {
            if (_canvasRect == null)
                return position;

            Rect rect = _canvasRect.rect;
            float minX = rect.xMin + Mathf.Max(0f, padding.x);
            float maxX = rect.xMax - Mathf.Max(0f, padding.x);
            float minY = rect.yMin + Mathf.Max(0f, padding.y);
            float maxY = rect.yMax - Mathf.Max(0f, padding.y);

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            return position;
        }

        /// <summary>
        /// 표시 타입에 맞는 텍스트 색상을 반환합니다.
        /// </summary>
        private static Color ResolveTextColor(GGemCoAffectSettings settings, DispelType dispelType)
        {
            return dispelType switch
            {
                DispelType.Buff => settings.timerBuffTextColor,
                DispelType.Debuff => settings.timerDebuffTextColor,
                _ => settings.timerDefaultTextColor
            };
        }

        /// <summary>
        /// 풀에서 View를 가져오거나 새로 생성합니다.
        /// </summary>
        private AffectTimerTextView GetOrCreateView(GGemCoAffectSettings settings)
        {
            while (_viewPool.Count > 0)
            {
                var pooled = _viewPool.Pop();
                if (pooled == null)
                    continue;

                pooled.transform.SetParent(_canvasRect, false);
                pooled.Initialize();
                return pooled;
            }

            AffectTimerTextView view = null;
            if (settings.timerTextPrefab != null)
            {
                GameObject go = Instantiate(settings.timerTextPrefab, _canvasRect);
                view = go.GetComponent<AffectTimerTextView>() ?? go.AddComponent<AffectTimerTextView>();
            }
            else
            {
                view = CreateDefaultTextView();
            }

            view.Initialize();
            return view;
        }

        /// <summary>
        /// 별도 프리팹이 없을 때 사용할 기본 TMP_Text View를 생성합니다.
        /// </summary>
        private AffectTimerTextView CreateDefaultTextView()
        {
            var go = new GameObject(DefaultTextName, typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI), typeof(AffectTimerTextView));
            go.transform.SetParent(_canvasRect, false);

            var rect = go.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(180f, 28f);
            }

            var text = go.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontSize = 20f;

            return go.GetComponent<AffectTimerTextView>();
        }

        /// <summary>
        /// View를 풀로 회수합니다.
        /// </summary>
        private void RecycleView(ViewState state)
        {
            if (state == null || state.View == null)
                return;

            state.View.Clear();
            if (_poolRoot != null)
                state.View.transform.SetParent(_poolRoot, false);
            _viewPool.Push(state.View);
        }

        /// <summary>
        /// Presenter별 Key 목록을 반환합니다.
        /// </summary>
        private List<AffectTimerViewKey> GetOwnerKeyList(int ownerId)
        {
            if (!_keysByOwner.TryGetValue(ownerId, out var list) || list == null)
            {
                list = new List<AffectTimerViewKey>(4);
                _keysByOwner[ownerId] = list;
            }
            return list;
        }

        /// <summary>
        /// 이번 렌더링에 포함되지 않은 기존 Key를 제거합니다.
        /// </summary>
        private void RemoveMissingOwnerKeys(int ownerId, List<AffectTimerViewKey> ownerKeys)
        {
            if (ownerKeys == null)
                return;

            _keysToRemove.Clear();
            for (int i = 0; i < ownerKeys.Count; i++)
            {
                var key = ownerKeys[i];
                if (!_currentRenderKeys.Contains(key))
                    _keysToRemove.Add(key);
            }

            for (int i = 0; i < _keysToRemove.Count; i++)
            {
                var key = _keysToRemove[i];
                if (_activeViews.TryGetValue(key, out var state))
                    RecycleView(state);
                _activeViews.Remove(key);
                ownerKeys.Remove(key);
            }

            if (ownerKeys.Count == 0)
                _keysByOwner.Remove(ownerId);
        }

        /// <summary>
        /// Key 목록에서 지정 Key를 제거합니다.
        /// </summary>
        private void RemoveOwnerKey(AffectTimerViewKey key)
        {
            if (!_keysByOwner.TryGetValue(key.OwnerId, out var list) || list == null)
                return;

            list.Remove(key);
            if (list.Count == 0)
                _keysByOwner.Remove(key.OwnerId);
        }

        /// <summary>
        /// 활성 View Dictionary에 사용할 복합 Key입니다.
        /// </summary>
        private readonly struct AffectTimerViewKey : IEquatable<AffectTimerViewKey>
        {
            public readonly int OwnerId;
            public readonly int DisplayKey;

            public AffectTimerViewKey(int ownerId, int displayKey)
            {
                OwnerId = ownerId;
                DisplayKey = displayKey;
            }

            public bool Equals(AffectTimerViewKey other)
            {
                return OwnerId == other.OwnerId && DisplayKey == other.DisplayKey;
            }

            public override bool Equals(object obj)
            {
                return obj is AffectTimerViewKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (OwnerId * 397) ^ DisplayKey;
                }
            }
        }

        /// <summary>
        /// 활성 View의 위치 갱신에 필요한 상태입니다.
        /// </summary>
        private sealed class ViewState
        {
            public int OwnerId;
            public int DisplayKey;
            public Transform Target;
            public AffectTimerAnchor Anchor;
            public int LineIndex;
            public string Text;
            public Color TextColor;
            public float FontSize;
            public AffectTimerTextView View;
        }
    }
}
