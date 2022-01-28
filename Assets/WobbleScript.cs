using System.Collections;
using UnityEngine;

public class WobbleScript : MonoBehaviour
{
    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(Wobble());
    }

    private IEnumerator Wobble()
    {
        Vector3 lea = Vector3.zero;
        float offset = Random.Range(0f, 1000f);
        float scale = Random.Range(0.98f, 1.02f);
        while(true)
        {
            float time = Time.time * scale + offset;
            lea.z = 15f * Mathf.Sin(5f * time + Mathf.Lerp(0.0f, 1.25f, Mathf.Pow((Mathf.Sin(2f * time) + 1f) / 2f, 16f)) * Mathf.Sin(100f * time));
            transform.localEulerAngles = lea;
            yield return null;
        }
    }
}
