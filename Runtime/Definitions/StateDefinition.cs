using System;

namespace GGemCo2DAffect
{
    [Serializable]
    public sealed class StateDefinition
    {
        public string Id;
        public string NameKey;

        /// <summary>기본 Dispel 분류.</summary>
        public DispelType DispelType;

        /// <summary>CC/Immune/Utility 등 분류 태그(선택).</summary>
        public string Category;
    }
}
