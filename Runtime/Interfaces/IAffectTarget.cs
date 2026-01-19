using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 시스템이 상호작용할 수 있는 대상(Target)을 나타내는 공통 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 스탯(Stat), 상태(State), 피해(Damage) 처리 등 Affect가 적용되기 위해
    /// 최소한으로 요구되는 기능 집합을 정의합니다.
    /// 구현체는 보통 캐릭터, 몬스터, 오브젝트 등의 게임 엔티티가 됩니다.
    /// </remarks>
    public interface IAffectTarget
    {
        /// <summary>
        /// 대상의 월드 변환 정보(위치/회전/스케일)를 제공합니다.
        /// </summary>
        /// <remarks>
        /// 시각 효과(VFX), 범위 판정, 위치 기반 Affect 처리 등에 사용될 수 있습니다.
        /// </remarks>
        Transform Transform { get; }

        /// <summary>
        /// 대상이 현재 생존 상태인지 여부를 나타냅니다.
        /// </summary>
        /// <remarks>
        /// false인 경우 Affect 적용이나 Tick 처리가 생략될 수 있습니다.
        /// </remarks>
        bool IsAlive { get; }

        /// <summary>
        /// 대상의 스탯을 변경/조회할 수 있는 인터페이스입니다.
        /// </summary>
        /// <remarks>
        /// Stat Modifier 적용, 재계산(Recalculate) 등에 사용됩니다.
        /// </remarks>
        IStatMutable Stats { get; }

        /// <summary>
        /// 대상에게 적용된 상태(State)를 관리하는 인터페이스입니다.
        /// </summary>
        /// <remarks>
        /// 상태 면역 여부 확인, 상태 적용/해제 및 토큰 관리에 사용됩니다.
        /// </remarks>
        IStateMutable States { get; }

        /// <summary>
        /// 대상이 피해(Damage)를 받도록 처리하는 인터페이스입니다.
        /// </summary>
        /// <remarks>
        /// 직접 피해, 지속 피해(DOT), 반사 피해 등의 Affect에서 사용될 수 있습니다.
        /// </remarks>
        IDamageReceiver Damage { get; }
    }
}