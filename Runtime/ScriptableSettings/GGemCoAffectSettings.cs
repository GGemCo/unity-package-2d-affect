using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    public enum AffectDebugSortMode
    {
        RemainingTimeAsc,
        RemainingTimeDesc,
        Name,
        ApplyOrder,
    }

    [System.Serializable]
    public sealed class AffectTypeIconStyle
    {
        [Tooltip("표시 정책이 적용될 DispelType")]
        public DispelType dispelType = DispelType.None;

        [Tooltip("타입 보조 아이콘 사용 여부")]
        public bool enabled = true;

        [Tooltip("보조 아이콘 Sprite")]
        public Sprite sprite;

        [Tooltip("보조 아이콘 크기")]
        public Vector2 size = new(18f, 18f);

        [Tooltip("메인 아이콘 중심 기준 배치 위치")]
        public AffectUiDecoratorAnchor anchor = AffectUiDecoratorAnchor.RightBottom;

        [Tooltip("anchor 기준 추가 오프셋")]
        public Vector2 offset = Vector2.zero;

        public bool IsValid => enabled && sprite != null;

        public void Normalize()
        {
            if (size.x <= 0f) size.x = 18f;
            if (size.y <= 0f) size.y = 18f;
        }
    }

    /// <summary>
    /// Affect 디버그 표시와 관련된 설정입니다.
    /// 릴리즈 빌드에서는 <see cref="GGemCoAffectSettings.enableAffectDebugHud"/> 가 항상 false 로 해석됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = ConfigScriptableObjectAffect.Main.FileName, menuName = ConfigScriptableObjectAffect.Main.MenuName, order = ConfigScriptableObjectAffect.Main.Ordering)]
    public sealed class GGemCoAffectSettings : ScriptableObject
    {
        [Header("Affect UI Type Icon")]
        [Tooltip("버프/디버프 타입 보조 아이콘 스타일 목록")]
        public AffectTypeIconStyle[] typeIconStyles;


        [Header("Affect Timer UI")]
        [Tooltip("캐릭터를 따라다니는 Affect 남은 시간 TMP_Text UI 사용 여부")]
        public bool enableAffectTimerUi = true;

        [Tooltip("Affect 적용 시 AffectTimerUiPresenter 를 대상 캐릭터에 자동 부착할지 여부")]
        public bool timerAutoAddPresenter = true;

        [Tooltip("타이머 UI TMP_Text 프리팹입니다. 비어 있으면 런타임에 기본 TMP_Text 를 생성합니다.")]
        public GameObject timerTextPrefab;

        [Tooltip("타이머 UI Canvas 이름입니다. 같은 이름의 Canvas가 있으면 재사용하고, 없으면 자동 생성합니다.")]
        public string timerCanvasName = "AffectTimerUiCanvas";

        [Tooltip("타이머 UI Canvas 렌더 모드")]
        public RenderMode timerCanvasRenderMode = RenderMode.ScreenSpaceOverlay;

        [Tooltip("Screen Space Camera / World Space Canvas에서 사용할 UI 카메라입니다. 비어 있으면 Camera.main 을 사용합니다.")]
        public Camera timerCanvasCamera;

        [Tooltip("타이머 UI Canvas Sorting Order")]
        public int timerCanvasSortingOrder = 5000;

        [Tooltip("타이머 UI Canvas 기준 해상도")]
        public Vector2 timerCanvasReferenceResolution = new(1920f, 1080f);

        [Range(0f, 1f)]
        [Tooltip("Canvas Scaler matchWidthOrHeight 값")]
        public float timerCanvasMatchWidthOrHeight = 0.5f;

        [Tooltip("타이머 UI 갱신 주기(초)")]
        public float timerRefreshInterval = 0.10f;

        [Tooltip("타이머 시간 감소 표시 주기에 unscaledDeltaTime 을 사용할지 여부")]
        public bool timerUseUnscaledTime = true;

        [Tooltip("기본 남은 시간 표시 형식")]
        public AffectTimerTextFormatMode timerTextFormatMode = AffectTimerTextFormatMode.SecondsDecimal;

        [Tooltip("같은 AffectUid 를 하나의 타이머 줄로 집계할지 여부")]
        public bool timerAggregateSameAffectUid = true;

        [Tooltip("버프 타입 Affect 타이머 표시 여부")]
        public bool timerShowBuff;

        [Tooltip("디버프 타입 Affect 타이머 표시 여부")]
        public bool timerShowDebuff = true;

        [Tooltip("None 타입 Affect 타이머 표시 여부")]
        public bool timerShowNoneType;

        [Tooltip("대상 1개당 표시할 최대 타이머 줄 수")]
        public int timerMaxVisiblePerTarget = 3;

        [Tooltip("타이머 표시 정렬 기준")]
        public AffectDebugSortMode timerSortMode = AffectDebugSortMode.RemainingTimeAsc;

        [Tooltip("이 시간 이하로 남은 Affect는 표시하지 않습니다.")]
        public float timerHideBelowSeconds = 0.05f;

        [Tooltip("캐릭터 Transform 기준 월드 오프셋입니다. AffectTimerAnchor 가 있으면 Anchor 설정을 우선 사용합니다.")]
        public Vector3 timerWorldOffset = new(0f, 1.6f, 0f);

        [Tooltip("화면 좌표 변환 후 추가로 적용할 Canvas 로컬 오프셋입니다.")]
        public Vector2 timerScreenOffset = Vector2.zero;

        [Tooltip("여러 Affect를 표시할 때 줄 사이 간격입니다.")]
        public float timerLineSpacing = 22f;

        [Tooltip("타이머 UI가 화면 밖으로 벗어나지 않도록 Canvas 영역 안으로 보정할지 여부")]
        public bool timerClampToScreen = true;

        [Tooltip("화면 안쪽으로 보정할 때 사용할 여백입니다.")]
        public Vector2 timerScreenPadding = new(24f, 24f);

        [Tooltip("타이머 기본 텍스트 색상")]
        public Color timerDefaultTextColor = Color.white;

        [Tooltip("버프 타이머 텍스트 색상")]
        public Color timerBuffTextColor = new(0.4f, 1f, 0.45f, 1f);

        [Tooltip("디버프 타이머 텍스트 색상")]
        public Color timerDebuffTextColor = new(1f, 0.45f, 0.45f, 1f);

        [Tooltip("타이머 TMP_Text 폰트 크기")]
        public float timerFontSize = 20f;

        [Header("Debug HUD")]
        [SerializeField, DebugOption("Affect Debug HUD 전체 사용 여부")]
        private bool enableAffectDebugHud;
        public bool EnableAffectDebugHud => DebugOptionRuntimeUtility.Resolve(enableAffectDebugHud);

        [Tooltip("몬스터에 적용된 Affect 만 표시할지 여부")]
        public bool showOnlyMonsters = true;

        [Tooltip("같은 AffectUid 를 하나로 묶어 표시할지 여부")]
        public bool aggregateSameAffectUid = true;

        [Tooltip("몬스터 1마리당 화면에 표시할 최대 줄 수")]
        public int maxVisibleLinesPerMonster = 4;

        [Tooltip("표시 정렬 기준")]
        public AffectDebugSortMode sortMode = AffectDebugSortMode.RemainingTimeAsc;

        [Tooltip("스택 수 표시 여부")]
        public bool showStacks = true;

        [Tooltip("Affect GroupId 표시 여부")]
        public bool showGroupId;

        [Tooltip("Affect Uid 표시 여부")]
        public bool showAffectUid;

        [Tooltip("Debug HUD 갱신 주기(초)")]
        public float refreshInterval = 0.10f;

        [Tooltip("최대 표시할 몬스터 수")]
        public int maxVisibleMonsters = 8;

        [Tooltip("남은 시간이 0 이하인 Affect 도 강제로 표시할지 여부")]
        public bool includeExpiredEntries;

        private void OnEnable()
        {
            if (maxVisibleLinesPerMonster <= 0) maxVisibleLinesPerMonster = 4;
            if (refreshInterval <= 0f) refreshInterval = 0.10f;
            if (maxVisibleMonsters <= 0) maxVisibleMonsters = 8;
            NormalizeTimerUiSettings();
            NormalizeTypeIconStyles();
        }

        private void Reset()
        {
            typeIconStyles = new[]
            {
                new AffectTypeIconStyle
                {
                    dispelType = DispelType.Buff,
                    enabled = true,
                    anchor = AffectUiDecoratorAnchor.LeftBottom,
                    size = new Vector2(18f, 18f),
                    offset = Vector2.zero
                },
                new AffectTypeIconStyle
                {
                    dispelType = DispelType.Debuff,
                    enabled = true,
                    anchor = AffectUiDecoratorAnchor.RightBottom,
                    size = new Vector2(18f, 18f),
                    offset = Vector2.zero
                }
            };

            enableAffectDebugHud = false;
            showOnlyMonsters = true;
            aggregateSameAffectUid = true;
            maxVisibleLinesPerMonster = 4;
            sortMode = AffectDebugSortMode.RemainingTimeAsc;
            showStacks = true;
            showGroupId = false;
            showAffectUid = false;
            refreshInterval = 0.10f;
            maxVisibleMonsters = 8;
            includeExpiredEntries = false;

            enableAffectTimerUi = true;
            timerAutoAddPresenter = true;
            timerTextPrefab = null;
            timerCanvasName = "AffectTimerUiCanvas";
            timerCanvasRenderMode = RenderMode.ScreenSpaceOverlay;
            timerCanvasCamera = null;
            timerCanvasSortingOrder = 5000;
            timerCanvasReferenceResolution = new Vector2(1920f, 1080f);
            timerCanvasMatchWidthOrHeight = 0.5f;
            timerRefreshInterval = 0.10f;
            timerUseUnscaledTime = true;
            timerTextFormatMode = AffectTimerTextFormatMode.SecondsDecimal;
            timerAggregateSameAffectUid = true;
            timerShowBuff = false;
            timerShowDebuff = true;
            timerShowNoneType = false;
            timerMaxVisiblePerTarget = 3;
            timerSortMode = AffectDebugSortMode.RemainingTimeAsc;
            timerHideBelowSeconds = 0.05f;
            timerWorldOffset = new Vector3(0f, 1.6f, 0f);
            timerScreenOffset = Vector2.zero;
            timerLineSpacing = 22f;
            timerClampToScreen = true;
            timerScreenPadding = new Vector2(24f, 24f);
            timerDefaultTextColor = Color.white;
            timerBuffTextColor = new Color(0.4f, 1f, 0.45f, 1f);
            timerDebuffTextColor = new Color(1f, 0.45f, 0.45f, 1f);
            timerFontSize = 20f;

            NormalizeTimerUiSettings();
            NormalizeTypeIconStyles();
        }

        public bool TryGetTypeIconStyle(DispelType dispelType, out AffectTypeIconStyle style)
        {
            style = null;
            if (typeIconStyles == null || typeIconStyles.Length == 0)
                return false;

            for (int i = 0; i < typeIconStyles.Length; i++)
            {
                var candidate = typeIconStyles[i];
                if (candidate == null || candidate.dispelType != dispelType)
                    continue;

                candidate.Normalize();
                if (!candidate.IsValid)
                    return false;

                style = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Affect 타이머 UI 설정값이 유효 범위를 벗어나지 않도록 보정합니다.
        /// </summary>
        private void NormalizeTimerUiSettings()
        {
            if (string.IsNullOrWhiteSpace(timerCanvasName))
                timerCanvasName = "AffectTimerUiCanvas";

            if (timerCanvasReferenceResolution.x <= 0f)
                timerCanvasReferenceResolution.x = 1920f;
            if (timerCanvasReferenceResolution.y <= 0f)
                timerCanvasReferenceResolution.y = 1080f;

            timerCanvasMatchWidthOrHeight = Mathf.Clamp01(timerCanvasMatchWidthOrHeight);

            if (timerRefreshInterval <= 0f)
                timerRefreshInterval = 0.10f;
            if (timerMaxVisiblePerTarget <= 0)
                timerMaxVisiblePerTarget = 1;
            if (timerLineSpacing <= 0f)
                timerLineSpacing = 22f;
            if (timerHideBelowSeconds < 0f)
                timerHideBelowSeconds = 0f;
            if (timerScreenPadding.x < 0f)
                timerScreenPadding.x = 0f;
            if (timerScreenPadding.y < 0f)
                timerScreenPadding.y = 0f;
            if (timerFontSize <= 0f)
                timerFontSize = 20f;
        }

        private void NormalizeTypeIconStyles()
        {
            if (typeIconStyles == null)
                return;

            for (int i = 0; i < typeIconStyles.Length; i++)
            {
                typeIconStyles[i]?.Normalize();
            }
        }
    }
}
