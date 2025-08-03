using System;
using UnityEngine;
using UnityEngine.UI;

namespace AsyncScrollView
{
    /// <summary>
    /// 滑动列表组件
    /// </summary>
    [DisallowMultipleComponent]
    public class ScrollView : MonoBehaviour
    {
        #region 序列化

        /// <summary>
        /// 滑动区域
        /// </summary>
        [SerializeField, Header("滑动列表区域组件（可选指定，不指定时自动从当前物体上查找）")]
        internal ScrollRect scrollRect;

        /// <summary>
        /// item布局
        /// </summary>
        [SerializeField, Header("item布局")] internal EScrollViewItemLayout itemLayout;

        /// <summary>
        /// 单行（列）item数量
        /// </summary>
        [SerializeField, Header("单行（列）item数量")]
        internal int itemCountOneLine = 1;

        /// <summary>
        /// item预制体
        /// </summary>
        [SerializeField, Header("item预制体")] internal GameObject[] itemPrefabs;

        /// <summary>
        /// 是否循环列表
        /// </summary>
        [SerializeField, Header("是否无限循环列表")] internal bool isLoop;

        /// <summary>
        /// 每帧实例化item最大数量
        /// </summary>
        [SerializeField, Header("每帧实例化最大数量")] internal int frameInstantiateCount = 2;

        /// <summary>
        /// 回收类型
        /// </summary>
        [SerializeField, Header("item回收类型")] internal EItemDespawnType itemDespawnType;

        #endregion 序列化

        #region 不序列化

        /// <summary>
        /// 数据
        /// </summary>
        [NonSerialized] internal ScrollViewData Data;

        /// <summary>
        /// 缓存池
        /// </summary>
        [NonSerialized] internal GameObjectPool GameObjectPool;

        /// <summary>
        /// 数据管理器
        /// </summary>
        [NonSerialized] internal ItemDataController ItemDataController;

        /// <summary>
        /// 根节点transform
        /// </summary>
        [NonSerialized] internal RectTransform RootTransform;

