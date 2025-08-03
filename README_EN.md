# Unity ScrollView Wrapper Component

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
        scrollView.Init(
            itemCount: 100, 
            onBindData: OnItemBindData,
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

## API Documentation

### Core Methods

#### `Init`
```csharp
void Init(
    int itemCount, 
    Action<(int dataIndex, GameObject itemInstance)> onBindData,
    Func<(int row, int totalRow), (float upHeight, float downHeight)> getItemWidth,
    Func<(int col, int totalCol), (float leftWidth, float rightWidth)> getItemHeight,
    int startIndex = 0,
    Func<(GameObject[] itemPrefabs, int dataIndex), int> getItemPrefabIndex = null
)
```
Initializes the ScrollView with basic parameters and callbacks.

#### `JumpToIndex`
```csharp
void JumpToIndexByPercentPercent(int index, float viewportPercent = 0f, float itemPercent = 0f)
void JumpToIndexByPercentOffset(int index, float viewportPercent = 0f, float itemOffset = 0f)
void JumpToIndexByOffsetPercent(int index, float viewportOffset = 0f, float itemPercent = 0f)
void JumpToIndexByOffsetOffset(int index, float viewportOffset = 0f, float itemOffset = 0f)
```
Jumps immediately to the specified index position.

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
Smoothly moves to the specified index position.

### Callback Explanation

- `onBindData`: Called when data needs to be bound to an item
- `getItemWidth`: Dynamically gets item width
- `getItemHeight`: Dynamically gets item height
- `getItemPrefabIndex`: Selects prefab index for multiple prefab templates
- `onMoveCompleted`: Called when the movement is completed, with a parameter indicating whether the movement was successful

## Example Scene

Check the `Assets/Test/ScrollView.unity` example scene for complete usage.

## License

MIT License
