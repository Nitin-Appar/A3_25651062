using UnityEngine;

public static class GameEffects
{
    public static void Burst(ParticleSystem prefab, Vector3 position)
    {
        if(!prefab)return;
        var effect=Object.Instantiate(prefab,position,Quaternion.identity);
        effect.Play();
        Object.Destroy(effect.gameObject,3);
    }
}
