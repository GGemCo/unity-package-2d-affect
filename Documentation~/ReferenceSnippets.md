# Affect ReferenceSnippets

작성일: 2026-02-18

목적:
- Affect 패키지에서 데이터 중심/Executor 분리/Bridge 연동 스타일을 자동 복제

우선순위:
1) `Docs/ReferenceSnippets.md`
2) `Docs/STYLE_CONTRACT.md`
3) `Docs/GOLDEN_REFERENCES.md`
4) `Docs/GGemCoPatterns/*`
5) Affect `CONVENTIONS/ARCHITECTURE/PLAYBOOK`

---

## AffectRuntime(적용/틱/해제 오케스트레이션)

- 경로: `System/AffectRuntime.cs`
- 포인트:
  - Apply→Tick→Remove 흐름의 중심
  - Tick 성능(주기/할당) 고려 지점

```csharp
    /// - 기본 구현은 인메모리/Null 객체로 설정되어 있으며,
    ///   실제 게임 초기화 시 외부에서 교체 주입하는 것을 전제로 한다.
    /// </remarks>
    public static class AffectRuntime
    {
        /// <summary>
        /// 어펙트 정의(AffectDefinition)를 조회하기 위한 리포지토리.
        /// </summary>
        /// <remarks>
        /// 기본값은 InMemoryAffectRepository이며,
        /// 런타임 또는 부트스트랩 단계에서 다른 구현(DB/ScriptableObject 기반 등)으로 교체할 수 있다.
        /// </remarks>
        public static IAffectDefinitionRepository AffectRepository { get; set; }
            = new InMemoryAffectRepository();

        /// <summary>
        /// 상태(Status) 정의를 조회하기 위한 리포지토리.
        /// </summary>
        /// <remarks>
        /// 어펙트 Modifier 중 State/Stat 처리 시 참조된다.
        /// </remarks>
        public static IStatusDefinitionRepository StatusRepository { get; set; }
            = new InMemoryStatusRepository();

        /// <summary>
        /// 어펙트에 의해 재생되는 이펙트 서비스를 제공한다.
        /// </summary>
        /// <remarks>
        /// 기본값은 NullAffectEffectService로, 이펙트가 필요 없는 환경에서도
        /// null 체크 없이 안전하게 호출할 수 있도록 한다(Null Object 패턴).
        /// </remarks>
        public static IAffectEffectService EffectService { get; set; }
            = new NullAffectEffectService();
    }
}
```
## AffectInstance(런타임 인스턴스 모델)

- 경로: `System/AffectInstance.cs`
- 포인트:
  - 인스턴스 상태(남은 시간/스택/소유자) 관리
  - Remove 시 정리 포인트

```csharp
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
```
## Executor 인터페이스(Kind별 분리)

- 경로: `Executors/IModifierExecutor.cs`
- 포인트:
  - if-else 누적 대신 Kind별 Executor 구현 추가
  - Core 연동은 Bridge/서비스로 위임

```csharp
    /// <remarks>
    /// - Modifier의 종류(Stat/Damage/State 등)에 따라 서로 다른 실행기가 이 인터페이스를 구현한다.
    /// - 하나의 Modifier는 Affect 수명 주기(Apply/Tick/Expire)에 따라 서로 다른 시점에서 실행될 수 있다.
    /// - 실제 실행 여부와 호출 타이밍은 Affect 런타임(AffectInstance/AffectComponent)에서 제어한다.
    /// </remarks>
    internal interface IModifierExecutor
    {
        /// <summary>
        /// Affect가 대상에 최초 적용될 때 호출된다.
        /// </summary>
        /// <param name="target">효과가 적용될 대상.</param>
        /// <param name="instance">현재 Affect 인스턴스.</param>
        /// <param name="mod">실행할 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소.</param>
        void ExecuteOnApply(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo);

        /// <summary>
        /// Affect의 틱(Tick) 주기마다 호출된다.
        /// </summary>
        /// <param name="target">효과가 적용된 대상.</param>
        /// <param name="instance">현재 Affect 인스턴스.</param>
        /// <param name="mod">실행할 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소.</param>
        void ExecuteOnTick(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo);

        /// <summary>
        /// Affect가 만료되거나 제거될 때 호출된다.
        /// </summary>
        /// <param name="target">효과가 적용되었던 대상.</param>
        /// <param name="instance">만료되는 Affect 인스턴스.</param>
        /// <param name="mod">실행할 Modifier 정의.</param>
        /// <param name="affectRepo">Affect 정의 저장소.</param>
        /// <param name="statusRepo">상태/저항 정의 저장소.</param>
        void ExecuteOnExpire(
            IAffectTarget target,
            AffectInstance instance,
            AffectModifierDefinition mod,
            IAffectDefinitionRepository affectRepo,
            IStatusDefinitionRepository statusRepo);
    }
}
```
## Repository(정의/캐시 조회)

