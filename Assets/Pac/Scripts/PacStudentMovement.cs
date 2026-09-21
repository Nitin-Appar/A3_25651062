using UnityEngine;


[RequireComponent(typeof(Animator), typeof(AudioSource))]
public sealed class PacStudentMovement : MonoBehaviour
{
    [SerializeField, Min(0.01f)]
    private float unitsPerSecond = 3f;

    [SerializeField]
    private AudioClip movingClip;

    private readonly Vector3[] corners =
    {
        new Vector3(1f, -1f, 0f),
        new Vector3(6f, -1f, 0f),
        new Vector3(6f, -5f, 0f),
        new Vector3(1f, -5f, 0f)
    };

    private readonly string[] states =
    {
        "WalkRight",
        "WalkDown",
        "WalkLeft",
        "WalkUp"
    };

    private Animator animator;
    private AudioSource movingAudio;
    private int segment;
    private float travelled;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movingAudio = GetComponent<AudioSource>();

        animator.SetBool("PreviewMode", false);
    }

    private void Start()
    {
        transform.position = corners[0];

        animator.Play(states[0], 0, 0f);

        movingAudio.clip = movingClip;
        movingAudio.playOnAwake = false;
        movingAudio.spatialBlend = 0f;
        movingAudio.loop = true;

        if (movingClip != null)
        {
            movingAudio.Play();
        }
        else
        {
            Debug.LogError("no movement", this);
        }
    }

    private void Update()
    {
        travelled += Mathf.Max(0.01f, unitsPerSecond) * Time.deltaTime;

        float length = Vector3.Distance(
            corners[segment],
            corners[(segment + 1) % 4]
        );

        while (travelled >= length)
        {
            travelled -= length;
            segment = (segment + 1) % 4;

            animator.Play(states[segment], 0, 0f);

            length = Vector3.Distance(
                corners[segment],
                corners[(segment + 1) % 4]
            );
        }

        transform.position = Vector3.Lerp(
            corners[segment],
            corners[(segment + 1) % 4],
            travelled / length
        );
    }

    private void OnDisable()
    {
        if (movingAudio != null)
        {
            movingAudio.Stop();
        }
    }

    private void OnEnable()
    {
        if (movingAudio != null && movingAudio.clip != null)
        {
            movingAudio.Play();
        }
    }
}