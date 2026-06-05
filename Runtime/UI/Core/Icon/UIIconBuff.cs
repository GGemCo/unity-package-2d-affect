using System.Threading.Tasks;
using GGemCo2DCore;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 플레이어 버프 정보 윈도우에서 사용하는 어펙트 아이콘입니다.
    /// </summary>
    /// <remarks>
    /// 테이블 row를 직접 조회하지 않고 <see cref="AffectUiItem"/> 스냅샷만 사용합니다.
    /// 아이콘 Sprite는 어펙트 아이콘 아틀라스에서 비동기로 조회하며,
    /// 슬롯 재사용 시 오래된 비동기 요청이 현재 아이콘에 적용되지 않도록 요청 버전을 확인합니다.
    /// </remarks>
    public class UIIconBuff : UIIcon
    {
        [Header("Affect Decorator")]
        [Tooltip("버프/디버프 타입 보조 아이콘입니다. 비어 있으면 런타임에 자동 생성합니다.")]
        [SerializeField] private Image imageDecorator;

        private string _iconKey;
        private float _totalDuration;
        private float _remainingTime;
        private AffectUiDecoratorData _currentDecorator;
        private int _buffIconImageRequestVersion;

        private int _cachedAffectUid;
        private string _cachedIconKey;
        private int _cachedStacks;
        private float _cachedTotalDuration;
        private bool _coolTimeHandlerStarted;

        /// <summary>
        /// 버프 아이콘의 공통 타입과 보조 데코레이터 초기 상태를 설정합니다.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            windowUid = UIWindowConstants.WindowUid.PlayerBuffInfo;
            IconType = IconConstants.Type.Buff;
            EnsureDecoratorImage();
            ClearDecorator();
        }

        /// <summary>
        /// Core 공통 아이콘 초기화 후 버프 아이콘 타입을 다시 지정합니다.
        /// </summary>
        protected override void OnInitialize()
        {
            IconType = IconConstants.Type.Buff;
        }

        /// <summary>
        /// 어펙트 UI 스냅샷을 현재 아이콘에 바인딩합니다.
        /// </summary>
        /// <param name="item">프리젠터가 생성한 어펙트 UI 스냅샷입니다.</param>
        public void Bind(in AffectUiItem item)
        {
            if (item.AffectUid <= 0)
            {
                return;
            }

            IconType = IconConstants.Type.Buff;

            int newUid = item.AffectUid;
            string newIconKey = item.IconKey;
            float newTotal = Mathf.Max(0f, item.TotalDuration);
            float newRemain = Mathf.Max(0f, item.RemainingTime);
            int newStacks = Mathf.Max(1, item.Stacks);

            bool uidChanged = _cachedAffectUid != newUid;
            bool iconChanged = uidChanged || !string.Equals(_cachedIconKey, newIconKey);
            bool totalChanged = !_coolTimeHandlerStarted || !Mathf.Approximately(_cachedTotalDuration, newTotal);

            uid = newUid;
            _iconKey = newIconKey;
            _totalDuration = newTotal;
            _remainingTime = newRemain;

            if (iconChanged)
            {
                // Sprite 로드는 상대적으로 비용이 있으므로 UID 또는 아이콘 키가 바뀐 경우에만 갱신합니다.
                UpdateInfo();
                _cachedAffectUid = newUid;
                _cachedIconKey = newIconKey;
            }

            if (_cachedStacks != newStacks)
            {
                SetCount(newStacks);
                _cachedStacks = newStacks;
            }

            ApplyDecorator(item.Decorator);
            SyncCoolTimeGauge(totalChanged);
        }

        /// <summary>
        /// 현재 아이콘의 쿨타임 표시를 정리합니다.
        /// </summary>
        public void ClearCoolTime()
        {
            UIIconCoolTimeManager mgr = SceneGame.Instance != null ? SceneGame.Instance.uIIconCoolTimeManager : null;
            if (mgr != null)
            {
                mgr.ResetCoolTime(windowUid, uid);
            }

            _coolTimeHandlerStarted = false;
            _cachedTotalDuration = 0f;
        }

        /// <summary>
        /// 보조 데코레이터 표시를 숨기고 Sprite 참조를 해제합니다.
        /// </summary>
        public void ClearDecorator()
        {
            _currentDecorator = AffectUiDecoratorData.Hidden;
            if (imageDecorator == null)
            {
                return;
            }

            imageDecorator.sprite = null;
            imageDecorator.enabled = false;
            imageDecorator.gameObject.SetActive(false);
        }

        /// <summary>
        /// 슬롯 재사용을 위해 아이콘 바인딩 캐시와 보조 표시 상태를 초기화합니다.
        /// </summary>
        public void ResetBindingCache()
        {
            _buffIconImageRequestVersion++;
            _cachedAffectUid = 0;
            _cachedIconKey = null;
            _cachedStacks = 0;
            _cachedTotalDuration = 0f;
            _coolTimeHandlerStarted = false;
            ClearDecorator();
        }

        /// <summary>
        /// 지속 시간 정보를 Core 쿨타임 매니저와 동기화합니다.
        /// </summary>
        /// <param name="totalChanged">전체 지속 시간 기준이 바뀌었는지 여부입니다.</param>
        private void SyncCoolTimeGauge(bool totalChanged)
        {
            UIIconCoolTimeManager mgr = SceneGame.Instance != null ? SceneGame.Instance.uIIconCoolTimeManager : null;
            if (mgr == null)
            {
                return;
            }

            if (_totalDuration > 0f)
            {
                if (totalChanged)
                {
                    mgr.StartHandler(windowUid, this, _totalDuration);
                    _cachedTotalDuration = _totalDuration;
                    _coolTimeHandlerStarted = true;
                }

                // 남은 시간은 구조 변화가 없어도 계속 줄어들므로 Render 주기마다 동기화합니다.
                mgr.SetRemainCoolTime(windowUid, uid, _remainingTime);
                return;
            }

            if (_coolTimeHandlerStarted)
            {
                mgr.ResetCoolTime(windowUid, uid);
                _cachedTotalDuration = 0f;
                _coolTimeHandlerStarted = false;
            }
        }

        /// <summary>
        /// 어펙트 타입 데코레이터를 현재 아이콘에 적용합니다.
        /// </summary>
        /// <param name="decorator">표시할 데코레이터 데이터입니다.</param>
        private void ApplyDecorator(in AffectUiDecoratorData decorator)
        {
            EnsureDecoratorImage();
            if (imageDecorator == null)
            {
                return;
            }

            if (!decorator.Visible || decorator.Sprite == null)
            {
                if (_currentDecorator.Visible)
                {
                    ClearDecorator();
                }

                return;
            }

            imageDecorator.sprite = decorator.Sprite;
            imageDecorator.enabled = true;
            imageDecorator.gameObject.SetActive(true);

            RectTransform rect = imageDecorator.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = decorator.Size;
            rect.anchoredPosition = ResolveDecoratorPosition(decorator.Anchor, decorator.Offset);

            _currentDecorator = decorator;
        }

        /// <summary>
        /// 데코레이터 기준 위치와 오프셋을 이용해 아이콘 내부 배치 좌표를 계산합니다.
        /// </summary>
        /// <param name="anchor">아이콘 기준 배치 위치입니다.</param>
        /// <param name="offset">추가 오프셋입니다.</param>
        /// <returns>데코레이터 anchoredPosition 값입니다.</returns>
        private Vector2 ResolveDecoratorPosition(AffectUiDecoratorAnchor anchor, Vector2 offset)
        {
            RectTransform iconRect = GetComponent<RectTransform>();
            float width = 0f;
            float height = 0f;
            if (iconRect != null)
            {
                width = Mathf.Abs(iconRect.rect.width);
                height = Mathf.Abs(iconRect.rect.height);
            }

            float halfX = width * 0.25f;
            float halfY = height * 0.25f;
            Vector2 basePosition = anchor switch
            {
                AffectUiDecoratorAnchor.LeftBottom => new Vector2(-halfX, -halfY),
                AffectUiDecoratorAnchor.RightBottom => new Vector2(halfX, -halfY),
                AffectUiDecoratorAnchor.LeftTop => new Vector2(-halfX, halfY),
                AffectUiDecoratorAnchor.RightTop => new Vector2(halfX, halfY),
                _ => new Vector2(halfX, -halfY)
            };

            return basePosition + offset;
        }

        /// <summary>
        /// 프리팹에 데코레이터 Image가 없을 경우 런타임에 생성합니다.
        /// </summary>
        private void EnsureDecoratorImage()
        {
            if (imageDecorator != null)
            {
                return;
            }

            Transform child = transform.Find("AffectTypeDecorator");
            if (child != null)
            {
                imageDecorator = child.GetComponent<Image>();
                if (imageDecorator != null)
                {
                    return;
                }
            }

            GameObject go = new GameObject("AffectTypeDecorator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);

            imageDecorator = go.GetComponent<Image>();
            imageDecorator.raycastTarget = false;
            imageDecorator.enabled = false;
            go.SetActive(false);
        }

        /// <summary>
        /// 어펙트 아이콘 아틀라스에서 사용할 Sprite 이름을 반환합니다.
        /// </summary>
        /// <returns>파일명만 남긴 아이콘 Sprite 이름입니다.</returns>
        protected override string GetIconImagePath()
        {
            return FileHelper.GetFileName(_iconKey);
        }

        /// <summary>
        /// 어펙트 아이콘 Sprite를 캐시 또는 비동기 로딩 경로로 갱신합니다.
        /// </summary>
        protected override void UpdateIconImage()
        {
            if (ImageIcon == null)
            {
                return;
            }

            int requestVersion = ++_buffIconImageRequestVersion;
            string key = GetIconImagePath();
            if (string.IsNullOrEmpty(key))
            {
                ImageIcon.sprite = spriteBlank;
                CacheNormalIconSprite(spriteBlank);
                return;
            }

            AddressableIconSpriteRequest request = new(AddressableIconAtlasType.AffectIcon, key);
            if (AddressableIconSpriteService.TryGetCachedSprite(request, out Sprite cachedSprite))
            {
                ImageIcon.sprite = cachedSprite;
                CacheNormalIconSprite(cachedSprite);
                return;
            }

            // Affect Atlas는 시작 로딩에서 제외될 수 있으므로 공용 Provider 경로로 비동기 로딩합니다.
            ImageIcon.sprite = spriteBlank;
            CacheNormalIconSprite(spriteBlank);
            _ = UpdateBuffIconImageAsync(requestVersion, request);
        }

        /// <summary>
        /// 어펙트 아이콘 Atlas가 아직 준비되지 않은 경우 비동기 로드 후 현재 요청에만 Sprite를 적용합니다.
        /// </summary>
        /// <param name="requestVersion">아이콘 요청 버전입니다.</param>
        /// <param name="request">Affect 아이콘 Sprite 요청 정보입니다.</param>
        private async Task UpdateBuffIconImageAsync(int requestVersion, AddressableIconSpriteRequest request)
        {
            try
            {
                Sprite sprite = await AddressableIconSpriteService.LoadSpriteAsync(request);
                if (this == null || requestVersion != _buffIconImageRequestVersion || ImageIcon == null)
                {
                    return;
                }

                ImageIcon.sprite = sprite != null ? sprite : spriteBlank;
                CacheNormalIconSprite(ImageIcon.sprite);
            }
            catch (System.Exception ex)
            {
                GcLogger.LogWarning($"버프 아이콘 비동기 갱신 중 오류가 발생했습니다. sprite={request.SpriteName}, error={ex.Message}");
            }
        }
    }
}
