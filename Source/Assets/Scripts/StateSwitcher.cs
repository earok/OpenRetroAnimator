using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//SImple way of making sure that only one item with this component is on at any time, including in edit mode
[ExecuteInEditMode]
public class StateSwitcher : MonoBehaviour
{
    public void ReEnable()
    {
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        foreach (var stateSwitcher in transform.parent.GetComponentsInChildren<StateSwitcher>())
        {
            //Don't switch this off
            if (stateSwitcher == this) continue;

            //Don't switch off parent state switchers
            if (stateSwitcher.transform == transform.parent) continue;

            stateSwitcher.gameObject.SetActive(false);
        }
    }
}
