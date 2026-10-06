using UnityEngine;

// Preserve serialized UnityEvents on older prefabs without running a second controller.
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerController controller;

    public void Jump()
    {
        if (controller == null)
            controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.Jump();
    }
}
