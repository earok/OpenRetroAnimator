/*

using RetroAnimator.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AnimationManager : Singleton<AnimationManager>
{





	internal void DroppedNewFrame(AllFrameTemplate allFrameTemplate)
	{
		if (SelectedAnimation == null) return;

		if (AnimationFrameContainer.transform.parent.GetComponent<RectTransform>().ContainsPoint(Input.mousePosition))
		{
			allFrameTemplate.transform.SetParent(AllFrameContainer.transform.parent);
			var height = allFrameTemplate.GetComponent<RectTransform>().sizeDelta.y;
			var count = SelectedAnimation.Data.FrameData.Count;
			var offset = (int)Mathf.Clamp(-allFrameTemplate.transform.localPosition.y / height, 0, count);

			var animationFrame = new AnimationFrameData() { Duration = 10, FrameID = allFrameTemplate.ID };
			SelectedAnimation.Data.FrameData.Insert(offset, animationFrame);
			RefreshAnimationFrameList();
			SelectedAnimationFrame = animationFrameTemplateList.SingleOrDefault(p => p.Data == animationFrame);
		}

		RefreshAllFrameList();
		Destroy(allFrameTemplate.gameObject);
	}



	public void OnEnable()
	{
		DetailPanelManager.Instance.Reset();
		DrawPanelManager.Instance.PreviewFrame(null);
		RefreshAnimationList();
	}

	private void RefreshAllFrameList()
	{
		allFrameList.Clear();
		foreach (Transform child in AllFrameContainer.transform)
		{
			Destroy(child.gameObject);
		}

		foreach (var key in Manager.Instance.Project.Frames.Keys.OrderBy(p => p))
		{
			var data = Manager.Instance.Project.Frames[key];
			var frameItem = Instantiate(AllFrameTemplate);
			frameItem.Initialize(key, data);
			frameItem.transform.SetParent(AllFrameContainer.transform);
			allFrameList.Add(frameItem);
		}
	}



}

*/