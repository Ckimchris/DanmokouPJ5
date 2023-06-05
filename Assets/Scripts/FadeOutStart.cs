using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOutStart : MonoBehaviour
{
    public SpriteRenderer sprite;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FlashThenFade());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator FlashThenFade()
    {

        float time = 0;
        Color startValue = sprite.color;
        Color endValue = new Color(startValue.r, startValue.g, startValue.b, 0);

        for(int i = 0; i < 3; i++)
        {
            while (time < 0.5f)
            {
                sprite.color = Color.Lerp(new Color(startValue.r, startValue.g, startValue.b, startValue.a), new Color(startValue.r, startValue.g, startValue.b, 1), time / 0.5f);
                time += Time.deltaTime;
                yield return null;

            }
            time = 0;

            while (time < 0.5f)
            {
                sprite.color = Color.Lerp(new Color(startValue.r, startValue.g, startValue.b, 1f), new Color(startValue.r, startValue.g, startValue.b,startValue.a), time / 0.5f);
                time += Time.deltaTime;
                yield return null;

            }
            time = 0;
        }

        sprite.color = startValue;

        time = 0;

        while (time < 0.5f)
        {
            sprite.color = Color.Lerp(startValue, endValue, time / 0.5f);
            time += Time.deltaTime;
            yield return null;
        }
        sprite.color = endValue;
    }
}
