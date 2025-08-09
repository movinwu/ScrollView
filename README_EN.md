# Unity ScrollView Wrapper Component

[中文版本](README.md)

A high-performance, customizable Unity ScrollView wrapper component that supports infinite scrolling, multiple layout modes, and dynamic sizing.

## Features

- ✅ High-performance infinite scrolling
- ✅ Supports frame-by-frame item instantiation to avoid stuttering during refresh
- ✅ Supports both horizontal and vertical layouts
- ✅ Dynamic item size control via delegate methods
- ✅ Multiple jump modes (instant/smooth), with support for percentage and absolute offsets
- ✅ Multiple prefab template support
- ✅ Built-in object pool management with various recycling strategies
- ✅ Single script supports both simple lists and multi-row/column lists
- ✅ No need to inherit base class for list items, more flexible usage
- ✅ Multiple initialization methods (center/percent/offset)
- ✅ Supports initializing list items to the center of scroll view
- ✅ Expansion feature for dynamically growing lists (e.g. leaderboards)

## Comparison with SuperScrollView

Among UGUI scroll view wrappers, SuperScrollView is the most widely used, but it has the following issues:

- ❌ No frame-by-frame item instantiation
- ❌ Different scripts for simple lists and multi-row/column lists, less flexible
- ❌ List items need to inherit base class for refresh, less straightforward
- ❌ Built-in object pool only supports hide mode, which has performance overhead
- ❌ Content area is only slightly larger than viewport, causing stuttering with fast scrolling
- ❌ No offset support when jumping or auto-scrolling
- ❌ No callbacks when items are recycled or destroyed
- ❌ Paid plugin

This scroll view component solves the above issues, but still has some limitations:

- ❌ Pre-calculates content size, causing lag during initialization with large datasets (e.g., 10000 items)
- ❌ No infinite scrolling support
- ❌ Requires UniTask plugin
- ❌ Currently no snap support

Choose the appropriate scroll view component based on your actual needs.

## Installation

1. Copy the `Assets/ScrollView` directory to your Unity project
2. Ensure UniTask dependency is installed (located at `Assets/Thirdly/UniTask`)

## Quick Start

```csharp
using UnityEngine;
using TMPro;

public class MyScrollView : MonoBehaviour
{
    [SerializeField] private ScrollView scrollView;
    
    void Start()
    {
        // Initialize ScrollView
        scrollView.InitCenter(
            itemCount: 100, 
            onItemBindData: OnItemBindData,
            getItemWidth: GetItemWidth,
            getItemHeight: GetItemHeight,
            startIndex: 0
        );
    }

    // Data binding callback
    private void OnItemBindData((int dataIndex, GameObject itemInstance) obj)
    {
        obj.itemInstance.transform.Find("Text").GetComponent<TMP_Text>().text = obj.dataIndex.ToString();
    }

    // Dynamic width callback
    private (float upHeight, float downHeight) GetItemWidth((int row, int totalRow) arg)
    {
        return (100, 100); // Fixed width
    }

    // Dynamic height callback
    private (float leftWidth, float rightWidth) GetItemHeight((int col, int totalCol) arg)
    {
        return (50, 50); // Fixed height
    }
}
```

### New Features Usage Examples

```csharp
// Initialize and center the 50th item
scrollView.InitCenter(
    itemCount: 100,
    onItemBindData: OnItemBindData,
    getItemWidth: GetItemWidth,
    getItemHeight: GetItemHeight,
    startIndex: 50 // Center the 50th item
);

// Initialize with percentage + offset positioning
scrollView.InitPercentOffset(
    itemCount: 100,
    onItemBindData: OnItemBindData,
    getItemWidth: GetItemWidth,
    getItemHeight: GetItemHeight,
    viewportPercent: 0.5f, // 50% position in viewport
    itemOffset: 10f // Additional 10 unit offset for item
);

// Expand list dynamically (e.g. for leaderboards)
void AddMoreItems()
{
    scrollView.Expand(10, () => {
        Debug.Log("List expanded by 10 items");
    });
}
```

## API Documentation

### Core Methods

#### `InitCenter`
```csharp
void InitCenter(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onItemBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    int startIndex = 0,
    Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
Initialize ScrollView and center the specified index item.

#### `InitPercentOffset`
```csharp
void InitPercentOffset(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onItemBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    float viewportPercent = 0f,
    float itemOffset = 0f,
    int startIndex = 0,
    Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
Initialize ScrollView with percentage + offset positioning.

#### `InitOffsetPercent`
```csharp
void InitOffsetPercent(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onItemBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    float viewportOffset = 0f,
    float itemPercent = 0f,
    int startIndex = 0,
    Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
Initialize ScrollView with offset + percentage positioning.

#### `InitOffsetOffset`
```csharp
void InitOffsetOffset(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onItemBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    float viewportOffset = 0f,
    float itemOffset = 0f,
    int startIndex = 0,
    Action<(int dataIndex, GameObject itemInstance)> onItemUnbindData = null,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
Initialize ScrollView with offset + offset positioning.

#### `Expand`
```csharp
void Expand(int expandCount, Action onExpandCompleted = null)
```
Expand the list item count for dynamically growing lists (e.g. leaderboards).

#### `JumpToIndex`
```csharp
void JumpToIndexByPercentPercent(int index, float viewportPercent = 0f, float itemPercent = 0f)
void JumpToIndexByPercentOffset(int index, float viewportPercent = 0f, float itemOffset = 0f)
void JumpToIndexByOffsetPercent(int index, float viewportOffset = 0f, float itemPercent = 0f)
void JumpToIndexByOffsetOffset(int index, float viewportOffset = 0f, float itemOffset = 0f)
```
Jump immediately to the specified index position.

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
Smoothly move to the specified index position.

### Callback Explanation

- `onItemBindData`: Called when data needs to be bound to an item (when item is taken from pool and displayed)
- `onItemUnbindData`: Called when data needs to be unbound from an item (when item is returned to pool)
- `getItemWidth`: Dynamically gets item width
- `getItemHeight`: Dynamically gets item height
- `getItemPrefabIndex`: Selects prefab index for multiple prefab templates
- `onMoveCompleted`: Called when smooth movement completes, with parameter indicating whether movement was successful

## Example Scene

Check the `Assets/Test/ScrollView.unity` example scene for complete usage.

## License

MIT License
