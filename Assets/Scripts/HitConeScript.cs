using UnityEngine;

public class HitConeScript : MonoBehaviour
{
    const float Lifetime = 0.75f;

    float sinceAlive;
    Color baseColor;
    Vector3 baseScale;
    float maxScale;

    public void Start()
	{
        float roll = Random.value * 360;
        Quaternion spreadRotation =
            Quaternion.Euler(0, 0, roll) *
            Quaternion.Euler(Random.value * 60, 0, 0) *
            Quaternion.Euler(0, 0, -roll);

        baseColor = GetComponentInChildren<Renderer>().material.GetColor("_TintColor");
        transform.rotation = transform.rotation * Quaternion.Euler(90, 0, 0) * spreadRotation;
	    baseScale = transform.localScale;
	    baseScale = new Vector3(baseScale.x, baseScale.y * Random.Range(0.5f, 1), baseScale.z);
	    maxScale = Random.Range(0.25f, 2);
	}

    public void Update()
	{
	    sinceAlive += Time.deltaTime;
	    var step = Easing.EaseOut(sinceAlive / Lifetime, EasingType.Cubic);

        transform.localScale = new Vector3(baseScale.x, baseScale.y * (1 + step * maxScale), baseScale.z);

        GetComponentInChildren<Renderer>().material.SetColor("_TintColor", new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(1 - step)));
        if (step >= 1)
            Destroy(gameObject);
	}
}
