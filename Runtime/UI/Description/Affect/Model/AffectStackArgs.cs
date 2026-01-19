namespace GGemCo2DAffect
{
    /// <summary>
    /// 스택 정보 라인 Smart String에 전달되는 인자 모델입니다.
    /// </summary>
    public sealed class AffectStackArgs
    {
        /// <summary>스택 정책의 표시 이름(None/Refresh/Add/Independent 등)입니다.</summary>
        public string StackPolicy { get; set; } // None/Refresh/Add/Independent

        /// <summary>최대 스택 표시가 필요한지 여부입니다.</summary>
        public bool HasMaxStacks { get; set; }

        /// <summary>최대 스택 수 텍스트입니다.</summary>
        public string MaxStacksText { get; set; }
    }
}