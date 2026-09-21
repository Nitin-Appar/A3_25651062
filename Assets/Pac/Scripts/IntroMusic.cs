using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class IntroMusic : MonoBehaviour
{
    [SerializeField] private AudioClip intro;
    [SerializeField] private AudioClip normal;

    private IEnumerator Start()
    {
        AudioSource source = GetComponent<AudioSource>();
        if (intro == null || normal == null)
        {
            Debug.LogError("no track", this);
            yield break;
        }
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        source.clip = intro;
        source.Play();
        yield return new WaitForSecondsRealtime(Mathf.Min(3f, intro.length));
        source.Stop();
        source.clip = normal;
        source.loop = true;
        source.Play();
    }
}
