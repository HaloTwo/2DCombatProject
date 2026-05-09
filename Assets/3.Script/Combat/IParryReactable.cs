using UnityEngine;

public interface IParryReactable
{
    void OnParried(Vector2 parryPoint, Vector2 parryDirection);
}
