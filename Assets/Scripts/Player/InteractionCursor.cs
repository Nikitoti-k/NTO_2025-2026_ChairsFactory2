using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InteractionCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform cursorRect;
    [SerializeField] private Image cursorImage;
    [SerializeField] private CameraController cameraController;

    [Header("Animation Settings")]
    [SerializeField] private float expandDuration = 0.3f;
    [SerializeField] private float shrinkDuration = 0.2f;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseIntensity = 0.1f;

    [Header("Interaction Settings")]
    [SerializeField] private LayerMask interactableLayer = -1;
    [SerializeField] private float interactionDistance = 3.5f;
    [SerializeField] private float toolInteractionDistance = 1.8f;

    [Header("Visuals")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color interactableColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private Color toolColor = new Color(1f, 0.6f, 0.2f, 1f);
    [SerializeField] private Vector3 expandedScale = Vector3.one;
    [SerializeField] private Vector3 normalScale = new Vector3(0.7f, 0.7f, 0.7f);

    private Vector3 _initialScale;
    private bool _isExpanded = false;
    private bool _isPulsing = false;
    private Tween _scaleTween;
    private Tween _colorTween;
    private Camera _mainCamera;
    private GrabbableType _currentTargetType;

    private void Awake()
    {
        if (cursorRect == null) cursorRect = GetComponent<RectTransform>();
        if (cursorImage == null) cursorImage = GetComponent<Image>();
        if (cameraController == null) cameraController = CameraController.Instance;

        _initialScale = cursorRect.localScale;
        _mainCamera = Camera.main;

        cursorRect.localScale = normalScale;
        cursorImage.color = defaultColor;
    }

    private void OnEnable()
    {
        UpdateCursorState();
    }

    private void OnDisable()
    {
        if (_scaleTween != null && _scaleTween.IsActive())
        {
            _scaleTween.Kill();
            _scaleTween = null;
        }

        if (_colorTween != null && _colorTween.IsActive())
        {
            _colorTween.Kill();
            _colorTween = null;
        }

        _isExpanded = false;
        _isPulsing = false;
    }

    private void Update()
    {
        if (cameraController == null)
        {
            cameraController = CameraController.Instance;
            return;
        }

        UpdateCursorState();

        if (cameraController.currentMode == CameraController.ControlMode.FPS)
        {
            UpdateFPSInteraction();
        }
        else
        {
            ShrinkCursor();
        }
    }

    private void UpdateCursorState()
    {
        bool shouldBeActive = cameraController != null &&
                             cameraController.currentMode == CameraController.ControlMode.FPS;

        if (cursorImage.gameObject.activeSelf != shouldBeActive)
        {
            cursorImage.gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
            {
                ShrinkCursor();
            }
        }
    }

    private void UpdateFPSInteraction()
    {
        if (_mainCamera == null) return;

        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
        RaycastHit hit;

        float maxDistance = interactionDistance;
        GrabbableType detectedType = GrabbableType.Junk;

        if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
        {
            var grabbable = hit.collider.GetComponent<GrabbableItem>();
            if (grabbable != null)
            {
                detectedType = grabbable.ItemType;

                if (detectedType == GrabbableType.Tool)
                {
                    maxDistance = toolInteractionDistance;
                    if (hit.distance <= maxDistance)
                    {
                        ExpandCursor(detectedType);
                        return;
                    }
                }
                else if (hit.distance <= maxDistance)
                {
                    ExpandCursor(detectedType);
                    return;
                }
            }

            var button = hit.collider.GetComponent<Button>();
            if (button != null && hit.distance <= interactionDistance)
            {
                ExpandCursor(GrabbableType.Junk);
                return;
            }
        }

        ShrinkCursor();
    }

    private void ExpandCursor(GrabbableType targetType)
    {
        if (_currentTargetType != targetType)
        {
            UpdateCursorColor(targetType);
            _currentTargetType = targetType;
        }

        if (!_isExpanded)
        {
            _isExpanded = true;

            if (_scaleTween != null && _scaleTween.IsActive())
            {
                _scaleTween.Kill();
            }

            _scaleTween = cursorRect.DOScale(expandedScale, expandDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => StartPulse());
        }
    }

    private void ShrinkCursor()
    {
        if (_isExpanded)
        {
            _isExpanded = false;
            StopPulse();

            if (_scaleTween != null && _scaleTween.IsActive())
            {
                _scaleTween.Kill();
            }

            _scaleTween = cursorRect.DOScale(normalScale, shrinkDuration)
                .SetEase(Ease.InOutQuad);
        }
    }

    private void UpdateCursorColor(GrabbableType targetType)
    {
        Color targetColor = defaultColor;

        switch (targetType)
        {
            case GrabbableType.Tool:
                targetColor = toolColor;
                break;
           
           
        }

        if (_colorTween != null && _colorTween.IsActive())
        {
            _colorTween.Kill();
        }

        _colorTween = cursorImage.DOColor(targetColor, expandDuration * 0.5f)
            .SetEase(Ease.InOutQuad);
    }

    private void StartPulse()
    {
        if (_isPulsing) return;

        _isPulsing = true;

        cursorRect.DOScale(expandedScale * (1f + pulseIntensity), pulseSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetId("CursorPulse");
    }

    private void StopPulse()
    {
        if (!_isPulsing) return;

        _isPulsing = false;
        DOTween.Kill("CursorPulse");

        cursorRect.localScale = expandedScale;
    }

    public void ForceShrink()
    {
        ShrinkCursor();

        if (_scaleTween != null && _scaleTween.IsActive())
        {
            _scaleTween.Kill();
        }

        cursorRect.localScale = normalScale;
        cursorImage.color = defaultColor;
        _isExpanded = false;
        _currentTargetType = GrabbableType.Junk;
        StopPulse();
    }
}