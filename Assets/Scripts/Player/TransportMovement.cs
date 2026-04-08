using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TransportMovement : SaveableObject, IControllable
{
    [Header("Скорость")]
    [SerializeField] private float forwardSpeed = 16f;
    [SerializeField] private float reverseSpeed = 10f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float brakeDeceleration = 30f;

    [Header("Поворот")]
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float lowSpeedTurnBonus = 3f;
    [SerializeField, Range(0.05f, 0.5f)] private float turnSmoothTime = 0.12f;
    [SerializeField] private float minTurnSpeed = 1.5f;

    [Header("Проходимость")]
    [SerializeField] private float raycastDistance = 3f;
    [SerializeField] private float slopeAssist = 1.4f;

    [Header("Стабилизация")]
    [SerializeField] private float uprightTorque = 500f;
    [SerializeField] private float uprightDamping = 80f;

    [Header("Звук мотора")]
    [SerializeField] private string engineIdleKey = "engine_idle";
    [SerializeField] private string engineRevKey = "engine_rev";
    [SerializeField] private string engineStartKey = "engine_start";
    [SerializeField] private float maxEnginePitch = 1.8f;
    [SerializeField] private float engineResponseSpeed = 2f;
    [SerializeField] private Transform engineSoundPosition;

    [Header("Посадка")]
    public Transform seatTransform;
    public Vector3 fallbackMountOffset = new Vector3(0f, 1.2f, 0.6f);

    private Rigidbody _rb;
    private Vector2 _input;
    private float _currentTurn;
    private float _targetForwardSpeed;

    private AudioSource _engineSource;
    private float _currentEnginePitch = 0.8f;
    private float _normalizedVolume;
    private bool _isEngineRunning;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.freezeRotation = false;
        _rb.angularDamping = 4f;
        _rb.centerOfMass = new Vector3(0f, -1.1f, 0.3f);
        _rb.solverVelocityIterations = 16;
        _rb.solverIterations = 10;
        CreateEngineAudioSource();
    }

    public void HandleMovement(Vector2 input) => _input = input;
    public void HandleInteract(bool pressed) {}
    public void HandleTransportInteract(bool pressed) { if (pressed) Dismount(); }
    public void HandlePhysicalInteract(bool pressed, bool held) { }
    public void HandleFlare(bool pressed) { }

    private void FixedUpdate()
    {
        if (InputRouter.Instance?.CurrentController != this)
        {
            StopEngine();
            return;
        }

        if (!_isEngineRunning && _input.sqrMagnitude < 0.01f)
            StartEngineIfNeeded();

        Drive();
        UpdateEngineSound();
    }

    private void Drive()
    {
        float forwardInput = _input.y;
        float turnInput = _input.x;

        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        float currentForwardSpeed = Vector3.Dot(flatVel, transform.forward);

        float desiredSpeed =
            forwardInput > 0.01f ? forwardSpeed :
            forwardInput < -0.01f ? -reverseSpeed : 0f;

        _targetForwardSpeed = Mathf.MoveTowards(
            _targetForwardSpeed,
            desiredSpeed,
            acceleration * Time.fixedDeltaTime);

        bool braking =
            Mathf.Abs(currentForwardSpeed) > 1f &&
            Mathf.Sign(currentForwardSpeed) != Mathf.Sign(forwardInput) &&
            forwardInput != 0f;

        float effectiveAccel = braking ? brakeDeceleration : acceleration;

        Vector3 comWorld = transform.TransformPoint(_rb.centerOfMass);
        Vector3 surfaceNormal =
            Physics.Raycast(comWorld, Vector3.down, out RaycastHit hit, raycastDistance)
            ? hit.normal
            : Vector3.up;

        Vector3 velOnPlane = Vector3.ProjectOnPlane(flatVel, surfaceNormal);
        Vector3 desiredVelOnPlane =
            Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized *
            _targetForwardSpeed;

        Vector3 velDelta = desiredVelOnPlane - velOnPlane;
        Vector3 force =
            Vector3.ClampMagnitude(velDelta / Time.fixedDeltaTime, effectiveAccel);

        _rb.AddForce(force, ForceMode.Acceleration);

        Vector3 slopeForward =
            Vector3.ProjectOnPlane(transform.forward, surfaceNormal).normalized;

        float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
        float slopeFactor = Mathf.Clamp01(slopeAngle / 45f);

        _rb.AddForce(
            slopeForward * _targetForwardSpeed * slopeAssist * slopeFactor,
            ForceMode.Acceleration);

        _currentTurn = Mathf.MoveTowards(
            _currentTurn,
            turnInput,
            Time.fixedDeltaTime / turnSmoothTime);

        float absSpeed = Mathf.Abs(currentForwardSpeed);

        if (absSpeed >= minTurnSpeed)
        {
            float speedFactor =
                Mathf.InverseLerp(minTurnSpeed, forwardSpeed * 0.6f, absSpeed);

            float turnBonus =
                Mathf.Lerp(lowSpeedTurnBonus, 1f, speedFactor);

            float rotation =
                _currentTurn * turnSpeed * turnBonus * Time.fixedDeltaTime;

            if (Mathf.Abs(rotation) > 0.001f)
                _rb.MoveRotation(
                    _rb.rotation * Quaternion.Euler(0f, rotation, 0f));
        }

        StabilizeUpright(surfaceNormal);
    }

    private void StabilizeUpright(Vector3 groundNormal)
    {
        Quaternion target =
            Quaternion.FromToRotation(transform.up, groundNormal) * _rb.rotation;

        Quaternion delta = target * Quaternion.Inverse(_rb.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;

        Vector3 torque =
            axis * angle * Mathf.Deg2Rad * uprightTorque
            - _rb.angularVelocity * uprightDamping;

        _rb.AddTorque(torque, ForceMode.Acceleration);
    }

    private void UpdateEngineSound()
    {
        if (!_isEngineRunning)
        {
            StartEngineIfNeeded();
            return;
        }

        if (_input.sqrMagnitude < 0.01f)
        {
            _currentEnginePitch =
                Mathf.Lerp(_currentEnginePitch, 0.8f,
                    Time.fixedDeltaTime * engineResponseSpeed);

            _normalizedVolume =
                Mathf.Lerp(_normalizedVolume, 0.6f,
                    Time.fixedDeltaTime * engineResponseSpeed);

            SwitchEngineClip(engineIdleKey);
        }
        else
        {
            float throttle = Mathf.Abs(_input.y);
            float speedFactor = Mathf.Abs(_targetForwardSpeed) / forwardSpeed;

            float targetPitch =
                Mathf.Lerp(0.8f, maxEnginePitch, (throttle + speedFactor) * 0.5f);

            _currentEnginePitch =
                Mathf.Lerp(_currentEnginePitch, targetPitch,
                    Time.fixedDeltaTime * engineResponseSpeed);

            _normalizedVolume =
                Mathf.Lerp(_normalizedVolume, 1f,
                    Time.fixedDeltaTime * engineResponseSpeed);

            SwitchEngineClip(engineRevKey);
        }

        UpdateVolumeFromManager();
        _engineSource.pitch = _currentEnginePitch;
    }

    private void StartEngineIfNeeded()
    {
        if (_isEngineRunning || _engineSource == null) return;

        _isEngineRunning = true;
        _normalizedVolume = 0.6f;
        _currentEnginePitch = 0.8f;

        AudioManager.Instance?.PlaySFX(
            engineStartKey, 1f, 1f, transform.position);

        SwitchEngineClip(engineIdleKey);
        _engineSource.Play();
    }

    private void StopEngine()
    {
        if (_engineSource != null)
        {
            _engineSource.Stop();
            _engineSource.clip = null;
        }

        _isEngineRunning = false;
        _normalizedVolume = 0f;
        _currentEnginePitch = 0.8f;
    }

    private void CreateEngineAudioSource()
    {
        GameObject go = new GameObject("EngineAudio");
        go.transform.SetParent(engineSoundPosition ? engineSoundPosition : transform);
        go.transform.localPosition = Vector3.zero;

        _engineSource = go.AddComponent<AudioSource>();
        _engineSource.loop = true;
        _engineSource.playOnAwake = false;
        _engineSource.spatialBlend = 0.7f;
        _engineSource.rolloffMode = AudioRolloffMode.Linear;
        _engineSource.maxDistance = 30f;
        _engineSource.volume = 0f;
    }

    private void UpdateVolumeFromManager()
    {
        if (AudioManager.Instance && _engineSource)
        {
            _engineSource.volume =
                _normalizedVolume *
                AudioManager.Instance.sfxVolume *
                AudioManager.Instance.masterVolume;
        }
    }

    private void SwitchEngineClip(string key)
    {
        if (AudioManager.Instance?.audioDatabase == null) return;

        if (AudioManager.Instance.audioDatabase.TryGetSound(key, out var sound)
            && _engineSource.clip != sound.clip)
        {
            _engineSource.clip = sound.clip;
            _engineSource.Play();
        }
    }

    private void Dismount()
    {
        StopEngine();

        var player = FindFirstObjectByType<PlayerMovement>();
        if (!player) return;

        var router = InputRouter.Instance;

        player.transform.SetParent(null);

        var prb = player.GetComponent<Rigidbody>();
        prb.isKinematic = false;
        prb.useGravity = true;
        prb.interpolation = RigidbodyInterpolation.Interpolate;

        var col = player.GetComponent<Collider>();
        col.enabled = true;
        col.isTrigger = false;

        player.transform.position =
            transform.position + transform.forward * 2.8f + Vector3.up * 1.5f;

        prb.linearVelocity = _rb.linearVelocity + Vector3.up * 5f;

        router?.SetController(player);
    }

    private void LateUpdate()
    {
        if (_engineSource && _engineSource.isPlaying)
            UpdateVolumeFromManager();
    }
}
