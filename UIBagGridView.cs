using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class BagItemTableViewCell : TableViewCell
{

    protected ItemInventory m_ItemInventory = null;
    public ItemInventory _ItemInventory
    {
        get { return m_ItemInventory; }
        set
        {
            m_ItemInventory = value;
            node = m_ItemInventory.gameObject;
        }
    }
}
public class UIBagGridView : TableViewDataSource, TableViewDelegate
{
    private UIGridView m_gridView = null;
    private EquipPart m_equipPart = null;
    private List<InventoryData> m_equipInfos = null;
    private Action<ItemInventory,ItemInventory> m_clickEquipItemCB;
    private Action<InventoryData> m_dontSelectItemCB;
    private int m_selectIdx = -1;
    private bool m_haveSelectItem = false;

    //实现TableViewDataSource接口
    //初始化/刷新每个格子
    public TableViewCell tableCellAtIndex(UIGridView table, TableViewCell cell, int idx)
    {
        BagItemTableViewCell itemCell = cell as BagItemTableViewCell;
        ItemInventory item = null;
        if (itemCell == null)
        {
            itemCell = new BagItemTableViewCell();
            item = ItemInventory.Create(m_equipInfos[idx]);
            itemCell._ItemInventory = item;
            
        }
        else
        {
            item = itemCell._ItemInventory;
            item.Flush(m_equipInfos[idx]);
        }
        EventTriggerClick.Get(item.gameObject).onClick = (o) =>
        {
            SelectItem(table, item, itemCell.Idx);
        };
        if (m_selectIdx == -1 || (m_selectIdx >= 0 && m_selectIdx == idx))
        {
            SelectItem(table,item, idx);
        }
        return itemCell;
    }

    //实现TableViewDataSource接口
    //返回格子数量
    public int numberOfCellsInTableView(UIGridView table)
    {
        return m_equipInfos.Count;
    }
    public void tableViewWillClose(List<TableViewCell> cellList)
    {
        foreach (var cell in cellList)
        {
            BagItemTableViewCell itemCell = cell as BagItemTableViewCell;
            itemCell._ItemInventory.__Recycle();
        }
    }

    static public UIBagGridView Create(GameObject objRoot)
    {
        UIBagGridView _view = new UIBagGridView();
        _view.Init(objRoot);
        return _view;
    }

    public void Init(GameObject objRoot)
    {
        m_equipPart = (EquipPart)Player.Instance.GetPart(emPartType.emPart_Equip);
        m_gridView = objRoot.gameObject.GetComponent<UIGridView>();
        if (m_gridView == null)
        {
            m_gridView = objRoot.gameObject.AddComponent<UIGridView>();
        }
        m_gridView.Init();
        m_gridView.DataSource = this;
        m_gridView.ViewDelegate = this;
    }

    public void reloadData()
    {
        m_haveSelectItem = false;
        var listEquipData = m_equipPart.GetUnWearEquipData();
        if (m_equipInfos == null)
        {
            m_equipInfos = new List<InventoryData>(listEquipData.Count);
        }
        m_equipInfos.Clear();
        
        foreach (var data in listEquipData)
        {
            m_equipInfos.Add(data.ToInventoryData());
        }
        m_equipInfos.Sort();
        m_selectIdx = Math.Min(m_selectIdx, m_equipInfos.Count-1);
        m_gridView.reloadData();

        if (!m_haveSelectItem)
        {
            m_dontSelectItemCB(getEquipInfoWithIdx());
        }
    }

    public InventoryData getEquipInfoWithIdx()
    {
        if (m_selectIdx >= 0 && m_selectIdx < m_equipInfos.Count)
        {
            return m_equipInfos[m_selectIdx];
        }
        return null;
    }
    public void setClickEquipItemCB(Action<ItemInventory, ItemInventory> clickEquipItemCB)
    {
        m_clickEquipItemCB = clickEquipItemCB;
    }
    public void setDontSelectItemCB(Action<InventoryData> dontSelectItemCB)
    {
        m_dontSelectItemCB = dontSelectItemCB;
    }
    public void SelectItem(UIGridView table, ItemInventory selectItem,int Idx)
    {
        m_haveSelectItem = true;
        ItemInventory oldItem = null;
        if (m_selectIdx >= 0)
        {
            BagItemTableViewCell itemCell = table.cellAtIndex(m_selectIdx) as BagItemTableViewCell;
            if (itemCell != null)
            {
                oldItem = itemCell._ItemInventory;
            }
        }
        m_selectIdx = Idx;
        if (m_clickEquipItemCB != null)
        {
            m_clickEquipItemCB(oldItem,selectItem);
        };
    }

    public void Recycle()
    {
        m_gridView.CloseViewWithCleanUp();
    }
}
