namespace GGemCo2DAffect
{
    
    /// <summary>
    /// AffectDescription String Table 키 정의.
    /// </summary>
    public static class AffectDescriptionKeys
    {
        /// <summary>확률 접두 문구 키(예: "발동 확률 {Chance}%").</summary>
        public const string PrefixChance = "Affect_Prefix_Chance";

        /// <summary>스탯 변화 라인 템플릿 키.</summary>
        public const string LineStat = "Affect_Line_Stat";

        /// <summary>데미지 라인 템플릿 키.</summary>
        public const string LineDamage = "Affect_Line_Damage";

        /// <summary>상태이상(State) 라인 템플릿 키.</summary>
        public const string LineState = "Affect_Line_State";

        /// <summary>지속시간 정보 라인 템플릿 키.</summary>
        public const string InfoDuration = "Affect_Info_Duration";

        /// <summary>틱 간격 정보 라인 템플릿 키.</summary>
        public const string InfoTick = "Affect_Info_Tick";

        /// <summary>스택 정책/최대 스택 정보 라인 템플릿 키.</summary>
        public const string InfoStack = "Affect_Info_Stack";
    }

}