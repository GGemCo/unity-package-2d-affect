using System;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 어펙트 UI 아이콘에 겹쳐 표시하는 보조 데코레이터 데이터입니다.
    /// </summary>
    [Serializable]
    public readonly struct AffectUiDecoratorData
    {
        /// <summary>데코레이터를 표시할지 여부입니다.</summary>
        public readonly bool Visible;

        /// <summary>표시할 데코레이터 Sprite입니다.</summary>
        public readonly Sprite Sprite;

        /// <summary>데코레이터 RectTransform 크기입니다.</summary>
        public readonly Vector2 Size;

        /// <summary>아이콘 기준 배치 위치입니다.</summary>
        public readonly AffectUiDecoratorAnchor Anchor;

        /// <summary>기준 위치에서 추가로 적용할 오프셋입니다.</summary>
        public readonly Vector2 Offset;

        /// <summary>
        /// 어펙트 아이콘 데코레이터 표시 데이터를 생성합니다.
        /// </summary>
        /// <param name="visible">데코레이터 표시 여부입니다.</param>
        /// <param name="sprite">표시할 Sprite입니다.</param>
        /// <param name="size">표시 크기입니다.</param>
        /// <param name="anchor">아이콘 기준 배치 위치입니다.</param>
        /// <param name="offset">기준 위치에서 추가할 오프셋입니다.</param>
        public AffectUiDecoratorData(bool visible, Sprite sprite, Vector2 size, AffectUiDecoratorAnchor anchor, Vector2 offset)
        {
            Visible = visible;
            Sprite = sprite;
            Size = size;
            Anchor = anchor;
            Offset = offset;
        }

        /// <summary>
        /// 데코레이터를 숨기는 기본 데이터입니다.
        /// </summary>
        public static AffectUiDecoratorData Hidden => new(false, null, Vector2.zero, AffectUiDecoratorAnchor.RightBottom, Vector2.zero);
    }

    /// <summary>
    /// 보조 데코레이터를 아이콘의 어느 기준점에 배치할지 정의합니다.
    /// </summary>
    public enum AffectUiDecoratorAnchor
    {
        /// <summary>아이콘 좌하단 기준입니다.</summary>
        LeftBottom = 0,

        /// <summary>아이콘 우하단 기준입니다.</summary>
        RightBottom = 1,

        /// <summary>아이콘 좌상단 기준입니다.</summary>
        LeftTop = 2,

        /// <summary>아이콘 우상단 기준입니다.</summary>
        RightTop = 3,
    }
}
