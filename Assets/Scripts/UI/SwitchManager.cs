using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwitchManager : MonoBehaviour
{
    public GameObject On;
    public GameObject Off;

    // Start is called before the first frame update
    void Start()
    {
        AddListenerForToggleEvent();
    }

    public void AddListenerForToggleEvent()
    {
        Toggle toggle = GameObject.Find("SwitchBackgroud").GetComponent<Toggle>();
        if (null == toggle)
        {
            Debug.Log(string.Format("UI Error"));
            return;
        }
        toggle.onValueChanged.AddListener(delegate { ToggleChangedHandler(toggle.isOn); });
    }

    public void GetSwitchObject()
    {
        On = GameObject.Find("SwitchOn");   // 查找隐藏对象使用：Transform.Find();
        if (On == null) { return; }
        Off = GameObject.Find("SwitchOff");
        if (Off == null) { return; }
        Debug.Log("Done!");
    }


    public void ToggleChangedHandler(bool isON)
    {
        On.SetActive(isON);
        Off.SetActive(!isON);
    }
}
