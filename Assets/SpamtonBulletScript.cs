using System.Collections;
using UnityEngine;

public sealed class SpamtonBulletScript : MonoBehaviour
{
    [SerializeField]
    private Sprite[] _sprites, _inOut;
    [SerializeField]
    private float _x;

    private NowsYourChanceToBeA _script;

    public bool Dangerous { get; private set; }
    public Vector3 Position { get; private set; }

    private void OnEnable()
    {
        StopAllCoroutines();
        _script = GetComponentInParent<NowsYourChanceToBeA>();
        StartCoroutine(Shoot());
    }

    private IEnumerator Shoot()
    {
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        Transform tr = sprite.transform;
        const float SPEED = 0.06f;

        sprite.gameObject.SetActive(false);
        Dangerous = false;
        yield return new WaitForSeconds(Random.Range(0f, 1f));
        sprite.gameObject.SetActive(true);

        while(true)
        {
            float bulletTime = Time.time;
            int i = 0;
            Position = new Vector3(_x, Random.Range(-0.088f, 0.088f), 0f);
            tr.localPosition = Position;
            foreach(Sprite s in _inOut)
            {
                sprite.sprite = s;
                yield return new WaitForSeconds(0.1f);
            }
            Dangerous = true;
            while(Time.time - bulletTime < 3f)
            {
                Sprite s = _sprites[i % _sprites.Length];
                sprite.sprite = s;
                float animTime = Time.time;
                while(Time.time - animTime < 0.2f)
                {
                    sprite.flipX = (_script.lp - Position).x > 0f;
                    Position += (_script.lp - Position).normalized * SPEED * Time.deltaTime;
                    tr.localPosition = Position;

                    yield return null;
                }
                i++;
            }
            Dangerous = false;
            sprite.flipX = false;
            for(int s = _inOut.Length - 1; s >= 0; s--)
            {
                sprite.sprite = _inOut[s];
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(Random.Range(0f, 0.2f));

            yield return null;
        }
    }
}
