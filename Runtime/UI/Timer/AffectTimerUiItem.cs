namespace GGemCo2DAffect
{
    /// <summary>
    /// 캐릭터 머리 위 어펙트 타이머 UI에 전달되는 렌더링 데이터입니다.
    /// </summary>
    public readonly struct AffectTimerUiItem
    {
        public readonly int DisplayKey;
        public readonly int AffectUid;
        public readonly int Stacks;
        public readonly float RemainingTime;
        public readonly float TotalDuration;
        public readonly string DisplayName;
        public readonly DispelType DispelType;

        /// <summary>
        /// 타이머 UI 1줄에 필요한 값을 생성합니다.
        /// </summary>
        public AffectTimerUiItem(
            int displayKey,
            int affectUid,
            int stacks,
            float remainingTime,
            float totalDuration,
            string displayName,
            DispelType dispelType)
        {
            DisplayKey = displayKey;
            AffectUid = affectUid;
            Stacks = stacks;
            RemainingTime = remainingTime;
            TotalDuration = totalDuration;
            DisplayName = displayName;
            DispelType = dispelType;
        }
    }
}
