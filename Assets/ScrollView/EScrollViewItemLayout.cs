using UnityEngine;

namespace AsyncScrollView
{
    /// <summary>
    /// 滑动列表元素排列方式
    /// </summary>
    public enum EScrollViewItemLayout : byte
    {
        [InspectorName("上->下，同行左->右")]
        Left2RightUp2Down,
        
        [InspectorName("上->下，同行右->左")]
        Right2LeftUp2Down,
        
        [InspectorName("下->上，同行左->右")]
        Left2RightDown2Up,
        
        [InspectorName("下->上，同行右->左")]
        Right2LeftDown2Up,
        
        [InspectorName("左->右，同列上->下")]
        Up2DownLeft2Right,
        
        [InspectorName("右->左，同列上->下")]
        Up2DownRight2Left,
        
        [InspectorName("左->右，同列下->上")]
        Down2UpLeft2Right,
        
        [InspectorName("右->左，同列下->上")]
        Down2UpRight2Left,
    }
}
