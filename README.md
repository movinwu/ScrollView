# Unity ScrollView 封装组件

[English Version](README_EN.md)

一个高性能、可定制的Unity ScrollView封装组件，支持无限滚动、多种布局方式和动态尺寸。

## 功能特性

- ✅ 高性能无限滚动
- ✅ 支持分帧实例化列表项，避免刷新时卡顿
- ✅ 支持水平和垂直布局
- ✅ 动态item尺寸控制，通过委托的方式控制每行（列）的item尺寸
- ✅ 多种跳转方式（立即跳转/平滑移动），跳转时支持设置百分比偏移和绝对偏移
- ✅ 多预制体模板支持
- ✅ 内置对象池管理，支持多种缓存池回收方式
- ✅ 一个脚本支持简单列表和多行（列）列表，使用更简单
- ✅ 使用列表元素无需继承基类，使用方法更为简单灵活
- ✅ 多种初始化方式（居中/百分比/偏移）
- ✅ 支持初始化列表项到滑动列表中央
- ✅ 扩张功能（用于动态增长的列表，如排行榜）

## 对比SuperScrollView

在UGUI的滑动列表组件封装中，SuperScrollView是使用最广泛的，但是它存在以下问题：

- ❌ 不支持分帧实例化列表项
- ❌ 简单滑动列表和多行（列）的列表对应脚本不同，使用方法不够灵活
- ❌ 使用时列表元素需要继承列表元素基类做刷新，使用方法不够简单
- ❌ 内置对象池只支持隐藏的回收方式，这种方式有一定的性能损耗
- ❌ 滑动列表的实际content区域只比viewport区域稍大，滑动速度过快时滑动会卡顿
- ❌ 跳转或自动滚动时不支持设置偏移，如果单个item尺寸超过viewport区域大小，跳转或自动滚动只能滚动到每个item开始的位置
- ❌ 不支持列表项放回缓存池或销毁时回调，无法在列表项放回缓存池或销毁时进行一些清理操作
- ❌ 收费插件

这个滑动列表组件解决了以上问题，但是同样存在以下问题：

- ❌ 采用预先计算content尺寸的方式，当数据量较大（例如10000个item）时，初始化的那一帧耗时较长，容易卡顿
- ❌ 不支持无限滚动
- ❌ 依赖UniTask插件
- ❌ 当前不支持snap功能

建议根据实际需求选择合适的滑动列表组件。

## 安装

1. 将`Assets/ScrollView`目录复制到您的Unity项目中
2. 确保已安装UniTask依赖（位于`Assets/Thirdly/UniTask`）

## 快速开始

```csharp
using UnityEngine;
using TMPro;

public class MyScrollView : MonoBehaviour
{
    [SerializeField] private ScrollView scrollView;
    
    void Start()
    {
        // 初始化ScrollView
        scrollView.InitCenter(
            itemCount: 100, 
            onItemBindData: OnItemBindData,
            getItemWidth: GetItemWidth,
            getItemHeight: GetItemHeight,
            startIndex: 0
        );
    }

    // 数据绑定回调
    private void OnItemBindData((int dataIndex, GameObject itemInstance) obj)
    {
        obj.itemInstance.transform.Find("Text").GetComponent<TMP_Text>().text = obj.dataIndex.ToString();
    }

    // 动态宽度回调
    private (float upHeight, float downHeight) GetItemWidth((int row, int totalRow) arg)
    {
        return (100, 100); // 固定宽度
    }

    // 动态高度回调
    private (float leftWidth, float rightWidth) GetItemHeight((int col, int totalCol) arg)
    {
        return (50, 50); // 固定高度
    }
}
```

### 新功能使用示例

```csharp
// 初始化并将第50个item居中显示
scrollView.InitCenter(
    itemCount: 100,
    onItemBindData: OnItemBindData,
    getItemWidth: GetItemWidth,
    getItemHeight: GetItemHeight,
    startIndex: 50 // 将第50个item居中显示
);

// 使用百分比+偏移量方式初始化
scrollView.InitPercentOffset(
    itemCount: 100,
    onItemBindData: OnItemBindData,
    getItemWidth: GetItemWidth,
    getItemHeight: GetItemHeight,
    viewportPercent: 0.5f, // 视口50%位置
    itemOffset: 10f // item额外偏移10单位
);

// 动态扩展列表（如排行榜）
void AddMoreItems()
{
    scrollView.Expand(10, () => {
        Debug.Log("列表已扩展10个item");
    });
}
```

