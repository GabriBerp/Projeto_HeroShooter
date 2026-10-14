using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class MouseOrbit : MonoBehaviour
{
    [Header("Alvo")]
    public Transform player;
    [Tooltip("Ponto que a camera orbita (altura da cabeca/ombros, relativo ao player)")]
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Terceira pessoa")]
    public float distance = 4f;
    [Tooltip("Deslocamento lateral/vertical da camera (x = ombro direito)")]
    public Vector3 shoulderOffset = new Vector3(0.5f, 0.1f, 0f);
    public float normalFOV = 60f;

    [Header("Zoom (primeira pessoa)")]
    public float zoomDistance = 0f;          // 0 = dentro da cabeca
    public Vector3 zoomOffset = Vector3.zero;
    public float zoomFOV = 50f;
    public float transitionSpeed = 10f;

    [Header("Mouse")]
    public float MouseSensitivity = 0.2f;
    public float minPitch = -60f;
    public float maxPitch = 60f;

    [Header("Primeira pessoa")]
    [Tooltip("Esconde o modelo do player quando a camera estiver bem perto")]
    public Renderer[] playerRenderers;
    public float hideThreshold = 0.5f;
    [Tooltip("Faz o player girar junto com a camera ao dar zoom")]
    public bool rotatePlayerOnZoom = true;

    public bool isZoom { get; private set; }

    private Camera cam;
    private float rotX;
    private float rotY;

    private float currentDistance;
    private Vector3 currentOffset;
    private float currentFOV;

    void Start()
    {
        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDistance = distance;
        currentOffset = shoulderOffset;
        currentFOV = normalFOV;

        // Comeca olhando na mesma direcao da camera atual
        Vector3 angles = transform.eulerAngles;
        rotX = angles.y;
        rotY = angles.x;
    }

    void Update()
    {
        isZoom = Mouse.current.rightButton.isPressed;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // --- Rotacao pelo mouse ---
        Vector2 delta = Mouse.current.delta.ReadValue() * MouseSensitivity;
        rotX += delta.x;
        rotY -= delta.y;
        rotY = Mathf.Clamp(rotY, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(rotY, rotX, 0f);

        // --- Transicao suave (independente de framerate) ---
        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);

        float targetDistance = isZoom ? zoomDistance : distance;
        Vector3 targetOffset = isZoom ? zoomOffset : shoulderOffset;
        float targetFOV = isZoom ? zoomFOV : normalFOV;

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, t);
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, t);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, t);

        // --- Posicao: o offset agora e LOCAL (gira junto com a camera) ---
        Vector3 pivot = player.position + pivotOffset;
        Vector3 localOffset = new Vector3(currentOffset.x, currentOffset.y, currentOffset.z - currentDistance);

        transform.position = pivot + rotation * localOffset;
        transform.rotation = rotation;
        cam.fieldOfView = currentFOV;

        // --- Esconde o corpo quando a camera estiver dentro da cabeca ---
        bool hide = currentDistance < hideThreshold;
        foreach (var r in playerRenderers)
        {
            if (r != null) r.enabled = !hide;
        }

        // --- Player acompanha a direcao da camera durante o zoom ---
        if (rotatePlayerOnZoom && isZoom)
        {
            Quaternion targetYaw = Quaternion.Euler(0f, rotX, 0f);
            player.rotation = Quaternion.Slerp(player.rotation, targetYaw, t);
        }
    }
}