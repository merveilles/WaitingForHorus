using UnityEngine;
using System.Collections;

public class AutoDestructScript : MonoBehaviour 
{
    public float timeToLive = 1.0f;
    public float fadeOutTime = -1;

    void Start()
    {
        if (fadeOutTime == -1)
            fadeOutTime = timeToLive / 3;
    }

    void Update()
    {
        timeToLive -= Time.deltaTime;

        if (timeToLive < fadeOutTime)
        {
            var opacity = timeToLive / fadeOutTime;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r.material.HasProperty("_TintColor"))
                {
                    var color = r.material.GetColor("_TintColor");
                    r.material.SetColor("_TintColor", new Color(color.r, color.g, color.b, opacity));
                }
                else
                {
                    var color = r.material.color;
                    r.material.color = new Color(color.r, color.g, color.b, opacity);
                }
            }
        }

        if(timeToLive <= 0)
            Object.Destroy(gameObject);
    }
}
