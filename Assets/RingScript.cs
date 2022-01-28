using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class RingScript : MonoBehaviour
{
    private void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(Spin());
    }

    private IEnumerator Spin()
    {
        Vector3 lea = Vector3.zero;
        float offset = Random.Range(0f, 1000f);

        List<Transform> children = new List<Transform>();

        for(int i = 0; i < transform.childCount; i++)
            children.Add(transform.GetChild(i));

        while(true)
        {
            float time = Time.time * 120f + offset;
            lea.z = time;
            Vector3 ls = Vector3.one * Mathf.LerpUnclamped(1f, 1.1f, Mathf.Sin(time * Mathf.PI / 128f));
            Vector3 ls2 = Vector3.one / Mathf.LerpUnclamped(1f, 1.1f, Mathf.Sin(time * Mathf.PI / 128f));
            transform.localEulerAngles = lea;
            transform.localScale = ls;
            foreach(Transform child in children)
            {
                child.localEulerAngles = -lea;
                child.localScale = ls2;
            }
           
            yield return null;
        }
    }
}
