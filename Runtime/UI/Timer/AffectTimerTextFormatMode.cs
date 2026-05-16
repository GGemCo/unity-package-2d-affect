namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 남은 시간 텍스트를 어떤 형식으로 표시할지 정의합니다.
    /// </summary>
    public enum AffectTimerTextFormatMode
    {
        /// <summary>소수점 1자리 초 단위로 표시합니다. 예: 3.4s</summary>
        SecondsDecimal,

        /// <summary>올림 처리한 정수 초 단위로 표시합니다. 예: 4</summary>
        SecondsCeil,

        /// <summary>내림 처리한 정수 초 단위로 표시합니다. 예: 3</summary>
        SecondsFloor,

        /// <summary>분:초 형식으로 표시합니다. 예: 00:03</summary>
        MinutesSeconds,

        /// <summary>한글 초 단위로 표시합니다. 예: 3.4초</summary>
        KoreanSeconds,

        /// <summary>전체 지속 시간 대비 남은 비율로 표시합니다. 예: 68%</summary>
        Percent,

        /// <summary>어펙트 이름과 남은 시간을 함께 표시합니다. 예: 중독 3.4s</summary>
        NameAndSeconds,

        /// <summary>스택 수와 남은 시간을 함께 표시합니다. 예: x3 3.4s</summary>
        StackAndSeconds,

        /// <summary>어펙트 이름, 스택 수, 남은 시간을 함께 표시합니다. 예: 중독 x3 3.4s</summary>
        NameStackAndSeconds
    }
}
