using GGemCo2DAffectEditor;
using GGemCo2DCore;
using GGemCo2DCoreEditor;

namespace GGemCo2DTcgEditor
{
    public class DefaultEditorWindowAffect : DefaultEditorWindow
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            packageType = ConfigPackageInfo.PackageType.Affect;
        }
    }
}