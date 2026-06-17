namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect가 데미지 파이프라인을 거치지 않고 대상의 속성 게이지를 직접 누적하기 위한 선택 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스를 구현한 대상만 <see cref="ModifierKind.ElementGauge"/> Modifier를 처리합니다.
    /// HP 데미지, 피격 반응, Hit VFX와 분리하여 게이지 변화만 전달하기 위한 계약입니다.
    /// </remarks>
    public interface IElementGaugeReceiver
    {
        /// <summary>
        /// 지정한 속성 게이지를 직접 누적합니다.
        /// </summary>
        /// <param name="elementTypeId">누적할 속성 타입 ID입니다.</param>
        /// <param name="gaugeValue">속성 게이지에 더할 수치입니다.</param>
        /// <param name="source">게이지를 발생시킨 원인 객체입니다.</param>
        /// <param name="sourceAffectUid">게이지를 발생시킨 Affect UID입니다.</param>
        /// <returns>게이지 값, 임계 상태, 반복 피격 상태 중 하나라도 변화가 발생했으면 true입니다.</returns>
        bool AccumulateElementGauge(
            string elementTypeId,
            float gaugeValue,
            object source,
            int sourceAffectUid);
    }
}
