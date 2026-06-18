using System;

namespace GGemCo2DAffect
{
    /// <summary>
    /// UI에서 어펙트 버프/디버프 아이콘을 표시하기 위한 최소 데이터입니다.
    /// </summary>
    /// <remarks>
    /// 실제 적용, 만료, 스택 규칙은 <see cref="AffectComponent"/>가 관리하고
    /// 이 구조체는 프리젠터가 윈도우에 전달하는 렌더링 스냅샷으로만 사용합니다.
    /// </remarks>
    [Serializable]
    public readonly struct AffectUiItem
    {
        /// <summary>표시할 어펙트 정의 UID입니다.</summary>
        public readonly int AffectUid;

        /// <summary>UI에 표시할 합산 스택 수입니다.</summary>
        public readonly int Stacks;

        /// <summary>쿨타임 게이지에 반영할 남은 시간입니다.</summary>
        public readonly float RemainingTime;

        /// <summary>쿨타임 게이지의 기준이 되는 전체 지속 시간입니다.</summary>
        public readonly float TotalDuration;

        /// <summary>어펙트 아이콘에 쿨타임 게이지를 표시할지 여부입니다.</summary>
        public readonly bool ShowCoolTimeGauge;

        /// <summary>어펙트 아이콘 아틀라스에서 조회할 아이콘 키입니다.</summary>
        public readonly string IconKey;

        /// <summary>버프/디버프 타입 등을 보조 표시할 데코레이터 데이터입니다.</summary>
        public readonly AffectUiDecoratorData Decorator;

        /// <summary>
        /// 어펙트 UI 아이템 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="affectUid">표시할 어펙트 정의 UID입니다.</param>
        /// <param name="stacks">UI에 표시할 합산 스택 수입니다.</param>
        /// <param name="remainingTime">남은 지속 시간입니다.</param>
        /// <param name="totalDuration">전체 지속 시간입니다.</param>
        /// <param name="showCoolTimeGauge">쿨타임 게이지 표시 여부입니다.</param>
        /// <param name="iconKey">아이콘 Sprite 조회 키입니다.</param>
        /// <param name="decorator">보조 데코레이터 표시 데이터입니다.</param>
        public AffectUiItem(
            int affectUid,
            int stacks,
            float remainingTime,
            float totalDuration,
            bool showCoolTimeGauge,
            string iconKey,
            AffectUiDecoratorData decorator)
        {
            AffectUid = affectUid;
            Stacks = stacks;
            RemainingTime = remainingTime;
            TotalDuration = totalDuration;
            ShowCoolTimeGauge = showCoolTimeGauge;
            IconKey = iconKey;
            Decorator = decorator;
        }
    }
}
