using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/**
 * Data source that governs table backend data.
 */
public interface TableViewDataSource
{

    /**
     * a cell instance at a given index
     *
     * @param idx index to search for a cell
     * @return cell found at idx
     */
    TableViewCell tableCellAtIndex(UIGridView table, TableViewCell cell, int idx);
    /**
     * Returns number of cells in a given table view.
     *
     * @return number of cells
     */
    int numberOfCellsInTableView(UIGridView table);



};

public interface TableCellDelegate
{
    /**
     * Delegate called when the cell is about to be recycled. Immediately
     * after this call the cell will be removed from the scene graph and
     * recycled.
     *
     * @param table table contains the given cell
     * @param cell  cell that is pressed
     * @js NA
     * @lua NA
     */
    void tableCellWillRecycle(UIGridView table, TableViewCell cell);

}

public interface TableViewDelegate
{
    void tableViewWillClose(List<TableViewCell> cellList);
}

public class UIGridView : MonoBehaviour
{

    /// <summary>
    /// 重写ScrollRect OnValueChanged方法，此方法在每次滑动时都会被调用
    /// </summary>
    /// <param name="offset"></param>
    public void OnScrollValueChanged(Vector2 offset)
    {
        //Debug.Log("[UITableView]----------------OnScrollValueChanged============" + offset.y);
        OnScrolling(offset);
    }

    /**
     * weak link to the data source object
     */
    private TableViewDataSource _dataSource = null;
    public TableViewDataSource DataSource
    {
        get { return _dataSource; }
        set { _dataSource = value; }
    }
    /**
     * weak link to the delegate object
     */
    private TableViewDelegate _tableViewDelegate = null;
    public TableViewDelegate ViewDelegate
    {
        get { return _tableViewDelegate; }
        set { _tableViewDelegate = value; }
    }


    private TableCellDelegate _tableViewCellDelegate = null;
    public TableCellDelegate ViewCellDelegate
    {
        get { return _tableViewCellDelegate; }
        set { _tableViewCellDelegate = value; }
    }
    /// <summary>
    /// ScrollRect组件
    /// </summary>
    private ScrollRect m_scrollRect;

    /// <summary>
    /// RectTransform组件
    /// </summary>
    private RectTransform m_rectTransform;

    /// <summary>
    /// 内容面板RectTransform组件
    /// </summary>
    private RectTransform m_contentRectTransform;

    /// <summary>
    /// 可见子项集合
    /// </summary>
    private List<TableViewCell> m_cellsUsed = null;
    /**
     * index set to query the indexes of the cells used.
     */
    HashSet<int> m_indices;
    /**
     * vector with all cell positions
     */
    protected List<Vector2> m_vCellsPositions = null;
    /// <summary>
    /// 可重用子项集合
    /// </summary>
    private List<TableViewCell> m_cellsFreed;
    /// <summary>
    /// 子项总数量
    /// </summary>
    private int m_CellCount;
    private int m_CellFixedPart;

