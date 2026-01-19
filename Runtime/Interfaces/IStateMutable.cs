namespace GGemCo2DAffect
{
    /// <summary>
    /// 대상이 보유한 상태(State)를 조회·적용·해제할 수 있는 변경 가능 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Affect 시스템은 상태의 실제 구현 방식(스택, 갱신 규칙, 중첩 정책 등)에 관여하지 않고,
    /// 이 인터페이스를 통해 상태 부여와 제거를 요청합니다.
    /// 면역/저항과 같은 정책은 구현체에서 자유롭게 확장할 수 있습니다.
    /// </remarks>
    public interface IStateMutable
    {
        /// <summary>
        /// 대상이 특정 상태(State)를 현재 보유하고 있는지 여부를 반환합니다.
        /// </summary>
        /// <param name="stateId">확인할 상태를 식별하는 ID입니다.</param>
        /// <returns>
        /// 해당 상태를 보유 중이면 true, 그렇지 않으면 false를 반환합니다.
        /// </returns>
        bool HasState(string stateId);

        /// <summary>
        /// 대상에게 상태(State)를 적용합니다.
        /// </summary>
        /// <param name="stateId">적용할 상태를 식별하는 ID입니다.</param>
        /// <param name="duration">상태의 지속 시간(초)입니다.</param>
        /// <returns>
        /// 적용된 상태를 식별하기 위한 토큰 객체입니다.
        /// 이후 <see cref="RemoveState"/> 호출 시 해당 토큰을 전달하여 상태를 해제합니다.
        /// </returns>
        object ApplyState(string stateId, float duration);

        /// <summary>
        /// <see cref="ApplyState"/>로 적용된 상태(State)를 해제합니다.
        /// </summary>
        /// <param name="token">
        /// <see cref="ApplyState"/> 호출 시 반환된 토큰 객체입니다.
        /// 유효하지 않거나 이미 제거된 토큰인 경우, 구현체에서 안전하게 무시할 수 있습니다.
        /// </param>
        void RemoveState(object token);

        /// <summary>
        /// 대상이 특정 상태(State)에 대해 면역 상태인지 여부를 반환합니다.
        /// </summary>
        /// <param name="stateId">확인할 상태를 식별하는 ID입니다.</param>
        /// <returns>
        /// 면역인 경우 true, 그렇지 않으면 false를 반환합니다.
        /// </returns>
        /// <remarks>
        /// 면역/저항/내성 등 상태 적용 정책을 구현체에서 제공할 수 있도록 한 확장 포인트입니다.
        /// </remarks>
        bool IsImmune(string stateId);
    }
}