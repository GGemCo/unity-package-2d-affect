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
