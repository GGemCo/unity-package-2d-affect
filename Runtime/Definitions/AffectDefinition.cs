using System;
using System.Collections.Generic;

namespace GGemCo2DAffect
{
    [Serializable]
    public sealed class AffectDefinition
    {
        public int Uid;
        public string NameKey;
        public string IconKey;

        public string GroupId;

        public float BaseDuration;
        public float TickInterval;

        public StackPolicy StackPolicy;
        public int MaxStacks;
        public RefreshPolicy RefreshPolicy;

        public DispelType DispelType;

        /// <summary>쉼표/공백 기준으로 파싱된 Tag 컬렉션.</summary>
        public List<string> Tags = new();

        public int VfxUid;
        public float VfxScale;
        public float VfxOffsetY;

        public float ApplyChance;

        public bool HasTick => TickInterval > 0f;

        public bool IsNoneGroup => string.IsNullOrWhiteSpace(GroupId) || GroupId.Equals("None", StringComparison.OrdinalIgnoreCase);

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;
            for (int i = 0; i < Tags.Count; i++)
            {
                if (string.Equals(Tags[i], tag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
