using RetroAnimator.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FrameTemplate : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public TextMeshProUGUI Text;
	public Image Background;

	internal string ID;

	public bool IsSelected
	{
		get
		{
			return FrameManager.Instance.SelectedFrame == ID;
		}
		set
		{
			if (value)
			{
				FrameManager.Instance.SelectedFrame = ID;
				FrameManager.Instance.CycleMode = FrameManager.CycleModes.Frame;

				//Special case - is the current animation frame a match for this one?
				if (FrameManager.Instance.SelectedAnimation == null
					|| FrameManager.Instance.SelectedAnimationFrame == -1
					|| FrameManager.Instance.SelectedAnimationFrame >= Manager.Instance.Project.Animations[FrameManager.Instance.SelectedAnimation].FrameData.Count
					|| Manager.Instance.Project.Animations[FrameManager.Instance.SelectedAnimation].FrameData[FrameManager.Instance.SelectedAnimationFrame].FrameID == ID) return;

				//If we get this far, we've selected a different frame, so unselect it from the timeline
				FrameManager.Instance.SelectedAnimationFrame = -1;
				FrameManager.Instance.SelectedFrame = ID;
			}
		}
	}

	internal Frame Data
	{
		get
		{
			return Manager.Instance.Project.Frames[ID];
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left) return;
		FrameManager.Instance.OnDragFrame(this,eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
	}

	public void OnEndDrag(PointerEventData eventData)
	{
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		IsSelected = true;
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			ContextMenuManager.Instance.SetFrameContextMenu(ID);
		}
	}

	public void Update()
	{
		Background.color = IsSelected ? Manager.Instance.SelectedColor : Manager.Instance.NotSelectedColor;
		Text.color = Color.white;
	}

	internal void Initialize(string id)
	{
		ID = id;
		Text.text = id;
		Update();
	}

}
