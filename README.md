# UGUI-TableView

#### 一. 概述
1. 沿用Scroll View组件
2. UIGridView组件会通过代码的方式添加
3. 需要在content中使用Grid Layout Group组件，设置cell宽高/间隔，但在代码中设置enabled = false
    > 有利于美术调效果
4. 通过ScrollRect组件OnValueChanged方法监听Scroll View滑动（在UIGridView中已添加）
5. 目前只支持Vertical的TableView


#### 二．TableView的组成
1. UIGridView.cs:UIGridView
2. UIGridView.cs:TableViewDataSource(interface)
3. UITableViewCell.cs

#### 三．TableView的原理
1. 通过代码将UIGridView组件挂载到ScrollRect组件所在的GameObject上
2. 初始化
    1. 初始化组件,设置OnValueChanged方法监听ScrollView滑动
        > private void InitComponent()  
        > m_scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
    
    2. 初始化变量，GridLayoutGroup enabled = false，获取GridLayoutGroup的spacing/cellSize，计算列数m_CellCols

3. TableViewDataSource
    > Data source that governs table backend data
    
    1. tableCellAtIndex：a cell instance at a given index
    2. numberOfCellsInTableView:获取cell数量
4. reloadData函数
    > reloads data from data source.  the view will be refreshed.
    
    1. 重置数据
    2. _updateCellPositions：计算所有cell的位置
    3. setContainerSize：设置content的宽高和锚点/pivot
5. OnScrollValueChanged 
    > 此方法在每次滑动时都会被调用
    
    1. OnScrolling 通过滑动区域计算出当前可见区域cell的开始下标/结束下标
    2. UpdateCells m_cellsUsed中删除可见cell下标外的其他的cell（_moveCellOutOfSight）
    3. updateCellAtIndex: 根据开始跟结束下标，重新生成cell
    4. 调用实现的tableCellAtIndex接口
    5. _setIndexForCell：设置cell的位置/锚点/pivot/SetParent
    6. insertSortableCell：有序插入

6. UITableViewCell只有两个属性
    1. int Idx下标-唯一
    2. GameObject node,用在5.5
    
#### 四．TableView的使用
> 参照 UIBagGridView.cs 交易所出售-背包界面
1. 实现TableViewDataSource接口
2. Init
    > 将ScrollRect组件所在的GameObject作为root节点传入
    
    1. 将UIGridView挂载到ScrollRect组件所在的GameObject上
    2. 设置m_gridView的DataSource代理
3. reloadData
    1. 设置数据
    2. 调用UIGridView的reloadData

4. 若cell比较特殊，UITableViewCell满足不了，则继承UITableViewCell，定制cell
    > 背包的cell参照BagItemTableViewCell  
    > 多了一个ItemInventory类型的属性



#### 五. 参考链接
1. http://blog.csdn.net/tmac3380809/article/details/51290387
2. http://blog.csdn.net/ab342854406/article/details/50651011
