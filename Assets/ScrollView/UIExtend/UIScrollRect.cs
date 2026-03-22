using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AsyncScrollView
{ 
    public class UIScrollRect : ScrollRect
    {
        /// <summary>
        /// 是否在拖动中
        /// </summary>
        private bool _isDragging = false;
    
        public override bool IsActive()
        {
            return base.IsActive() && !(content.lossyScale.x == 0 && content.lossyScale.y == 0);
        }

        /// <summary>
        /// 当content拓展时处理
        /// </summary>
        /// <param name="expandOffset"></param>
        public void OnContentExpand(Vector2 expandOffset)
        {
            // 停止回弹移动
            // TODO 根据滑动方向和其他参数等判断是否在回弹中
            // TODO 当前仅作简单判断，此判断只适应唯一情况，不具备通用性
            if (content.anchoredPosition.y < -0.1f)
            {
                StopMovement();
            }
        
            if (_isDragging)
            {
                // 拖动中处理
                m_ContentStartPosition += expandOffset;
                StopMovement();
            }
        
            // content位置处理
            content.anchoredPosition += expandOffset;
        
            // 更新各种数值
            UpdateBounds();
            UpdatePrevData();
        }

        protected override void LateUpdate()
        {
            if (!IsActive())
                return;

            base.LateUpdate();
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            _isDragging = true;
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            _isDragging = false;
        }
    }
}