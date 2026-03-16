using UnityEngine;
using System;
using System.Collections;
using UnityEngine.InputSystem;
using DG.Tweening;

[RequireComponent(typeof(PlayerMovement))]
public class CanGrab : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;
    [SerializeField] private Transform doorGrabPoint;
    [SerializeField] private float maxGrabDistance = 3.5f;
    [SerializeField] public Transform toolGrabPoint;
    [SerializeField] private float toolMaxGrabDistance = 1.8f;
    [SerializeField] private LayerMask grabbableMask = -1;
    [SerializeField] private LayerMask collisionReleaseMask = -1;

    [Header("UI Interaction Indicator")]
    [SerializeField] private GameObject indicatorParentObject;
    [SerializeField] private Transform indicatorScalablePart;
    [SerializeField] private float indicatorAnimationTime = 0.2f;
    [SerializeField] private Ease indicatorEase = Ease.OutBack;

    [Header("Mineral Physics")]
    [SerializeField] private float mineralHoldDistance = 2.5f;
    [SerializeField] private float mineralPullForce = 38f;
    [SerializeField] private float mineralDrag = 10f;
    [SerializeField] private float mineralAngularDrag = 15f;
    [SerializeField] private float mineralMaxVelocity = 12f;
    [SerializeField] private float mineralBreakDistance = 6f;
    [SerializeField] private float pullSpeed = 20f;
    [SerializeField] private bool toolsHaveGravity = false;
    [SerializeField] private bool mineralsHaveGravity = true;
    [SerializeField] private Camera cam;

    private Rigidbody heldRb;
    private Transform heldTransform;
    private GrabbableItem heldItem;
    private Transform activePoint;
    private Vector3 lockedOffset;
    private Quaternion lockedRotation;
    private bool isPulling = false;
    private bool wasTriggerBeforeGrab = false;
    private bool wasKinematicBeforeGrab = false;
    private ConfigurableJoint mineralJoint;
    private float originalDrag;
    private float originalAngularDrag;
    private RigidbodyInterpolation originalInterpolation;
    private bool isSnapping = false;
    private bool _isFocused = false;
    private bool _isLookingAtGrabbable = false;
    private GrabbableItem _currentLookedItem;
    private Tweener _indicatorTweener;
    private CameraController.ControlMode _currentMode = CameraController.ControlMode.UI;

    public static Action<CanGrab, Rigidbody> OnGrabbed;
    public static Action<CanGrab, Rigidbody> OnReleased;
    public static CanGrab Instance { get; private set; }
    public float LastReleaseTime { get; private set; } = -999f;

    private void Awake()
    {
        Instance = this;
        if (grabPoint == null) grabPoint = transform;
        if (toolGrabPoint == null) toolGrabPoint = grabPoint;
        if (doorGrabPoint == null) doorGrabPoint = grabPoint;

        var player = GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.OnFocusStateChanged += OnFocusStateChanged;
        }

        InitializeIndicator();
    }

    private void InitializeIndicator()
    {
        if (indicatorScalablePart != null)
        {
            indicatorScalablePart.localScale = Vector3.zero;
        }

        if (indicatorParentObject != null)
        {
            indicatorParentObject.SetActive(true);
            StartCoroutine(DelayedIndicatorSetup());
        }
    }

    private IEnumerator DelayedIndicatorSetup()
    {
        yield return null;

        var cameraController = CameraController.Instance;
        if (cameraController != null)
        {
            _currentMode = cameraController.currentMode;
            UpdateIndicatorActiveState(_currentMode);
        }
    }

    private void OnFocusStateChanged(bool focused)
    {
        _isFocused = focused;
        if (focused)
        {
            if (IsHoldingObject())
            {
                ForceRelease();
            }
        }
    }

    public void OnCameraModeChanged(CameraController.ControlMode mode)
    {
        _currentMode = mode;
        UpdateIndicatorActiveState(mode);
    }

    private void UpdateIndicatorActiveState(CameraController.ControlMode mode)
    {
        if (indicatorParentObject == null) return;

        bool shouldBeActive = (mode == CameraController.ControlMode.FPS);

        if (shouldBeActive != indicatorParentObject.activeSelf)
        {
            indicatorParentObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
            {
                _isLookingAtGrabbable = false;
                _currentLookedItem = null;
                if (indicatorScalablePart != null)
                {
                    indicatorScalablePart.localScale = Vector3.zero;
                }
            }
            else
            {
                _isLookingAtGrabbable = false;
                _currentLookedItem = null;
            }
        }
    }

    private void Update()
    {
        HandleInteractionIndicator();
    }

    private void HandleInteractionIndicator()
    {
        if (indicatorParentObject == null || !indicatorParentObject.activeSelf || indicatorScalablePart == null)
            return;

        if (_isFocused)
            return;

        if (IsHoldingObject())
        {
            if (_isLookingAtGrabbable)
            {
                _isLookingAtGrabbable = false;
                _currentLookedItem = null;
                AnimateIndicator(false);
            }
            return;
        }

        GrabbableItem lookedItem = null;
        bool canGrab = CheckLookingAtGrabbable(out lookedItem);

        if (canGrab && lookedItem != null)
        {
            if (!_isLookingAtGrabbable || _currentLookedItem != lookedItem)
            {
                _isLookingAtGrabbable = true;
                _currentLookedItem = lookedItem;
                AnimateIndicator(true);
            }
        }
        else if (_isLookingAtGrabbable)
        {
            _isLookingAtGrabbable = false;
            _currentLookedItem = null;
            AnimateIndicator(false);
        }
    }

    private bool CheckLookingAtGrabbable(out GrabbableItem item)
    {
        item = null;

        if (cam == null)
            return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabbableMask))
        {
            item = hit.collider.GetComponent<GrabbableItem>();
            if (item != null)
            {
                var rb = item.GetComponent<Rigidbody>() ?? hit.collider.attachedRigidbody;
                if (rb != null)
                {
                    if (item.ItemType == GrabbableType.Tool)
                    {
                        float dist = Vector3.Distance(cam.transform.position, hit.point);
                        return dist <= toolMaxGrabDistance;
                    }
                    return true;
                }
            }
        }

        return false;
    }

    private void AnimateIndicator(bool show)
    {
        if (indicatorScalablePart == null) return;

        if (_indicatorTweener != null && _indicatorTweener.IsActive())
        {
            _indicatorTweener.Kill();
        }

        Vector3 targetScale = show ? Vector3.one : Vector3.zero;
        _indicatorTweener = indicatorScalablePart.DOScale(targetScale, indicatorAnimationTime)
            .SetEase(indicatorEase)
            .SetUpdate(true);
    }

    public bool IsHoldingObject() => heldRb != null;
    public GrabbableItem GetGrabbedItem() => heldItem;
    public bool IsHoldingTool() => heldItem != null && heldItem.ItemType == GrabbableType.Tool;

    public void StartSnappingToZone() => isSnapping = true;
    public void EndSnappingToZoneComplete() => isSnapping = false;

    public void HandlePhysicalInteract(bool pressed, bool held)
    {
        if (_isFocused) return;
        if (isSnapping) return;
        if (held && heldRb == null) TryGrab();
        else if (!held && heldRb != null) Release();
    }

    public void ForceRelease() => Release(true);

    private void TryGrab()
    {
        if (TryGrabAt(toolGrabPoint, toolMaxGrabDistance, out var item) && item.ItemType == GrabbableType.Tool)
        {
            GrabOldStyle(item, toolGrabPoint);
            return;
        }

        if (TryGrabAt(doorGrabPoint, maxGrabDistance, out item) && item.CompareTag("Door"))
        {
            GrabOldStyle(item, doorGrabPoint);
            return;
        }

        if (TryGrabAt(grabPoint, maxGrabDistance, out item))
        {
            if (item.ItemType == GrabbableType.Mineral)
            {
                var mineralData = item.GetComponentInChildren<MineralData>();
                if (mineralData != null)
                {
                    var vehicleSnapZone = TutorialManager.Instance?.vehicleMineralSnapZone;
                    if (vehicleSnapZone != null && item.GetComponentInParent<SnapZone>() == vehicleSnapZone)
                    {
                        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanGrabAnyMineralFromVehicle())
                        {
                            return;
                        }

                        if (mineralData.isLastInTutorialQueue && TutorialManager.Instance != null && !TutorialManager.Instance.CanGrabLastTutorialMineral())
                        {
                            return;
                        }
                    }
                }
            }
            if (item.ItemType == GrabbableType.Mineral)
                GrabMineralVotV(item);
            else
                GrabOldStyle(item, grabPoint);
        }
    }

    private bool TryGrabAt(Transform point, float dist, out GrabbableItem item)
    {
        item = null;
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, dist, grabbableMask))
            return false;

        item = hit.collider.GetComponent<GrabbableItem>();
        if (item == null)
            return false;

        if (point == toolGrabPoint && item.ItemType != GrabbableType.Tool)
            return false;
        if (point == grabPoint && item.ItemType == GrabbableType.Tool)
            return false;

        var rb = item.GetComponent<Rigidbody>() ?? hit.collider.attachedRigidbody;
        return rb != null;
    }

    private void GrabMineralVotV(GrabbableItem item)
    {
        var rb = item.GetComponent<Rigidbody>();
        var snapZone = item.GetComponentInParent<SnapZone>();
        snapZone?.OnItemGrabbedFromZone(item);
        heldRb = rb;
        heldTransform = rb.transform;
        heldItem = item;
        activePoint = grabPoint;
        originalInterpolation = rb.interpolation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
        rb.linearDamping = mineralDrag;
        rb.angularDamping = mineralAngularDrag;
        mineralJoint = rb.gameObject.AddComponent<ConfigurableJoint>();
        mineralJoint.connectedBody = null;
        mineralJoint.autoConfigureConnectedAnchor = false;
        mineralJoint.connectedAnchor = cam.transform.position + cam.transform.forward * mineralHoldDistance;
        mineralJoint.xMotion = mineralJoint.yMotion = mineralJoint.zMotion = ConfigurableJointMotion.Limited;
        mineralJoint.angularXMotion = mineralJoint.angularYMotion = mineralJoint.angularZMotion = ConfigurableJointMotion.Locked;
        var drive = new JointDrive { positionSpring = mineralPullForce * 100f, positionDamper = mineralPullForce * 10f, maximumForce = 1e8f };
        mineralJoint.xDrive = mineralJoint.yDrive = mineralJoint.zDrive = drive;
        rb.useGravity = mineralsHaveGravity;
        rb.isKinematic = false;
        OnGrabbed?.Invoke(this, heldRb);
        var col = item.GetComponent<Collider>();
        if (col) col.isTrigger = false;

        AnimateIndicator(false);
        _isLookingAtGrabbable = false;
    }

    private void GrabOldStyle(GrabbableItem item, Transform point)
    {
        var rb = item.GetComponent<Rigidbody>() ?? item.GetComponentInParent<Rigidbody>();
        var snapZone = item.GetComponentInParent<SnapZone>();
        snapZone?.OnItemGrabbedFromZone(item);
        heldRb = rb;
        heldTransform = rb.transform;
        heldItem = item;
        activePoint = point;
        wasKinematicBeforeGrab = rb.isKinematic;
        bool isDoor = item.CompareTag("Door");
        if (!isDoor && (item.ItemType == GrabbableType.Tool || item.ItemType == GrabbableType.Mineral))
        {
            var col = item.GetComponent<Collider>();
            if (col) { wasTriggerBeforeGrab = col.isTrigger; col.isTrigger = true; }
        }
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        OnGrabbed?.Invoke(this, heldRb);
        isPulling = true;

        AnimateIndicator(false);
        _isLookingAtGrabbable = false;
    }

    private void Release(bool force = false)
    {
        if (heldRb == null) return;
        OnReleased?.Invoke(this, heldRb);
        LastReleaseTime = Time.time;
        if (heldItem != null && heldItem.ItemType == GrabbableType.Mineral && mineralJoint != null)
        {
            heldRb.interpolation = originalInterpolation;
            heldRb.linearDamping = originalDrag;
            heldRb.angularDamping = originalAngularDrag;
            Destroy(mineralJoint);
            mineralJoint = null;
        }
        if (!force && !isSnapping && heldItem != null)
        {
            bool isDoor = heldItem.CompareTag("Door");
            var col = heldItem.GetComponent<Collider>();
            if (col && !isDoor)
            {
                if (heldItem.ItemType == GrabbableType.Mineral)
                    col.isTrigger = false;
                else
                    col.isTrigger = wasTriggerBeforeGrab;
            }
            bool hasGravity = heldItem.ItemType == GrabbableType.Tool ? toolsHaveGravity : mineralsHaveGravity;
            heldRb.useGravity = hasGravity;
            heldRb.isKinematic = wasKinematicBeforeGrab;
            heldRb.linearVelocity *= 0.3f;
            heldRb.angularVelocity *= 0.3f;
        }
        heldRb = null;
        heldTransform = null;
        heldItem = null;
        activePoint = null;
        isPulling = false;
    }

    private void FixedUpdate()
    {
        if (heldRb == null || heldItem == null || isSnapping || activePoint == null) return;
        if (heldItem.ItemType == GrabbableType.Mineral && mineralJoint != null)
        {
            mineralJoint.connectedAnchor = grabPoint.position;
            if (Vector3.Distance(heldTransform.position, grabPoint.position) > mineralBreakDistance)
                Release();
            if (heldRb.linearVelocity.sqrMagnitude > mineralMaxVelocity * mineralMaxVelocity)
                heldRb.linearVelocity = heldRb.linearVelocity.normalized * mineralMaxVelocity;
        }
        else if (isPulling)
        {
            Vector3 dir = activePoint.position - heldTransform.position;
            if (dir.sqrMagnitude < 0.02f)
            {
                isPulling = false;
                lockedOffset = activePoint.InverseTransformPoint(heldTransform.position);
                lockedRotation = Quaternion.Inverse(activePoint.rotation) * heldTransform.rotation;
                heldRb.linearVelocity = Vector3.zero;
            }
            else
            {
                heldRb.linearVelocity = dir.normalized * pullSpeed;
            }
        }
        else
        {
            heldRb.linearVelocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (heldRb == null || isPulling || isSnapping || activePoint == null) return;
        if (heldItem.ItemType == GrabbableType.Mineral) return;
        heldTransform.position = activePoint.TransformPoint(lockedOffset);
        heldTransform.rotation = activePoint.rotation * lockedRotation;
    }

    private void OnDestroy()
    {
        if (_indicatorTweener != null && _indicatorTweener.IsActive())
        {
            _indicatorTweener.Kill();
        }
    }
}