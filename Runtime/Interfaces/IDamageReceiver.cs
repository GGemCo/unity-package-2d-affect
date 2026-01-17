namespace GGemCo2DAffect
{
    /// <summary>
    /// 데미지/회복 처리 인터페이스.
    /// - DamageType 기반 파이프라인을 구현체에서 처리할 수 있다.
    /// </summary>
    public interface IDamageReceiver
    {
        void ApplyDamage(string damageTypeId, float amount, bool canCrit, bool isDot, object source);
        void ApplyHeal(float amount, object source);
    }
}
