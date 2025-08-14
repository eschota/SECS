using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Networked]
    public int team_id { get; set; }

    private Vector3 _velocity;
    private bool _jumpPressed;

    private CharacterController _controller;

    public float PlayerSpeed = 10f;
    public float SprintMultiplier = 2f;

    public float JumpForce = 5f;
    public float GravityValue = -9.81f;

    [SerializeField] public float MaxStamina = 200f;
    [SerializeField] public float StaminaConsumptionPerSecond = 100f;
    [SerializeField] public float StaminaRecoveryPerSecond = 25f;
    public float Stamina => _stamina;
    private float _stamina;

    private MeshRenderer[] _meshRenderers;
    private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    private int _lastAppliedTeamId = int.MinValue;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        _skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        _stamina = MaxStamina;
    }

    public override void Spawned()
    {
        _lastAppliedTeamId = int.MinValue;
        TryApplyTeamColorIfChanged();
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            _jumpPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // FixedUpdateNetwork is only executed on the StateAuthority

        if (_controller.isGrounded)
        {
            _velocity = new Vector3(0, -1, 0);
        }

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool canSprint = _stamina > 0.01f;
        float speed = PlayerSpeed * ((wantsSprint && canSprint) ? SprintMultiplier : 1f);
        Vector3 move = input * speed * Runner.DeltaTime;

        _velocity.y += GravityValue * Runner.DeltaTime;
        if (_jumpPressed && _controller.isGrounded)
        {
            _velocity.y += JumpForce;
        }
        _controller.Move(move + _velocity * Runner.DeltaTime);

        if (input != Vector3.zero)
        {
            gameObject.transform.forward = input;
        }

        // Stamina update
        if ((wantsSprint && canSprint) && input.sqrMagnitude > 0.0001f)
        {
            _stamina -= StaminaConsumptionPerSecond * Runner.DeltaTime;
            if (_stamina < 0f) _stamina = 0f;
        }
        else
        {
            _stamina += StaminaRecoveryPerSecond * Runner.DeltaTime;
            if (_stamina > MaxStamina) _stamina = MaxStamina;
        }

        _jumpPressed = false;
    }

    public override void Render()
    {
        TryApplyTeamColorIfChanged();
    }

    private void TryApplyTeamColorIfChanged()
    {
        if (_lastAppliedTeamId != team_id)
        {
            ApplyTeamColor();
            _lastAppliedTeamId = team_id;
        }
    }

    private void ApplyTeamColor()
    {
        Color targetColor = team_id == 0 ? Color.red : Color.blue;

        for (int i = 0; i < _meshRenderers.Length; i++)
        {
            var materials = _meshRenderers[i].materials;
            for (int m = 0; m < materials.Length; m++)
            {
                var mat = materials[m];
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", targetColor);
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.color = targetColor;
                }
            }
        }

        for (int i = 0; i < _skinnedMeshRenderers.Length; i++)
        {
            var materials = _skinnedMeshRenderers[i].materials;
            for (int m = 0; m < materials.Length; m++)
            {
                var mat = materials[m];
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", targetColor);
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.color = targetColor;
                }
            }
        }
    }
}