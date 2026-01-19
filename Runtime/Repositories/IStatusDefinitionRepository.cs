namespace GGemCo2DAffect
{
    /// <summary>
    /// 스탯(Stat), 데미지 타입(DamageType), 상태(State) 정의의 유효성을 조회하고,
    /// 저항(Resistance) 규칙을 제공하는 저장소 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// - Affect 시스템은 각 정의의 실제 저장 방식이나 계산 로직에 직접 의존하지 않습니다.
    /// - 구현체는 인메모리, 에셋 기반, 서버 동기화 등 다양한 형태가 될 수 있습니다.
    /// - 저항 계산 방식은 구현체에서 자유롭게 확장할 수 있습니다.
    /// </remarks>
    public interface IStatusDefinitionRepository
    {
        /// <summary>
        /// 스탯 ID가 유효한지 여부를 반환합니다.
        /// </summary>
        /// <param name="statId">확인할 스탯 ID입니다.</param>
        /// <returns>등록된 스탯이면 true, 아니면 false를 반환합니다.</returns>
        bool IsValidStat(string statId);

        /// <summary>
        /// 데미지 타입 ID가 유효한지 여부를 반환합니다.
        /// </summary>
        /// <param name="damageTypeId">확인할 데미지 타입 ID입니다.</param>
        /// <returns>등록된 데미지 타입이면 true, 아니면 false를 반환합니다.</returns>
        bool IsValidDamageType(string damageTypeId);

        /// <summary>
        /// 상태(State) ID가 유효한지 여부를 반환합니다.
        /// </summary>
        /// <param name="stateId">확인할 상태 ID입니다.</param>
        /// <returns>등록된 상태이면 true, 아니면 false를 반환합니다.</returns>
        bool IsValidState(string stateId);

        /// <summary>
        /// 대상의 특정 데미지 타입에 대한 저항 수치를 퍼센트(0~100)로 반환합니다.
        /// </summary>
        /// <param name="damageTypeId">저항을 조회할 데미지 타입 ID입니다.</param>
        /// <param name="target">저항 스탯을 보유한 Affect 대상입니다.</param>
        /// <returns>
        /// 저항 수치(퍼센트)이며, 대상이나 정의가 없을 경우 0을 반환합니다.
        /// </returns>
        float GetResistancePercent(string damageTypeId, IAffectTarget target);
    }
}