using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Rock : MonoBehaviour
{
    public System.Action<Vector3, Quaternion, TurnManager.Team> OnRockStopped;
    public Renderer rockRenderer;

    public ParticleSystem leftParticles;
    public ParticleSystem rightParticles;
    public GameObject leftTrail;
    public GameObject rightTrail;
    public Color iceEffectColor = Color.white;

    private float stillTime = 0f;
    public float trailSpeedThreshold = 100f;
    public float stopDelay = 0.75f;
    private float shotCooldown = 0.2f;
    private float shotCooldownTimer = 0f;

    private Rigidbody rb;
    private Vector2 movementInput;
    private Vector3 mouseWorld;

    public bool inputEnabled = true;
    private bool hasShot = false;
    public bool crossedLine = false;
    public bool isRoundEnding = false;
    private bool effectsActive = false;
    public bool fragileIceEnabled = false;
    public bool lavaEnabled = false;

    public Camera mainCamera;
    public Camera shotCamera;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    private TurnManager.Team currentTeam;

    public float resetThreshold = -20f;

    private float power;
    public float minPower = 5f;
    public float maxPower = 25f;
    private float powerRate = 15f;
    private float curlInput = 0f;
    public float curlStrength = 2.5f;
    private float lockedCurl = 0f;
    public float aimCurveStrength = 30f;

    private bool wasHoldingForward = false;

    private Vector3 shotDirection;

    public Image powerBackground;
    public Image powerFill;
    public Image aimArrowImage;
    public Sprite arrowStraight;
    public Sprite arrowLeft;
    public Sprite arrowRight;
    public RectTransform aimArrowUI;

    public Camera cam;

    public GameObject SpawnedRockPrefab;
    private bool hasSpawnedRock = false;

    void Start()
    {
        inputEnabled = false;
        rb = GetComponent<Rigidbody>();

        bool particlesEnabled =
        PlayerPrefs.GetInt("ParticlesEnabled", 1) == 1;

        if (leftParticles != null)
            leftParticles.gameObject.SetActive(particlesEnabled);

        if (rightParticles != null)
            rightParticles.gameObject.SetActive(particlesEnabled);

        if (mainCamera != null) mainCamera.enabled = true;
        if (shotCamera != null) shotCamera.enabled = false;

        startingPosition = rb.position;
        startingRotation = transform.rotation;

        power = minPower;
    }

    public void OnMove(InputValue value)
    {
        if (!inputEnabled) return;

        movementInput = value.Get<Vector2>();

        curlInput = movementInput.x;

        UpdateAimArrowSprite();
    }

    void UpdateAimArrowSprite()
    {
        if (aimArrowImage == null) return;

        if (Mathf.Abs(curlInput) < 0.1f)
        {
            aimArrowImage.sprite = arrowStraight;
        }
        else if (curlInput > 0f)
        {
            aimArrowImage.sprite = arrowRight;
        }
        else
        {
            aimArrowImage.sprite = arrowLeft;
        }
    }

    public void SetTeam(TurnManager.Team team)
    {
        currentTeam = team;
    }

    public void SetTeamColor(Color color)
    {
        if (rockRenderer != null)
            rockRenderer.material.color = color;
    }

    public void SetIceEffectColor(Color color)
    {
        iceEffectColor = color;

        // Particle colors
        if (leftParticles != null)
        {
            var main = leftParticles.main;
            main.startColor = color;
        }

        if (rightParticles != null)
        {
            var main = rightParticles.main;
            main.startColor = color;
        }


        // Trail colors
        SetTrailColor(leftTrail, color);
        SetTrailColor(rightTrail, color); 
    }

    void SetTrailColor(GameObject trailObj, Color color)
    {
        if (trailObj == null) return;

        TrailRenderer trail = trailObj.GetComponent<TrailRenderer>();

        if (trail != null)
        {
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);

            Material mat = trail.material;

            if (mat != null)
            {
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
            }
        }
    }

    void Shoot()
    {
        hasShot = true;
        inputEnabled = false;

        lockedCurl = curlInput;

        // apply impulse force to launch rock forward
        rb.AddForce(shotDirection * power, ForceMode.Impulse);
        shotCooldownTimer = shotCooldown;

        // hide ui after shooting
        if (powerFill != null) powerFill.gameObject.SetActive(false);
        if (powerBackground != null) powerBackground.gameObject.SetActive(false);
    }

    void HandleStopDetection()
    {
        // prevent double-triggering and wait for cooldown
        if (!hasShot || hasSpawnedRock) return;
        if (shotCooldownTimer > 0f) return;

        // reset timer while rock is still moving
        if (IsMoving())
        {
            stillTime = 0f;
            return;
        }

        stillTime += Time.deltaTime;

        // trigger stop event after rock remains still for delay
        if (stillTime >= stopDelay)
        {
            hasSpawnedRock = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // notify TurnManager that rock has stopped
            OnRockStopped?.Invoke(transform.position, transform.rotation, currentTeam);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("FragileIce"))
        {
            rb.linearVelocity *= 0.99f;
        }

        if(other.CompareTag("Lava"))
        {
            rb.linearVelocity *= 0.97f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered with: " + other.name);

        if (other.CompareTag("Line"))
        {
            crossedLine = true;

            mainCamera.enabled = false;
            shotCamera.enabled = true;
        }
    }

    Vector3 GetMouseWorldPoint()
    {
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (groundPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return transform.position;
    }

    bool IsMoving()
    {
        float linear = rb.linearVelocity.magnitude;
        float angular = rb.angularVelocity.magnitude;

        return rb.linearVelocity.sqrMagnitude > 0.004f;
    }

    bool IsFastEnoughForEffects()
    {
        float speed = rb.linearVelocity.magnitude;

        return speed > trailSpeedThreshold;
    }

    void DisableIceEffects()
    {
        effectsActive = false;

        if (leftParticles != null)
            leftParticles.gameObject.SetActive(false);

        if (rightParticles != null)
            rightParticles.gameObject.SetActive(false);


        if(leftTrail != null)
        {
            TrailRenderer t = leftTrail.GetComponent<TrailRenderer>();
            if(t != null)
                t.Clear();

            leftTrail.SetActive(false);
        }


        if(rightTrail != null)
        {
            TrailRenderer t = rightTrail.GetComponent<TrailRenderer>();
            if(t != null)
                t.Clear();

            rightTrail.SetActive(false);
        }
    }
    
    void FixedUpdate()
    {
        ApplyCurl();
        HandleStopDetection();
    }

    void ApplyCurl()
    {
        if (!hasShot) return;

        // get horizontal movement only (ignore vertical motion)
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float speed = horizontalVel.magnitude;
        if (speed < 0.05f) return;

        // calculate sideways direction for curling effect
        Vector3 sideways = Vector3.Cross(Vector3.up, horizontalVel.normalized);

        // reduce curl effect as speed increases (more realistic motion)
        float speedFactor = Mathf.Clamp01(speed / maxPower);

        float curl = lockedCurl * curlStrength * (1f - speedFactor);

        // apply continuous sideways force for curling motion
        rb.AddForce(sideways * curl, ForceMode.Acceleration);

        if (horizontalVel.sqrMagnitude < 0.01f) return;
    }

    void Update()
    {
        Debug.Log("Rock Speed: " + rb.linearVelocity.magnitude);

        if (isRoundEnding)
        {
            if (aimArrowUI != null) aimArrowUI.gameObject.SetActive(false);
            if (powerFill != null) powerFill.gameObject.SetActive(false);
            if (powerBackground != null) powerBackground.gameObject.SetActive(false);
            return;
        }

        if (shotCooldownTimer > 0f)
        {
            shotCooldownTimer -= Time.deltaTime;
        }

        if (inputEnabled)
        {
            mouseWorld = GetMouseWorldPoint();

            Vector3 aimDir = mouseWorld - transform.position;
            aimDir.y = 0f;

            if (aimDir.sqrMagnitude > 0.001f)
                shotDirection = aimDir.normalized;

            if (movementInput.y > 0.1f)
                power += powerRate * Time.deltaTime;

            power = Mathf.Clamp(power, minPower, maxPower);

            float normalizedPower = Mathf.InverseLerp(minPower, maxPower, power);
            if (powerFill != null)
                powerFill.fillAmount = normalizedPower;

            bool isHoldingForward = movementInput.y > 0.1f;

            if (wasHoldingForward && !isHoldingForward && power > minPower)
                Shoot();

            wasHoldingForward = isHoldingForward;

            Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
            aimArrowUI.position = screenPos;

            Vector3 dirToMouse = mouseWorld - transform.position;
            dirToMouse.y = 0f;

            if (dirToMouse.sqrMagnitude > 0.01f)
            {
                float baseAngle = Mathf.Atan2(dirToMouse.z, dirToMouse.x) * Mathf.Rad2Deg;
                float curlOffset = curlInput * aimCurveStrength;
                aimArrowUI.rotation = Quaternion.Euler(0f, 0f, baseAngle + curlOffset);
            }

            if (!isRoundEnding)
                aimArrowUI.gameObject.SetActive(inputEnabled && !hasShot);

            if (rb.transform.position.y < resetThreshold)
            {
                ResetRock();

                mainCamera.enabled = true;
                shotCamera.enabled = false;

                inputEnabled = true;
                hasShot = false;

                power = minPower;

                if (powerFill != null) powerFill.gameObject.SetActive(true);
                if (powerBackground != null) powerBackground.gameObject.SetActive(true);
            }
        }
        if (hasShot)
        {
            if (IsFastEnoughForEffects())
            {
                if (!effectsActive)
                {
                    effectsActive = true;

                    leftParticles.gameObject.SetActive(true);
                    rightParticles.gameObject.SetActive(true);

                    leftTrail.SetActive(true);
                    rightTrail.SetActive(true);
                }
            }
            else
            {
                if (effectsActive)
                    DisableIceEffects();
            }
        }
    }

    public void ResetRock()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // reset all physics and gameplay state for next turn
        hasSpawnedRock = false;
        hasShot = false;
        inputEnabled = true;
        stillTime = 0f;
        power = minPower;
        curlInput = 0f;
        lockedCurl = 0f;
        UpdateAimArrowSprite();

        // reset physics state
        rb.position = startingPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.rotation = startingRotation;

        if (!isRoundEnding)
        {
            if (mainCamera != null) mainCamera.enabled = true;
            if (shotCamera != null) shotCamera.enabled = false;
        }

        bool showGameplayUI = !isRoundEnding;

        if (powerFill != null)
        {
            powerFill.gameObject.SetActive(showGameplayUI);
            if (showGameplayUI) powerFill.fillAmount = 0f;
        }

        if (powerBackground != null)
            powerBackground.gameObject.SetActive(showGameplayUI);

        if (aimArrowUI != null)
            aimArrowUI.gameObject.SetActive(showGameplayUI);

        DisableIceEffects();
    }
}