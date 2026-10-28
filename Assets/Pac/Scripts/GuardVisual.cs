using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class GuardVisual : MonoBehaviour
{
    public GameManager game;
    SpriteRenderer ring;

    void Awake()
    {
        ring = GetComponent<SpriteRenderer>();
        ring.enabled = false;
    }

    void LateUpdate()
    {
        ring.enabled = game != null && game.IsPlaying && game.GuardRemaining > 0f;
    }
}