    /// <summary>
    /// 子项尺寸+间隔(宽或高)
    /// </summary>
    private Vector2 m_cellSize;
    private Vector2 m_cellsizeDelta;
    /// <summary>
    /// 面板总尺寸(宽或高)
    /// </summary>
    private float m_totalViewSize;
    private Vector2 m_nearestScrollVec = Vector2.up;
    private RectOffset m_contentPadding;
    private float m_initContentY = 0f;
    private float m_initContentX = 0f;
    private Direction m_currentDir;
    public enum Direction
    {
        HORIZONTAL,//横向滑动
        VERTICAL,//纵向滑动
    }
    public void Init()
    {
        InitComponent();
        InitFields();
    }


    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitComponent()
    {
        m_scrollRect = this.GetComponent<ScrollRect>();
        if (m_scrollRect != null)
        {
            m_scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
        m_rectTransform = this.GetComponent<RectTransform>();
        m_contentRectTransform = m_scrollRect.content;
        m_initContentY = m_contentRectTransform.transform.localPosition.y;
        m_initContentX = m_contentRectTransform.transform.localPosition.x;
        if (m_scrollRect.horizontal == true)//根据ScrollRect组件属性设置滑动方向
        {
            m_currentDir = Direction.HORIZONTAL;
        }
        if (m_scrollRect.vertical == true)//根据ScrollRect组件属性设置滑动方向
        {
            m_currentDir = Direction.VERTICAL;
        }
    }

    /// <summary>
    /// 初始化变量
    /// </summary>
    private void InitFields()
    {
        m_cellsUsed = new List<TableViewCell>();
        m_cellsFreed = new List<TableViewCell>();
        m_indices = new HashSet<int>();
    }


    /**
     * reloads data from data source.  the view will be refreshed.
     */
    public void reloadData()
    {
        //Debug.Log("[UITableView]---[begin]reloadData============"+ DataSource.numberOfCellsInTableView(this));
        _updateLayout();
        
        if (m_cellsUsed.Count > 0)
        {
            for (int i = 0; i < m_cellsUsed.Count; i++)
            {
                TableViewCell cell = m_cellsUsed[i];

                m_cellsFreed.Add(cell);
                cell.reset();
                cell.node.SetActive(false);
            }
        }
        m_cellsUsed.Clear();
        m_indices.Clear();

        this._updateCellPositions();
        if (DataSource.numberOfCellsInTableView(this) > 0)
        {
            //_AlignmentScrollPosition();
            this.OnScrollValueChanged(m_nearestScrollVec);
        }
        //Debug.Log("[UITableView]---[end]reloadData============" + DataSource.numberOfCellsInTableView(this));

    }


    private void _updateLayout()
    {
        GridLayoutGroup contentInfo = m_contentRectTransform.GetComponent<GridLayoutGroup>();
        contentInfo.enabled = false;
        m_cellSize = contentInfo.cellSize + contentInfo.spacing;
        m_cellsizeDelta = contentInfo.cellSize;
        m_contentPadding = contentInfo.padding;
        switch (m_currentDir)
        {
            case Direction.HORIZONTAL:
                m_CellFixedPart = Mathf.Max(1, Mathf.FloorToInt((m_rectTransform.sizeDelta.y - m_contentPadding.vertical) / (contentInfo.cellSize.y + contentInfo.spacing.y)));
                break;
            default:
                m_CellFixedPart = Mathf.Max(1, Mathf.FloorToInt((m_rectTransform.sizeDelta.x - m_contentPadding.horizontal) / (contentInfo.cellSize.x + contentInfo.spacing.x)));
                break;
        }
    }
    private void _updateCellPositions()
    {
        m_CellCount = DataSource.numberOfCellsInTableView(this);
        if (m_CellCount == 0)
        {
            return;
        }
        int cellExtendPart = Mathf.CeilToInt(m_CellCount * 1.0f / m_CellFixedPart * 1.0f);
        Vector2 _ContainerSize;
        switch (m_currentDir)
        {
            case Direction.HORIZONTAL:
                m_totalViewSize = m_cellSize.x * cellExtendPart + m_contentPadding.horizontal;
                _ContainerSize = new Vector2(m_totalViewSize, m_CellFixedPart * m_cellSize.y + m_contentPadding.vertical);
                break;
            default:
                m_totalViewSize = m_cellSize.y * cellExtendPart + m_contentPadding.vertical;
                _ContainerSize = new Vector2(m_CellFixedPart * m_cellSize.x + m_contentPadding.horizontal, m_totalViewSize);
                break;
        }
        this.setContainerSize(_ContainerSize);
        this._CalculationCellsPositions();
    }

    private void _CalculationCellsPositions()
    {
        if (m_vCellsPositions == null)
        {
            m_vCellsPositions = new List<Vector2>(m_CellCount);
        }

        switch (m_currentDir)
        {
            case Direction.HORIZONTAL:
                _CalculationCellsPositions_HORIZONTAL();
                break;
            default:
                _CalculationCellsPositions_VERTICAL();
                break;
        }
    }

    private void _CalculationCellsPositions_HORIZONTAL()
    {
        float nx = m_contentPadding.left;
        float ny = -m_contentPadding.top;
        for (int idx = 0; idx < m_CellCount; ++idx)
        {
            if (idx != 0 && idx % m_CellFixedPart == 0)
            {
                nx = nx + m_cellSize.x;
                ny = -m_contentPadding.top;
            }

            if (m_vCellsPositions.Count > idx)
            {
                m_vCellsPositions[idx].Set(nx, ny);
            }
            else
            {
                m_vCellsPositions.Add(new Vector2(nx, ny));
            }
            ny -= m_cellSize.y;
        }
    }

    private void _CalculationCellsPositions_VERTICAL()
    {
        float nx = m_contentPadding.left;
        float ny = -m_contentPadding.top;
        for (int idx = 0; idx < m_CellCount; ++idx)
        {
            if (idx != 0 && idx % m_CellFixedPart == 0)
            {
                nx = m_contentPadding.left;
                ny = ny - m_cellSize.y;
            }

            if (m_vCellsPositions.Count > idx)
            {
                m_vCellsPositions[idx].Set(nx, ny);
            }
            else
            {
                m_vCellsPositions.Add(new Vector2(nx, ny));
            }
            nx += m_cellSize.x;
        }
    }
    private void setContainerSize(Vector2 size)
    {
        m_contentRectTransform.pivot = Vector2.up;
        m_contentRectTransform.anchorMin = Vector2.up;
        m_contentRectTransform.anchorMax = Vector2.up;

        if (m_contentRectTransform.sizeDelta != size)
        {
            setContentOffsetToTop();
        }
        //设置内容面板尺寸
        m_contentRectTransform.sizeDelta = size;

        

        //Debug.Log("设置内容面板尺寸 m_contentRectTransform.sizeDelta=" + m_contentRectTransform.sizeDelta);
    }

    //任意时间调用，调用后会刷新到顶端
    public void setContentOffsetToTopWithScrolling()
    {
        if (m_currentDir==Direction.VERTICAL)
        {
            if (m_nearestScrollVec == Vector2.up)
            {
                return;
            }
            m_nearestScrollVec = Vector2.up;
            m_contentRectTransform.transform.localPosition = new Vector3(m_contentRectTransform.transform.localPosition.x, m_initContentY, 0);
        }
        else
        {
            if (m_nearestScrollVec == Vector2.zero)
            {
                return;
            }
            m_nearestScrollVec = Vector2.zero;
            m_contentRectTransform.transform.localPosition = new Vector3(m_initContentX, m_contentRectTransform.transform.localPosition.y, 0);
        }
        this.OnScrollValueChanged(m_nearestScrollVec);
    }

    //在reloadData之前调用，调用后只改变数据
    public void setContentOffsetToTop()
    {
        if (m_currentDir == Direction.VERTICAL)
        {
            if (m_nearestScrollVec == Vector2.up)
            {
                return;
            }
            m_nearestScrollVec = Vector2.up;
            m_contentRectTransform.transform.localPosition = new Vector3(m_contentRectTransform.transform.localPosition.x, m_initContentY, 0);
        }
        else
        {
            if (m_nearestScrollVec == Vector2.zero)
            {
                return;
            }
            m_nearestScrollVec = Vector2.zero;
            m_contentRectTransform.transform.localPosition = new Vector3(m_initContentX, m_contentRectTransform.transform.localPosition.y, 0);
        }
    }

    //滑动区域计算
    //offset的x和y都为0~1的浮点数，分别代表横向滑出可见区域的宽度百分比和纵向划出可见区域的高度百分比
    private void OnScrolling(Vector2 offset)
    {
        if (m_cellsUsed == null || m_contentPadding == null)
        {
            return;
        }
        
        m_nearestScrollVec = new Vector2(Mathf.Clamp01(offset.x), Mathf.Clamp01(offset.y))  ;
        int startIndex = 0;
        int endIndex = 0;
        getStartEndIndex(ref startIndex, ref endIndex);
        UpdateCells(startIndex, endIndex - 1);
    }
    
    private void getStartEndIndex(ref int startIndex, ref int endIndex)
    {
        float contentOffset = 0;
        float startOffset = 0;
        float endOffset = 0;
        switch (m_currentDir)
        {
            case Direction.HORIZONTAL:
                contentOffset = (m_totalViewSize - m_rectTransform.sizeDelta.x) * m_nearestScrollVec.x - m_contentPadding.top;//滑出可见区域高度
                startOffset = Math.Max(0, contentOffset);//当前可见区域起始x坐标
                endOffset = Math.Min(m_totalViewSize, Math.Max(0, contentOffset + m_rectTransform.sizeDelta.x));//当前可见区域结束x坐标
                startIndex = Math.Max(0, (int)(startOffset / m_cellSize.x) * m_CellFixedPart);//子项对象开始下标
                endIndex = Math.Min(m_CellCount, Mathf.CeilToInt(endOffset / m_cellSize.x) * m_CellFixedPart);//子项对象结束下标
                break;
            default:
                contentOffset = (m_totalViewSize - m_rectTransform.sizeDelta.y) * (1 - m_nearestScrollVec.y) - m_contentPadding.top;//滑出可见区域高度
                startOffset = Math.Max(0, contentOffset);//当前可见区域起始y坐标
                endOffset = Math.Min(m_totalViewSize, Math.Max(0, contentOffset + m_rectTransform.sizeDelta.y));//当前可见区域结束y坐标
                startIndex = Math.Max(0, (int)(startOffset / m_cellSize.y) * m_CellFixedPart);//子项对象开始下标
                endIndex = Math.Min(m_CellCount, Mathf.CeilToInt(endOffset / m_cellSize.y) * m_CellFixedPart);//子项对象结束下标
                break;
        }
    }

    //管理子项对象集合
    private void UpdateCells(int startIndex = 0, int endIndex = 0)
    {
        //Debug.Log("===UpdateCells===startIndex" + startIndex + "====endIndex:" + endIndex);
        //Debug.Log("===UpdateCells===m_cellsUsed.Count" + m_cellsUsed.Count);
        while (m_cellsUsed.Count > 0)
        {
            TableViewCell cell = m_cellsUsed[0];
            if (cell.Idx < startIndex)
            {
                this._moveCellOutOfSight(cell);
            }
            else
            {
                break;
            }
        }

        while (m_cellsUsed.Count > 0)
        {
            TableViewCell cell = m_cellsUsed[m_cellsUsed.Count - 1];
            if (cell.Idx > endIndex)
            {
                this._moveCellOutOfSight(cell);
            }
            else
            {
                break;
            }
        }

        ////根据开始跟结束下标，重新生成子项对象
        for (int i = startIndex; i <= endIndex; i++)
        {
            if (m_indices.Contains(i))
            {
                continue;
            }
            updateCellAtIndex(i);
        }
    }

    public void _moveCellOutOfSight(TableViewCell cell)
    {
        if (ViewCellDelegate != null)
        {
            ViewCellDelegate.tableCellWillRecycle(this, cell);
        }
        m_cellsFreed.Add(cell);
        m_cellsUsed.Remove(cell);
        m_indices.Remove(cell.Idx);
        cell.reset();
        cell.node.SetActive(false);
    }

    /**
     * Updates the content of the cell at a given index.
     *
     * @param idx index to find a cell
     */
    public void updateCellAtIndex(int index)
    {
        ////Debug.Log("===updateCellAtIndex===index" + index);
        if (index == TableViewCell.INVALID_INDEX)
        {
            return;
        }
        int countOfItems = DataSource.numberOfCellsInTableView(this);
        if (0 == countOfItems || index > countOfItems - 1)
        {
            return;
        }

        TableViewCell cell = DataSource.tableCellAtIndex(this, dequeueCell(), index);
        _setIndexForCell(index, cell);
        insertSortableCell(cell, index);
        //System.DateTime timeStart = System.DateTime.Now;
        //Debug.LogFormat("index:[{0}], cost time:{1}", index.ToString(), (System.DateTime.Now - timeStart).TotalSeconds);
    }

    /**
     * Returns an existing cell at a given index. Returns nil if a cell is nonexistent at the moment of query.
     *
     * @param idx index
     * @return a cell at a given index
     */
    public TableViewCell cellAtIndex(int idx)
    {
        if (m_indices.Contains(idx))
        {
            for (int i = 0; i < m_cellsUsed.Count; i++)
            {
                if (m_cellsUsed[i].Idx == idx)
                {
                    return m_cellsUsed[i];
                }
            }
        }

        return null;
    }


    void _setIndexForCell(int index, TableViewCell cell)
    {
        RectTransform cellRectTrans = cell.node.GetComponent<RectTransform>();
        cellRectTrans.pivot = Vector2.up;
        cellRectTrans.anchorMin = Vector2.up;
        cellRectTrans.anchorMax = Vector2.up;
        cellRectTrans.sizeDelta = m_cellsizeDelta;

        cell.node.transform.SetParent(m_contentRectTransform);
        cell.node.transform.localScale = Vector3.one;


        cell.node.transform.localPosition = _offsetFromIndex(index);
        cell.node.SetActive(true);
        cell.Idx = index;
    }

    protected Vector2 _offsetFromIndex(int idx)
    {
        return m_vCellsPositions[idx];
    }

    public void insertSortableCell(TableViewCell cell, int idx)
    {
        m_indices.Add(cell.Idx);
        if (m_cellsUsed.Count == 0)
        {
            m_cellsUsed.Add(cell);
        }
        else
        {
            for (int i = 0; i < m_cellsUsed.Count; i++)
            {
                if (m_cellsUsed[i].Idx > idx)
                {
                    m_cellsUsed.Insert(i, cell);
                    return;
                }
            }
            m_cellsUsed.Add(cell);
            return;
        }
    }

    /**
     * Dequeues a free cell if available. nil if not.
     *
     * @return free cell
     */
    public TableViewCell dequeueCell()
    {
        TableViewCell cell = null;
        if (m_cellsFreed.Count > 0)//有可重用子项对象时，复用之
        {
            cell = m_cellsFreed[0];
            m_cellsFreed.RemoveAt(0);
        }
        else//没有可重用子项对象则创建
        {
            return null;
        }
        return cell;
    }

    public void ClearAll()
    {
        while (m_cellsUsed.Count > 0)
        {
            _moveCellOutOfSight(m_cellsUsed[0]);
        }
        m_cellsUsed.Clear();
        m_indices.Clear();
    }


    public void CloseView()
    {
        ClearAll();
        if (ViewDelegate != null)
        {
            ViewDelegate.tableViewWillClose(m_cellsFreed);
        }
    }

    public void CloseViewWithCleanUp()
    {
        CloseView();
        m_cellsFreed.Clear();
    }
}