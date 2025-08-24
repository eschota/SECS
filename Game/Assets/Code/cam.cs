using Fusion;
using UnityEngine;

public class cam : MonoBehaviour
{
    public Camera _cam;
    public Vector3 target_pivot_position;
    public Quaternion target_pivot_rotation;
    public float speed = 10;
    public float movementSpeed = 5f;
    public float rotationSpeed = 2f;
    public Vector3 cameraOffset = Vector3.zero; // Смещение камеры относительно центра машины
    public float zoomSpeed = 2f;
    public float minZoomDistance = 0.3f;
    public float maxZoomDistance = 2f;
    public float minFov = 6.3f;
    public float maxFov = 90f;
    public float verticalMoveDelay = 0.1f;

    private GameObject cameraPivot;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float initialDistanceToPivot;
    private bool isInitialized = false;
    private float baseFov;
    private float targetFov;
    private float verticalMoveTimer = 0f;

    // follow
    private bool followMachine = false;
    private Machine followedMachine;

    // layer for io_base
    private LayerMask ioBaseMask;

    void Awake()
    {
        _cam = Camera.main;
        baseFov = _cam.fieldOfView;
        targetFov = baseFov;

        ioBaseMask = LayerMask.GetMask("io_base");

        CreatePivot();
        SetupCameraToFirstCell(); // стартовое поведение как раньше

        Play.OnPlayStateChange += OnPlayStateChange;
        Machine.OnLocalMachineReady += OnLocalMachineReady;
    }

    void OnDestroy()
    {
        Play.OnPlayStateChange -= OnPlayStateChange;
        Machine.OnLocalMachineReady -= OnLocalMachineReady;
    }

