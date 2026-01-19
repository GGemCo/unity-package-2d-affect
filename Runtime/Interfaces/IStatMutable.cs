namespace GGemCo2DAffect
{
    /// <summary>
    /// 대상의 스탯(Stat)을 변경·조회할 수 있는 변경 가능 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Affect 시스템은 스탯 계산의 구체적인 규칙(가산/곱연산 순서, 캡, 의존 관계 등)에 직접 관여하지 않고,
    /// 이 인터페이스를 통해 스탯 모디파이어의 적용과 제거를 요청합니다.
    /// </remarks>
    public interface IStatMutable
    {
        /// <summary>
        /// 대상의 특정 스탯에 모디파이어를 적용합니다.
        /// </summary>
        /// <param name="statId">변경할 스탯을 식별하는 ID입니다.</param>
        /// <param name="value">적용할 값(배율 적용 후 최종 값)입니다.</param>
        /// <param name="statValueType">값의 해석 방식(예: 고정값, 비율 등)을 나타냅니다.</param>
        /// <param name="operation">스탯에 값을 적용하는 연산 방식입니다.</param>
        /// <returns>
        /// 적용된 모디파이어를 식별하기 위한 토큰 객체입니다.
        /// 이후 <see cref="RemoveModifier"/> 호출 시 해당 토큰을 전달하여 제거합니다.
        /// </returns>
        object ApplyModifier(string statId, float value, StatValueType statValueType, StatOperation operation);

        /// <summary>
        /// <see cref="ApplyModifier"/>로 적용된 스탯 모디파이어를 제거합니다.
        /// </summary>
        /// <param name="token">
        /// <see cref="ApplyModifier"/> 호출 시 반환된 토큰 객체입니다.
        /// 유효하지 않거나 이미 제거된 토큰인 경우, 구현체에서 안전하게 무시할 수 있습니다.
        /// </param>
        void RemoveModifier(object token);

        /// <summary>
        /// 현재 적용된 모든 모디파이어를 반영하여 스탯 값을 재계산합니다.
        /// </summary>
        /// <remarks>
        /// 여러 모디파이어를 연속으로 적용/제거하는 경우,
        /// 성능을 위해 외부에서 호출 시점을 제어하는 것이 바람직할 수 있습니다.
        /// </remarks>
        void Recalculate();

        /// <summary>
        /// 특정 스탯의 현재 계산된 값을 반환합니다.
        /// </summary>
        /// <param name="statId">조회할 스탯을 식별하는 ID입니다.</param>
        /// <returns>현재 최종 계산된 스탯 값입니다.</returns>
        float GetValue(string statId);
    }
}