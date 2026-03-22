using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AsyncScrollView
{
    /// <summary>
    /// 元素回池类型
    /// </summary>
    public enum EItemDespawnType : byte
    {
        [InspectorName("设置元素隐藏")]
        SetActiveFalse,
        
        [InspectorName("移动元素到不可见位置")]
        MoveInvisiblePosition,
        
        [InspectorName("设置元素缩放为0")]
        SetScaleZero,
        
        [InspectorName("不处理")]
        None,
    }
}
