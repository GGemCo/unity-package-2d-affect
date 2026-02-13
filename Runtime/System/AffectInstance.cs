using System.Collections.Generic;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 정의(AffectDefinition) 1건이 실제로 적용된 "런타임 인스턴스"를 표현한다.
    /// 남은 시간/스택/틱 누적 상태와, 적용 시 생성된 토큰(Stat/State)을 보관한다.
    /// </summary>
    /// <remarks>
    /// - Duration은 RemainingTime/TotalDuration로 관리한다.
    /// - Tick은 TickElapsed에 누적 후 interval 단위로 소비(Consume)한다.
    /// - Stat/State 토큰은 만료/해제 시 원복(Cleanup)하기 위한 핸들 역할이다.
    /// </remarks>
    public sealed class AffectInstance
    {
        /// <summary>
        /// 이 인스턴스가 참조하는 어펙트 정의.
        /// </summary>
        public readonly AffectDefinition Definition;

        /// <summary>
        /// 적용 컨텍스트(시전자/원인/옵션, 지속시간 오버라이드 등)를 담는다.
        /// </summary>
        public readonly AffectApplyContext Context;

        /// <summary>
        /// 현재 스택 수(최소 1).
        /// </summary>
        public int Stacks { get; private set; } = 1;

        /// <summary>
        /// 남은 지속 시간(초). 0이면 만료된 것으로 간주한다.
        /// </summary>
        public float RemainingTime { get; private set; }

        /// <summary>
        /// 총 지속 시간(초). 생성 시점 duration을 기록하며, Refresh로는 변경되지 않는다.
        /// </summary>
        public float TotalDuration { get; }

        /// <summary>
        /// 틱 누적 시간(초). interval 단위로 소비되며 남은 나머지를 유지한다.
        /// </summary>
        public float TickElapsed { get; private set; }

        private readonly List<object> _statTokens = new();
        private readonly List<object> _stateTokens = new();

        /// <summary>
        /// VFX 서비스가 반환한 토큰(핸들)입니다.
        /// 만료/해제 시 <see cref="IAffectEffectService.Stop"/> 호출에 사용됩니다.
        /// </summary>
        public object EffectToken { get; set; }

        /// <summary>
        /// 어펙트 인스턴스를 생성한다.
        /// </summary>
        /// <param name="definition">적용할 어펙트 정의.</param>
        /// <param name="context">적용 컨텍스트.</param>
        /// <param name="duration">초 단위 지속시간(남은 시간/총 시간의 초기값).</param>
        public AffectInstance(AffectDefinition definition, AffectApplyContext context, float duration)
        {
            Definition = definition;
            Context = context;
            RemainingTime = duration;
            TotalDuration = duration;
            TickElapsed = 0f;
        }

        /// <summary>
        /// 스택을 1 증가시키되, <paramref name="maxStacks"/>를 상한으로 제한한다.
        /// </summary>
        /// <param name="maxStacks">허용되는 최대 스택 수(0 이하이면 1로 보정).</param>
        public void AddStack(int maxStacks)
        {
            if (maxStacks <= 0) maxStacks = 1;
            Stacks = System.Math.Min(maxStacks, Stacks + 1);
        }

        /// <summary>
        /// 남은 시간을 새 duration으로 갱신한다(스택/총 시간은 변경하지 않는다).
        /// </summary>
        /// <param name="duration">갱신할 남은 지속시간(초).</param>
        public void Refresh(float duration)
        {
            RemainingTime = duration;
        }

        /// <summary>
        /// 남은 시간을 dt만큼 감소시킨다(0 미만으로 내려가지 않음).
        /// </summary>
        /// <param name="dt">경과 시간(초).</param>
        public void UpdateTime(float dt)
        {
            if (RemainingTime <= 0f) return;
            RemainingTime -= dt;
            if (RemainingTime < 0f) RemainingTime = 0f;
        }

        /// <summary>
        /// 만료 여부(남은 시간이 0 이하인지).
        /// </summary>
        public bool IsExpired => RemainingTime <= 0f;

        /// <summary>
        /// TickElapsed에 dt를 누적한다(0 미만으로 내려가지 않음).
        /// </summary>
        /// <param name="dt">누적할 경과 시간(초). 음수 입력 시 감소(리셋 용도)도 가능.</param>
        /// <remarks>
        /// 컴포넌트 쪽에서 tick reset을 위해 음수를 전달할 수 있으므로 하한을 0으로 클램프한다.
        /// </remarks>
        public void AccumulateTick(float dt)
        {
            TickElapsed += dt;
            if (TickElapsed < 0f) TickElapsed = 0f;
        }

        /// <summary>
        /// 누적된 TickElapsed가 interval 이상이면 interval만큼 소비한다.
        /// </summary>
        /// <param name="interval">틱 간격(초). 0 이하이면 소비하지 않는다.</param>
        /// <returns>interval을 소비했으면 true, 아직 interval에 도달하지 못했으면 false.</returns>
        public bool TryConsumeTick(float interval)
        {
            if (interval <= 0f) return false;
            if (TickElapsed < interval) return false;
            TickElapsed -= interval;
            return true;
        }

        /// <summary>
        /// Stat 적용 결과로 생성된 토큰을 추가한다(만료/해제 시 원복용).
        /// </summary>
        /// <param name="token">Stat 시스템이 반환한 토큰(핸들) 객체.</param>
        public void AddStatToken(object token)
        {
            if (token != null) _statTokens.Add(token);
        }

        /// <summary>
        /// State 적용 결과로 생성된 토큰을 추가한다(만료/해제 시 원복용).
        /// </summary>
        /// <param name="token">State 시스템이 반환한 토큰(핸들) 객체.</param>
        public void AddStateToken(object token)
        {
            if (token != null) _stateTokens.Add(token);
        }

        /// <summary>
        /// 적용 중 생성된 Stat 토큰 목록(읽기 전용).
        /// </summary>
        public IReadOnlyList<object> StatTokens => _statTokens;

        /// <summary>
        /// 적용 중 생성된 State 토큰 목록(읽기 전용).
        /// </summary>
        public IReadOnlyList<object> StateTokens => _stateTokens;
    }
}
