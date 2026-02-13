using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 단일 Affect(버프/디버프) 정의를 표현하는 데이터 클래스.
    /// </summary>
    /// <remarks>
    /// - 주로 테이블(Row) 데이터를 런타임에서 사용 가능한 형태로 보관하기 위한 목적이다.
    /// - 이 클래스 자체는 상태를 가지지 않으며, 실제 적용/스택/만료 로직은 런타임(AffectInstance/AffectComponent)에서 처리한다.
    /// - <see cref="Serializable"/>로 선언되어 에디터/디버깅/직렬화에 사용될 수 있다.
    /// </remarks>
    [Serializable]
    public sealed class AffectDefinition
    {
        /// <summary>
        /// Affect의 고유 식별자(UID).
        /// </summary>
        public int uid;

        /// <summary>
        /// 로컬라이즈된 이름을 조회하기 위한 키.
        /// </summary>
        public string nameKey;

        /// <summary>
        /// UI 표시용 아이콘 키(Addressables Key 등).
        /// </summary>
        public string iconKey;

        /// <summary>
        /// Affect 그룹 ID.
        /// </summary>
        /// <remarks>
        /// - 동일 그룹 내 Affect 간 중첩/상호 배제 정책에 사용될 수 있다.
        /// - 비어 있거나 "None"인 경우 그룹이 없는 것으로 간주한다.
        /// </remarks>
        public string groupId;

        /// <summary>
        /// 기본 지속 시간(초).
        /// </summary>
        public float baseDuration;

        /// <summary>
        /// 틱 간격(초). 0 이하이면 틱 효과가 없는 것으로 간주한다.
        /// </summary>
        public float tickInterval;

        /// <summary>
        /// 스택 정책(중첩/독립/갱신 등).
        /// </summary>
        public StackPolicy stackPolicy;

        /// <summary>
        /// 허용되는 최대 스택 수.
        /// </summary>
        public int maxStacks;

        /// <summary>
        /// 스택 추가 시 지속 시간 갱신 정책.
        /// </summary>
        public RefreshPolicy refreshPolicy;

        /// <summary>
        /// 디스펠(해제) 분류 타입.
        /// </summary>
        public DispelType dispelType;

        /// <summary>
        /// Affect에 부여된 태그 목록.
        /// </summary>
        /// <remarks>
        /// - 원본 문자열은 쉼표/공백 기준으로 파싱되어 저장된다.
        /// - 조건부 효과, 필터링, UI 분류 등에 사용될 수 있다.
        /// </remarks>
        public List<string> tags = new();

        /// <summary>
        /// 연출에 사용되는 VFX 식별자.
        /// </summary>
        public int effectUid;

        /// <summary>
        /// VFX 스케일 배율.
        /// </summary>
        public float effectScale;

        /// <summary>
        /// VFX Y축 오프셋 값.
        /// </summary>
        public float effectOffsetY;

        /// <summary>
        /// VFX 표시 기준 위치 타입.
        /// </summary>
        public AffectEffectPositionType effectPositionType;

        /// <summary>
        /// VFX 추적(Follow) 타입.
        /// </summary>
        public AffectEffectFollowType effectFollowType;

        /// <summary>
        /// VFX 정렬 레이어 키. Core의 <see cref="GGemCo2DCore.ConfigSortingLayer.Keys"/> 중 하나를 문자열로 저장한다.
        /// </summary>
        public ConfigSortingLayer.Keys effectSortingLayerKey;

        /// <summary>
        /// Affect 적용 확률(0~1 범위 기대).
        /// </summary>
        public float applyChance;

        /// <summary>
        /// 틱 효과가 존재하는지 여부를 반환한다.
        /// </summary>
        public bool HasTick => tickInterval > 0f;

        /// <summary>
        /// 그룹이 없다고 간주되는 Affect인지 여부를 반환한다.
        /// </summary>
        /// <remarks>
        /// - <see cref="groupId"/>가 비어 있거나 "None"(대소문자 무시)인 경우 true.
        /// </remarks>
        public bool IsNoneGroup =>
            string.IsNullOrWhiteSpace(groupId) ||
            groupId.Equals("None", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 지정한 태그를 포함하고 있는지 여부를 확인한다.
        /// </summary>
        /// <param name="tag">확인할 태그 문자열.</param>
        /// <returns>태그가 존재하면 <c>true</c>, 그렇지 않으면 <c>false</c>.</returns>
        /// <remarks>
        /// - 대소문자를 구분하지 않고 비교한다.
        /// - <paramref name="tag"/>가 null 또는 공백이면 항상 false를 반환한다.
        /// </remarks>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
