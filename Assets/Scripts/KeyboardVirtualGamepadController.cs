using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// 1P用のキーボード入力を仮想Gamepadへ変換し、通常プレイヤーと同じ入力経路へ送る。
[DisallowMultipleComponent]
public sealed class KeyboardVirtualGamepadController : MonoBehaviour
{
    Gamepad gamepad;

    public void Initialize(Gamepad virtualGamepad)
    {
        gamepad = virtualGamepad;

        if (gamepad != null)
            gameObject.name = $"{gameObject.name} [KEYBOARD 1P]";
    }

    void Update()
    {
        if (gamepad == null || !gamepad.added) return;

        var keyboard = Keyboard.current;
        var state = new GamepadState();
        if (keyboard != null)
        {
            // WASD -> D-pad
            if (keyboard.wKey.isPressed) state = state.WithButton(GamepadButton.DpadUp);
            if (keyboard.sKey.isPressed) state = state.WithButton(GamepadButton.DpadDown);
            if (keyboard.aKey.isPressed) state = state.WithButton(GamepadButton.DpadLeft);
            if (keyboard.dKey.isPressed) state = state.WithButton(GamepadButton.DpadRight);

            // Y=X, G=Y, H=A, B=B
            if (keyboard.yKey.isPressed) state = state.WithButton(GamepadButton.North);
            if (keyboard.gKey.isPressed) state = state.WithButton(GamepadButton.West);
            if (keyboard.hKey.isPressed) state = state.WithButton(GamepadButton.East);
            if (keyboard.bKey.isPressed) state = state.WithButton(GamepadButton.South);
        }

        InputSystem.QueueStateEvent(gamepad, state);
    }

    void OnDestroy()
    {
        if (gamepad != null && gamepad.added)
            InputSystem.RemoveDevice(gamepad);
    }
}
