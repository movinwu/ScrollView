using UnityEngine;

namespace AsyncScrollView
{
    /// <summary>
    /// 滑动列表元素排列方式
    /// </summary>
    public enum EScrollViewItemLayout : byte
    {
        /// <summary>
        /// 先从左到右，再从上到下
        /// </summary>
        [InspectorName("left --> right, then up --> down")]
        Left2Right_Up2Down,
        
        /// <summary>
        /// 先从右到左，再从上到下
        /// </summary>
        [InspectorName("right --> left, then up --> down")]
        Right2Left_Up2Down,
        
        /// <summary>
        /// 先从左到右，再从下到上
        /// </summary>
        [InspectorName("left --> right, then down --> up")]
        Left2Right_Down2Up,
        
        /// <summary>
        /// 先从右到左，再从下到上
        /// </summary>
        [InspectorName("right --> left, then down --> up")]
        Right2Left_Down2Up,
        
        /// <summary>
        /// 先从上到下，再从左到右
        /// </summary>
        [InspectorName("up --> down, then left --> right")]
        Up2Down_Left2Right,
        
        /// <summary>
        /// 先从上到下，再从右到左
        /// </summary>
        [InspectorName("up --> down, then right --> left")]
        Up2Down_Right2Left,
        
        /// <summary>
        /// 先从下到上，再从左到右
        /// </summary>
        [InspectorName("down --> up, then left --> right")]
        Down2Up_Left2Right,
        
        /// <summary>
        /// 先从下到上，再从右到左
        /// </summary>
        [InspectorName("down --> up, then right --> left")]
        Down2Up_Right2Left,
    }
}
