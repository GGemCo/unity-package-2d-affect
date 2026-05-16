using System.Globalization;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 남은 시간 표시 문자열을 생성하는 포매터입니다.
    /// </summary>
    /// <remarks>
    /// UI View는 텍스트를 그대로 표시하고, 표시 형식 변경은 이 클래스에서만 처리합니다.
    /// </remarks>
    public static class AffectTimerTextFormatter
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private const string StyledSecondsCentisecondsTemplate =
            "<mspace=0.45em>{0}<mspace=0.3em>.</mspace><mspace=0.3em><size=0.6em>{1}</size></mspace></mspace>";

        /// <summary>
        /// 지정한 표시 모드에 맞춰 어펙트 남은 시간 텍스트를 생성합니다.
        /// </summary>
        /// <param name="mode">표시 형식입니다.</param>
        /// <param name="remainingTime">남은 시간(초)입니다.</param>
        /// <param name="totalDuration">전체 지속 시간(초)입니다.</param>
        /// <param name="stacks">표시할 스택 수입니다.</param>
        /// <param name="displayName">표시할 어펙트 이름입니다.</param>
        /// <returns>TMP_Text에 적용할 문자열입니다.</returns>
        public static string Format(
            AffectTimerTextFormatMode mode,
            float remainingTime,
            float totalDuration,
            int stacks,
            string displayName)
        {
            remainingTime = Mathf.Max(0f, remainingTime);
            totalDuration = Mathf.Max(0f, totalDuration);
            stacks = Mathf.Max(1, stacks);
            displayName ??= string.Empty;

            switch (mode)
            {
                case AffectTimerTextFormatMode.SecondsCeil:
                    return Mathf.CeilToInt(remainingTime).ToString(Invariant);

                case AffectTimerTextFormatMode.SecondsFloor:
                    return Mathf.FloorToInt(remainingTime).ToString(Invariant);

                case AffectTimerTextFormatMode.MinutesSeconds:
                    return FormatMinutesSeconds(remainingTime);

                case AffectTimerTextFormatMode.KoreanSeconds:
                    return $"{remainingTime.ToString("0.0", Invariant)}초";

                case AffectTimerTextFormatMode.Percent:
                    return FormatPercent(remainingTime, totalDuration);

                case AffectTimerTextFormatMode.NameAndSeconds:
                    return string.IsNullOrWhiteSpace(displayName)
                        ? FormatSecondsDecimal(remainingTime)
                        : $"{displayName} {FormatSecondsDecimal(remainingTime)}";

                case AffectTimerTextFormatMode.StackAndSeconds:
                    return stacks > 1
                        ? $"x{stacks.ToString(Invariant)} {FormatSecondsDecimal(remainingTime)}"
                        : FormatSecondsDecimal(remainingTime);

                case AffectTimerTextFormatMode.NameStackAndSeconds:
                    return FormatNameStackAndSeconds(displayName, stacks, remainingTime);

                case AffectTimerTextFormatMode.StyledSecondsCentiseconds:
                    return FormatStyledSecondsCentiseconds(remainingTime);

                case AffectTimerTextFormatMode.SecondsDecimal:
                default:
                    return FormatSecondsDecimal(remainingTime);
            }
        }

        /// <summary>
        /// 소수점 1자리 초 단위 문자열을 생성합니다.
        /// </summary>
        private static string FormatSecondsDecimal(float seconds)
        {
            return $"{seconds.ToString("0.0", Invariant)}s";
        }

        /// <summary>
        /// 분:초 형식 문자열을 생성합니다.
        /// </summary>
        private static string FormatMinutesSeconds(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(seconds);
            int minutes = Mathf.Max(0, totalSeconds / 60);
            int remainSeconds = Mathf.Max(0, totalSeconds % 60);
            return $"{minutes.ToString("00", Invariant)}:{remainSeconds.ToString("00", Invariant)}";
        }

        /// <summary>
        /// TMP RichText 태그를 사용해 <c>SS.cc</c> 형식 문자열을 생성합니다.
        /// </summary>
        /// <param name="seconds">남은 시간(초)입니다.</param>
        /// <returns>
        /// 예: <c>&lt;mspace=0.45em&gt;02&lt;mspace=0.3em&gt;.&lt;/mspace&gt;&lt;mspace=0.3em&gt;&lt;size=0.6em&gt;00&lt;/size&gt;&lt;/mspace&gt;&lt;/mspace&gt;</c>
        /// </returns>
        /// <remarks>
        /// 밀리세컨즈 2자리 표기 정책을 위해 센티세컨드(1/100초) 단위로 계산합니다.
        /// </remarks>
        private static string FormatStyledSecondsCentiseconds(float seconds)
        {
            int totalCentiseconds = ToTotalCentiseconds(seconds);
            int wholeSeconds = totalCentiseconds / 100;
            int centiseconds = totalCentiseconds % 100;

            string wholeText = wholeSeconds.ToString("00", Invariant);
            string centiText = centiseconds.ToString("00", Invariant);

            return string.Format(Invariant, StyledSecondsCentisecondsTemplate, wholeText, centiText);
        }

        /// <summary>
        /// 초 단위 시간을 센티세컨드 정수로 변환합니다.
        /// </summary>
        /// <param name="seconds">변환할 시간(초)입니다.</param>
        /// <returns>0 이상 센티세컨드 값입니다.</returns>
        /// <remarks>
        /// 카운트다운 숫자가 역행하지 않도록 내림(Floor) 기반으로 계산합니다.
        /// </remarks>
        private static int ToTotalCentiseconds(float seconds)
        {
            float clampedSeconds = Mathf.Max(0f, seconds);
            return Mathf.Max(0, Mathf.FloorToInt(clampedSeconds * 100f));
        }

        /// <summary>
        /// 전체 지속 시간 대비 남은 비율 문자열을 생성합니다.
        /// </summary>
        private static string FormatPercent(float remainingTime, float totalDuration)
        {
            if (totalDuration <= 0f)
                return "100%";

            float ratio = Mathf.Clamp01(remainingTime / totalDuration);
            int percent = Mathf.CeilToInt(ratio * 100f);
            return $"{percent.ToString(Invariant)}%";
        }

        /// <summary>
        /// 이름, 스택, 시간을 조합한 문자열을 생성합니다.
        /// </summary>
        private static string FormatNameStackAndSeconds(string displayName, int stacks, float remainingTime)
        {
            string time = FormatSecondsDecimal(remainingTime);
            bool hasName = !string.IsNullOrWhiteSpace(displayName);
            bool hasStack = stacks > 1;

            if (hasName && hasStack)
                return $"{displayName} x{stacks.ToString(Invariant)} {time}";

            if (hasName)
                return $"{displayName} {time}";

            if (hasStack)
                return $"x{stacks.ToString(Invariant)} {time}";

            return time;
        }
    }
}
