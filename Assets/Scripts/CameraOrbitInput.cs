using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lets the player rotate the Cinemachine camera around Clodi,
/// but only while the right mouse button is held down.
/// This way the mouse cursor stays free for aiming the mud ball.
/// Put it on the FreeLook Camera (the object with the Cinemachine Input Axis Controller).
/// </summary>
[RequireComponent(typeof(CinemachineInputAxisController))]
public class CameraOrbitInput : MonoBehaviour
{
    [SerializeField] private bool hideCursorWhileRotating = true;

    private CinemachineInputAxisController axisController;

    private void Awake()
    {
        axisController = GetComponent<CinemachineInputAxisController>();
        axisController.enabled = false;
    }

    private void Update()
    {
        // No camera rotation while the game is paused, won or lost
        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        bool rotating = playing && Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (axisController.enabled == rotating) return;
        axisController.enabled = rotating;

        if (hideCursorWhileRotating)
        {
            Cursor.lockState = rotating ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !rotating;
        }
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}