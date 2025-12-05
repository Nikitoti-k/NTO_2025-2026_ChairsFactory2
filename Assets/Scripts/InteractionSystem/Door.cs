using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
public class Door : GrabbableItem
{
    [Header("Настройки двери")]
    [SerializeField] private float maxAngle = 110f;
    [SerializeField] private float closeSpring = 0f;
    [SerializeField] private float closeDamper = 0f;

    [Header("Скрип двери")]
    [SerializeField] private AudioClip doorCreak;           // один клип со скрипом (1–4 сек)
    [SerializeField] [Range(1f, 100f)] private float minVelocity = 20f;  // порог скорости
    [SerializeField] [Range(0.3f, 1.5f)] private float basePitch = 1f;
    [SerializeField] [Range(0f, 0.3f)] private float randomPitch = 0.15f;

    private HingeJoint hinge;
    private AudioSource creakSource;
    private float lastAngle;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        var rb = GetComponent<Rigidbody>();

        // Настройка пружины закрытия
        var limits = hinge.limits;
        limits.min = -maxAngle;
        limits.max = maxAngle;
        hinge.limits = limits;
        hinge.useLimits = true;
        hinge.useSpring = true;
        hinge.spring = new JointSpring
        {
            spring = closeSpring,
            damper = closeDamper,
            targetPosition = 0
        };

        // Один AudioSource прямо на двери — самый простой и надёжный способ
        creakSource = gameObject.AddComponent<AudioSource>();
        creakSource.clip = doorCreak;
        creakSource.loop = true;
        creakSource.playOnAwake = false;
        creakSource.spatialBlend = 1f;     // 3D-звук
        creakSource.volume = 0.7f;
        creakSource.dopplerLevel = 0f;

        lastAngle = hinge.angle;
    }

    private void Update()
    {
        float velocity = Mathf.Abs(hinge.angle - lastAngle) / Time.deltaTime;
        lastAngle = hinge.angle;

        if (velocity > minVelocity)
        {
            if (!creakSource.isPlaying)
            {
                creakSource.pitch = basePitch + Random.Range(-randomPitch, randomPitch);
                creakSource.Play();
            }
        }
        else
        {
            if (creakSource.isPlaying)
                creakSource.Stop();
        }
    }

    public void OnGrabbed() => hinge.spring = new JointSpring { damper = 5f };
    public void OnReleased() => hinge.spring = new JointSpring { spring = closeSpring, damper = closeDamper };
}