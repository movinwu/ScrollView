using UnityEngine;

namespace AsyncScrollView
{
    /// <summary>
    /// scroll view item 数据
    /// </summary>
    internal class ScrollViewItemData
    {
        /// <summary>
        /// item实例
        /// </summary>
        public GameObject ItemInstance;

        /// <summary>
        /// item实例对应的数据下标
        /// </summary>
        public int InstanceDataIndex;

        /// <summary>
        /// 实例位置
        /// </summary>
        public Vector2 ItemPosition;

        /// <summary>
        /// item容器（所有item都初始在itemContainer下）
        /// </summary>
        public readonly RectTransform ItemContainer;
        
        /// <summary>
        /// 列表数据控制器
        /// </summary>
        private readonly ItemDataController _dataController;

        private int _dataIndex = -1;
        /// <summary>
        /// 对应数据下标
        /// </summary>
        public int DataIndex
        {
            get => _dataIndex;
            set
            {
                if (_dataIndex == value) return;
                _dataIndex = value;
                _isInstanceDirty = true;
            }
        }

        /// <summary>
        /// 实例化instance是否为脏
        /// </summary>
        private bool _isInstanceDirty;
        
        public ScrollViewItemData(ItemDataController dataController, RectTransform itemRoot)
        {
            _dataController = dataController;
            ItemContainer = itemRoot;
        }

        /// <summary>
        /// 刷新实例
        /// </summary>
        /// <returns>是否新实例化了实例</returns>
        public bool RefreshInstance()
        {
            if (!_isInstanceDirty) return false;
            
            DespawnInstance();
                
            _isInstanceDirty = false;
            
            return DataIndex >= 0 && SpawnInstance();
        }

        /// <summary>
        /// 实例化实例
        /// </summary>
        /// <returns></returns>
        public bool SpawnInstance()
        {
            // 实例化
            var isNew = _dataController.GameObjectPool.Spawn(this);
            // 调用刷新委托，刷新数据
            _dataController.Data.OnItemBindData?.Invoke((InstanceDataIndex, ItemInstance));
            return isNew;
        }

        /// <summary>
        /// 回收实例
        /// </summary>
        public void DespawnInstance()
        {
            // 回收已有的实例
            if (null != ItemInstance)
            {
                _dataController.Data.OnItemUnbindData?.Invoke((InstanceDataIndex, ItemInstance));
                _dataController.GameObjectPool.Despawn(this);
                ItemInstance = null;
            }
        }
    }
}
