using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector2 MovementInput { get; private set; }
    public bool IsSprinting { get; private set; }
    
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
            return;
        }
        
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        MovementInput = new Vector2(horizontal, vertical);

        IsSprinting = Input.GetKey(KeyCode.LeftShift);
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