using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class GameplayInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset actions;
    private InputActionAsset runtimeActions;
    private InputAction move;
    private InputAction jump;
    private InputAction attack;
    private InputAction pause;
    private Pause_script pauseMenu;
    private PlayerHealth health;

    public InputActionAsset Actions => runtimeActions;
    public float Horizontal => move != null ? move.ReadValue<Vector2>().x : 0f;
    public bool JumpPressed => jump != null && jump.WasPressedThisFrame();
    public bool AttackPressed => attack != null && attack.WasPressedThisFrame();

    private void Awake()
    {
        if (actions == null)
        {
            Debug.LogError("Assign the Castle Journey Input Actions asset.", this);
            enabled = false;
            return;
        }
        runtimeActions = Instantiate(actions);
        move = runtimeActions.FindAction("Gameplay/Move", true);
        jump = runtimeActions.FindAction("Gameplay/Jump", true);
        attack = runtimeActions.FindAction("Gameplay/Attack", true);
        pause = runtimeActions.FindAction("Gameplay/Pause", true);
        health = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        runtimeActions?.FindActionMap("Gameplay", true).Enable();
    }

    private void OnDisable()
    {
        runtimeActions?.Disable();
    }

    private void Update()
    {
        if (pause == null || !pause.WasPressedThisFrame() || (health != null && health.Health <= 0))
            return;
        if (pauseMenu == null)
            pauseMenu = FindAnyObjectByType<Pause_script>(FindObjectsInactive.Include);
        if (pauseMenu == null)
            return;
        if (Time.timeScale == 0f)
            pauseMenu.Resumegame();
        else
            pauseMenu.Pausegame();
    }

    private void OnDestroy()
    {
        if (runtimeActions != null)
            Destroy(runtimeActions);
    }
}
