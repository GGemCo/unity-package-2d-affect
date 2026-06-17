using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 관련 테이블 로딩을 총괄하는 매니저 클래스.
    /// </summary>
    /// <remarks>
    /// - TableLoaderBase를 상속하여 테이블 로딩 파이프라인에 참여한다.
    /// - Affect / AffectModifier / Kind별 Modifier 상세 테이블을 함께 관리한다.
    /// - Unity 씬 전환 간에도 유지되도록 Singleton + DontDestroyOnLoad 패턴을 사용한다.
    /// </remarks>
    public class TableLoaderManagerAffect : TableLoaderBase
    {
        /// <summary>
        /// <see cref="TableLoaderManagerAffect"/>의 전역 접근을 위한 Singleton 인스턴스.
        /// </summary>
        /// <remarks>
        /// Awake 시점에 최초 1회만 설정되며,
        /// 이후 중복 생성된 객체는 즉시 파괴된다.
        /// </remarks>
        public static TableLoaderManagerAffect Instance;

        /// <summary>
        /// 어펙트 기본 정의 테이블.
        /// </summary>
        public TableAffect TableAffect { get; private set; } = new TableAffect();

        /// <summary>
        /// 어펙트 Modifier 공통 메타 테이블.
        /// </summary>
        public TableAffectModifier TableAffectModifier { get; private set; } = new TableAffectModifier();

        /// <summary>
        /// Stat Modifier 상세 테이블.
        /// </summary>
        public TableAffectModifierStat TableAffectModifierStat { get; private set; } = new TableAffectModifierStat();

        /// <summary>
        /// Damage Modifier 상세 테이블.
        /// </summary>
        public TableAffectModifierDamage TableAffectModifierDamage { get; private set; } = new TableAffectModifierDamage();

        /// <summary>
        /// ElementGauge Modifier 상세 테이블.
        /// </summary>
        public TableAffectModifierElementGauge TableAffectModifierElementGauge { get; private set; } = new TableAffectModifierElementGauge();

        /// <summary>
        /// Heal Modifier 상세 테이블.
        /// </summary>
        public TableAffectModifierHeal TableAffectModifierHeal { get; private set; } = new TableAffectModifierHeal();

        /// <summary>
        /// State Modifier 상세 테이블.
        /// </summary>
        public TableAffectModifierState TableAffectModifierState { get; private set; } = new TableAffectModifierState();

        /// <summary>
        /// CrowdControl Modifier 상세 테이블.
        /// </summary>
        public TableAffectModifierCrowdControl TableAffectModifierCrowdControl { get; private set; } = new TableAffectModifierCrowdControl();

        /// <summary>
        /// ApplyAffectToTarget Modifier 상세 테이블.
        /// </summary>
        public TableAffectModifierApplyAffect TableAffectModifierApplyAffect { get; private set; } = new TableAffectModifierApplyAffect();

        /// <summary>
        /// FormulaVariable Modifier 상세 테이블.
        /// </summary>
        public TableAffectModifierFormulaVariable TableAffectModifierFormulaVariable { get; private set; } = new TableAffectModifierFormulaVariable();

        /// <summary>
        /// 어펙트 비주얼 액션 서브테이블.
        /// </summary>
        public TableAffectVisualAction TableAffectVisualAction { get; private set; } = new TableAffectVisualAction();

        /// <summary>
        /// 어펙트 애니메이션 서브테이블.
        /// </summary>
        public TableAffectAnimation TableAffectAnimation { get; private set; } = new TableAffectAnimation();

        /// <summary>
        /// 어펙트 사망 연출 서브테이블.
        /// </summary>
        public TableAffectDeathPresentation TableAffectDeathPresentation { get; private set; } = new TableAffectDeathPresentation();

        /// <summary>
        /// Modifier 목록에 Kind별 상세 테이블 Payload를 적용합니다.
        /// </summary>
        /// <param name="modifiers">AffectUid 기준으로 조회한 Modifier 목록입니다.</param>
        /// <remarks>
        /// <c>affect_modifier</c>는 공통 메타만 보관하므로, 실행에 필요한 상세 값은 반드시
        /// Kind별 상세 테이블에서 Payload로 주입되어야 합니다. 기존 Executor가 legacy 필드를 읽는 구조를
        /// 유지하기 위해 Payload 적용 후 <see cref="AffectModifierDefinition.ApplyPayloadToLegacyFields"/>를 호출합니다.
        /// </remarks>
        public void ApplyModifierDetailPayloads(IList<AffectModifierDefinition> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
                return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null)
                    continue;

                if (!TryCreateDetailPayload(modifier, out IAffectModifierPayload payload))
                {
                    LogMissingRequiredDetailPayload(modifier);
                    continue;
                }

                modifier.payload = payload;
                modifier.ApplyPayloadToLegacyFields();
            }
        }

        /// <summary>
        /// 상세 테이블 Payload가 필요한 Modifier에서 상세 Row를 찾지 못했을 때 경고를 출력합니다.
        /// </summary>
        /// <param name="modifier">상세 Payload를 찾지 못한 Modifier 정의입니다.</param>
        /// <remarks>
        /// Custom Kind는 아직 프로젝트별 확장 영역이므로 기본 상세 Payload를 요구하지 않습니다.
        /// 그 외 Kind는 실행 값이 상세 테이블에만 존재하므로 누락 시 데이터 오류로 판단할 수 있습니다.
        /// </remarks>
        private static void LogMissingRequiredDetailPayload(AffectModifierDefinition modifier)
        {
            if (modifier == null || !RequiresDetailPayload(modifier.kind))
                return;

            GcLogger.LogWarning($"[AffectModifier] Kind별 상세 테이블 Row를 찾지 못했습니다. AffectUid={modifier.affectUid}, ModifierId={modifier.modifierId}, Kind={modifier.kind}");
        }

        /// <summary>
        /// 지정한 Modifier Kind가 상세 테이블 Payload를 반드시 필요로 하는지 확인합니다.
        /// </summary>
        /// <param name="kind">검사할 Modifier Kind입니다.</param>
        /// <returns>상세 Payload가 필요한 Kind이면 true입니다.</returns>
        private static bool RequiresDetailPayload(ModifierKind kind)
        {
            switch (kind)
            {
                case ModifierKind.Stat:
                case ModifierKind.Damage:
                case ModifierKind.ElementGauge:
                case ModifierKind.Heal:
                case ModifierKind.State:
                case ModifierKind.CrowdControl:
                case ModifierKind.ApplyAffectToTarget:
                case ModifierKind.FormulaVariable:
                    return true;
                case ModifierKind.Custom:
                default:
                    return false;
            }
        }

        /// <summary>
        /// Modifier Kind에 맞는 상세 테이블에서 Payload 생성을 시도합니다.
        /// </summary>
        /// <param name="modifier">상세 Payload를 적용할 Modifier 정의입니다.</param>
        /// <param name="payload">상세 테이블에서 생성한 Payload입니다.</param>
        /// <returns>상세 테이블 Row를 찾고 Payload를 생성했으면 true입니다.</returns>
        private bool TryCreateDetailPayload(AffectModifierDefinition modifier, out IAffectModifierPayload payload)
        {
            payload = null;
            if (modifier == null)
                return false;

            switch (modifier.kind)
            {
                case ModifierKind.Stat:
                    return TableAffectModifierStat.TryCreatePayload(modifier.affectUid, modifier.modifierId, modifier.kind, out payload);

                case ModifierKind.Damage:
                    return TableAffectModifierDamage.TryCreatePayload(modifier.affectUid, modifier.modifierId, modifier.kind, out payload);

                case ModifierKind.ElementGauge:
                    return TableAffectModifierElementGauge.TryCreatePayload(modifier.affectUid, modifier.modifierId, modifier.kind, out payload);

                case ModifierKind.Heal:
                    return TableAffectModifierHeal.TryCreatePayload(modifier.affectUid, modifier.modifierId, modifier.kind, out payload);

                case ModifierKind.State:
                    return TableAffectModifierState.TryCreatePayload(modifier.affectUid, modifier.modifierId, modifier.kind, out payload);

                case ModifierKind.CrowdControl:
                    return TableAffectModifierCrowdControl.TryCreatePayload(modifier.affectUid, modifier.modifierId, modifier.kind, out payload);

                case ModifierKind.ApplyAffectToTarget:
                    return TableAffectModifierApplyAffect.TryCreatePayload(modifier.affectUid, modifier.modifierId, modifier.kind, out payload);

                case ModifierKind.FormulaVariable:
                    return TableAffectModifierFormulaVariable.TryCreatePayload(modifier.affectUid, modifier.modifierId, modifier.kind, out payload);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Unity Awake 생명주기 메서드.
        /// </summary>
        /// <remarks>
        /// - Singleton 인스턴스를 초기화한다.
        /// - 테이블 레지스트리를 생성하고, 공통 Modifier 및 Kind별 상세 테이블을 등록한다.
        /// - 이미 인스턴스가 존재하면 중복 객체를 파괴한다.
        /// </remarks>
        protected void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // 테이블 간 참조 및 의존성 해결을 위해 등록 순서가 중요하다.
                // Modifier 공통 메타와 상세 Payload 테이블을 먼저 로드한 뒤 Affect 정의를 구성한다.
                registry = new TableRegistry();
                registry.Register(TableAffectModifier);
                registry.Register(TableAffectModifierStat);
                registry.Register(TableAffectModifierDamage);
                registry.Register(TableAffectModifierElementGauge);
                registry.Register(TableAffectModifierHeal);
                registry.Register(TableAffectModifierState);
                registry.Register(TableAffectModifierCrowdControl);
                registry.Register(TableAffectModifierApplyAffect);
                registry.Register(TableAffectModifierFormulaVariable);
                registry.Register(TableAffectVisualAction);
                registry.Register(TableAffectAnimation);
                registry.Register(TableAffectDeathPresentation);
                registry.Register(TableAffect);
            }
            else
            {
                // 이미 Singleton 인스턴스가 존재하는 경우 중복 생성을 방지한다.
                Destroy(gameObject);
            }
        }
    }
}
