using GGemCo2DCore;
using GGemCo2DCoreEditor;

namespace GGemCo2DAffectEditor
{
    public class DefaultSceneEditorAffect : DefaultSceneEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            packageType = ConfigPackageInfo.PackageType.Affect;
        }
    }
}