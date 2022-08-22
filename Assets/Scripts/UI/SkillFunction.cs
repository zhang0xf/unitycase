#define NO_ERROR_STEP
using UnityEngine;
using UnityEngine.UI;

public class SkillFunction : MonoBehaviour
{
    public float coolingTime = 2.0f;
    public Image coolDownImage;
    private float timer = 0f;
    private bool isStartTimer = false;
    public KeyCode key;

    // Start is called before the first frame update
    void Start()
    {
        coolDownImage = transform.Find("CoolDownImage").GetComponent<Image>(); 
        if (null == coolDownImage)
        {
            Debug.LogError("find no image!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            isStartTimer = true;
        }

#if NO_ERROR_STEP
        if (isStartTimer)
        {
            timer += Time.deltaTime;
            coolDownImage.fillAmount = (coolingTime - timer) / coolingTime;
            if (timer >= coolingTime)
            {
                timer = 0f;
                isStartTimer = false;
                coolDownImage.fillAmount = 0f;
            }
        }
#else
        if (isStartTimer)
        {
            // update() 只有一帧！
            do
            {
                timer += Time.deltaTime;
                coolDownImage.fillAmount = (coolingTime - timer) / coolingTime;
                Debug.Log(string.Format("coolDownImage.fillAmount = {0}", coolDownImage.fillAmount.ToString()));
            } while (timer <= coolingTime);
            coolDownImage.fillAmount = 0f;
            timer = 0f;
            isStartTimer = false;
        }
#endif
    }

    public void OnClick()
    {
        isStartTimer = true;
    }
}
