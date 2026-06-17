using System.Collections.Generic;
using GGemCo2DAffect;
using GGemCo2DCore;
using GGemCo2DCoreEditor;

namespace GGemCo2DAffectEditor
{
    /// <summary>
    /// 공용 TableEditor에 Affect 패키지 테이블 정의를 등록하는 모듈입니다.
    /// </summary>
    /// <remarks>
    /// affect_modifier는 3단계 리팩터링 이후 공통 메타 테이블로 유지하고,
    /// Kind별 상세 값은 affect_modifier_* 상세 테이블로 분리하여 함께 노출합니다.
    /// </remarks>
    internal sealed class AffectTableEditorModule : ITableEditorModule
    {
        private sealed class AffectModifierEditorRow
        {
            public int AffectUid;
            public int ModifierId;
            public AffectPhase Phase;
            public ModifierKind Kind;
            public string StatId;
            public float StatValue;
            public StatValueType StatValueType;
            public StatOperation StatOperation;
            public string DamageTypeId;
            public float DamageBaseValue;
            public string ScalingStatId;
            public float ScalingCoefficient;
            public bool CanCrit;
            public bool IsDot;
            public bool SuppressDamageReaction;
            public bool ShowHitEffect;
            public float HealBaseValue;
            public string HealScalingStatId;
            public float HealScalingCoefficient;
            public string StateId;
            public float StateChance;
            public float StateDurationOverride;
            public int CrowdControlUid;
            public int ApplyAffectUid;
            public float ApplyAffectChance;
            public float ApplyAffectDurationOverride;
            public bool ConsumeOnProc;
            public string FormulaVariableId;
            public float FormulaVariableValue;
            public StatValueType FormulaVariableValueType;
            public StatOperation FormulaVariableOperation;
        }

        private sealed class AffectVisualActionEditorRow
        {
            public int Uid;
            public string Name;
            public string Memo;
            public int AffectUid;
            public int Order;
            public AffectPhase Phase;
            public int VfxUid;
            public AffectVfxPlayMode VfxPlayMode;
            public float VfxScale;
            public float VfxOffsetY;
            public AffectVfxPositionType VfxPositionType;
            public AffectVfxFollowType VfxFollowType;
            public ConfigSortingLayer.Keys VfxSortingLayerKey;
            public float DurationOverride;
        }

        private sealed class AffectAnimationEditorRow
        {
            public int Uid;
            public string Name;
            public string Memo;
            public int AffectUid;
            public bool StopCharacterOnApply;
            public string StartAnimationName;
            public string LoopAnimationName;
            public string EndAnimationName;
            public int Priority;
            public bool RestoreWaitOnEnd;
        }

        private sealed class AffectDeathPresentationEditorRow
        {
            public int Uid;
            public string Name;
            public string Memo;
            public int AffectUid;
            public int Priority;
            public string DeathAnimationName;
            public int DeathVfxUid;
            public float DeathVfxScale;
            public float DeathVfxOffsetY;
            public AffectVfxPositionType DeathVfxPositionType;
            public AffectVfxFollowType DeathVfxFollowType;
            public ConfigSortingLayer.Keys DeathVfxSortingLayerKey;
            public bool UseDeathVfxSortingLayer;
            public float DeathVfxDurationOverride;
            public int DeathCutsceneUid;
            public bool SuppressDefaultDeathAnimation;
            public bool FreezeLastFrame;
        }

        public string ModuleName => "Affect";
        public string PackageName => "Affect";

