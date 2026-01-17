using System.Collections.Generic;

namespace GGemCo2DAffect
{
    public sealed class DispelQuery
    {
        public DispelType DispelType = DispelType.None;
        public int MaxRemoveCount = int.MaxValue;

        public readonly List<string> RequireTags = new();
        public readonly List<string> ExcludeTags = new();

        public bool Match(AffectDefinition def)
        {
            if (def == null) return false;

            if (DispelType != DispelType.None && def.DispelType != DispelType)
                return false;

            for (int i = 0; i < RequireTags.Count; i++)
            {
                if (!def.HasTag(RequireTags[i])) return false;
            }

            for (int i = 0; i < ExcludeTags.Count; i++)
            {
                if (def.HasTag(ExcludeTags[i])) return false;
            }

            return true;
        }
    }
}
