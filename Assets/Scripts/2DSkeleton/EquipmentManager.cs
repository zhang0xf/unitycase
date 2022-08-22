using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EquipmentManager : MonoBehaviour
{
    public List<UnityEngine.U2D.Animation.SpriteResolver> m_List = new List<UnityEngine.U2D.Animation.SpriteResolver>();

    // Start is called before the first frame update
    void Start()
    {
        // 寻找场景视图中所有挂载了SpriteResolver组件（脚本）的对象，不必一个一个拖拽。
        foreach (var resolver in FindObjectsOfType<UnityEngine.U2D.Animation.SpriteResolver>())
        {
            m_List.Add(resolver);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            foreach (var resolver in FindObjectsOfType<UnityEngine.U2D.Animation.SpriteResolver>())
            {
                resolver.SetCategoryAndLabel(resolver.GetCategory(), "armor");
            }
        }
    }
}