        #endregion 不序列化

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="itemCount">数据数量</param>
        /// <param name="onItemBindData">item绑定数据时回调</param>
        /// <param name="getItemHeight">获取item高度回调</param>
        /// <param name="getItemWidth">获取item宽度回调</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="onItemUnbindData">item解绑数据时回调</param>
        /// <param name="getItemPrefabIndex">获取item预制体索引回调</param>
        public void Init(
            int itemCount,
            Action<(int dataIndex, GameObject itemInstance)> onItemBindData,
            Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemHeight,
            Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemWidth,
            int startIndex = 0,
            Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
            Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null)
        {
            // 获取scrollRect组件
            scrollRect ??= GetComponent<ScrollRect>();
            // 为空则抛出异常
            if (null == scrollRect)
            {
                throw new ArgumentException("ScrollRect is null");
            }

            // 检验每行数量
            if (itemCountOneLine <= 0)
            {
                Release();
                if (null != Data)
                {
                    Data.ItemCount = 0;
                }

                scrollRect.content.pivot = Vector2.one * 0.5f;
                scrollRect.content.anchorMin = Vector2.one * 0.5f;
                scrollRect.content.anchorMax = Vector2.one * 0.5f;
                scrollRect.content.sizeDelta = Vector2.zero;
                return;
            }

            // 检验每帧实例化数量
            if (frameInstantiateCount <= 0)
            {
                Debug.LogWarning(
                    $"frameInstantiateCount (current: {frameInstantiateCount}) must greater than 0, force change to max value");
                frameInstantiateCount = int.MaxValue;
            }

            // 检验获取高度和宽度的委托
            if (null == getItemHeight)
            {
                throw new ArgumentException("getItemHeight is null");
            }

            if (null == getItemWidth)
            {
                throw new ArgumentException("getItemWidth is null");
            }

            // 检验预制体模板列表
            if (itemPrefabs is not { Length: > 0 }
                || (itemPrefabs.Length == 1 && null == itemPrefabs[0]))
            {
                throw new ArgumentException("itemPrefabs is null or empty");
            }

            if (itemPrefabs.Length > 1)
            {
                // 检验不能有空的模板
                for (var i = 0; i < itemPrefabs.Length; i++)
                {
                    if (null == itemPrefabs[i])
                    {
                        throw new ArgumentException($"itemPrefabs[{i}] is null");
                    }
                }
            }

            // 每行数量和每列数量
            int row, col;

            // 根据参数情况设置滚动视图状态
            // 根据设置的滑动方向，确定是否是纵向滑动
            scrollRect.vertical = itemLayout switch
            {
                EScrollViewItemLayout.Left2Right_Up2Down
                    or EScrollViewItemLayout.Right2Left_Up2Down
                    or EScrollViewItemLayout.Left2Right_Down2Up
                    or EScrollViewItemLayout.Right2Left_Down2Up => true,
                _ => false
            };
            // 1.滑动列表只能有一种滑动方向，不支持水平和竖直同时可以滑动，优先竖直方向滑动
            if (scrollRect.vertical)
            {
                scrollRect.horizontal = false;
                scrollRect.horizontalScrollbar = null;
                // 2.无限滑动列表不支持滑动条
                if (isLoop)
                {
                    scrollRect.verticalScrollbar = null;
                }

                // 3. 计算行数和列数等相关参数
                col = itemCountOneLine;
                row = itemCount / itemCountOneLine + (itemCount % itemCountOneLine > 0 ? 1 : 0);
            }
            else
            {
                scrollRect.horizontal = true;
                scrollRect.verticalScrollbar = null;
                // 2.无限滑动列表不支持滑动条
                if (isLoop)
                {
                    scrollRect.horizontalScrollbar = null;
                }

                // 3. 计算行数和列数等相关参数
                row = itemCountOneLine;
                col = itemCount / itemCountOneLine + (itemCount % itemCountOneLine > 0 ? 1 : 0);
            }

            // 4. 初始化数据
            Data ??= new ScrollViewData();
            Data.Init(
                itemCount,
                row,
                col,
                itemCountOneLine,
                itemPrefabs,
                onItemBindData,
                onItemUnbindData,
                itemLayout,
                getItemHeight,
                getItemWidth,
                frameInstantiateCount,
                getItemPrefabIndex);

            // 5. 初始化根节点
            if (null == RootTransform)
            {
                var root = new GameObject("Root");
                root.transform.SetParent(scrollRect.content);
                RootTransform = root.GetComponent<RectTransform>();
                if (null == RootTransform)
                {
                    RootTransform = root.AddComponent<RectTransform>();
                }
            }

            RootTransform.sizeDelta = Vector2.zero;
            RootTransform.pivot = Vector2.one * 0.5f;

            // 6. 位置控制器创建
            ItemDataController ??= new ItemDataController();

            // 7. 缓存池创建
            GameObjectPool ??= new GameObjectPool();

            // 8. 控制器和缓存池初始化
            ItemDataController.DespawnAllItem();
            GameObjectPool.Init(Data, itemDespawnType);
            ItemDataController.Init(this, startIndex);
        }

        /// <summary>
        /// 释放所有
        /// </summary>
        public void Release()
        {
            ItemDataController?.DespawnAllItem();
            GameObjectPool?.Release();
        }

        /// <summary>
        /// 刷新单个
        /// </summary>
        /// <param name="index"></param>
        public void RefreshSingle(int index)
        {
            ItemDataController?.RefreshSingle(index);
        }

        /// <summary>
        /// 异步刷新所有
        /// </summary>
        public void RefreshAll()
        {
            ItemDataController?.RefreshAll();
        }
        
        #region 跳转

        /// <summary>
        /// 根据viewport的百分比和item的百分比跳转到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="viewportPercent">viewport对齐百分比</param>
        /// <param name="itemPercent">item对齐百分比</param>
        public void JumpToIndexByPercentPercent(int index, float viewportPercent = 0f, float itemPercent = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var viewportOffset = CalculateViewportOffset(viewportPercent);
            var itemOffset = CalculateItemOffset(index, itemPercent);

            // 跳转
            ItemDataController?.JumpToIndex(index, viewportOffset, itemOffset);
        }

