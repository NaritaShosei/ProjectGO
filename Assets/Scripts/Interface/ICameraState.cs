/// <summary>
/// カメラ状態の共通契約です。
/// </summary>
public interface ICameraState
{
    public void Enter();
    public void Tick(float timeScale, UnityEngine.Vector2 cameraInput);
    public void Exit();
}