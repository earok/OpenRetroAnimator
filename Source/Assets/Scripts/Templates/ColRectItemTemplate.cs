using RetroAnimator.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColRectItemTemplate : MonoBehaviour, IPointerClickHandler
{
	public TextMeshProUGUI Text;
	public Image Background;

	internal string Frame;
	internal string ID;

	internal ColRect Data
	{
		get
		{
			return Manager.Instance.Project.Frames[Frame].ColRects[ID];
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		IsSelected = true;
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			ContextMenuManager.Instance.SetColRectContextMenu(Frame, ID);
		}
	}

	public bool IsSelected
	{
		get
		{
			return FrameManager.Instance.SelectedColRect == ID;
		}
		set
		{
			FrameManager.Instance.SelectedColRect = ID;
		}
	}

	internal void Initialize(string frame, string id)
	{
		Frame = frame;
		ID = id;
		Text.text = id + " = " + Data.Value;
	}

	void Update()
	{
		Background.color = IsSelected ? Manager.Instance.SelectedColor : Manager.Instance.NotSelectedColor;
		Text.color = Color.white;
	}

}
