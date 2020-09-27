using Assets.Game.Code;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListViewItemSimpleTextImage : ListViewItemSimpleText
{
    private string imageLink = "";
    public Image image;

    public async void SetImage(string imgLink)
    {
        imageLink = imgLink;
        image.sprite = PSTreeApiManager.Instance.GetSpriteForNode(await PSTreeApiManager.Instance.GetTextureNode(imageLink));
    }


}
