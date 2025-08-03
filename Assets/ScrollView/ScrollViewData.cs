using System;
using UnityEngine;

namespace AsyncScrollView
{
    /// <summary>
    /// 滑动列表数据
    /// </summary>
    internal class ScrollViewData
    {
        /// <summary>
        /// content内容区域拓展时的单次拓展长度
        /// </summary>
        public const float ContentExpandLength = 10000f;
        
        /// <summary>
        /// item总数量
        /// </summary>
        public int ItemCount;

        /// <summary>
        /// 行数
        /// </summary>
        public int Row;

        /// <summary>
        /// 列数
        /// </summary>
        public int Col;

        /// <summary>
        /// 获取item高度
        /// </summary>
        public Func<(int row, int totalRow), (float upHeight, float downHeight)> GetItemHeight;

        /// <summary>
        /// 获取item宽度
        /// </summary>
        public Func<(int col, int totalCol), (float leftWidth, float rightWidth)> GetItemWidth;

        /// <summary>
        /// item模板预制体
        /// </summary>
        public GameObject[] ItemPrefabs;

        /// <summary>
        /// 获取元素模板预制体下标委托
        /// </summary>
        public Func<(GameObject[] itemPrefabs, int dataIndex), int> GetItemPrefabIndex;
        
        /// <summary>
        /// item布局
        /// </summary>
        public EScrollViewItemLayout ItemLayout = EScrollViewItemLayout.Left2Right_Up2Down;

        /// <summary>
        /// 当item绑定数据显示时调用
        /// </summary>
        public Action<(int dataIndex, GameObject itemInstance)> OnItemBindData;

        /// <summary>
        /// 当item解绑数据显示时调用
        /// </summary>
        public Action<(int dataIndex, GameObject itemInstance)> OnItemUnbindData;

        /// <summary>
        /// 每帧实例化item数量
        /// </summary>
        public int FrameInstantiateCount;

        /// <summary>
        /// 每行item数量
        /// </summary>
        public int ItemCountOneLine;

        /// <summary>
        /// 初始化函数
        /// </summary>
        /// <param name="itemCount">item数量</param>
        /// <param name="row">总行数</param>
        /// <param name="col">总列数</param>
        /// <param name="itemCountOneLine">单行item数量</param>
        /// <param name="itemPrefabs">预制体模板</param>
        /// <param name="onItemBindData">item绑定数据显示时调用</param>
        /// <param name="onItemUnbindData">item解绑数据显示时调用</param>
        /// <param name="itemLayout">item布局</param>
        /// <param name="frameInstantiateCount">每帧实例化数量</param>
        /// <param name="getItemHeight">获取item高度委托</param>
        /// <param name="getItemWidth">获取item宽度委托</param>
        /// <param name="getItemPrefabIndex">获取item预制体</param>
        internal void Init(
            int itemCount,
            int row,
            int col,
            int itemCountOneLine,
            GameObject[] itemPrefabs,
            Action<(int dataIndex, GameObject itemInstance)> onItemBindData,
            Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData,
            EScrollViewItemLayout itemLayout,
            Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemHeight,
            Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemWidth,
            int frameInstantiateCount,
            Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex)
        {
            ItemCount = itemCount;
            Row = row;
            Col = col;
            ItemCountOneLine = itemCountOneLine;
            ItemPrefabs = itemPrefabs;
            GetItemHeight = getItemHeight;
            GetItemWidth = getItemWidth;
            GetItemPrefabIndex = getItemPrefabIndex;
            OnItemBindData = onItemBindData;
            OnItemUnbindData = onItemUnbindData;
            ItemLayout = itemLayout;
            FrameInstantiateCount = frameInstantiateCount;
            
            GetItemPrefabIndex ??= DefaultGetItemPrefab;
        }

        /// <summary>
        /// 默认获取item预制体模板下标实现
        /// </summary>
        /// <param name="_"></param>
        private int DefaultGetItemPrefab((GameObject[] itemPrefabs, int dataIndex) _) => 0;
    }
}
