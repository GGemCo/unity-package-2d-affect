using UnityEngine;

namespace GGemCo2DAffect
{
    public interface IAffectTarget
    {
        Transform Transform { get; }
        bool IsAlive { get; }

        IStatMutable Stats { get; }
        IStateMutable States { get; }
        IDamageReceiver Damage { get; }
    }
}