    void Update()
    {
        if (UI_Canvas.i?.currentState == UI_Canvas.UI_State.Chatting) return;

        // Разделяем логику для разных режимов
        if (Play.i?.currentState == Play.State.Create)
        {
            // Конструктор: работаем с target_pivot_position
            HandleMovement();
            HandleRotation();
            HandleZoom();
            HandleFocusConstructor();
            
            // Тянем pivot к target_pivot_position
            cameraPivot.transform.position = Vector3.Lerp(cameraPivot.transform.position, target_pivot_position, Time.deltaTime * speed);
        }
        else
        {
            // Симуляция: камера прикреплена к машине как дочерний объект
            // Позиция обновляется автоматически через иерархию
            HandleRotation();
            HandleZoom();
        }

        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * speed);
    }

    // --- режимы ---
    private void OnPlayStateChange(Play.State state)
    {
        if (state == Play.State.Create)
        {
            // вернуться к сборке
            followMachine = false;
            followedMachine = null;
            
            // Отключаем камеру от машины
            if (cameraPivot != null)
            {
                cameraPivot.transform.SetParent(null, true);
            }
            
            FocusOnConstructor();
        }
        else
        {
            // переходим в симуляцию: отключаем WASD (followMachine = true)
            followMachine = true;

            // гарантируем, что камера ребёнок pivot (на случай, если что-то сбросилось)
            if (_cam.transform.parent != cameraPivot.transform)
                _cam.transform.SetParent(cameraPivot.transform, true);

            // пробуем сразу найти уже существующую «свою» машину
            TryAttachToOwnedMachine();
        }
    }

    private void OnLocalMachineReady(Machine m)
    {
        if (m && m.Object.HasInputAuthority)
        {
            if (cameraPivot == null) CreatePivot();

            // Прикрепляем pivot к машине как дочерний объект
            cameraPivot.transform.SetParent(m.transform, true);
            
            // Устанавливаем локальную позицию в центре машины с возможным смещением
            // Поскольку машина уже построена с центром в центроиде клеток
            cameraPivot.transform.localPosition = cameraOffset;

            initialDistanceToPivot = Vector3.Distance(_cam.transform.position, cameraPivot.transform.position);
            followedMachine = m;
            followMachine = true;

            Debug.Log($"<color=#4DA3FF>[cam]</color> attached to local machine as child at center with offset {cameraOffset}");
        }
    }

    private void TryAttachToOwnedMachine()
    {
        var all = FindObjectsOfType<Machine>();
        foreach (var m in all)
            if (m && m.Object.HasInputAuthority) { AttachToMachine(m); return; }
    }

    private void AttachToMachine(Machine m)
    {
        followedMachine = m;
        followMachine = true;

        // Прикрепляем pivot к машине как дочерний объект для плавного следования
        cameraPivot.transform.SetParent(m.transform, true);

        // Устанавливаем локальную позицию в центре машины с возможным смещением
        // Поскольку машина уже построена с центром в центроиде клеток
        cameraPivot.transform.localPosition = cameraOffset;

        initialDistanceToPivot = Vector3.Distance(_cam.transform.position, cameraPivot.transform.position);
        
        Debug.Log($"<color=#4DA3FF>[cam]</color> attached to machine as child at center with offset {cameraOffset}");
        Debug.Log($"<color=#4DA3FF>[cam]</color> Machine center: {m.transform.position}, LocalCenter: {m.LocalCenter}");
    }

    // --- управление камерой ---
    void HandleMovement()
    {
        if (!isInitialized) return;

        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input.z += 1f;
        if (Input.GetKey(KeyCode.S)) input.z -= 1f;
        if (Input.GetKey(KeyCode.A)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D)) input.x += 1f;

        if (input.magnitude > 0)
        {
            input.Normalize();
            Vector3 forward = _cam.transform.forward;
            Vector3 right   = _cam.transform.right;
            forward.y = 0; right.y = 0; forward.Normalize(); right.Normalize();
            Vector3 movement = (forward * input.z + right * input.x) * movementSpeed * Time.deltaTime;
            target_pivot_position += movement;
        }

        if (Input.GetKey(KeyCode.E))
        {
            verticalMoveTimer -= Time.deltaTime;
            if (verticalMoveTimer <= 0) { target_pivot_position.y += 1; verticalMoveTimer = verticalMoveDelay; }
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            verticalMoveTimer -= Time.deltaTime;
            if (verticalMoveTimer <= 0) { target_pivot_position.y -= 1; verticalMoveTimer = verticalMoveDelay; }
        }
        else verticalMoveTimer = 0;

        target_pivot_position.y = Mathf.Clamp(Mathf.Round(target_pivot_position.y), 0f, 100f);
    }

    void HandleRotation()
    {
        if (!isInitialized) return;

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            Vector3 eulerAngles = _cam.transform.eulerAngles;
            float currentX = eulerAngles.x; if (currentX > 180f) currentX -= 360f;

            float newX = Mathf.Clamp(currentX - mouseY, -70f, 70f);
            float newY = eulerAngles.y + mouseX;

            _cam.transform.rotation = Quaternion.Euler(newX, newY, 0);
            _cam.transform.position = cameraPivot.transform.position - _cam.transform.forward * initialDistanceToPivot;
        }
        else if (Play.i?.currentState != Play.State.Create)
        {
            // поддерживаем выбранную дистанцию в симуляции
            _cam.transform.position = cameraPivot.transform.position - _cam.transform.forward * initialDistanceToPivot;
        }
    }

    void HandleZoom()
    {
        if (!isInitialized) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // в режиме конструктора: если луч попал ровно в текущий предпросмотр — крутим её и не зумим
            if (Play.i?.currentState == Play.State.Create && TryRotatePreviewUnderMouse(scroll))
                return;

            // обычный зум
            targetFov -= scroll * zoomSpeed * 30f;
            targetFov = Mathf.Clamp(targetFov, minFov, maxFov);
        }
    }

    /// <summary>
    /// Если под курсором именно Creator.current_prefab — повернуть её на ±90°.
    /// Возвращает true, если вращение выполнено (зум в этот кадр подавляем).
    /// </summary>
 bool TryRotatePreviewUnderMouse(float scroll)
{
    if (_cam == null) return false;
    if(Creator.instance.current_prefab.Status == io_base.io_base_status.Creating
    || Creator.instance.current_prefab.Status == io_base.io_base_status.Intersected){

                    int dir = scroll > 0f ? +1 : -1;
                    Creator.instance.current_prefab.Rotate(dir);
                    return true;
    }

    return false;
}


    void HandleFocusConstructor()
    {
        if (Play.i?.currentState != Play.State.Create) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusOnConstructor();
        }
    }

    void CreatePivot()
    {
        cameraPivot = new GameObject("CameraPivot");
        cameraPivot.transform.parent = null;
    }

    void SetupCameraToFirstCell()
    {
        Creator cr = FindObjectOfType<Creator>();
        if (cr != null && cr.cells.Count > 0)
        {
            io_base first = cr.cells[0];
            cameraPivot.transform.position = first.target_world_position;

            // — не меняем старое поведение: сохраняем world позу и делаем ребёнком pivot
            baseLocalPosition = _cam.transform.position;
            baseLocalRotation = _cam.transform.rotation;
            _cam.transform.SetParent(cameraPivot.transform, true);
            _cam.transform.localPosition = baseLocalPosition; // да, оставляем как у тебя
            _cam.transform.localRotation = baseLocalRotation;

            initialDistanceToPivot = Vector3.Distance(_cam.transform.position, cameraPivot.transform.position);
            target_pivot_position   = first.target_world_position;
            isInitialized = true;
        }
    }

    void SetupCameraToLastCell()
    {
        Creator cr = FindObjectOfType<Creator>();
        if (cr != null && cr.cells.Count > 0)
        {
            io_base last = cr.cells[cr.cells.Count - 1];
            cameraPivot.transform.position = last.target_world_position;

            // не ломаем стартовую позу камеры: сохраняем world и просто подвешиваем к pivot
            baseLocalPosition = _cam.transform.position;
            baseLocalRotation = _cam.transform.rotation;

            if (_cam.transform.parent != cameraPivot.transform)
                _cam.transform.SetParent(cameraPivot.transform, true);

            _cam.transform.localPosition = baseLocalPosition;
            _cam.transform.localRotation = baseLocalRotation;

            initialDistanceToPivot = Vector3.Distance(_cam.transform.position, cameraPivot.transform.position);
            target_pivot_position   = last.target_world_position;
            isInitialized = true;
        }
    }

    /// <summary>
    /// Публичный метод для смещения камеры к конструктору и клеткам
    /// </summary>
    public void FocusOnConstructor()
    {
          Creator cr = Creator.instance;
            if (cr != null && cr.cells.Count > 0)
            {
                Vector3 avg = Vector3.zero;
                foreach (var cell in cr.cells) avg += cell.transform.position;
                avg /= cr.cells.Count;

                io_base closest = null;
                float minDist = float.MaxValue;
                foreach (var cell in cr.cells)
                {
                    float d = Vector3.Distance(cell.transform.position, avg);
                    if (d < minDist) { minDist = d; closest = cell; }
                }
                if (closest) target_pivot_position = closest.transform.position;
            }
    }
}
