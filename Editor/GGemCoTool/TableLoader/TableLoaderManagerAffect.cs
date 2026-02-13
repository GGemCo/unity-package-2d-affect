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