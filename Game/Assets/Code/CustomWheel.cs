using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomWheel : MonoBehaviour
{
    [Header("Подвеска")]
    public Transform wheelVisual; // Меш колеса
    public float suspensionLength = 0.3f;
    public float springStrength = 20000f;
    public float damperStrength = 4500f;

    [Header("Колесо")]
    public float wheelRadius = 0.35f;
    public float motorTorque = 0f;   // Движущий момент
    public float brakeTorque = 0f;   // Торможение
    public float steerAngle = 0f;    // Поворот (ось вращения колеса)

    private Rigidbody rb;
    private Vector3 suspensionDir;   // направление подвески (локальная -Y)
    private Vector3 wheelForward;    // ось вращения колеса (локальная Z)
    private Vector3 wheelRight;      // боковая ось (локальная X)

    private float wheelRotation;     // накопленный угол вращения
    private float lastCompression;   // для демпфера

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Локальные оси
        suspensionDir = -transform.up;
        wheelForward  = transform.forward;
        wheelRight    = transform.right;

        // Луч подвески
        if (Physics.Raycast(transform.position, suspensionDir, out RaycastHit hit, suspensionLength + wheelRadius))
        {
            // ---- Подвеска ----
            float compression = (suspensionLength + wheelRadius - hit.distance) / suspensionLength;
            float springForce = compression * springStrength;

            float damperForce = (lastCompression - compression) / Time.fixedDeltaTime * damperStrength;
            lastCompression = compression;

            Vector3 totalForce = (springForce + damperForce) * -suspensionDir;
            rb.AddForceAtPosition(totalForce, transform.position);

            // ---- Движение колеса ----
            Vector3 vel = rb.GetPointVelocity(transform.position);
            float forwardVel = Vector3.Dot(vel, wheelForward);

            // Моторный момент
            float driveForce = motorTorque / wheelRadius;
            rb.AddForceAtPosition(wheelForward * driveForce, transform.position);

            // Торможение
            float brakeForce = Mathf.Min(Mathf.Abs(forwardVel), brakeTorque);
            rb.AddForceAtPosition(-Mathf.Sign(forwardVel) * wheelForward * brakeForce, transform.position);

            // ---- Визуализация вращения ----
            wheelRotation += forwardVel / wheelRadius * Time.fixedDeltaTime * Mathf.Rad2Deg;
            if (wheelVisual)
            {
                wheelVisual.position = hit.point + suspensionDir * wheelRadius;
                wheelVisual.rotation = Quaternion.LookRotation(wheelForward, -suspensionDir);
                wheelVisual.Rotate(Vector3.right, wheelRotation, Space.Self);
            }
        }
    }

    void OnGUI()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z > 0)
        {
            screenPos.y = Screen.height - screenPos.y;

            GUI.color = Color.green;
            GUI.Label(new Rect(screenPos.x, screenPos.y, 200, 20), "Custom Wheel");

            GUI.color = Color.red;
            GUI.Label(new Rect(screenPos.x, screenPos.y + 15, 200, 20), $"SuspDir: {suspensionDir}");

            GUI.color = Color.blue;
            GUI.Label(new Rect(screenPos.x, screenPos.y + 30, 200, 20), $"Forward: {wheelForward}");

            GUI.color = Color.yellow;
            GUI.Label(new Rect(screenPos.x, screenPos.y + 45, 200, 20), $"Compression: {lastCompression:F2}");
        }
    }
}
