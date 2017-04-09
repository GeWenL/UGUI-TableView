using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/**
 * Abstract class for SWTableView cell node
 */
public class TableViewCell
{
    public static int INVALID_INDEX = -1;
    protected int _idx;
    public int Idx
    {
        get;
        set;
    }
    protected GameObject _node;
    public GameObject node
    {
        get { return _node; }
        set
        {
            _node = value;
            RectTransform rtran = _node.GetComponent<RectTransform>();
            if (rtran == null)
            {
                rtran = _node.AddComponent<RectTransform>();
            }
            rtran.pivot = Vector2.up;
            rtran.anchorMax = Vector2.up;
            rtran.anchorMin = Vector2.up;
        }
    }
    public void reset()
    {
        _idx = INVALID_INDEX;
    }
}