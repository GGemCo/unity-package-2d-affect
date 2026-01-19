namespace GGemCo2DAffect
{
    /// <summary>
    /// "확률 접두" Smart String에 전달되는 인자 모델입니다.
    /// </summary>
    public sealed class AffectChancePrefixArgs
    {
        /// <summary>퍼센트로 표시된 확률 텍스트(예: "25").</summary>
        public string ChancePercentText { get; set; }
    }
}