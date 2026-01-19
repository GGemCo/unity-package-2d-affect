namespace GGemCo2DAffect
{
    /// <summary>
    /// 데미지 및 회복 처리를 담당하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// - DamageType 기반의 처리 파이프라인(방어, 저항, 증폭, 치명타 등)을
    ///   구현체에서 자유롭게 확장·구현할 수 있도록 설계되었습니다.
    /// - Affect 시스템은 실제 계산 로직에 관여하지 않고, 이 인터페이스를 통해서만 상호작용합니다.
    /// </remarks>
    public interface IDamageReceiver
    {
        /// <summary>
        /// 대상에게 데미지를 적용합니다.
        /// </summary>
        /// <param name="damageTypeId">데미지 타입을 식별하는 ID입니다(예: Fire, Poison, Physical).</param>
        /// <param name="amount">적용할 데미지의 기본 수치입니다.</param>
        /// <param name="canCrit">치명타(Critical) 판정이 가능한지 여부입니다.</param>
        /// <param name="isDot">지속 피해(Damage over Time) 여부입니다.</param>
        /// <param name="source">데미지의 원천(공격자, 스킬, Affect 등)을 식별하기 위한 객체입니다.</param>
        void ApplyDamage(string damageTypeId, float amount, bool canCrit, bool isDot, object source);

        /// <summary>
        /// 대상에게 회복(Heal)을 적용합니다.
        /// </summary>
        /// <param name="amount">회복할 수치입니다.</param>
        /// <param name="source">회복의 원천(스킬, 아이템, Affect 등)을 식별하기 위한 객체입니다.</param>
        void ApplyHeal(float amount, object source);
    }
}