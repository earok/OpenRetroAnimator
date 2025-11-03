using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleTick : MonoBehaviour
{
    public Image Check;
    public Toggle Toggle => GetComponent<Toggle>();

    public void OnEnable()
    {
        GetComponent<Toggle>().onValueChanged.AddListener(Refresh);
        Refresh(Toggle.isOn);
    }

    public void OnDisable()
    {
        GetComponent<Toggle>().onValueChanged.RemoveListener(Refresh);
    }

    public void Refresh(bool value)
    {
        Check.enabled = value;
    }
}