        /// <summary>
        /// 根据viewport的百分比和item的偏移跳转到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="viewportPercent">viewport对齐百分比</param>
        /// <param name="itemOffset">item偏移</param>
        public void JumpToIndexByPercentOffset(int index, float viewportPercent = 0f, float itemOffset = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var viewportOffset = CalculateViewportOffset(viewportPercent);

            // 跳转
            ItemDataController?.JumpToIndex(index, viewportOffset, itemOffset);
        }

        /// <summary>
        /// 根据viewport的偏移和item的百分比跳转到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="viewportOffset">viewport对齐偏移</param>
        /// <param name="itemPercent">item对齐百分比</param>
        public void JumpToIndexByOffsetPercent(int index, float viewportOffset = 0f, float itemPercent = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var itemOffset = CalculateItemOffset(index, itemPercent);

            // 跳转
            ItemDataController?.JumpToIndex(index, viewportOffset, itemOffset);
        }

        /// <summary>
        /// 根据viewport的偏移和item的偏移跳转到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="viewportOffset">viewport对齐偏移</param>
        /// <param name="itemOffset">item对齐偏移</param>
        public void JumpToIndexByOffsetOffset(int index, float viewportOffset = 0f, float itemOffset = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            ItemDataController?.JumpToIndex(index, viewportOffset, itemOffset);
        }
        
        #endregion 跳转
        
        #region 指定速度移动

        /// <summary>
        /// 根据viewport的偏移和item的偏移指定速度移动到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="speed">移动速度</param>
        /// <param name="onMoveCompleted">移动完成回调</param>
        /// <param name="viewportPercent">viewport对齐百分比</param>
        /// <param name="itemPercent">item对齐百分比</param>
        public void MoveToIndexBySpeedPercentPercent(int index, float speed, Action<bool> onMoveCompleted = null,
            float viewportPercent = 0f, float itemPercent = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var viewportOffset = CalculateViewportOffset(viewportPercent);
            var itemOffset = CalculateItemOffset(index, itemPercent);

            // 移动
            ItemDataController?.MoveToIndexBySpeed(index, viewportOffset, itemOffset, speed, onMoveCompleted);
        }

        /// <summary>
        /// 根据viewport的百分比和item的百分比指定移动速度移动到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="speed">移动速度</param>
        /// <param name="onMoveCompleted">移动完成回调</param>
        /// <param name="viewportPercent">viewport对齐百分比</param>
        /// <param name="itemOffset">item对齐偏移</param>
        public void MoveToIndexBySpeedPercentOffset(int index, float speed, Action<bool> onMoveCompleted = null,
            float viewportPercent = 0f, float itemOffset = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var viewportOffset = CalculateViewportOffset(viewportPercent);

            ItemDataController?.MoveToIndexBySpeed(index, viewportOffset, itemOffset, speed, onMoveCompleted);
        }

        /// <summary>
        /// 根据viewport的百分比和item的偏移指定移动速度移动到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="speed">移动速度</param>
        /// <param name="onMoveCompleted">移动完成回调</param>
        /// <param name="viewportOffset">viewport对齐偏移</param>
        /// <param name="itemPercent">item对齐百分比</param>
        public void MoveToIndexBySpeedOffsetPercent(int index, float speed, Action<bool> onMoveCompleted = null,
            float viewportOffset = 0f, float itemPercent = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var itemOffset = CalculateItemOffset(index, itemPercent);

            ItemDataController?.MoveToIndexBySpeed(index, viewportOffset, itemOffset, speed, onMoveCompleted);
        }

        /// <summary>
        /// 根据viewport偏移和item的偏移指定移动速度移动到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="speed">移动速度</param>
        /// <param name="onMoveCompleted">移动完成回调</param>
        /// <param name="viewportOffset">viewport对齐偏移</param>
        /// <param name="itemOffset">item对齐偏移</param>
        public void MoveToIndexBySpeedOffsetOffset(int index, float speed, Action<bool> onMoveCompleted = null,
            float viewportOffset = 0f, float itemOffset = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            ItemDataController?.MoveToIndexBySpeed(index, viewportOffset, itemOffset, speed, onMoveCompleted);
        }
        
        #endregion 指定速度移动

        #region 指定时间移动
        
