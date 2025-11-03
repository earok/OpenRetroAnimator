using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OverlayManager : MonoBehaviour
{
	public static OverlayManager Instance
	{
		get
		{
			return Manager.Instance.OverlayManager;
		}
	}

	[Header("Question form")]
	public StateSwitcher QuestionPanel;
	public TextMeshProUGUI QuestionText;
	public TextMeshProUGUI QuestionPlaceholder;
	public TMP_InputField QuestionInput;

	[Header("Confirm Form")]
	public StateSwitcher ConfirmPanel;
	public TextMeshProUGUI ConfirmText;

	[Header("Color pick form")]
	public StateSwitcher ColorPanel;
	public RawImage ColorPreviewImage;
	public TMP_InputField R;
	public TMP_InputField G;
	public TMP_InputField B;

	[Header("Multichoice Form")]
	public GameObject MultichoicePanel;
	public GameObject MultichoiceTemplate;
	public Transform MultichoiceRoot;

	UnityAction ConfirmCallback;
	UnityAction<string> StringCallback;
	UnityAction<Color32> ColorCallback;
	UnityAction<SortedSet<string>> MultichoiceCallback;

	SortedSet<string> Selections = new SortedSet<string>();

	public void RefreshColors()
	{
		ColorPreviewImage.color = new Color32(_red, _green, _blue, 255);
	}

	public string RED
	{
		set
		{
			byte.TryParse(value, out _red);
			RefreshColors();
		}
	}
	byte _red;

	public string GREEN
	{
		set
		{
			byte.TryParse(value, out _green);
			RefreshColors();
		}
	}
	byte _green;

	public string BLUE
	{
		set
		{
			byte.TryParse(value, out _blue);
			RefreshColors();
		}
	}
	byte _blue;

	public void AskTextQuestion(string header, string question, string defaultValue, UnityAction<string> callback)
	{
		Reset();

		StringCallback = callback;
		QuestionText.text = header;
		QuestionPlaceholder.text = question;
		QuestionInput.text = defaultValue;
		gameObject.SetActive(true);
		QuestionPanel.gameObject.SetActive(true);
		QuestionInput.Select();
		QuestionInput.ActivateInputField();
	}

	public void AskConfirmQuestion(string header, UnityAction callback)
	{
		Reset();

		ConfirmCallback = callback;
		ConfirmText.text = header;
		gameObject.SetActive(true);
		ConfirmPanel.gameObject.SetActive(true);
	}

	public void AskColorQuestion(Color32 oldColor, UnityAction<Color32> callback)
	{
		Reset();

		_red = oldColor.r;
		_green = oldColor.g;
		_blue = oldColor.b;

		R.text = _red.ToString();
		G.text = _green.ToString();
		B.text = _blue.ToString();

		ColorCallback = callback;
		RefreshColors();
		gameObject.SetActive(true);
		ColorPanel.gameObject.SetActive(true);
	}

	public void AskMultichoiceQuestion(string header, SortedSet<string> options, UnityAction<SortedSet<string>> result)
	{
		MultichoiceCallback = result;
		Selections.Clear();

		//Clear the old selections
		foreach (Transform transform in MultichoiceRoot)
		{
			Destroy(transform.gameObject);
		}

		foreach (var option in options)
		{
			var template = Instantiate(MultichoiceTemplate, MultichoiceRoot, false);

			foreach(var tmp in template.GetComponentsInChildren<TextMeshProUGUI>())
			{
				tmp.text = option;
			}

			template.GetComponentInChildren<Toggle>().onValueChanged.AddListener((value) =>
			{
				if (value)
				{
					Selections.Add(option);
				}
				else
				{
					Selections.Remove(option);
				}
			});
			template.gameObject.SetActive(true);
		}

		gameObject.SetActive(true);
		MultichoicePanel.SetActive(true);
	}

	public void Reset()
	{
		QuestionInput.text = "";
		gameObject.SetActive(false);
		StringCallback = null;
		ColorCallback = null;
		MultichoiceCallback = null;
	}

	public void OnSubmit()
	{
		if (StringCallback != null)
		{
			StringCallback(QuestionInput.text);
		}

		if (ColorCallback != null)
		{
			ColorCallback(new Color32(_red, _green, _blue, 255));
		}

		if (MultichoiceCallback != null)
		{
			MultichoiceCallback(Selections);
		}

		Reset();
	}

	public void OnConfirm()
	{
		if (ConfirmCallback != null)
		{
			ConfirmCallback();
		}
		Reset();
	}

	public void OnCancel()
	{
		Reset();
	}

	public void Update()
	{
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			Reset();
		}
		else if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
		{
			OnSubmit();
		}
	}

}
