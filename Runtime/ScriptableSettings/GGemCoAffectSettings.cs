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

    /// <summary>
    /// Affect 디버그 표시와 관련된 설정입니다.
    /// 릴리즈 빌드에서는 <see cref="EnableAffectDebugHud"/> 가 항상 false 로 해석됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = ConfigScriptableObjectAffect.Main.FileName, menuName = ConfigScriptableObjectAffect.Main.MenuName, order = ConfigScriptableObjectAffect.Main.Ordering)]
    public sealed class GGemCoAffectSettings : ScriptableObject
    {
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
        }

        private void Reset()
        {
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
        }
    }
}