## API文档

### 核心方法

#### `InitCenter`
```csharp
void InitCenter(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    int startIndex = 0,
    Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
初始化ScrollView并将指定索引的item居中显示。

#### `InitPercentOffset`
```csharp
void InitPercentOffset(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    float viewportPercent = 0f,
    float itemOffset = 0f,
    int startIndex = 0,
    Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
初始化ScrollView并使用百分比+偏移量方式定位初始item。

#### `InitOffsetPercent`
```csharp
void InitOffsetPercent(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    float viewportOffset = 0f,
    float itemPercent = 0f,
    int startIndex = 0,
    Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
初始化ScrollView并使用偏移量+百分比方式定位初始item。

#### `InitOffsetOffset`
```csharp
void InitOffsetOffset(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    float viewportOffset = 0f,
    float itemOffset = 0f,
    int startIndex = 0,
    Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
初始化ScrollView并使用偏移量+偏移量方式定位初始item。

#### `Expand`
```csharp
void Expand(int expandCount, Action onExpandCompleted = null)
```
扩展列表项数量，用于动态增长的列表（如排行榜）。

#### `JumpToIndex`
```csharp
void JumpToIndexByPercentPercent(int index, float viewportPercent = 0f, float itemPercent = 0f)
void JumpToIndexByPercentOffset(int index, float viewportPercent = 0f, float itemOffset = 0f)
void JumpToIndexByOffsetPercent(int index, float viewportOffset = 0f, float itemPercent = 0f)
void JumpToIndexByOffsetOffset(int index, float viewportOffset = 0f, float itemOffset = 0f)
```
立即跳转到指定索引位置。

#### `MoveToIndex`
```csharp
void MoveToIndexBySpeedPercentPercent(int index, float speed, Action<bool> onMoveCompleted = null,
            float viewportPercent = 0f, float itemPercent = 0f)
void MoveToIndexBySpeedPercentOffset(int index, float speed, Action<bool> onMoveCompleted = null,
            float viewportPercent = 0f, float itemOffset = 0f)
void MoveToIndexBySpeedOffsetPercent(int index, float speed, Action<bool> onMoveCompleted = null,
            float viewportOffset = 0f, float itemPercent = 0f)
void MoveToIndexBySpeedOffsetOffset(int index, float speed, Action<bool> onMoveCompleted = null,
            float viewportOffset = 0f, float itemOffset = 0f)
void MoveToIndexByTimePercentPercent(int index, float time, Action<bool> onMoveCompleted = null,
            float viewportPercent = 0f, float itemPercent = 0f)
void MoveToIndexByTimePercentOffset(int index, float time, Action<bool> onMoveCompleted = null,
            float viewportPercent = 0f, float itemOffset = 0f)
void MoveToIndexByTimeOffsetPercent(int index, float time, Action<bool> onMoveCompleted = null,
            float viewportOffset = 0f, float itemPercent = 0f)
void MoveToIndexByTimeOffsetOffset(int index, float time, Action<bool> onMoveCompleted = null,
            float viewportOffset = 0f, float itemOffset = 0f)
```
平滑移动到指定索引位置。

### 回调说明

- `onItemBindData`: 当需要绑定数据到item时调用（从缓存池中取出item并显示时调用）
- `onItemUnbindData`: 当需要解绑数据到item时调用（将item放回缓存池时调用）
- `getItemWidth`: 动态获取item宽度
- `getItemHeight`: 动态获取item高度
- `getItemPrefabIndex`: 多预制体模板时选择预制体索引
- `onMoveCompleted`: 当平滑移动结束后调用，参数为是否成功移动到指定索引位置

## 示例场景

查看`Assets/Test/ScrollView.unity`示例场景了解完整用法。

## 许可证

MIT License