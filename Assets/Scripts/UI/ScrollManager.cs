using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public delegate void OnEndDragHandler(float position);

public class ScrollManager : MonoBehaviour,IBeginDragHandler,IEndDragHandler
{
    ScrollRect scrollRect;
    private const float min = 0f;
    private const float max = 1.0f;
    private float targetHorizontalPosition = 0.0f;
    private float speed = 4;
    private event OnEndDragHandler handler;
    private bool isDraging = false;
    private Toggle[] toggles;
    private Dictionary<int, float> dict = null; // key : 页索引， value : float类型坐标

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDraging = true;
        // throw new System.NotImplementedException();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDraging = false;
        targetHorizontalPosition = BinarySearch(scrollRect.horizontalNormalizedPosition, min, max);
        SetToggleIsOn(targetHorizontalPosition);
        // throw new System.NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {
        dict = new Dictionary<int, float>();
        CreateDictionary();
        AddListenerForToggleEvent();
        scrollRect = gameObject.GetComponent<ScrollRect>();
        handler += new OnEndDragHandler(DragEndEvent);
    }

    // Update is called once per frame
    void Update()
    {
        // 产生过渡效果
        if (!isDraging && handler != null)
        {
            // "Mathf.Lerp" return : "float" The interpolated float result between the two float values.
            handler(Mathf.Lerp(scrollRect.horizontalNormalizedPosition, targetHorizontalPosition, Time.deltaTime * speed));
        }
    }

    public float BinarySearch(float position,float min, float max)
    {
        Debug.Log(string.Format("val is : {0}", position.ToString()));

        if (position < 0) { return 0.0f; }
        if (position > 1) { return 1.0f; }

        if (position > 0f && position <= 0.25f) { return dict[1]; }   // 第一页
        else if (position <= 0.5f) { return dict[2]; } //   第二页
        else if (position <= 0.75) { return dict[3]; } // 第三页
        else if (position <= 1.0f) { return dict[4]; } // 第四页

        float half = (min + max) / 2;

        if (position < half)
            BinarySearch(position, min, half);
        else
            BinarySearch(position, half, max);

        return float.NaN;
    }

    public void DragEndEvent(float position)
    {
        scrollRect.horizontalNormalizedPosition = position;
    }

    public void AddListenerForToggleEvent()
    {
        ToggleGroup toggleGroup = GameObject.Find("EmptyForToggleGroup").GetComponent<ToggleGroup>();
        if (null == toggleGroup)
        {
            Debug.Log(string.Format("can not find toggleGroup"));
            return;
        }

        toggles = toggleGroup.GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            Debug.Log(string.Format("toggle name is {0}", toggle.name));
            toggle.onValueChanged.AddListener(delegate { ChangeScrollRectByToggle(toggle); });
        }
    }

    public void ChangeScrollRectByToggle(Toggle toggle)
    {
        if (toggle && toggle.isOn)
        {
            switch (toggle.name)
            {
                case "LabelButton":
                    targetHorizontalPosition = dict[1];  // 第一页
                    break;
                case "LabelButton1":
                    targetHorizontalPosition = dict[2];  // 第二页
                    break;
                case "LabelButton2":
                    targetHorizontalPosition = dict[3];  // 第三页
                    break;
                case "LabelButton3":
                    targetHorizontalPosition = dict[4];  // 第四页
                    break;
            }
        }
    }

    public void SetToggleIsOn(float position)
    {
        string name = GetToggleName(position);
        if (!string.IsNullOrEmpty(name))
        {
            foreach (Toggle toggle in toggles)
            {
                if (name.Equals(toggle.name))
                    toggle.isOn = true;
                else
                    toggle.isOn = false;
            }
        }
    }

    public string GetToggleName(float position)
    {
        if (position == dict[1]) { return "LabelButton"; }
        if (position == dict[2]) { return "LabelButton1"; }
        if (position == dict[3]) { return "LabelButton2"; }
        if (position == dict[4]) { return "LabelButton3"; }
        return null;
    }

    public void CreateDictionary()
    {
        dict.Add(1, 0.0f);
        dict.Add(2, 0.33333f);
        dict.Add(3, 0.66666f);
        dict.Add(4, 1.0f);
    }

}
