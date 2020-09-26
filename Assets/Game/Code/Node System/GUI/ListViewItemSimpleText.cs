using Assets.Game.Code;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public enum ListViewItemType
{
    Default,
    Skill,
    State
}

public class ListViewItemSimpleText : RecyclingListViewItem, IPointerClickHandler
{
    string text = "";
    public int id = 0;
    public bool IsSelected { get; private set; } = false;

    public delegate void ActionDoTo(int id);

    public ActionDoTo onSelectCallback, onUnSelectCallback;


    public void SetText(string value)
    {
        text = value;
        transform.GetChild(0).GetComponent<TMP_Text>().text = text;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsSelected)
        {
            UnSelect();
        }
        else
        {
            Select();
        }
    }

    public void Select()
    {
        IsSelected = true;
        GetComponent<Image>().color = new Color(255, 255, 255, 0.5f);
        onSelectCallback?.Invoke(id);
    }

    public void UnSelect()
    {
        IsSelected = false;
        GetComponent<Image>().color = new Color(255, 255, 255, 0.0f);
        onUnSelectCallback?.Invoke(id);
    }


}
