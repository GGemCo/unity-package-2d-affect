using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect의 Outline 요청을 Core 캐릭터(스프라이트) 외곽선 표시로 연결하는 구현체.
    /// </summary>
    public sealed class CoreAffectOutlineService : IAffectOutlineService
    {
        public object Apply(IAffectTarget target, int pixelSize, Color color)
        {
            if (target == null || target.Transform == null) return null;

            if (pixelSize <= 0) pixelSize = 1;

            // Core 캐릭터(= CharacterBase)를 가진 GameObject에 컨트롤러를 부착/재사용한다.
            var go = target.Transform.gameObject;
            var controller = go.GetComponent<CharacterOutlineController>();
            if (controller == null)
                controller = go.AddComponent<CharacterOutlineController>();

            return controller.Acquire(pixelSize, color);
        }

        public void Remove(object token)
        {
            if (token is not CharacterOutlineController.OutlineHandle handle) return;
            handle.Release();
        }
    }
}