        /// <summary>
        /// Affect 패키지에서 TableEditor에 노출할 테이블 정의 목록을 생성합니다.
        /// </summary>
        /// <returns>공용 TableEditor가 사용할 테이블 정의 열거자입니다.</returns>
        /// <remarks>
        /// Kind별 상세 테이블은 마이그레이션 기간 동안 선택적으로 비어 있을 수 있지만,
        /// 신규 데이터 작성과 검증을 위해 Editor 목록에는 항상 등록합니다.
        /// </remarks>
        public IEnumerable<TableEditorTableDefinition> BuildDefinitions()
        {
            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableAffect.Affect,
                ConfigAddressableTableAffect.TableAffect.Path,
                ConfigAddressableTableAffect.Affect,
                typeof(TableAffect),
                typeof(StruckTableAffect),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableAffect.TableAffect.Path),
                ResolveReference);

            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableAffect.AffectModifier,
                ConfigAddressableTableAffect.TableAffectModifier.Path,
                ConfigAddressableTableAffect.AffectModifier,
                typeof(TableAffectModifier),
                typeof(AffectModifierEditorRow),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableAffect.TableAffectModifier.Path),
                ResolveReference);

            yield return CreateModifierDetailDefinition(
                ConfigAddressableTableAffect.AffectModifierStat,
                ConfigAddressableTableAffect.TableAffectModifierStat.Path,
                typeof(TableAffectModifierStat),
                typeof(StruckTableAffectModifierStat));

            yield return CreateModifierDetailDefinition(
                ConfigAddressableTableAffect.AffectModifierDamage,
                ConfigAddressableTableAffect.TableAffectModifierDamage.Path,
                typeof(TableAffectModifierDamage),
                typeof(StruckTableAffectModifierDamage));

            yield return CreateModifierDetailDefinition(
                ConfigAddressableTableAffect.AffectModifierHeal,
                ConfigAddressableTableAffect.TableAffectModifierHeal.Path,
                typeof(TableAffectModifierHeal),
                typeof(StruckTableAffectModifierHeal));

            yield return CreateModifierDetailDefinition(
                ConfigAddressableTableAffect.AffectModifierState,
                ConfigAddressableTableAffect.TableAffectModifierState.Path,
                typeof(TableAffectModifierState),
                typeof(StruckTableAffectModifierState));

            yield return CreateModifierDetailDefinition(
                ConfigAddressableTableAffect.AffectModifierCrowdControl,
                ConfigAddressableTableAffect.TableAffectModifierCrowdControl.Path,
                typeof(TableAffectModifierCrowdControl),
                typeof(StruckTableAffectModifierCrowdControl));

            yield return CreateModifierDetailDefinition(
                ConfigAddressableTableAffect.AffectModifierApplyAffect,
                ConfigAddressableTableAffect.TableAffectModifierApplyAffect.Path,
                typeof(TableAffectModifierApplyAffect),
                typeof(StruckTableAffectModifierApplyAffect));

            yield return CreateModifierDetailDefinition(
                ConfigAddressableTableAffect.AffectModifierFormulaVariable,
                ConfigAddressableTableAffect.TableAffectModifierFormulaVariable.Path,
                typeof(TableAffectModifierFormulaVariable),
                typeof(StruckTableAffectModifierFormulaVariable));

            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableAffect.AffectVisualAction,
                ConfigAddressableTableAffect.TableAffectVisualAction.Path,
                ConfigAddressableTableAffect.AffectVisualAction,
                typeof(TableAffectVisualAction),
                typeof(AffectVisualActionEditorRow),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableAffect.TableAffectVisualAction.Path),
                ResolveReference);

            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableAffect.AffectAnimation,
                ConfigAddressableTableAffect.TableAffectAnimation.Path,
                ConfigAddressableTableAffect.AffectAnimation,
                typeof(TableAffectAnimation),
                typeof(AffectAnimationEditorRow),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableAffect.TableAffectAnimation.Path),
                ResolveReference);

            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableAffect.AffectDeathPresentation,
                ConfigAddressableTableAffect.TableAffectDeathPresentation.Path,
                ConfigAddressableTableAffect.AffectDeathPresentation,
                typeof(TableAffectDeathPresentation),
                typeof(AffectDeathPresentationEditorRow),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(ConfigAddressableTableAffect.TableAffectDeathPresentation.Path),
                ResolveReference);
        }

        /// <summary>
        /// Kind별 Modifier 상세 테이블용 TableEditor 정의를 생성합니다.
        /// </summary>
        /// <param name="tableKey">논리 테이블 키입니다.</param>
        /// <param name="tablePath">테이블 txt 파일 경로입니다.</param>
        /// <param name="tableType">런타임 테이블 파서 타입입니다.</param>
        /// <param name="rowType">Editor에서 표시할 Row 타입입니다.</param>
        /// <returns>공용 TableEditor 정의입니다.</returns>
        private TableEditorTableDefinition CreateModifierDetailDefinition(
            string tableKey,
            string tablePath,
            System.Type tableType,
            System.Type rowType)
        {
            return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                tableKey,
                tablePath,
                tableKey,
                tableType,
                rowType,
                TableEditorDefinitionFactory.CreateDefaultReloadAction(tablePath),
                ResolveReference);
        }

        /// <summary>
        /// 컬럼명에 맞는 참조 테이블 정의를 반환합니다.
        /// </summary>
        /// <param name="headerName">참조를 해석할 컬럼명입니다.</param>
        /// <returns>참조 대상 테이블 정의입니다. 없으면 null입니다.</returns>
        private static TableEditorTableDefinition ResolveReference(string headerName)
        {
            switch (headerName)
            {
                case "AffectUid":
                case "ApplyAffectUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTableAffect.Affect);
                case "CrowdControlUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTable.CrowdControl);
                case "DeathCutsceneUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTable.Cutscene);
                case "DeathVfxUid":
                case "VfxUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTable.VfxEffect);
                case "VfxEffectUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTable.VfxEffect);
                case "VfxParticleUid":
                    return TableEditorRegistry.FindByKey(ConfigAddressableTable.VfxParticle);
                default:
                    return TableEditorRegistry.FindReferenceTable(headerName);
            }
        }
    }
}
