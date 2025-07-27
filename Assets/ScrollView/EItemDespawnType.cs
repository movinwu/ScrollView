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
        /// <summary>
        /// 设置元素失活
        /// </summary>
        [InspectorName("set GameObject active false")]
        SetActiveFalse,
        
        /// <summary>
        /// 移动元素到不可见位置
        /// </summary>
        [InspectorName("move GameObject far away from scroll")]
        MoveInvisiblePosition,
        
        /// <summary>
        /// 不处理
        /// </summary>
        [InspectorName("do nothing")]
        None,
    }
}
