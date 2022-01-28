using UnityEngine;

[UnityEditor.CustomEditor(typeof(RingScript))]
[UnityEditor.CanEditMultipleObjects]
public class RingEditor : UnityEditor.Editor
{
    private int count = 8;

    public override void OnInspectorGUI()
    {
        count = UnityEditor.EditorGUILayout.IntSlider(count, 8, 32);

        if(GUILayout.Button("Generate"))
        {
            foreach(RingScript t in targets)
                Generate(t);
        }
    }

    private void Generate(RingScript t)
    {
        GameObject obj = t.transform.GetChild(0).gameObject;
        for(int i = 1; i < t.transform.childCount; i++)
            DestroyImmediate(t.transform.GetChild(i).gameObject);

        float mag = obj.transform.localPosition.magnitude;
        obj.transform.localPosition = new Vector3(mag, 0f, 0f);
        for(int i = 1; i < count; i++)
        {
            GameObject newObj = Instantiate(obj, t.transform);
            newObj.transform.localPosition = new Vector3(mag * Mathf.Cos((2f * Mathf.PI / count) * i), mag * Mathf.Sin((2f * Mathf.PI / count) * i), 0f);
        }
    }
}