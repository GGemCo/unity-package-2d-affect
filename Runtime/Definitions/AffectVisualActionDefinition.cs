using System;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// Affect에 연결된 단일 비주얼 액션 정의입니다.
    /// </summary>
    [Serializable]
    public sealed class AffectVisualActionDefinition
    {
        public int uid;
        public int affectUid;
        public int order;
        public AffectPhase phase;
        public int vfxUid;
        public AffectVfxPlayMode vfxPlayMode;
        public float vfxScale;
        public float vfxOffsetY;
        public AffectVfxPositionType vfxPositionType;
        public AffectVfxFollowType vfxFollowType;
        public ConfigSortingLayer.Keys vfxSortingLayerKey;
        public float durationOverride;

        public bool IsValid => affectUid > 0 && vfxUid > 0;

        public float ResolveDuration(float defaultDuration)
        {
            return durationOverride > 0f ? durationOverride : defaultDuration;
        }
    }
}
