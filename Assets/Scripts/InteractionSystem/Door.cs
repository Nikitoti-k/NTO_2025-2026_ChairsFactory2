using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(HingeJoint))]
public class Door : GrabbableItem
{
    [Header("Настройки двери")]
    [SerializeField] private float maxAngle = 110f;
    [SerializeField] private float closeSpring = 80f;
    [SerializeField] private float closeDamper = 12f;

    private HingeJoint _hinge;
    private JointSpring _closedSpring;
    private JointSpring _freeSpring;
    private Rigidbody _rb;

    private void Awake()
    {
        _hinge = GetComponent<HingeJoint>();
        _rb = GetComponent<Rigidbody>(); // из GrabbableItem

        // Пружинка закрытия
        _closedSpring = new JointSpring
        {
            spring = closeSpring,
            damper = closeDamper,
            targetPosition = 0f
        };

        // Свободное состояние (когда держим)
        _freeSpring = new JointSpring
        {
            spring = 0f,
            damper = 5f,
            targetPosition = 0f
        };

        // Ограничение угла
        var limits = _hinge.limits;
        limits.min = -maxAngle;
        limits.max = maxAngle;
        _hinge.limits = limits;
        _hinge.useLimits = true;

        // По умолчанию — закрывается
        _hinge.useSpring = true;
        _hinge.spring = _closedSpring;
    }

    // Вызывается из CanGrab при захвате
    public void OnGrabbed()
    {
        _hinge.useSpring = true;
        _hinge.spring = _freeSpring; // убираем пружину закрытия
    }

    // Вызывается при отпускании
    public void OnReleased()
    {
        _hinge.spring = _closedSpring; // возвращаем пружину
    }
}