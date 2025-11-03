using RetroAnimator.Entities;
using RetroAnimator.Undo;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnimationFrameTemplate : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
	public const float ConversionRate = 0.02f; //One frame = 0.02 secs

	public Color SelectedColor;
	public Color NotSelectedColor;

	public TMP_InputField Duration;
	public TextMeshProUGUI FrameText;
	public TextMeshProUGUI Text;
	public Image Background;
	public Transform DrawRoot;

	static Vector3[] Corners = new Vector3[4];
	string Animation;
	int Index;
	internal bool IsDragging
	{
		get
		{
			return _isDragging;
		}
		set
		{
			_isDragging = value;
			if(value)
			{
				FrameManager.Instance.EmptyFrame.parent = FrameManager.Instance.AnimationFrameContainer.transform;
			}
			else
			{
				FrameManager.Instance.EmptyFrame.parent = null;
			}
		}
	}
	bool _isDragging;

	public bool IsSelected
	{
		get
		{
			if (FrameManager.Instance.PlayMode == FrameManager.PlayModes.None)
			{
				return FrameManager.Instance.SelectedAnimationFrame == Index;
			}
			return FrameManager.Instance.DrawAnimationFrame == Index;
		}
		set
		{
			if (value)
			{
				FrameManager.Instance.SelectedAnimationFrame = Index;
			}
		}
	}

	public AnimationFrameData Data
	{
		get
		{
			return Manager.Instance.Project.Animations[Animation].FrameData[Index];
		}
	}

	public Frame Frame
	{
		get
		{
			return Manager.Instance.Project.Frames[Data.FrameID];
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (IsDragging) return;

		if (eventData.button == PointerEventData.InputButton.Right) return;
		IsSelected = true;
		transform.parent = FrameManager.Instance.transform;
		IsDragging = true;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right) return;
		transform.localPosition += new Vector3(eventData.delta.x, eventData.delta.y, 0);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right) return;
		Undo.Instance.PushUndo();
		Drop();

	}

	int PositionOffset
	{
		get
		{
			var widthOffset = (GetComponent<RectTransform>().sizeDelta.x) / 2;
			var i = 0;
			var local1 = FrameManager.Instance.AnimationFrameContainer.transform.InverseTransformPoint(transform.position);
			foreach (Transform child in FrameManager.Instance.AnimationFrameContainer.transform)
			{
				var local2 = FrameManager.Instance.AnimationFrameContainer.transform.InverseTransformPoint(child.position);
				if (local1.x - widthOffset < local2.x)
				{
					return i;
				}
				i++;
			}
			return FrameManager.Instance.AnimationFrameContainer.transform.childCount - 1;
		}
	}

	internal void Drop()
	{
		transform.parent = FrameManager.Instance.AnimationFrameContainer.transform;
		IsDragging = false;
		FrameManager.Instance.InsertAnimationFrameAt(Data, PositionOffset);
	}

	public void NudgeScrollBar(float value)
	{
		var offset = Input.mousePosition.x - transform.position.x;

		FrameManager.Instance.AnimationFrameScrollbar.value = Mathf.Clamp01(FrameManager.Instance.AnimationFrameScrollbar.value + value);
		Canvas.ForceUpdateCanvases();
		Canvas.ForceUpdateCanvases();

		var offset2 = Input.mousePosition.x - transform.position.x;
		if (offset != offset2)
		{
			transform.localPosition += new Vector3(offset2 - offset, 0, 0);
		}
	}



	public void OnPointerClick(PointerEventData eventData)
	{
		IsSelected = true;
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			ContextMenuManager.Instance.SetAnimationFrameContextMenu(Animation, Index);
		}
	}

	public void Update()
	{
		Background.color = IsSelected ? SelectedColor : NotSelectedColor;
		Text.color = Color.white;

		if (IsDragging)
		{
			//Scroll left/right?
			FrameManager.Instance.AnimationFramePanelRT.GetWorldCorners(Corners);

			if (Input.mousePosition.x < Corners[0].x)
			{
				NudgeScrollBar(-Time.deltaTime);
			}
			if (Input.mousePosition.x > Corners[2].x)
			{
				NudgeScrollBar(Time.deltaTime);
			}

			//Set 'empty slot' position
			FrameManager.Instance.EmptyFrame.SetSiblingIndex(PositionOffset);
		}
	}

	public void RefreshImage()
	{
		foreach (Transform child in DrawRoot.transform)
		{
			Destroy(child.gameObject);
		}
		DrawPanelManager.PreviewFrame(Data.FrameID, DrawRoot, false);
	}

	internal void Initialize(string animation, int index)
	{
		Animation = animation;
		Index = index;
		FrameText.text = (index + 1).ToString();
		Text.text = Data.FrameID;


		RefreshImage();
		Duration.onValueChanged.RemoveAllListeners();
		Duration.text = (Math.Round(Data.Duration * ConversionRate * 50, MidpointRounding.AwayFromZero) / 50).ToString();
		Duration.onValueChanged.AddListener((value) =>
		{
			Undo.Instance.PushUndo();
			Data.Duration = Mathf.RoundToInt(float.Parse(value) / ConversionRate);
		});
	}
}