        /// <summary>
        /// 根据viewport的偏移和item的偏移指定时间移动到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="time">移动时间</param>
        /// <param name="onMoveCompleted">移动完成回调</param>
        /// <param name="viewportPercent">viewport对齐百分比</param>
        /// <param name="itemPercent">item对齐百分比</param>
        public void MoveToIndexByTimePercentPercent(int index, float time, Action<bool> onMoveCompleted = null,
            float viewportPercent = 0f, float itemPercent = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var viewportOffset = CalculateViewportOffset(viewportPercent);
            var itemOffset = CalculateItemOffset(index, itemPercent);

            // 移动
            ItemDataController?.MoveToIndexByTime(index, viewportOffset, itemOffset, time, onMoveCompleted);
        }

        /// <summary>
        /// 根据viewport的百分比和item的百分比指定移动时间移动到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="time">移动时间</param>
        /// <param name="onMoveCompleted">移动完成回调</param>
        /// <param name="viewportPercent">viewport对齐百分比</param>
        /// <param name="itemOffset">item对齐偏移</param>
        public void MoveToIndexByTimePercentOffset(int index, float time, Action<bool> onMoveCompleted = null,
            float viewportPercent = 0f, float itemOffset = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var viewportOffset = CalculateViewportOffset(viewportPercent);

            ItemDataController?.MoveToIndexByTime(index, viewportOffset, itemOffset, time, onMoveCompleted);
        }

        /// <summary>
        /// 根据viewport的百分比和item的偏移指定移动时间移动到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="time">移动时间</param>
        /// <param name="onMoveCompleted">移动完成回调</param>
        /// <param name="viewportOffset">viewport对齐偏移</param>
        /// <param name="itemPercent">item对齐百分比</param>
        public void MoveToIndexByTimeOffsetPercent(int index, float time, Action<bool> onMoveCompleted = null,
            float viewportOffset = 0f, float itemPercent = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            // 计算偏移
            var itemOffset = CalculateItemOffset(index, itemPercent);

            ItemDataController?.MoveToIndexByTime(index, viewportOffset, itemOffset, time, onMoveCompleted);
        }

        /// <summary>
        /// 根据viewport偏移和item的偏移指定移动时间移动到指定索引
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="time">移动时间</param>
        /// <param name="onMoveCompleted">移动完成回调</param>
        /// <param name="viewportOffset">viewport对齐偏移</param>
        /// <param name="itemOffset">item对齐偏移</param>
        public void MoveToIndexByTimeOffsetOffset(int index, float time, Action<bool> onMoveCompleted = null,
            float viewportOffset = 0f, float itemOffset = 0f)
        {
            // 校验下标
            if (null == Data
                || Data.ItemCount <= 0
                || (isLoop
                    && (index < 0 || index >= Data.ItemCount)))
            {
                return;
            }

            ItemDataController?.MoveToIndexByTime(index, viewportOffset, itemOffset, time, onMoveCompleted);
        }

        #endregion 指定时间移动

        /// <summary>
        /// 计算viewport偏移
        /// </summary>
        /// <param name="viewportPercent">百分比</param>
        /// <returns></returns>
        private float CalculateViewportOffset(float viewportPercent)
        {
            var viewportSize = scrollRect.viewport.rect.size;
            switch (itemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    return viewportSize.y * viewportPercent;
                    break;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    return viewportSize.x * viewportPercent;
                    break;
            }
            return 0f;
        }
        
        /// <summary>
        /// 计算item偏移
        /// </summary>
        /// <param name="index">下标</param>
        /// <param name="itemPercent">item偏移百分比</param>
        /// <returns></returns>
        private float CalculateItemOffset(int index, float itemPercent)
        {
            switch (itemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    var itemHeight = Data.GetItemHeight((index / Data.ItemCountOneLine, Data.Row));
                    return (itemHeight.upHeight + itemHeight.downHeight) * itemPercent;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    var itemWidth = Data.GetItemWidth((index / Data.ItemCountOneLine, Data.Col));
                    return (itemWidth.leftWidth + itemWidth.rightWidth) * itemPercent;
            }

            return 0f;
        }

        private void Update()
        {
            ItemDataController?.Tick();
        }

        private void OnDestroy()
        {
            Release();
        }
    }
}