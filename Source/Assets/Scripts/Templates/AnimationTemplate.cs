using RetroAnimator.Entities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnimationTemplate : MonoBehaviour, IPointerClickHandler
{
	public TextMeshProUGUI Text;
	public Image Background;

	internal string ID;
	internal RetroAnimator.Entities.Animation Data
	{
		get
		{
			return Manager.Instance.Project.Animations[ID];
		}
	}

	public bool IsSelected
	{
		get
		{
			return FrameManager.Instance.SelectedAnimation == ID;
		}
		set
		{
			if (value)
			{
				FrameManager.Instance.SelectedAnimation = ID;
				FrameManager.Instance.CycleMode = FrameManager.CycleModes.Animation;
			}
		}
	}

	public void Update()
	{
		Background.color = IsSelected ? Manager.Instance.SelectedColor : Manager.Instance.NotSelectedColor;
		Text.color = Color.white;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		IsSelected = true;
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			ContextMenuManager.Instance.SetAnimationContextMenu(ID);
		}
	}

	internal void Initialize(string id)
	{
		ID = id;
		Text.text = id;
	}
}
