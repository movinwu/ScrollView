using System.Collections.Generic;
using UnityEngine;

namespace AsyncScrollView
{
    /// <summary>
    /// gameObject池
    /// </summary>
    internal class GameObjectPool
    {
        /// <summary>
        /// 不可见位置
        /// </summary>
        private static readonly Vector3 InvisiblePosition = new Vector3(-10000, -10000, 0);
        
        /// <summary>
        /// 所有没有使用的预制体数据
        /// </summary>
        private List<GameObject>[] _freeGameObjects;
        
        /// <summary>
        /// 所有item预制体模板
        /// </summary>
        private GameObject[] _itemPrefabs;

        /// <summary>
        /// 所有正在使用中的item预制体
        /// </summary>
        private readonly Dictionary<GameObject, int> _usingGameObjects = new Dictionary<GameObject, int>();
        
        /// <summary>
        /// 所有item预制体缓存
        /// </summary>
        private readonly Dictionary<GameObject, List<GameObject>> _itemCache = new Dictionary<GameObject, List<GameObject>>();
        
        /// <summary>
        /// 元素回收类型
        /// </summary>
        private EItemDespawnType _itemDespawnType = EItemDespawnType.SetActiveFalse;

        /// <summary>
        /// 滑动列表数据
        /// </summary>
        private ScrollViewData _data;
        
        public void Init(ScrollViewData data, EItemDespawnType itemDespawnType)
        {
            _data = data;
            _itemDespawnType = itemDespawnType;
            if (null != _freeGameObjects)
            {
                // 回收所有正在使用的预制体数据
                foreach (var pair in _usingGameObjects)
                {
                    var list = _freeGameObjects[pair.Value];
                    if (null == list)
                    {
                        list = new List<GameObject>();
                        _freeGameObjects[pair.Value] = list;
                    }
                    // 做回收时处理
                    if (_itemDespawnType == EItemDespawnType.SetActiveFalse)
                    {
                        pair.Key.SetActive(false);
                    }
                    list.Add(pair.Key);
                }
                _usingGameObjects.Clear();
                _itemCache.Clear();
                // 将所有当前的实例化数据存储到缓存中
                for (int i = 0; i < _freeGameObjects.Length; i++)
                {
                    // 模板已经销毁，清理所有实例缓存
                    if (null == _itemPrefabs[i])
                    {
                        var freeList = _freeGameObjects[i];
                        if (null == freeList) continue;
                        foreach (var freeItem in freeList)
                        {
                            Object.Destroy(freeItem);
                        }
                    }
                    // 模板没有销毁，加入到临时缓存中
                    else
                    {
                        var freeList = _freeGameObjects[i];
                        if (null != freeList)
                        {
                            _itemCache.Add(_itemPrefabs[i], freeList);
                        }
                    }
                }
            }
            
            this._itemDespawnType = itemDespawnType;
            this._itemPrefabs = data.ItemPrefabs;
            // 构建新的缓存列表
            _freeGameObjects = new List<GameObject>[_itemPrefabs.Length];
            _usingGameObjects.Clear();
            // 遍历，根据预制体模板查看是否复用实例化对象
            for (var i = 0; i < _itemPrefabs.Length; i++)
            {
                var prefab = _itemPrefabs[i];
                if (!_itemCache.TryGetValue(prefab, out var list)) continue;
                _freeGameObjects[i] = list;
                _itemCache.Remove(prefab);
            }
            // 销毁剩余的实例化对象
            foreach (var cacheList in _itemCache.Values)
            {
                foreach (var item in cacheList)
                {
                    Object.Destroy(item);
                }
            }
            _itemCache.Clear();
        }
        
        public bool Spawn(ScrollViewItemData itemData)
        {
            var itemIndex = _data.GetItemPrefabIndex((_data.ItemPrefabs, itemData.DataIndex));
            var list = _freeGameObjects[itemIndex];
            if (null == list)
            {
                list = new List<GameObject>();
                _freeGameObjects[itemIndex] = list;
            }

            bool isNew = false;
            if (list.Count > 0)
            {
                itemData.ItemInstance = list[^1];
                list.RemoveAt(list.Count - 1);
                itemData.ItemInstance.transform.localPosition = Vector3.zero;
                itemData.ItemInstance.transform.localRotation = Quaternion.identity;
            
                // 设置从缓存池中取出
                switch (_itemDespawnType)
                {
                    case EItemDespawnType.SetActiveFalse:
                        itemData.ItemInstance.SetActive(true);
                        break;
                }
            }
            else
            {
                var instantiateItem = Object.Instantiate(_itemPrefabs[itemIndex], itemData.ItemContainer);
                instantiateItem.transform.localPosition = Vector3.zero;
                instantiateItem.transform.localRotation = Quaternion.identity;
                itemData.ItemInstance = instantiateItem;
                isNew = true;
            }
            itemData.InstanceDataIndex = itemData.DataIndex;
            itemData.ItemInstance.gameObject.name = itemData.DataIndex.ToString();
            itemData.ItemInstance.GetComponent<RectTransform>().anchoredPosition = itemData.ItemPosition;
            
            // 正在使用中的预制体记录
            _usingGameObjects.Add(itemData.ItemInstance, itemIndex);

            return isNew;
        }

        public void Despawn(ScrollViewItemData data)
        {
            // 检查实例
            if (null == data.ItemInstance)
            {
                return;
            }
            
            // 从正在使用中的预制体记录中移除
            if (!_usingGameObjects.Remove(data.ItemInstance, out var itemIndex))
            {
                Debug.LogError("Can't find item instance in usingGameObjects");
                return;
            }

            var list = _freeGameObjects[itemIndex];
            if (null == list)
            {
                list = new List<GameObject>();
                _freeGameObjects[itemIndex] = list;
            }
            list.Add(data.ItemInstance);
            data.ItemInstance.gameObject.name = "Free";
            _data.OnItemUnbindData?.Invoke((data.InstanceDataIndex, data.ItemInstance));
            // 当回收时，根据回收类型对预制体进行相应操作
            switch (_itemDespawnType)
            {
                case EItemDespawnType.SetActiveFalse:
                    data.ItemInstance.SetActive(false);
                    break;
                case EItemDespawnType.MoveInvisiblePosition:
                    data.ItemInstance.transform.localPosition = InvisiblePosition;
                    break;
            }
            data.ItemInstance = null;
        }

        public void Release()
        {
            if (null == _freeGameObjects) return;
            foreach (var freeGameObjects in _freeGameObjects)
            {
                if (null == freeGameObjects) continue;
                foreach (var freeGameObject in freeGameObjects)
                {
                    if (null != freeGameObject)
                    {
                        Object.Destroy(freeGameObject);
                    }
                }
            }
            _freeGameObjects = null;
            foreach (var gameObject in _usingGameObjects.Keys)
            {
                if (null != gameObject)
                {
                    Object.Destroy(gameObject);
                }
            }
            _usingGameObjects.Clear();
        }
    }
}
