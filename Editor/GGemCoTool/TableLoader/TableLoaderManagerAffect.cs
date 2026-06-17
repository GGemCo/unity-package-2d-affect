using GGemCo2DAffect;
using GGemCo2DCore;
using GGemCo2DCoreEditor;

namespace GGemCo2DAffectEditor
{
    public class TableLoaderManagerAffect : TableLoaderManagerBase
    {
        public static TableAffect LoadAffectTable()
        {
            return LoadTable<TableAffect>(ConfigAddressableTableAffect.TableAffect.Path);
        }
        public static TableAffectModifier LoadAffectModifierTable()
        {
            return LoadTable<TableAffectModifier>(ConfigAddressableTableAffect.TableAffectModifier.Path);
        }

        /// <summary>
        /// Affect Modifier Stat 상세 테이블을 에디터 환경에서 로드합니다.
        /// </summary>
        public static TableAffectModifierStat LoadAffectModifierStatTable()
        {
            return LoadTable<TableAffectModifierStat>(ConfigAddressableTableAffect.TableAffectModifierStat.Path);
        }

        /// <summary>
        /// Affect Modifier Damage 상세 테이블을 에디터 환경에서 로드합니다.
        /// </summary>
        public static TableAffectModifierDamage LoadAffectModifierDamageTable()
        {
            return LoadTable<TableAffectModifierDamage>(ConfigAddressableTableAffect.TableAffectModifierDamage.Path);
        }

        /// <summary>
        /// Affect Modifier Heal 상세 테이블을 에디터 환경에서 로드합니다.
        /// </summary>
        public static TableAffectModifierHeal LoadAffectModifierHealTable()
        {
            return LoadTable<TableAffectModifierHeal>(ConfigAddressableTableAffect.TableAffectModifierHeal.Path);
        }

        /// <summary>
        /// Affect Modifier State 상세 테이블을 에디터 환경에서 로드합니다.
        /// </summary>
        public static TableAffectModifierState LoadAffectModifierStateTable()
        {
            return LoadTable<TableAffectModifierState>(ConfigAddressableTableAffect.TableAffectModifierState.Path);
        }

        /// <summary>
        /// Affect Modifier CrowdControl 상세 테이블을 에디터 환경에서 로드합니다.
        /// </summary>
        public static TableAffectModifierCrowdControl LoadAffectModifierCrowdControlTable()
        {
            return LoadTable<TableAffectModifierCrowdControl>(ConfigAddressableTableAffect.TableAffectModifierCrowdControl.Path);
        }

        /// <summary>
        /// Affect Modifier ApplyAffect 상세 테이블을 에디터 환경에서 로드합니다.
        /// </summary>
        public static TableAffectModifierApplyAffect LoadAffectModifierApplyAffectTable()
        {
            return LoadTable<TableAffectModifierApplyAffect>(ConfigAddressableTableAffect.TableAffectModifierApplyAffect.Path);
        }

        /// <summary>
        /// Affect Modifier FormulaVariable 상세 테이블을 에디터 환경에서 로드합니다.
        /// </summary>
        public static TableAffectModifierFormulaVariable LoadAffectModifierFormulaVariableTable()
        {
            return LoadTable<TableAffectModifierFormulaVariable>(ConfigAddressableTableAffect.TableAffectModifierFormulaVariable.Path);
        }

        public static TableAffectVisualAction LoadAffectVisualActionTable()
        {
            return LoadTable<TableAffectVisualAction>(ConfigAddressableTableAffect.TableAffectVisualAction.Path);
        }

        public static TableAffectAnimation LoadAffectAnimationTable()
        {
            return LoadTable<TableAffectAnimation>(ConfigAddressableTableAffect.TableAffectAnimation.Path);
        }

        /// <summary>
        /// Affect 사망 연출 테이블을 에디터 환경에서 로드합니다.
        /// </summary>
        /// <returns>로드된 Affect 사망 연출 테이블입니다.</returns>
        public static TableAffectDeathPresentation LoadAffectDeathPresentationTable()
        {
            return LoadTable<TableAffectDeathPresentation>(ConfigAddressableTableAffect.TableAffectDeathPresentation.Path);
        }
        
        /// <summary>
        /// Core 패키지 테이블을 논리 이름으로 로드한다. (예: "stat", "state", "damage_type")
        /// </summary>
        public static TTable LoadCoreTable<TTable>(string tableName, bool keepCached = true)
            where TTable : class, ITableParser, new()
        {
            var info = ConfigAddressableTable.Make(tableName);
            return LoadTable<TTable>(info.Path, keepCached);
        }
    }
}