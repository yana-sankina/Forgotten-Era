using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector2 MovementInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public Vector2 MouseInput { get; private set; }
    public float ScrollInput { get; private set; }
    public bool InteractInput { get; private set; }
    
    public bool InteractHeldInput { get; private set; }
    
    private bool isControlLocked = false;
    

    private void OnEnable()
    {
        EventBroker.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBroker.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
    }

    private void OnDisable()
    {
        EventBroker.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBroker.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
    }

    void Update()
    {
        if (isControlLocked)
        {
            MovementInput = Vector2.zero;
            IsSprinting = false;
            MouseInput = Vector2.zero;
            ScrollInput = 0f;
            InteractInput = false;
            InteractHeldInput = false;
            return; 
        }
        
        
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        MovementInput = new Vector2(horizontal, vertical);

        IsSprinting = Input.GetKey(KeyCode.LeftShift);
        
        MouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        ScrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            EventBroker.Publish(new PlayerAttackEvent());
        }
        InteractInput = Input.GetKeyDown(KeyCode.E); 
        InteractHeldInput = Input.GetKey(KeyCode.E);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        isControlLocked = true;
    }

    private void OnPlayerRespawn(PlayerRespawnedEvent e)
    {
        isControlLocked = false;
    }
}