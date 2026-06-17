using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect 도메인에서 사용하는 Addressables 테이블 리소스 정의를 중앙에서 관리한다.
    /// </summary>
    /// <remarks>
    /// - 테이블 이름 문자열과 AddressableAssetInfo 생성을 단일 소스(Single Source of Truth)로 유지한다.
    /// - 로딩/초기화 단계에서 필요한 테이블 집합을 <see cref="All"/>로 일괄 참조할 수 있다.
    /// </remarks>
    public static class ConfigAddressableTableAffect
    {
        /// <summary>
        /// Affect 기본 정의 테이블의 논리적 이름.
        /// </summary>
        public const string Affect = "affect";

        /// <summary>
        /// Affect Modifier 공통 메타 정의 테이블의 논리적 이름.
        /// </summary>
        /// <remarks>
        /// 3단계 리팩터링 이후 이 테이블은 Phase/Kind/Condition 같은 공통 정보와
        /// 기존 wide-row 호환 필드를 함께 보관합니다.
        /// </remarks>
        public const string AffectModifier = "affect_modifier";

        /// <summary>
        /// Stat Modifier 상세 테이블의 논리적 이름.
        /// </summary>
        public const string AffectModifierStat = "affect_modifier_stat";

        /// <summary>
        /// Damage / ElementDamage Modifier 상세 테이블의 논리적 이름.
        /// </summary>
        public const string AffectModifierDamage = "affect_modifier_damage";

        /// <summary>
        /// Heal Modifier 상세 테이블의 논리적 이름.
        /// </summary>
        public const string AffectModifierHeal = "affect_modifier_heal";

        /// <summary>
        /// State Modifier 상세 테이블의 논리적 이름.
        /// </summary>
        public const string AffectModifierState = "affect_modifier_state";

        /// <summary>
        /// CrowdControl Modifier 상세 테이블의 논리적 이름.
        /// </summary>
        public const string AffectModifierCrowdControl = "affect_modifier_crowd_control";

        /// <summary>
        /// ApplyAffectToTarget Modifier 상세 테이블의 논리적 이름.
        /// </summary>
        public const string AffectModifierApplyAffect = "affect_modifier_apply_affect";

        /// <summary>
        /// FormulaVariable Modifier 상세 테이블의 논리적 이름.
        /// </summary>
        public const string AffectModifierFormulaVariable = "affect_modifier_formula_variable";

        /// <summary>
        /// Affect 비주얼 액션 정의 테이블의 논리적 이름.
        /// </summary>
        public const string AffectVisualAction = "affect_visual_action";

        /// <summary>
        /// Affect 애니메이션 정의 테이블의 논리적 이름.
        /// </summary>
        public const string AffectAnimation = "affect_animation";

        /// <summary>
        /// Affect 사망 연출 정의 테이블의 논리적 이름.
        /// </summary>
        public const string AffectDeathPresentation = "affect_death_presentation";

        /// <summary>
        /// Affect 기본 정의 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffect =
            ConfigAddressableTable.Make(Affect);

        /// <summary>
        /// Affect Modifier 공통 메타 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifier =
            ConfigAddressableTable.Make(AffectModifier);

        /// <summary>
        /// Stat Modifier 상세 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifierStat =
            ConfigAddressableTable.Make(AffectModifierStat);

        /// <summary>
        /// Damage / ElementDamage Modifier 상세 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifierDamage =
            ConfigAddressableTable.Make(AffectModifierDamage);

        /// <summary>
        /// Heal Modifier 상세 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifierHeal =
            ConfigAddressableTable.Make(AffectModifierHeal);

        /// <summary>
        /// State Modifier 상세 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifierState =
            ConfigAddressableTable.Make(AffectModifierState);

        /// <summary>
        /// CrowdControl Modifier 상세 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifierCrowdControl =
            ConfigAddressableTable.Make(AffectModifierCrowdControl);

        /// <summary>
        /// ApplyAffectToTarget Modifier 상세 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifierApplyAffect =
            ConfigAddressableTable.Make(AffectModifierApplyAffect);

        /// <summary>
        /// FormulaVariable Modifier 상세 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectModifierFormulaVariable =
            ConfigAddressableTable.Make(AffectModifierFormulaVariable);

        /// <summary>
        /// Affect 비주얼 액션 정의 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectVisualAction =
            ConfigAddressableTable.Make(AffectVisualAction);

        /// <summary>
        /// Affect 애니메이션 정의 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectAnimation =
            ConfigAddressableTable.Make(AffectAnimation);

        /// <summary>
        /// Affect 사망 연출 정의 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableAffectDeathPresentation =
            ConfigAddressableTable.Make(AffectDeathPresentation);

        /// <summary>
        /// Affect 도메인에서 사용하는 모든 테이블 Addressables 자산 목록.
        /// </summary>
        /// <remarks>
        /// - 초기 로딩 단계에서 일괄 로드/검증 용도로 사용한다.
        /// - Kind별 상세 테이블은 마이그레이션 기간 동안 선택적으로 비어 있을 수 있다.
        /// - 테이블이 추가되면 반드시 이 목록에 함께 등록한다.
        /// </remarks>
        public static readonly List<AddressableAssetInfo> All = new()
        {
            TableAffect,
            TableAffectModifier,
            TableAffectModifierStat,
            TableAffectModifierDamage,
            TableAffectModifierHeal,
            TableAffectModifierState,
            TableAffectModifierCrowdControl,
            TableAffectModifierApplyAffect,
            TableAffectModifierFormulaVariable,
            TableAffectVisualAction,
            TableAffectAnimation,
            TableAffectDeathPresentation,
        };
    }
}
