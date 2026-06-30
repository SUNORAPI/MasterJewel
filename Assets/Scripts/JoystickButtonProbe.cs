using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// 実機パッドのボタン番号を特定するためのデバッグ用スクリプト。
// 使い方:
//   1. 空のGameObjectを作り、このスクリプトをアタッチしてPlayする。
//   2. 接続中のデバイス一覧がConsoleに出る（Joystickとして出るのが実機パッド）。
//   3. 物理ボタンを1つずつ押すと、押されたコントロール名とバインドパスがConsoleに出る。
//      例:「[押下] button2  →  バインドパス: <Joystick>/button2」
//   4. その <Joystick>/buttonN を InputSystem_Actions の各アクションに割り当てる。
//      （番号を伝えてもらえれば .inputactions をこちらで書き換えられます）
//
// 番号特定が終わったらこのスクリプト（GameObject）は外してOK。
public class JoystickButtonProbe : MonoBehaviour
{
    void OnEnable()
    {
        LogConnectedDevices();
        Debug.Log("[JoystickButtonProbe] 準備完了。実機のボタンを1つずつ押してください。");
    }

    void Update()
    {
        // 毎フレーム、全デバイスのボタンを走査して「今押された」ものを出力する。
        foreach (var device in InputSystem.devices)
        {
            foreach (var control in device.allControls)
            {
                // ButtonControl 以外（スティックの軸など）は無視
                if (control is not ButtonControl button) continue;
                if (!button.wasPressedThisFrame) continue;

                // バインドに使う形（例: <Joystick>/button2）。HID汎用パッドは layout が
                // "HID" 等になることがあるので、Joystick系は <Joystick> 表記も併記する。
                string bindPath = $"<{device.layout}>/{control.name}";
                string joystickHint = device is Joystick ? $"  または  <Joystick>/{control.name}" : "";

                Debug.Log(
                    $"[押下] {control.name}  →  バインドパス: {bindPath}{joystickHint}\n" +
                    $"        device = {device.displayName} (layout={device.layout})  fullPath = {control.path}");
            }
        }
    }

    void LogConnectedDevices()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[JoystickButtonProbe] 接続中のデバイス一覧:");
        foreach (var device in InputSystem.devices)
        {
            string kind =
                device is Joystick ? "★Joystick(実機候補)" :
                device is Gamepad ? "Gamepad" : device.GetType().Name;
            sb.AppendLine($"  - {device.displayName}  [{kind}]  layout={device.layout}");
        }
        Debug.Log(sb.ToString());
    }
}