- 경로: `Repositories/InMemoryAffectRepository.cs`
- 포인트:
  - 정의 조회/캐시 표준
  - 키 누락/기본값 처리 방식 참고

```csharp
    /// - 디스크나 서버와의 동기화 없이, 단일 실행 컨텍스트 내에서만 유효합니다.
    /// - 테스트, 프로토타이핑, 또는 에디터/부트스트랩 단계에서의 임시 저장소로 적합합니다.
    /// </remarks>
    public sealed class InMemoryAffectRepository : IAffectDefinitionRepository
    {
        /// <summary>
        /// Affect UID를 키로 하는 Affect 정의 캐시입니다.
        /// </summary>
        private readonly Dictionary<int, AffectDefinition> _affects = new();

        /// <summary>
        /// Affect UID를 키로 하는 모디파이어 정의 목록 캐시입니다.
        /// </summary>
        private readonly Dictionary<int, List<AffectModifierDefinition>> _modifiers = new();

        /// <summary>
        /// 저장된 모든 Affect 정의와 모디파이어를 제거합니다.
        /// </summary>
        public void Clear()
        {
            _affects.Clear();
            _modifiers.Clear();
        }

        /// <summary>
        /// Affect 정의와 해당 Affect에 속한 모디파이어 목록을 등록합니다.
        /// </summary>
        /// <param name="definition">등록할 Affect 정의입니다.</param>
        /// <param name="modifiers">
        /// Affect에 연결된 모디파이어 정의 목록입니다.
        /// null인 경우 빈 목록으로 등록됩니다.
        /// </param>
        public void Register(AffectDefinition definition, List<AffectModifierDefinition> modifiers)
        {
            // NOTE: 중복 UID가 등록되면 기존 정의를 덮어씁니다.
            // Debug.Log($"affect 등록. Uid: {definition.uid} / vfxPositionType: {definition.effectPositionType} / vfxFollowType: {definition.effectFollowType}");

            _affects[definition.uid] = definition;
            _modifiers[definition.uid] = modifiers ?? new List<AffectModifierDefinition>(0);
        }

        /// <summary>
        /// 지정한 UID에 해당하는 Affect 정의를 조회합니다.
        /// </summary>
        /// <param name="affectUid">조회할 Affect를 식별하는 고유 UID입니다.</param>
        /// <param name="definition">조회에 성공한 경우 반환되는 Affect 정의입니다.</param>
        /// <returns>
        /// 정의가 존재하면 true, 존재하지 않으면 false를 반환합니다.
        /// </returns>
        public bool TryGetAffect(int affectUid, out AffectDefinition definition)
        {
            // Debug.Log($"affect 가져오기. count: {_affects.Count}, uid: {affectUid}");
            return _affects.TryGetValue(affectUid, out definition);
        }

        /// <summary>
        /// 지정한 Affect UID에 연결된 모디파이어 정의 목록을 반환합니다.
        /// </summary>
        /// <param name="affectUid">모디파이어를 조회할 Affect를 식별하는 고유 UID입니다.</param>
        /// <returns>
        /// Affect에 속한 모디파이어 정의의 읽기 전용 목록입니다.
        /// UID가 등록되지 않은 경우 빈 목록을 반환합니다.
        /// </returns>
        public IReadOnlyList<AffectModifierDefinition> GetModifiers(int affectUid)
        {
            if (_modifiers.TryGetValue(affectUid, out var list))
                return list;

            return System.Array.Empty<AffectModifierDefinition>();
        }
    }
}
```
## BridgeRegistrar(Core 연동 연결)

- 경로: `Bridge/AffectBridgeRegistrar.cs`
- 포인트:
  - Core 서비스(이펙트/사운드/스탯 등) 연결 지점
  - 패키지 의존성 역전 방지를 위한 등록 방식

```csharp
    /// Affect 미설치 시 이 클래스 자체가 포함되지 않으므로, Core는 기본(Null Provider) 구현으로 동작합니다.
    /// 또한 RuntimeInitializeOnLoadMethod를 사용하여 씬 로드 이전(BeforeSceneLoad)에 등록을 보장합니다.
    /// </remarks>
    internal static class AffectBridgeRegistrar
    {
        /// <summary>
        /// 플레이/빌드 환경에서 씬 로드 전에 AffectDescriptionProvider를 Core 브리지에 주입합니다.
        /// </summary>
        /// <remarks>
        /// Core 측에서 Affect 기능을 직접 참조하지 않도록(의존성 역전) 브리지 패턴을 사용하며,
        /// 이 메서드는 Affect 패키지가 존재하는 경우에만 실행됩니다.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            // 플레이/빌드 모두에서 안전하게 등록
            AffectBridge.SetProvider(new AffectDescriptionProvider());

            // Affect 이펙트를 Core 이펙트 시스템으로 연결
            AffectRuntime.EffectService = new CoreAffectEffectService();
        }
    }
}
```
