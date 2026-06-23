using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class ControllerInput : MonoBehaviour
{
    //Playerのプレハブにアタッチする。
    private InputAction buttonA, buttonB, buttonX, buttonY, buttonL, buttonR, dpad;

    public bool ButtonA { get; private set; }
    public bool ButtonB { get; private set; }
    public bool ButtonX { get; private set; }
    public bool ButtonY { get; private set; }
    public bool ButtonL { get; private set; }
    public bool ButtonR { get; private set; }
    public Vector2 Dpad  { get; private set; }

    void Awake()
    {
        var actions = GetComponent<PlayerInput>().actions;
        buttonA = actions["ButtonA"];
        buttonB = actions["ButtonB"];
        buttonX = actions["ButtonX"];
        buttonY = actions["ButtonY"];
        buttonL = actions["ButtonL"];
        buttonR = actions["ButtonR"];
        dpad    = actions["Dpad"];
    }

    // Update is called once per frame
    void Update()
    {
        ButtonA = buttonA.IsPressed();
        ButtonB = buttonB.IsPressed();
        ButtonX = buttonX.IsPressed();
        ButtonY = buttonY.IsPressed();
        ButtonL = buttonL.IsPressed();
        ButtonR = buttonR.IsPressed();
        Dpad    = dpad.ReadValue<Vector2>();
    }
}
