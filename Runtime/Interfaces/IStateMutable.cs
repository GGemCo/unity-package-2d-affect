namespace GGemCo2DAffect
{
    public interface IStateMutable
    {
        bool HasState(string stateId);
        object ApplyState(string stateId, float duration);
        void RemoveState(object token);

        /// <summary>면역/저항 등 정책을 구현체에서 제공할 수 있도록 확장 포인트.</summary>
        bool IsImmune(string stateId);
    }
}
