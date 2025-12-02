using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AsyncScrollView
{
    /// <summary>
    /// 数据管理器类，负责管理滑动列表下的元素数据的新建、刷新、销毁等逻辑
    /// </summary>
    internal class ItemDataController
    {
        /// <summary>
        /// 列表项数据
        /// </summary>
        private readonly LinkedList<ScrollViewItemData> _itemData = new LinkedList<ScrollViewItemData>();

        /// <summary>
        /// 没有使用的所有item数据
        /// </summary>
        private readonly List<ScrollViewItemData> _freeItemData = new List<ScrollViewItemData>();

        /// <summary>
        /// 版本，用于异步操作时检查该异步操作是否过期
        /// </summary>
        private ulong _version;

        /// <summary>
        /// 滑动列表组件
        /// </summary>
        private ScrollView _scrollView;

        /// <summary>
        /// 滑动列表数据
        /// </summary>
        public ScrollViewData Data => _scrollView.Data;

        /// <summary>
        /// 滑动列表缓存池
        /// </summary>
        public GameObjectPool GameObjectPool => _scrollView.GameObjectPool;

        /// <summary>
        /// content缓存
        /// </summary>
        private RectTransform _contentCache;

        /// <summary>
        /// 虚拟窗口位置缓存
        /// </summary>
        private float _visibleWindowPositionCache;

        /// <summary>
        /// 放置所有data的默认父节点的根节点
        /// </summary>
        private RectTransform _itemDataRoot;

        /// <summary>
        /// 分帧实例化数量和帧数
        /// </summary>
        private int _frameInstanceCounter, _frameNum;

        /// <summary>
        /// 是否初始化完毕
        /// </summary>
        private bool _isInited = false;

        /// <summary>
        /// 每行y坐标缓存
        /// </summary>
        private float[] _itemYPositionCache;

        /// <summary>
        /// 每列x坐标缓存
        /// </summary>
        private float[] _itemXPositionCache;

        /// <summary>
        /// 每列宽度计算结果缓存
        /// </summary>
        private (float leftWidth, float rightWidth)[] _itemWidthCache;

        /// <summary>
        /// 每行高度计算结果缓存
        /// </summary>
        private (float upHeight, float downHeight)[] _itemHeightCache;

        /// <summary>
        /// 最大尺寸缓存
        /// </summary>
        private (float width, float height) _maxSizeCache;

        /// <summary>
        /// 是否正在自动滚动
        /// </summary>
        private bool _isAutoScrolling = false;

        /// <summary>
        /// 上次自动滚动位置
        /// </summary>
        private float _lastAutoScrollingPosition = 0f;

        /// <summary>
        /// 自动滚动目标位置
        /// </summary>
        private float _autoScrollingTargetPosition = 0f;

        /// <summary>
        /// 自动滚动速度
        /// </summary>
        private float _autoScrollingSpeed = 0f;

        /// <summary>
        /// 自动滚动完成回调
        /// </summary>
        private Action<bool> _onAutoScrollingCompleted = null;

        /// <summary>
        /// item位置偏移
        /// </summary>
        private Vector2 _itemPositionOffset;

        /// <summary>
        /// 是否强制居中
        /// </summary>
        private bool _forceCenter = false;

        public void Init(ScrollView scrollView, int startIndex, float viewportOffset, float itemOffset, bool forceCenter)
        {
            _isInited = false;
            _scrollView = scrollView;
            _itemDataRoot = _scrollView.RootTransform;
            _isAutoScrolling = false; // 中断任何可能的自动滚动任务
            _forceCenter = forceCenter;

            if (null == _scrollView
                || null == _scrollView.scrollRect
                || null == _scrollView.scrollRect.viewport)
            {
                return;
            }

            var viewportSize = _scrollView.scrollRect.viewport.rect.size;
            if (viewportSize.x == 0 || viewportSize.y == 0)
            {
                return;
            }

            startIndex = Data.ItemCount <= 0 ? 0 : startIndex % Data.ItemCount;
            // 给定的负数索引，需要修正为正数索引
            if (startIndex < 0)
            {
                startIndex = Data.ItemCount + startIndex;
            }

            // 索引修正到每行/列 第一个的下标
            startIndex = startIndex - startIndex % Data.ItemCountOneLine;

            // 获取content缓存
            _contentCache = scrollView.scrollRect.content;

            // 初始化宽度和高度缓存，计算最大高度和宽度
            _maxSizeCache = CalculateWidthHeight();

            // 初始化content的锚点和root的锚点
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Left2Right_Up2Down:
                    _contentCache.anchorMin = Vector2.up;
                    _contentCache.anchorMax = Vector2.up;
                    _contentCache.pivot = Vector2.up;
                    _itemDataRoot.anchorMin = Vector2.up;
                    _itemDataRoot.anchorMax = Vector2.up;
                    _itemDataRoot.pivot = Vector2.up;
                    break;
                case EScrollViewItemLayout.Down2Up_Left2Right:
                case EScrollViewItemLayout.Left2Right_Down2Up:
                    _contentCache.anchorMin = Vector2.zero;
                    _contentCache.anchorMax = Vector2.zero;
                    _contentCache.pivot = Vector2.zero;
                    _itemDataRoot.anchorMin = Vector2.zero;
                    _itemDataRoot.anchorMax = Vector2.zero;
                    _itemDataRoot.pivot = Vector2.zero;
                    break;
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    _contentCache.anchorMin = Vector2.one;
                    _contentCache.anchorMax = Vector2.one;
                    _contentCache.pivot = Vector2.one;
                    _itemDataRoot.anchorMin = Vector2.one;
                    _itemDataRoot.anchorMax = Vector2.one;
                    _itemDataRoot.pivot = Vector2.one;
                    break;
                case EScrollViewItemLayout.Down2Up_Right2Left:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    _contentCache.anchorMin = Vector2.right;
                    _contentCache.anchorMax = Vector2.right;
                    _contentCache.pivot = Vector2.right;
                    _itemDataRoot.anchorMin = Vector2.right;
                    _itemDataRoot.anchorMax = Vector2.right;
                    _itemDataRoot.pivot = Vector2.right;
                    break;
            }

            // content大小和内容区域保持一致
            ResetContentSize(viewportSize);
            // item根节点坐标
            _itemDataRoot.anchoredPosition = Vector2.zero;
            _itemPositionOffset = Vector2.zero;
            // 设置content位置
            if (_forceCenter)
            {
                var centerPosition = Vector2.zero;
                centerPosition.x = (Mathf.Max(viewportSize.x, _maxSizeCache.width) - viewportSize.x) / 2f;
                centerPosition.y = (Mathf.Max(viewportSize.y, _maxSizeCache.height) - viewportSize.y) / 2f;
                switch (Data.ItemLayout)
                {
                    case EScrollViewItemLayout.Left2Right_Up2Down:
                        _itemPositionOffset.x = Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = -Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        centerPosition.x = -centerPosition.x;
                        _visibleWindowPositionCache = centerPosition.y;
                        break;
                    case EScrollViewItemLayout.Right2Left_Up2Down:
                        _itemPositionOffset.x = -Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = -Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        _visibleWindowPositionCache = centerPosition.y;
                        break;
                    case EScrollViewItemLayout.Left2Right_Down2Up:
                        _itemPositionOffset.x = Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        centerPosition.x = -centerPosition.x;
                        _visibleWindowPositionCache = centerPosition.y;
                        centerPosition.y = -centerPosition.y;
                        break;
                    case EScrollViewItemLayout.Right2Left_Down2Up:
                        _itemPositionOffset.x = -Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        _visibleWindowPositionCache = centerPosition.y;
                        centerPosition.y = -centerPosition.y;
                        break;
                    case EScrollViewItemLayout.Up2Down_Left2Right:
                        _itemPositionOffset.x = Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = -Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        _visibleWindowPositionCache = centerPosition.x;
                        centerPosition.x = -centerPosition.x;
                        break;
                    case EScrollViewItemLayout.Up2Down_Right2Left:
                        _itemPositionOffset.x = -Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = -Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        _visibleWindowPositionCache = centerPosition.x;
                        break;
                    case EScrollViewItemLayout.Down2Up_Left2Right:
                        _itemPositionOffset.x = Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        centerPosition.y = -centerPosition.y;
                        _visibleWindowPositionCache = centerPosition.x;
                        centerPosition.x = -centerPosition.x;
                        break;
                    case EScrollViewItemLayout.Down2Up_Right2Left:
                        _itemPositionOffset.x = -Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        centerPosition.y = -centerPosition.y;
                        _visibleWindowPositionCache = centerPosition.x;
                        break;
                }
                _contentCache.anchoredPosition = centerPosition;
            }
            else
            {
                _visibleWindowPositionCache = CalculateItemJumpPosition(startIndex, viewportOffset, itemOffset);
                SetContentPosition(_visibleWindowPositionCache);
            }

            // 回收所有当前的item数据
            DespawnAllItem();

            // 初始化完毕
            _isInited = true;
        }

        /// <summary>
        /// 扩张（增加或减少数据数量，item位置和content位置不会变化）
        /// </summary>
        /// <param name="newItemCount"></param>
        public void Expand(int newItemCount)
        {
            _maxSizeCache = CalculateWidthHeight();
            var viewportSize = _scrollView.scrollRect.viewport.rect.size;
            ResetContentSize(viewportSize);
            
            _itemPositionOffset = Vector2.zero;
            // 设置content位置
            if (_forceCenter)
            {
                switch (Data.ItemLayout)
                {
                    case EScrollViewItemLayout.Left2Right_Down2Up:
                    case EScrollViewItemLayout.Down2Up_Left2Right:
                        _itemPositionOffset.x = Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        break;
                    case EScrollViewItemLayout.Right2Left_Down2Up:
                    case EScrollViewItemLayout.Down2Up_Right2Left:
                        _itemPositionOffset.x = -Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        break;
                    case EScrollViewItemLayout.Left2Right_Up2Down:
                    case EScrollViewItemLayout.Up2Down_Left2Right:
                        _itemPositionOffset.x = Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = -Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        break;
                    case EScrollViewItemLayout.Right2Left_Up2Down:
                    case EScrollViewItemLayout.Up2Down_Right2Left:
                        _itemPositionOffset.x = -Mathf.Max((viewportSize.x - _maxSizeCache.width) / 2f, 0f);
                        _itemPositionOffset.y = -Mathf.Max((viewportSize.y - _maxSizeCache.height) / 2f, 0f);
                        break;
                }
            }
        }

        /// <summary>
        /// 刷新单个item数据
        /// </summary>
        /// <param name="index">item索引</param>
        public void RefreshSingle(int index)
        {
            // 下标范围检查
            if (index < 0
                || index >= Data.ItemCount
                || _itemData.Count <= 0
                || _itemData.First.Value.DataIndex > index
                || _itemData.Last.Value.DataIndex < index)
            {
                return;
            }

            // 根据下标确定遍历方向
            var firstIndex = _itemData.First.Value.DataIndex;
            var lastIndex = _itemData.Last.Value.DataIndex;
            if (index - firstIndex > lastIndex - index)
            {
                // 从后往前遍历
                var item = _itemData.Last;
                while (null != item)
                {
                    if (item.Value.DataIndex == index)
                    {
                        item.Value.RefreshInstance();
                        return;
                    }

                    item = item.Previous;
                }
            }
            else
            {
                // 从前往后遍历
                var item = _itemData.First;
                while (null != item)
                {
                    if (item.Value.DataIndex == index)
                    {
                        item.Value.RefreshInstance();
                        return;
                    }

                    item = item.Next;
                }
            }
        }

        /// <summary>
        /// 刷新所有item数据
        /// </summary>
        public void RefreshAll()
        {
            var item = _itemData.First;
            while (null != item)
            {
                item.Value.RefreshInstance();
                item = item.Next;
            }
        }

        /// <summary>
        /// 跳转到指定索引位置
        /// </summary>
        /// <param name="index">item索引</param>
        /// <param name="viewportOffset">viewport偏移</param>
        /// <param name="itemOffset">item偏移</param>
        public void JumpToIndex(int index, float viewportOffset, float itemOffset)
        {
            // 计算跳转位置
            var position = CalculateItemJumpPosition(index, viewportOffset, itemOffset);
            // 直接设置content位置
            SetContentPosition(position);
        }

        /// <summary>
        /// 移动到指定索引位置
        /// </summary>
        /// <param name="index">item索引</param>
        /// <param name="viewportOffset">viewport偏移</param>
        /// <param name="itemOffset">item偏移</param>
        /// <param name="speed">移动速度</param>
        /// <param name="onCompleted">移动完成回调</param>
        public void MoveToIndexBySpeed(int index, float viewportOffset, float itemOffset, float speed,
            Action<bool> onCompleted)
        {
            // 计算跳转位置
            var position = CalculateItemJumpPosition(index, viewportOffset, itemOffset);
            
            _lastAutoScrollingPosition = GetVisibleWindowPosition();
            _autoScrollingTargetPosition = position;
            _autoScrollingSpeed = speed;
            _onAutoScrollingCompleted = onCompleted;
            // 设置自动滚动生效
            _isAutoScrolling = true;
        }
        
        /// <summary>
        /// 指定时间移动到指定索引位置
        /// </summary>
        /// <param name="index">item索引</param>
        /// <param name="viewportOffset">viewport偏移</param>
        /// <param name="itemOffset">item偏移</param>
        /// <param name="time">移动时间</param>
        /// <param name="onCompleted">移动完成回调</param>
        public void MoveToIndexByTime(int index, float viewportOffset, float itemOffset, float time, Action<bool> onCompleted)
        {
            // 时间小于等于0，直接走跳转
            if (time <= 0f)
            {
                JumpToIndex(index, viewportOffset, itemOffset);
                onCompleted?.Invoke(true);
                return;
            }
            
            // 计算跳转位置
            var position = CalculateItemJumpPosition(index, viewportOffset, itemOffset);
            
            _lastAutoScrollingPosition = GetVisibleWindowPosition();
            _autoScrollingTargetPosition = position;
            _autoScrollingSpeed = Mathf.Abs(position - _lastAutoScrollingPosition) / time;
            _onAutoScrollingCompleted = onCompleted;
            // 设置自动滚动生效
            _isAutoScrolling = true;
        }

        /// <summary>
        /// 计算高度和宽度缓存
        /// </summary>
        /// <returns>最大宽度和最大高度</returns>
        private (float maxWidth, float maxHeight) CalculateWidthHeight()
        {
            float maxWidth = 0f;
            float maxHeight = 0f;
            _itemYPositionCache = new float[Data.Row];
            _itemHeightCache = new (float upHeight, float downHeight)[Data.Row];
            for (int i = 0; i < Data.Row; i++)
            {
                var height = Data.GetItemHeight((i, Data.Row));
                switch (Data.ItemLayout)
                {
                    case EScrollViewItemLayout.Left2Right_Up2Down:
                    case EScrollViewItemLayout.Right2Left_Up2Down:
                    case EScrollViewItemLayout.Up2Down_Left2Right:
                    case EScrollViewItemLayout.Up2Down_Right2Left:
                        maxHeight += height.upHeight;
                        _itemYPositionCache[i] = maxHeight;
                        maxHeight += height.downHeight;
                        break;
                    case EScrollViewItemLayout.Left2Right_Down2Up:
                    case EScrollViewItemLayout.Right2Left_Down2Up:
                    case EScrollViewItemLayout.Down2Up_Left2Right:
                    case EScrollViewItemLayout.Down2Up_Right2Left:
                        maxHeight += height.downHeight;
                        _itemYPositionCache[i] = maxHeight;
                        maxHeight += height.upHeight;
                        break;
                }

                _itemHeightCache[i] = height;
            }

            _itemXPositionCache = new float[Data.Col];
            _itemWidthCache = new (float leftWidth, float rightWidth)[Data.Col];
            for (int i = 0; i < Data.Col; i++)
            {
                var width = Data.GetItemWidth((i, Data.Col));
                switch (Data.ItemLayout)
                {
                    case EScrollViewItemLayout.Left2Right_Up2Down:
                    case EScrollViewItemLayout.Left2Right_Down2Up:
                    case EScrollViewItemLayout.Up2Down_Left2Right:
                    case EScrollViewItemLayout.Down2Up_Left2Right:
                        maxWidth += width.leftWidth;
                        _itemXPositionCache[i] = maxWidth;
                        maxWidth += width.rightWidth;
                        break;
                    case EScrollViewItemLayout.Right2Left_Up2Down:
                    case EScrollViewItemLayout.Right2Left_Down2Up:
                    case EScrollViewItemLayout.Up2Down_Right2Left:
                    case EScrollViewItemLayout.Down2Up_Right2Left:
                        maxWidth += width.rightWidth;
                        _itemXPositionCache[i] = maxWidth;
                        maxWidth += width.leftWidth;
                        break;
                }

                _itemWidthCache[i] = width;
            }
            return (maxWidth, maxHeight);
        }

        /// <summary>
        /// 重置content大小
        /// </summary>
        private void ResetContentSize(Vector2 viewportSize)
        {
            _contentCache.sizeDelta =
                new Vector2(Mathf.Max(viewportSize.x, _maxSizeCache.width), Mathf.Max(viewportSize.y, _maxSizeCache.height));
        }

        /// <summary>
        /// 计算item跳转位置
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="viewportOffset">viewport偏移</param>
        /// <param name="itemOffset">item偏移</param>
        /// <returns></returns>
        private float CalculateItemJumpPosition(int index, float viewportOffset, float itemOffset)
        {
            if (Data.ItemCount <= 0) return 0f;
            
            float sizeMax = 0f;
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                    case EScrollViewItemLayout.Right2Left_Up2Down:
                    case EScrollViewItemLayout.Left2Right_Down2Up:
                    case EScrollViewItemLayout.Right2Left_Down2Up:
                    sizeMax = _maxSizeCache.height;
                    break;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                    case EScrollViewItemLayout.Down2Up_Left2Right:
                    case EScrollViewItemLayout.Down2Up_Right2Left:
                    case EScrollViewItemLayout.Up2Down_Right2Left:
                    sizeMax = _maxSizeCache.width;
                    break;
            }
            var position = 0f;
            var size = GetVisibleWindowSize();
            // 不足一屏的，不会产生跳转
            if (size >= sizeMax)
            {
                return position;
            }
            // 获取item位置
            position = GetVisibleItemPosition(index / Data.ItemCountOneLine);
            var heightOrWidth = GetVisibleItemLength(index / Data.ItemCountOneLine);
            position -= heightOrWidth.length1;
            position += itemOffset;
            position -= viewportOffset;
            // 判断越界
            if (position < 0f)
            {
                position = 0f;
            }
            else if (position > sizeMax - size)
            {
                position = sizeMax - size;
            }

            return position;
        }

        /// <summary>
        /// 获取可视窗口大小
        /// </summary>
        /// <returns></returns>
        private float GetVisibleWindowSize()
        {
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    return _scrollView.scrollRect.viewport.rect.size.y;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    return _scrollView.scrollRect.viewport.rect.size.x;
            }
            return 0f;
        }

        /// <summary>
        /// 获取可视窗口位置
        /// </summary>
        /// <returns></returns>
        private float GetVisibleWindowPosition()
        {
            var visibleWindow = _contentCache.anchoredPosition;
            // 当前滚动位置
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    return visibleWindow.y;
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    return -visibleWindow.y;
                case EScrollViewItemLayout.Down2Up_Right2Left:
                case EScrollViewItemLayout.Up2Down_Right2Left:
                    return visibleWindow.x;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                    return -visibleWindow.x;
            }
            return 0f;
        }

        /// <summary>
        /// 获取虚拟item长度
        /// </summary>
        /// <param name="rowOrColIndex"></param>
        /// <returns></returns>
        private (float length1, float length2) GetVisibleItemLength(int rowOrColIndex)
        {
            if (Data.ItemCount <= 0) return (0f, 0f);

            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    return _itemHeightCache[rowOrColIndex];
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    var height = _itemHeightCache[rowOrColIndex];
                    return (height.downHeight, height.upHeight);
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                    return _itemWidthCache[rowOrColIndex];
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    var width = _itemWidthCache[rowOrColIndex];
                    return (width.rightWidth, width.leftWidth);
            }
            return (0f, 0f);
        }

        /// <summary>
        /// 获取虚拟item位置
        /// </summary>
        /// <param name="rowOrColIndex"></param>
        /// <returns></returns>
        private float GetVisibleItemPosition(int rowOrColIndex)
        {
            if (Data.ItemCount <= 0) return 0f;

            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    return _itemYPositionCache[rowOrColIndex];
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    return _itemXPositionCache[rowOrColIndex];
            }
            return 0f;
        }

        /// <summary>
        /// 设置content位置
        /// </summary>
        /// <param name="position"></param>
        private void SetContentPosition(float position)
        {
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    _contentCache.anchoredPosition = new Vector2(0f, position);
                    break;
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    _contentCache.anchoredPosition = new Vector2(0f, -position);
                    break;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                    _contentCache.anchoredPosition = new Vector2(-position, 0f);
                    break;
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    _contentCache.anchoredPosition = new Vector2(position, 0f);
                    break;
            }
        }

        /// <summary>
        /// 刷新所有item实例
        /// <para> 异步执行，分帧实例化，通过版本号确定刷新是否继续有效 </para>
        /// </summary>
        private async UniTask RefreshAllItemInstance()
        {
            // 申请新的版本号
            var version = ++_version;

            if (_itemData.Count <= 0) return;

            // 更新分帧实例化数据缓存
            var curFrame = Time.frameCount;
            if (curFrame != _frameNum)
            {
                _frameNum = curFrame;
                _frameInstanceCounter = 0;
            }

            // 循环所有item
            foreach (var itemData in _itemData)
            {
                if (itemData.RefreshInstance())
                {
                    _frameInstanceCounter++;
                }

                if (_frameInstanceCounter < Data.FrameInstantiateCount) continue;

                _frameInstanceCounter = 0;
                // 延迟到下一帧
                await UniTask.DelayFrame(1);

                // 异步后需要检验版本号
                if (version != _version)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 分配item数据
        /// </summary>
        private ScrollViewItemData SpawnItem(int dataIndex)
        {
            if (_freeItemData.Count > 0)
            {
                var item = _freeItemData[^1];
                _freeItemData.RemoveAt(_freeItemData.Count - 1);
                item.DataIndex = dataIndex;
                SyncItemDataPosition(item);
                return item;
            }
            
            var newItem = new ScrollViewItemData(this, _itemDataRoot)
            {
                DataIndex = dataIndex
            };
            SyncItemDataPosition(newItem);
            return newItem;
        }

        /// <summary>
        /// 释放第一个item数据
        /// </summary>
        private void DespawnFirstItem()
        {
            if (_itemData.Count <= 0) return;
            var item = _itemData.First;
            _freeItemData.Add(item.Value);
            _itemData.RemoveFirst();
        }

        private void DespawnLastItem()
        {
            if (_itemData.Count <= 0) return;
            var item = _itemData.Last;
            _freeItemData.Add(item.Value);
            _itemData.RemoveLast();
        }

        /// <summary>
        /// 释放所有item数据
        /// </summary>
        public void DespawnAllItem()
        {
            foreach (var item in _itemData)
            {
                item.DataIndex = -1;
                item.DespawnInstance();
                _freeItemData.Add(item);
            }

            _itemData.Clear();
        }

        /// <summary>
        /// 当发生异常时调用
        /// </summary>
        /// <param name="ex"></param>
        private void OnException(Exception ex)
        {
            Debug.LogError(ex);
        }

        /// <summary>
        /// 同步单个item数据的位置
        /// </summary>
        /// <param name="itemData"></param>
        private void SyncItemDataPosition(ScrollViewItemData itemData)
        {
            var dataIndex = itemData.DataIndex;
            var rowCol = Vector2Int.zero;
            var position = Vector2.zero;
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                    rowCol.x = dataIndex / Data.ItemCountOneLine;
                    rowCol.y = dataIndex % Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y];
                    position.y = _itemYPositionCache[rowCol.x % Data.Row];
                    position.y += (rowCol.x / Data.Row) * _maxSizeCache.height;
                    position.y = -position.y;
                    break;
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    rowCol.x = dataIndex / Data.ItemCountOneLine;
                    rowCol.y = dataIndex % Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y];
                    position.y = _itemYPositionCache[rowCol.x % Data.Row];
                    position.y += (rowCol.x / Data.Row) * _maxSizeCache.height;
                    position.y = -position.y;
                    position.x = -position.x;
                    break;
                case EScrollViewItemLayout.Left2Right_Down2Up:
                    rowCol.x = dataIndex / Data.ItemCountOneLine;
                    rowCol.y = dataIndex % Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y];
                    position.y = _itemYPositionCache[rowCol.x % Data.Row];
                    position.y += (rowCol.x / Data.Row) * _maxSizeCache.height;
                    break;
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    rowCol.x = dataIndex / Data.ItemCountOneLine;
                    rowCol.y = dataIndex % Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y];
                    position.y = _itemYPositionCache[rowCol.x % Data.Row];
                    position.y += (rowCol.x / Data.Row) * _maxSizeCache.height;
                    position.x = -position.x;
                    break;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                    rowCol.x = dataIndex % Data.ItemCountOneLine;
                    rowCol.y = dataIndex / Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y % Data.Col];
                    position.y = _itemYPositionCache[rowCol.x];
                    position.x += (rowCol.x / Data.Col) * _maxSizeCache.width;
                    position.y = -position.y;
                    break;
                case EScrollViewItemLayout.Down2Up_Left2Right:
                    rowCol.x = dataIndex % Data.ItemCountOneLine;
                    rowCol.y = dataIndex / Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y % Data.Col];
                    position.y = _itemYPositionCache[rowCol.x];
                    position.x += (rowCol.x / Data.Col) * _maxSizeCache.width;
                    break;
                case EScrollViewItemLayout.Up2Down_Right2Left:
                    rowCol.x = dataIndex % Data.ItemCountOneLine;
                    rowCol.y = dataIndex / Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y % Data.Col];
                    position.y = _itemYPositionCache[rowCol.x];
                    position.x += (rowCol.x / Data.Col) * _maxSizeCache.width;
                    position.x = -position.x;
                    position.y = -position.y;
                    break;
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    rowCol.x = dataIndex % Data.ItemCountOneLine;
                    rowCol.y = dataIndex / Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y % Data.Col];
                    position.y = _itemYPositionCache[rowCol.x];
                    position.x += (rowCol.x / Data.Col) * _maxSizeCache.width;
                    position.x = -position.x;
                    break;
            }

            position += _itemPositionOffset;
            itemData.ItemPosition = position;
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Tick()
        {
            if (!_isInited) return;
            
            // 检查数量为0时，不刷新
            if (Data.ItemCount <= 0) return;
            
            // 查看当前缓存窗口位置和实际位置的偏移
            var lastVisibleWindowPosition = _visibleWindowPositionCache;
            var currentVisibleWindowPosition = GetVisibleWindowPosition();

            // 回调调用
            bool isInvokeAutoScrollingCallback = false;
            bool isAutoScrollingCompleted = false;

            // 自动滚动处理
            if (_isAutoScrolling)
            {
                // 直接停止自动滚动的情况
                if (_autoScrollingSpeed <= 0f // 滚动速度小于等于0
                    || Mathf.Approximately(_lastAutoScrollingPosition, _autoScrollingTargetPosition)) // 当前滚动位置和目标位置相同
                {
                    _isAutoScrolling = false;
                    isInvokeAutoScrollingCallback = true;
                    isAutoScrollingCompleted = true;
                }
                else
                {
                    if (Mathf.Approximately(currentVisibleWindowPosition, _lastAutoScrollingPosition))
                    {
                        // 计算下一个自动滚动的位置
                        var time = Time.deltaTime;
                        var distance = _autoScrollingSpeed * time;
                        // 滚动方向
                        if (_autoScrollingTargetPosition > _lastAutoScrollingPosition)
                        {
                            _lastAutoScrollingPosition += distance;
                            if (_lastAutoScrollingPosition > _autoScrollingTargetPosition)
                            {
                                _lastAutoScrollingPosition = _autoScrollingTargetPosition;
                                _isAutoScrolling = false;
                                isInvokeAutoScrollingCallback = true;
                                isAutoScrollingCompleted = true;
                            }
                        }
                        else
                        {
                            _lastAutoScrollingPosition -= distance;
                            if (_lastAutoScrollingPosition < _autoScrollingTargetPosition)
                            {
                                _lastAutoScrollingPosition = _autoScrollingTargetPosition;
                                _isAutoScrolling = false;
                                isInvokeAutoScrollingCallback = true;
                                isAutoScrollingCompleted = true;
                            }
                        }

                        // 更新当前content位置
                        SetContentPosition(_lastAutoScrollingPosition);
                        _visibleWindowPositionCache = _lastAutoScrollingPosition;
                        currentVisibleWindowPosition = _lastAutoScrollingPosition;
                    }
                    // 记录滚动位置和当前位置不同，则列表被拖动或以其他方式移动，直接结束滚动
                    else
                    {
                        _isAutoScrolling = false;
                        isInvokeAutoScrollingCallback = true;
                        isAutoScrollingCompleted = false;
                    }
                }
            }

            // 偏移计算
            float offset = currentVisibleWindowPosition - lastVisibleWindowPosition;
            
            bool changed = false;
            bool empty = false;
            // 如果item数据为空，则填入第一个item元素
            if (_itemData.Count <= 0)
            {
                empty = true;
                // 找到第一个item的下标
                var lineCount = Mathf.CeilToInt(Data.ItemCount * 1.0f / Data.ItemCountOneLine);
                for (int i = 0; i < lineCount; i++)
                {
                    var itemPosition = GetVisibleItemPosition(i);
                    var visibleLength = GetVisibleItemLength(i);
                    if (currentVisibleWindowPosition >= itemPosition - visibleLength.length1
                        && currentVisibleWindowPosition <= itemPosition + visibleLength.length2)
                    {
                        // 填入第一个item
                        var item = SpawnItem(i * Data.ItemCountOneLine);
                        _itemData.AddFirst(item);
                        changed = true;
                        break;
                    }
                }
            }

            // 没有偏移量，不更新
            if (!Mathf.Approximately(offset, 0f) || empty)
            {
                // 缓存第一个和最后一个的下标和位置，然后更新到新的下标和位置，最后再比较确定哪些需要回收，哪些需要新增
                var cachedStartIndex = _itemData.First.Value.DataIndex;
                var cachedEndIndex = _itemData.Last.Value.DataIndex;
                // 计算后最终得到的心得开始和结束下标
                var startIndex = cachedStartIndex - cachedStartIndex % Data.ItemCountOneLine;
                var endIndex = cachedEndIndex - cachedEndIndex % Data.ItemCountOneLine;
                float windowLimitMin = currentVisibleWindowPosition;
                float windowLimitMax = currentVisibleWindowPosition + GetVisibleWindowSize();
                // 计算开始坐标
                var itemPosition = GetVisibleItemPosition(startIndex / Data.ItemCountOneLine);
                var virtualLength = GetVisibleItemLength(startIndex / Data.ItemCountOneLine);
                var itemMax = itemPosition + virtualLength.length2;
                var itemMin = itemPosition - virtualLength.length1;
                // 处在可视范围下方，向上移动
                if (itemMax < windowLimitMin)
                {
                    while (itemMax < windowLimitMin)
                    {
                        startIndex += Data.ItemCountOneLine;
                        if (startIndex >= Data.ItemCount)
                        {
                            startIndex = Data.ItemCount - 1;
                            break;
                        }

                        virtualLength = GetVisibleItemLength(startIndex / Data.ItemCountOneLine);
                        itemMax += virtualLength.length1;
                        itemMax += virtualLength.length2;
                    }
                }
                // 处在可视范围上方，向下移动
                else if (itemMin >= windowLimitMin)
                {
                    while (itemMin >= windowLimitMin)
                    {
                        startIndex -= Data.ItemCountOneLine;
                        if (startIndex < 0)
                        {
                            startIndex = 0;
                            break;
                        }

                        virtualLength = GetVisibleItemLength(startIndex / Data.ItemCountOneLine);
                        itemMin -= virtualLength.length1;
                        itemMin -= virtualLength.length2;
                    }
                }

                // 计算结束坐标
                itemPosition = GetVisibleItemPosition(endIndex / Data.ItemCountOneLine);
                virtualLength = GetVisibleItemLength(endIndex / Data.ItemCountOneLine);
                itemMax = itemPosition + virtualLength.length2;
                itemMin = itemPosition - virtualLength.length1;
                // 处在可视范围下方，向上移动
                if (itemMax <= windowLimitMax)
                {
                    while (itemMax <= windowLimitMax)
                    {
                        endIndex += Data.ItemCountOneLine;
                        if (endIndex >= Data.ItemCount)
                        {
                            endIndex = Data.ItemCount - 1;
                            break;
                        }

                        virtualLength = GetVisibleItemLength(endIndex / Data.ItemCountOneLine);
                        itemMax += virtualLength.length1;
                        itemMax += virtualLength.length2;
                    }
                }
                // 处在可视范围上方，向下移动
                else if (itemMin > windowLimitMax)
                {
                    while (itemMin > windowLimitMax)
                    {
                        endIndex -= Data.ItemCountOneLine;
                        if (endIndex < 0)
                        {
                            endIndex = 0;
                            break;
                        }

                        virtualLength = GetVisibleItemLength(endIndex / Data.ItemCountOneLine);
                        itemMin -= virtualLength.length1;
                        itemMin -= virtualLength.length2;
                    }
                }

                // 结束坐标修正到该行/列最后一个
                endIndex = Mathf.Min(endIndex + Data.ItemCountOneLine - 1, Data.ItemCount - 1);

                // 比较开始下标和结束下标，先走回收，再走新增，否则缓存池中数量会一直增加
                while (startIndex > cachedStartIndex)
                {
                    cachedStartIndex++;
                    DespawnFirstItem();
                    changed = true;
                }

                while (endIndex < cachedEndIndex)
                {
                    cachedEndIndex--;
                    DespawnLastItem();
                    changed = true;
                }

                while (startIndex < cachedStartIndex)
                {
                    cachedStartIndex--;
                    if (cachedStartIndex > endIndex)
                    {
                        continue;
                    }

                    var item = SpawnItem(cachedStartIndex);
                    _itemData.AddFirst(item);
                    changed = true;
                }

                while (endIndex > cachedEndIndex)
                {
                    cachedEndIndex++;
                    if (cachedEndIndex < startIndex)
                    {
                        continue;
                    }

                    var item = SpawnItem(cachedEndIndex);
                    _itemData.AddLast(item);
                    changed = true;
                }
            }

            // 发生变化，做一次刷新
            if (changed)
            {
                RefreshAllItemInstance().Forget(OnException);
            }
            
            // 自动滚动回调调用
            if (isInvokeAutoScrollingCallback)
            {
                _onAutoScrollingCompleted?.Invoke(isAutoScrollingCompleted);
                _onAutoScrollingCompleted = null;
            }
        }
    }
}