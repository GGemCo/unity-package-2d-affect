using GGemCo2DAffect;
using GGemCo2DCoreEditor;

namespace GGemCo2DAffectEditor
{
    public class TableLoaderManagerAffect : TableLoaderManagerBase
    {

        public static TableAffect LoadAffectTable()
        {
            return LoadTable<TableAffect>(ConfigAddressableTableAffect.TableAffect.Path);
        }
    }
}