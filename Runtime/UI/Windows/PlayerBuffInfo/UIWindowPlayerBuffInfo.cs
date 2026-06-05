using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DAffect
{
    /// <summary>
    /// 플레이어에게 적용된 어펙트 버프/디버프 아이콘을 표시하는 윈도우입니다.
    /// </summary>
    /// <remarks>
    /// 적용, 만료, 스택 규칙은 <see cref="AffectComponent"/>와
    /// <see cref="PlayerAffectUiPresenter"/>가 담당합니다.
    /// 이 윈도우는 Core 공통 <see cref="UISlot"/>과 <see cref="UIIcon"/> 생성 흐름을 사용하고,
    /// 전달받은 스냅샷을 기존 슬롯 배열에 바인딩하는 역할만 수행합니다.
    /// </remarks>
    public class UIWindowPlayerBuffInfo : UIWindow
    {
        private const int DefaultBuffSlotCount = 9;

        private readonly Dictionary<int, int> _slotIndexByAffectUid = new(DefaultBuffSlotCount);
        private readonly List<int> _affectUidsToRemove = new(DefaultBuffSlotCount);
        private readonly HashSet<int> _renderedAffectUids = new();

        /// <summary>
        /// 윈도우 UID와 기본 슬롯 수를 설정한 뒤 Core 공통 슬롯/아이콘 초기화를 실행합니다.
        /// </summary>
        protected override void Awake()
        {
            if (!TableLoaderManager.Instance) return;
            
            uid = UIWindowConstants.WindowUid.PlayerBuffInfo;

            if (maxCountIcon <= 0)
            {
                maxCountIcon = DefaultBuffSlotCount;
            }

            base.Awake();

            _slotIndexByAffectUid.Clear();
            _affectUidsToRemove.Clear();
            _renderedAffectUids.Clear();
            ClearAllIcons();
        }

        /// <summary>
        /// Presenter가 전달하는 어펙트 UI 스냅샷을 슬롯과 아이콘에 반영합니다.
        /// </summary>
        /// <param name="items">현재 표시해야 하는 어펙트 UI 아이템 목록입니다.</param>
        public void Render(IReadOnlyList<AffectUiItem> items)
        {
            if (icons == null || slots == null)
            {
                return;
            }

            _renderedAffectUids.Clear();

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    AffectUiItem item = items[i];
                    if (item.AffectUid <= 0)
                    {
                        continue;
                    }

                    UIIconBuff icon = GetOrAssignIcon(item.AffectUid);
                    if (icon == null)
                    {
                        continue;
                    }

                    _renderedAffectUids.Add(item.AffectUid);
                    icon.gameObject.SetActive(true);
                    icon.Bind(item);

                    UISlot slot = GetSlotByIndex(icon.slotIndex);
                    if (slot != null)
                    {
                        slot.gameObject.SetActive(true);
                    }
                }
            }

            ReleaseMissingIcons();
        }

        /// <summary>
        /// 지정한 어펙트 UID가 사용할 아이콘을 찾거나 빈 슬롯에 새로 배정합니다.
        /// </summary>
        /// <param name="affectUid">표시할 어펙트 UID입니다.</param>
        /// <returns>바인딩 가능한 버프 아이콘입니다. 빈 슬롯이 없으면 null을 반환합니다.</returns>
        private UIIconBuff GetOrAssignIcon(int affectUid)
        {
            if (_slotIndexByAffectUid.TryGetValue(affectUid, out int slotIndex))
            {
                UIIconBuff assignedIcon = GetBuffIconByIndex(slotIndex);
                if (assignedIcon != null)
                {
                    return assignedIcon;
                }

                _slotIndexByAffectUid.Remove(affectUid);
            }

            int emptySlotIndex = FindEmptySlotIndex();
            if (emptySlotIndex < 0)
            {
                return null;
            }

            _slotIndexByAffectUid[affectUid] = emptySlotIndex;
            return GetBuffIconByIndex(emptySlotIndex);
        }

        /// <summary>
        /// 현재 렌더링 스냅샷에서 사라진 어펙트 아이콘을 비우고 슬롯 매핑을 해제합니다.
        /// </summary>
        private void ReleaseMissingIcons()
        {
            _affectUidsToRemove.Clear();

            foreach (KeyValuePair<int, int> pair in _slotIndexByAffectUid)
            {
                if (!_renderedAffectUids.Contains(pair.Key))
                {
                    _affectUidsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < _affectUidsToRemove.Count; i++)
            {
                int affectUid = _affectUidsToRemove[i];
                if (_slotIndexByAffectUid.TryGetValue(affectUid, out int slotIndex))
                {
                    ClearIconAt(slotIndex);
                }

                _slotIndexByAffectUid.Remove(affectUid);
            }

            _affectUidsToRemove.Clear();
        }

        /// <summary>
        /// 비어 있는 버프 슬롯 인덱스를 찾습니다.
        /// </summary>
        /// <returns>사용 가능한 슬롯 인덱스입니다. 없으면 -1을 반환합니다.</returns>
        private int FindEmptySlotIndex()
        {
            for (int i = 0; i < icons.Length; i++)
            {
                UIIconBuff icon = GetBuffIconByIndex(i);
                if (icon == null)
                {
                    continue;
                }

                if (icon.uid <= 0 || icon.GetCount() <= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 지정한 슬롯 인덱스의 버프 아이콘을 반환합니다.
        /// </summary>
        /// <param name="slotIndex">조회할 슬롯 인덱스입니다.</param>
        /// <returns>해당 슬롯의 <see cref="UIIconBuff"/>입니다.</returns>
        private UIIconBuff GetBuffIconByIndex(int slotIndex)
        {
            if (slotIndex < 0 || icons == null || slotIndex >= icons.Length)
            {
                return null;
            }

            GameObject iconObject = icons[slotIndex];
            return iconObject != null ? iconObject.GetComponent<UIIconBuff>() : null;
        }

        /// <summary>
        /// 지정한 슬롯의 아이콘 정보를 비우고 재사용 가능한 상태로 초기화합니다.
        /// </summary>
        /// <param name="slotIndex">초기화할 슬롯 인덱스입니다.</param>
        private void ClearIconAt(int slotIndex)
        {
            UIIconBuff icon = GetBuffIconByIndex(slotIndex);
            if (icon != null)
            {
                icon.ClearCoolTime();
                icon.ResetBindingCache();
                icon.ClearIconInfos();
                icon.gameObject.SetActive(false);
            }

            UISlot slot = GetSlotByIndex(slotIndex);
            if (slot != null)
            {
                slot.SetSelected(false);
                slot.SetEquippedState(false);
                slot.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 초기화 직후 모든 버프 아이콘을 비워 빈 슬롯 상태로 맞춥니다.
        /// </summary>
        private void ClearAllIcons()
        {
            if (icons == null)
            {
                return;
            }

            for (int i = 0; i < icons.Length; i++)
            {
                ClearIconAt(i);
            }
        }
    }
}
