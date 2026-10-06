using UnityEngine;

// Compatibility entry point for UnityEvents in older prefabs.
public class PlayerAttack : MonoBehaviour
{
    public void ButtonClick()
    {
        var controller = GetComponentInParent<PlayerController>();
        if (controller != null)
            controller.Attack();
    }
}
