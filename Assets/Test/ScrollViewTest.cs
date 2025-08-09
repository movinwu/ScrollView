using System.Collections;
using System.Collections.Generic;
using AsyncScrollView;
using TMPro;
using UnityEngine;

public class ScrollViewTest : MonoBehaviour
{
    [SerializeField] private ScrollView scrollView;
    
    void Start()
    {
        // scrollView.InitOffsetOffset(500, OnItemBindData, GetItemWidth, GetItemHeight, 5, getItemPrefabIndex: GetItemPrefabIndex);
        scrollView.InitCenter(200, OnItemBindData, _ => (80, 80), _ => (80, 80));
        // StartCoroutine(Cor());
    }

    private IEnumerator Cor()
    {
        yield return new WaitForSeconds(1f);
        scrollView.InitOffsetOffset(0, OnItemBindData, GetItemWidth, GetItemHeight, 5, getItemPrefabIndex: GetItemPrefabIndex);
        yield return new WaitForSeconds(1f);
        scrollView.InitOffsetOffset(1000, OnItemBindData, GetItemWidth, GetItemHeight, 5, getItemPrefabIndex: GetItemPrefabIndex);
        yield return new WaitForSeconds(1f);
        scrollView.JumpToIndexByOffsetOffset(160);
        yield return new WaitForSeconds(3f);
        scrollView.MoveToIndexBySpeedOffsetOffset(105, 500f, onMoveCompleted: success =>
        {
            Debug.Log("MoveToIndexBySpeedOffsetOffset: " + success);
        });
    }

    private int GetItemPrefabIndex((GameObject[] itemPrefabs, int dataIndex) arg)
    {
        return arg.dataIndex % arg.itemPrefabs.Length;
    }

    private (float leftWidth, float rightWidth) GetItemHeight((int col, int totalCol) arg)
    {
        if (arg.col % 3 == 0)
        {
            return (80, 60);
        }
        else if (arg.col % 3 == 1)
        {
            return (70, 100);
        }
        else
        {
            return (50, 50);
        }
    }

    private (float upHeight, float downHeight) GetItemWidth((int row, int totalRow) arg)
    {
        if (arg.row % 3 == 0)
        {
            return (80, 60);
        }
        else if (arg.row % 3 == 1)
        {
            return (70, 100);
        }
        else
        {
            return (50, 50);
        }
    }

    private void OnItemBindData((int dataIndex, GameObject itemInstance) obj)
    {
        obj.itemInstance.transform.Find("ContentTxt").GetComponent<TMP_Text>().text = obj.dataIndex.ToString();
    }
}
