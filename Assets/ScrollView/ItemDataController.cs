using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AsyncScrollView
{
    /// <summary>
    /// 数据管理器基类，负责管理滑动列表下的元素数据的新建、刷新、销毁等逻辑
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
        /// 可视窗口
        /// </summary>
        private Rect _visibleWindow;

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

        public void Init(ScrollView scrollView, int startIndex)
        {
            _isInited = false;
            _scrollView = scrollView;
            _itemDataRoot = _scrollView.RootTransform;

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

            startIndex = startIndex % Data.ItemCount;
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

            _maxSizeCache = (maxWidth, maxHeight);

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
            _contentCache.sizeDelta = new Vector2(maxWidth, maxHeight);
            // item根节点坐标
            _itemDataRoot.anchoredPosition = Vector2.zero;
            // 计算起点位置
            var startPosition = Vector2.zero;
            int startRowOrCol = startIndex / Data.ItemCountOneLine; // 起始行或列
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    // 可视范围不足一屏
                    if (viewportSize.y >= maxHeight)
                    {
                        startIndex = 0;
                    }
                    else
                    {
                        startPosition.y = _itemYPositionCache[startRowOrCol] -
                                          _itemHeightCache[startRowOrCol].upHeight;
                        // 可视范围超框
                        if (startPosition.y + viewportSize.y > maxHeight)
                        {
                            // 修正起点下标和坐标
                            var currentY = startPosition.y;
                            var limitY = maxHeight - viewportSize.y;
                            // 起始下标每次都向下一行，直到当前下标退到限定位置以下
                            while (currentY > limitY && startRowOrCol > 0)
                            {
                                startRowOrCol--;
                                var heightCache = _itemHeightCache[startRowOrCol];
                                currentY -= heightCache.upHeight;
                                currentY -= heightCache.downHeight;
                                startIndex -= Data.ItemCountOneLine;
                            }

                            startPosition.y = currentY;
                        }
                    }

                    break;
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    // 可视范围不足一屏
                    if (viewportSize.y >= maxHeight)
                    {
                        startIndex = 0;
                    }
                    else
                    {
                        startPosition.y = _itemYPositionCache[startRowOrCol] -
                                          _itemHeightCache[startRowOrCol].downHeight;
                        // 可视范围超框
                        if (startPosition.y + viewportSize.y > maxHeight)
                        {
                            // 修正起点下标和坐标
                            var currentY = startPosition.y;
                            var limitY = maxHeight - viewportSize.y;
                            // 起始下标每次都向下一行，直到当前下标退到限定位置以下
                            while (currentY > limitY && startRowOrCol > 0)
                            {
                                startRowOrCol--;
                                var heightCache = _itemHeightCache[startRowOrCol];
                                currentY -= heightCache.upHeight;
                                currentY -= heightCache.downHeight;
                                startIndex -= Data.ItemCountOneLine;
                            }

                            startPosition.y = currentY;
                        }
                    }

                    break;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                    // 可视范围不足一屏
                    if (viewportSize.x >= maxWidth)
                    {
                        startIndex = 0;
                    }
                    else
                    {
                        startPosition.x = _itemXPositionCache[startRowOrCol] -
                                          _itemWidthCache[startRowOrCol].leftWidth;
                        // 可视范围超框
                        if (startPosition.x + viewportSize.x > maxWidth)
                        {
                            // 修正起点下标和坐标
                            var currentX = startPosition.x;
                            var limitX = maxWidth - viewportSize.x;
                            // 起始下标每次都向下一行，直到当前下标退到限定位置以下
                            while (currentX > limitX && startRowOrCol > 0)
                            {
                                startRowOrCol--;
                                var widthCache = _itemWidthCache[startRowOrCol];
                                currentX -= widthCache.leftWidth;
                                currentX -= widthCache.rightWidth;
                                startIndex -= Data.ItemCountOneLine;
                            }

                            startPosition.x = currentX;
                        }
                    }

                    break;
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    // 可视范围不足一屏
                    if (viewportSize.x >= maxWidth)
                    {
                        startIndex = 0;
                    }
                    else
                    {
                        startPosition.x = _itemXPositionCache[startRowOrCol] -
                                          _itemWidthCache[startRowOrCol].rightWidth;
                        // 可视范围超框
                        if (startPosition.x + viewportSize.x > maxWidth)
                        {
                            // 修正起点下标和坐标
                            var currentX = startPosition.x;
                            var limitX = maxWidth - viewportSize.x;
                            // 起始下标每次都向下一行，直到当前下标退到限定位置以下
                            while (currentX > limitX && startRowOrCol > 0)
                            {
                                startRowOrCol--;
                                var widthCache = _itemWidthCache[startRowOrCol];
                                currentX -= widthCache.leftWidth;
                                currentX -= widthCache.rightWidth;
                                startIndex -= Data.ItemCountOneLine;
                            }

                            startPosition.x = currentX;
                        }
                    }

                    break;
            }

            // 初始化可视窗口位置
            _visibleWindow = new Rect(startPosition, viewportSize);
            // content同步可视窗口位置
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    _contentCache.anchoredPosition = new Vector2(0f, startPosition.y);
                    break;
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    _contentCache.anchoredPosition = new Vector2(0f, -startPosition.y);
                    break;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                    _contentCache.anchoredPosition = new Vector2(-startPosition.x, 0f);
                    break;
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    _contentCache.anchoredPosition = new Vector2(startPosition.x, 0f);
                    break;
            }

            // 填充item数据
            DespawnAllItem();
            var currentPosition = _visibleWindow.position;
            var limitPosition = _visibleWindow.position + _visibleWindow.size;
            var index = startIndex;
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    while (currentPosition.y < limitPosition.y && index < Data.ItemCount)
                    {
                        // 初始化一行item数据
                        for (var i = 0; i < Data.ItemCountOneLine; i++)
                        {
                            if (index >= Data.ItemCount)
                            {
                                break;
                            }

                            var itemData = SpawnItem(index);
                            _itemData.AddLast(itemData);
                            index++;
                        }

                        var heightCache = _itemHeightCache[index / Data.ItemCountOneLine];
                        currentPosition.y += heightCache.upHeight;
                        currentPosition.y += heightCache.downHeight;
                    }

                    break;
                case EScrollViewItemLayout.Down2Up_Left2Right:
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                case EScrollViewItemLayout.Up2Down_Right2Left:
                    while (currentPosition.x < limitPosition.x && index < Data.ItemCount)
                    {
                        // 初始化一行item数据
                        for (var i = 0; i < Data.ItemCountOneLine; i++)
                        {
                            if (index >= Data.ItemCount)
                            {
                                break;
                            }

                            var itemData = SpawnItem(index);
                            _itemData.AddLast(itemData);
                            index++;
                        }

                        var widthCache = _itemWidthCache[index / Data.ItemCountOneLine];
                        currentPosition.x += widthCache.leftWidth;
                        currentPosition.x += widthCache.rightWidth;
                    }

                    break;
            }

            // 初始化完毕
            _isInited = true;

            // 刷新所有item数据(异步执行）
            RefreshAllItemInstance().Forget(OnException);
        }

        public void RefreshSingle(int itemIndex)
        {
            throw new System.NotImplementedException();
        }

        public void RefreshAll()
        {
            throw new System.NotImplementedException();
        }

        public void JumpToIndex()
        {
            throw new System.NotImplementedException();
        }

        public void MoveDistance()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 刷新所有item实例
        /// <para> 异步执行，分帧实例化，通过版本号确定刷新是否继续有效 </para>
        /// </summary>
        private async UniTask RefreshAllItemInstance()
        {
            // 申请新的版本号
            var version = ++_version;

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
                item.ItemRoot.gameObject.name = $"Item_{dataIndex}";
                item.DataIndex = dataIndex;
                SyncItemDataPosition(item);
                return item;
            }

            var gameObject = new GameObject("free");
            gameObject.transform.SetParent(_itemDataRoot);
            var rectTrans = gameObject.GetComponent<RectTransform>();
            if (null == rectTrans)
            {
                rectTrans = gameObject.AddComponent<RectTransform>();
            }

            rectTrans.sizeDelta = Vector2.zero;
            var newItem = new ScrollViewItemData(this, rectTrans);
            newItem.ItemRoot.gameObject.name = $"Item_{dataIndex}";
            newItem.DataIndex = dataIndex;
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
            item.Value.ItemRoot.name = "free";
            _itemData.RemoveFirst();
        }

        private void DespawnLastItem()
        {
            if (_itemData.Count <= 0) return;
            var item = _itemData.Last;
            _freeItemData.Add(item.Value);
            item.Value.ItemRoot.name = "free";
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
                item.ItemRoot.name = "free";
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
                    itemData.ItemRoot.anchoredPosition = position;
                    break;
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    rowCol.x = dataIndex / Data.ItemCountOneLine;
                    rowCol.y = dataIndex % Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y];
                    position.y = _itemYPositionCache[rowCol.x % Data.Row];
                    position.y += (rowCol.x / Data.Row) * _maxSizeCache.height;
                    position.y = -position.y;
                    position.x = -position.x;
                    itemData.ItemRoot.anchoredPosition = position;
                    break;
                case EScrollViewItemLayout.Left2Right_Down2Up:
                    rowCol.x = dataIndex / Data.ItemCountOneLine;
                    rowCol.y = dataIndex % Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y];
                    position.y = _itemYPositionCache[rowCol.x % Data.Row];
                    position.y += (rowCol.x / Data.Row) * _maxSizeCache.height;
                    itemData.ItemRoot.anchoredPosition = position;
                    break;
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    rowCol.x = dataIndex / Data.ItemCountOneLine;
                    rowCol.y = dataIndex % Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y];
                    position.y = _itemYPositionCache[rowCol.x % Data.Row];
                    position.y += (rowCol.x / Data.Row) * _maxSizeCache.height;
                    position.x = -position.x;
                    itemData.ItemRoot.anchoredPosition = position;
                    break;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                    rowCol.x = dataIndex % Data.ItemCountOneLine;
                    rowCol.y = dataIndex / Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y % Data.Col];
                    position.y = _itemYPositionCache[rowCol.x];
                    position.x += (rowCol.x / Data.Col) * _maxSizeCache.width;
                    position.y = -position.y;
                    itemData.ItemRoot.anchoredPosition = position;
                    break;
                case EScrollViewItemLayout.Down2Up_Left2Right:
                    rowCol.x = dataIndex % Data.ItemCountOneLine;
                    rowCol.y = dataIndex / Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y % Data.Col];
                    position.y = _itemYPositionCache[rowCol.x];
                    position.x += (rowCol.x / Data.Col) * _maxSizeCache.width;
                    itemData.ItemRoot.anchoredPosition = position;
                    break;
                case EScrollViewItemLayout.Up2Down_Right2Left:
                    rowCol.x = dataIndex % Data.ItemCountOneLine;
                    rowCol.y = dataIndex / Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y % Data.Col];
                    position.y = _itemYPositionCache[rowCol.x];
                    position.x += (rowCol.x / Data.Col) * _maxSizeCache.width;
                    position.x = -position.x;
                    position.y = -position.y;
                    itemData.ItemRoot.anchoredPosition = position;
                    break;
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    rowCol.x = dataIndex % Data.ItemCountOneLine;
                    rowCol.y = dataIndex / Data.ItemCountOneLine;
                    position.x = _itemXPositionCache[rowCol.y % Data.Col];
                    position.y = _itemYPositionCache[rowCol.x];
                    position.x += (rowCol.x / Data.Col) * _maxSizeCache.width;
                    position.x = -position.x;
                    itemData.ItemRoot.anchoredPosition = position;
                    break;
            }
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Tick()
        {
            if (!_isInited) return;

            // 查看当前缓存窗口位置和实际位置的偏移
            var currentPosition = _contentCache.anchoredPosition;
            var windowPosition = _visibleWindow.position;
            float offset = 0f;
            switch (Data.ItemLayout)
            {
                case EScrollViewItemLayout.Left2Right_Up2Down:
                case EScrollViewItemLayout.Right2Left_Up2Down:
                    offset = Mathf.Abs(currentPosition.y - windowPosition.y);
                    break;
                case EScrollViewItemLayout.Left2Right_Down2Up:
                case EScrollViewItemLayout.Right2Left_Down2Up:
                    currentPosition.y = -currentPosition.y;
                    offset = Mathf.Abs(currentPosition.y - windowPosition.y);
                    break;
                case EScrollViewItemLayout.Up2Down_Left2Right:
                case EScrollViewItemLayout.Down2Up_Left2Right:
                    currentPosition.x = -currentPosition.x;
                    offset = Mathf.Abs(currentPosition.x - windowPosition.x);
                    break;
                case EScrollViewItemLayout.Up2Down_Right2Left:
                case EScrollViewItemLayout.Down2Up_Right2Left:
                    offset = Mathf.Abs(currentPosition.x - windowPosition.x);
                    break;
            }
            // 偏移量大于等于1，则更新
            if (offset >= 1f)
            {
                // 更新窗口位置
                _visibleWindow.position = currentPosition;

                // 缓存第一个和最后一个的下标和位置，然后更新到新的下标和位置，最后再比较确定哪些需要回收，哪些需要新增
                var cachedStartIndex = _itemData.First.Value.DataIndex;
                var cachedEndIndex = _itemData.Last.Value.DataIndex;
                var startIndex = cachedStartIndex;
                var endIndex = cachedEndIndex;
                var startPosition = _itemData.First.Value.ItemRoot.anchoredPosition;
                var endPosition = _itemData.Last.Value.ItemRoot.anchoredPosition;
                startIndex = startIndex - startIndex % Data.ItemCountOneLine;
                endIndex = endIndex - endIndex % Data.ItemCountOneLine;
                float limitMin, limitMax, position;
                switch (Data.ItemLayout)
                {
                    case EScrollViewItemLayout.Left2Right_Down2Up:
                    case EScrollViewItemLayout.Right2Left_Down2Up:
                        // 计算起始坐标
                        position = Mathf.Abs(startPosition.y);
                        limitMax = position + _itemHeightCache[startIndex / Data.ItemCountOneLine].upHeight;
                        limitMin = position - _itemHeightCache[startIndex / Data.ItemCountOneLine].downHeight;
                        // 处在可视范围下方，向上移动
                        if (limitMax < _visibleWindow.yMin)
                        {
                            while (limitMax < _visibleWindow.yMin)
                            {
                                startIndex += Data.ItemCountOneLine;
                                if (startIndex >= Data.ItemCount)
                                {
                                    startIndex = Data.ItemCount - 1;
                                    break;
                                }

                                var height = _itemHeightCache[startIndex / Data.ItemCountOneLine];
                                limitMax += height.upHeight;
                                limitMax += height.downHeight;
                            }
                        }
                        // 处在可视范围上方，向下移动
                        else if (limitMin > _visibleWindow.yMin)
                        {
                            while (limitMin > _visibleWindow.yMin)
                            {
                                startIndex -= Data.ItemCountOneLine;
                                if (startIndex < 0)
                                {
                                    startIndex = 0;
                                    break;
                                }

                                var height = _itemHeightCache[startIndex / Data.ItemCountOneLine];
                                limitMin -= height.upHeight;
                                limitMin -= height.downHeight;
                            }
                        }

                        // 计算结束坐标
                        position = Mathf.Abs(endPosition.y);
                        limitMax = position + _itemHeightCache[endIndex / Data.ItemCountOneLine].upHeight;
                        limitMin = position - _itemHeightCache[endIndex / Data.ItemCountOneLine].downHeight;
                        // 处在可视范围下方，向上移动
                        if (limitMax < _visibleWindow.yMax)
                        {
                            while (limitMax < _visibleWindow.yMax)
                            {
                                endIndex += Data.ItemCountOneLine;
                                if (endIndex >= Data.ItemCount)
                                {
                                    endIndex = Data.ItemCount - 1;
                                    break;
                                }

                                var height = _itemHeightCache[endIndex / Data.ItemCountOneLine];
                                limitMax += height.upHeight;
                                limitMax += height.downHeight;
                            }
                        }
                        // 处在可视范围上方，向下移动
                        else if (limitMin > _visibleWindow.yMax)
                        {
                            while (limitMin > _visibleWindow.yMax)
                            {
                                endIndex -= Data.ItemCountOneLine;
                                if (endIndex < 0)
                                {
                                    endIndex = 0;
                                    break;
                                }

                                var height = _itemHeightCache[endIndex / Data.ItemCountOneLine];
                                limitMin -= height.upHeight;
                                limitMin -= height.downHeight;
                            }
                        }

                        // 结束坐标修正到该行/列最后一个
                        endIndex = Mathf.Min(endIndex + Data.ItemCountOneLine - 1, Data.ItemCount - 1);
                        break;
                    case EScrollViewItemLayout.Left2Right_Up2Down:
                    case EScrollViewItemLayout.Right2Left_Up2Down:
                        // 计算起始坐标
                        position = Mathf.Abs(startPosition.y);
                        limitMax = position + _itemHeightCache[startIndex / Data.ItemCountOneLine].downHeight;
                        limitMin = position - _itemHeightCache[startIndex / Data.ItemCountOneLine].upHeight;
                        // 处在可视范围下方，向上移动
                        if (limitMax < _visibleWindow.yMin)
                        {
                            while (limitMax < _visibleWindow.yMin)
                            {
                                startIndex += Data.ItemCountOneLine;
                                if (startIndex >= Data.ItemCount)
                                {
                                    startIndex = Data.ItemCount - 1;
                                    break;
                                }

                                var height = _itemHeightCache[startIndex / Data.ItemCountOneLine];
                                limitMax += height.downHeight;
                                limitMax += height.upHeight;
                            }
                        }
                        // 处在可视范围上方，向下移动
                        else if (limitMin > _visibleWindow.yMin)
                        {
                            while (limitMin > _visibleWindow.yMin)
                            {
                                startIndex -= Data.ItemCountOneLine;
                                if (startIndex < 0)
                                {
                                    startIndex = 0;
                                    break;
                                }

                                var height = _itemHeightCache[startIndex / Data.ItemCountOneLine];
                                limitMin -= height.downHeight;
                                limitMin -= height.upHeight;
                            }
                        }

                        // 计算结束坐标
                        position = Mathf.Abs(endPosition.y);
                        limitMax = position + _itemHeightCache[endIndex / Data.ItemCountOneLine].downHeight;
                        limitMin = position - _itemHeightCache[endIndex / Data.ItemCountOneLine].upHeight;
                        // 处在可视范围下方，向上移动
                        if (limitMax < _visibleWindow.yMax)
                        {
                            while (limitMax < _visibleWindow.yMax)
                            {
                                endIndex += Data.ItemCountOneLine;
                                if (endIndex >= Data.ItemCount)
                                {
                                    endIndex = Data.ItemCount - 1;
                                    break;
                                }

                                var height = _itemHeightCache[endIndex / Data.ItemCountOneLine];
                                limitMax += height.downHeight;
                                limitMax += height.upHeight;
                            }
                        }
                        // 处在可视范围上方，向下移动
                        else if (limitMin > _visibleWindow.yMax)
                        {
                            while (limitMin > _visibleWindow.yMax)
                            {
                                endIndex -= Data.ItemCountOneLine;
                                if (endIndex < 0)
                                {
                                    endIndex = 0;
                                    break;
                                }

                                var height = _itemHeightCache[endIndex / Data.ItemCountOneLine];
                                limitMin -= height.downHeight;
                                limitMin -= height.upHeight;
                            }
                        }

                        // 结束坐标修正到该行/列最后一个
                        endIndex = Mathf.Min(endIndex + Data.ItemCountOneLine - 1, Data.ItemCount - 1);
                        break;
                    case EScrollViewItemLayout.Up2Down_Left2Right:
                    case EScrollViewItemLayout.Down2Up_Left2Right:
                        // 计算起始坐标
                        position = Mathf.Abs(startPosition.x);
                        limitMax = position + _itemWidthCache[startIndex / Data.ItemCountOneLine].rightWidth;
                        limitMin = position - _itemWidthCache[startIndex / Data.ItemCountOneLine].leftWidth;
                        // 处在可视范围左侧，向右移动
                        if (limitMax < _visibleWindow.xMin)
                        {
                            while (limitMax < _visibleWindow.xMin)
                            {
                                startIndex += Data.ItemCountOneLine;
                                if (startIndex >= Data.ItemCount)
                                {
                                    startIndex = Data.ItemCount - 1;
                                    break;
                                }

                                var width = _itemWidthCache[startIndex / Data.ItemCountOneLine];
                                limitMax += width.rightWidth;
                                limitMax += width.leftWidth;
                            }
                        }
                        // 处在可视范围右侧，向左移动
                        else if (limitMin > _visibleWindow.xMin)
                        {
                            while (limitMin > _visibleWindow.xMin)
                            {
                                startIndex -= Data.ItemCountOneLine;
                                if (startIndex < 0)
                                {
                                    startIndex = 0;
                                    break;
                                }

                                var width = _itemWidthCache[startIndex / Data.ItemCountOneLine];
                                limitMin -= width.rightWidth;
                                limitMin -= width.leftWidth;
                            }
                        }

                        // 计算结束坐标
                        position = Mathf.Abs(endPosition.x);
                        limitMax = position + _itemWidthCache[endIndex / Data.ItemCountOneLine].rightWidth;
                        limitMin = position - _itemWidthCache[endIndex / Data.ItemCountOneLine].leftWidth;
                        // 处在可视范围左侧，向右移动
                        if (limitMax < _visibleWindow.xMax)
                        {
                            while (limitMax < _visibleWindow.xMax)
                            {
                                endIndex += Data.ItemCountOneLine;
                                if (endIndex >= Data.ItemCount)
                                {
                                    endIndex = Data.ItemCount - 1;
                                    break;
                                }

                                var width = _itemWidthCache[endIndex / Data.ItemCountOneLine];
                                limitMax += width.rightWidth;
                                limitMax += width.leftWidth;
                            }
                        }
                        // 处在可视范围右侧，向左移动
                        else if (limitMin > _visibleWindow.xMax)
                        {
                            while (limitMin > _visibleWindow.xMax)
                            {
                                endIndex -= Data.ItemCountOneLine;
                                if (endIndex < 0)
                                {
                                    endIndex = 0;
                                    break;
                                }

                                var width = _itemWidthCache[endIndex / Data.ItemCountOneLine];
                                limitMin -= width.rightWidth;
                                limitMin -= width.leftWidth;
                            }
                        }

                        // 结束坐标修正到该行/列最后一个
                        endIndex = Mathf.Min(endIndex + Data.ItemCountOneLine - 1, Data.ItemCount - 1);
                        break;
                    case EScrollViewItemLayout.Up2Down_Right2Left:
                    case EScrollViewItemLayout.Down2Up_Right2Left:
                        // 计算起始坐标
                        position = Mathf.Abs(startPosition.x);
                        limitMax = position + _itemWidthCache[startIndex / Data.ItemCountOneLine].leftWidth;
                        limitMin = position - _itemWidthCache[startIndex / Data.ItemCountOneLine].rightWidth;
                        // 处在可视范围左侧，向右移动
                        if (limitMax < _visibleWindow.xMin)
                        {
                            while (limitMax < _visibleWindow.xMin)
                            {
                                startIndex += Data.ItemCountOneLine;
                                if (startIndex >= Data.ItemCount)
                                {
                                    startIndex = Data.ItemCount - 1;
                                    break;
                                }

                                var width = _itemWidthCache[startIndex / Data.ItemCountOneLine];
                                limitMax += width.rightWidth;
                                limitMax += width.leftWidth;
                            }
                        }
                        // 处在可视范围右侧，向左移动
                        else if (limitMin > _visibleWindow.xMin)
                        {
                            while (limitMin > _visibleWindow.xMin)
                            {
                                startIndex -= Data.ItemCountOneLine;
                                if (startIndex < 0)
                                {
                                    startIndex = 0;
                                    break;
                                }

                                var width = _itemWidthCache[startIndex / Data.ItemCountOneLine];
                                limitMin -= width.rightWidth;
                                limitMin -= width.leftWidth;
                            }
                        }

                        // 计算结束坐标
                        position = Mathf.Abs(endPosition.x);
                        limitMax = position + _itemWidthCache[endIndex / Data.ItemCountOneLine].leftWidth;
                        limitMin = position - _itemWidthCache[endIndex / Data.ItemCountOneLine].rightWidth;
                        // 处在可视范围左侧，向右移动
                        if (limitMax < _visibleWindow.xMax)
                        {
                            while (limitMax < _visibleWindow.xMax)
                            {
                                endIndex += Data.ItemCountOneLine;
                                if (endIndex >= Data.ItemCount)
                                {
                                    endIndex = Data.ItemCount - 1;
                                    break;
                                }

                                var width = _itemWidthCache[endIndex / Data.ItemCountOneLine];
                                limitMax += width.rightWidth;
                                limitMax += width.leftWidth;
                            }
                        }
                        // 处在可视范围右侧，向左移动
                        else if (limitMin > _visibleWindow.xMax)
                        {
                            while (limitMin > _visibleWindow.xMax)
                            {
                                endIndex -= Data.ItemCountOneLine;
                                if (endIndex < 0)
                                {
                                    endIndex = 0;
                                    break;
                                }

                                var width = _itemWidthCache[endIndex / Data.ItemCountOneLine];
                                limitMin -= width.rightWidth;
                                limitMin -= width.leftWidth;
                            }
                        }

                        // 结束坐标修正到该行/列最后一个
                        endIndex = Mathf.Min(endIndex + Data.ItemCountOneLine - 1, Data.ItemCount - 1);
                        break;
                }
                
                ScrollViewItemData itemData = null;
                bool changed = false;
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
                
                // 发生变化，做一次刷新
                if (changed)
                {
                    RefreshAllItemInstance().Forget(OnException);
                }
            }
        }
    }
}