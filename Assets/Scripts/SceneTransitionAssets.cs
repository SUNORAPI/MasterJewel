using UnityEngine;

public sealed class SceneTransitionAssets : ScriptableObject
{
    [SerializeField] Sprite leftPanelSprite;
    [SerializeField] Sprite rightPanelSprite;

    public Sprite LeftPanelSprite => leftPanelSprite;
    public Sprite RightPanelSprite => rightPanelSprite;
}
