using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Outline을 사용하지 않는 환경에서도 안전하게 호출할 수 있는 Null 구현체.
    /// </summary>
    public sealed class NullAffectOutlineService : IAffectOutlineService
    {
        public object Apply(IAffectTarget target, int pixelSize, Color color) => null;
        public void Remove(object token) { }
    }
}
