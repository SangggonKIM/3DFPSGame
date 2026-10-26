using UnityEngine;

public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private MinimapCamera _minimapCamera;

    // +/- 버튼의 OnClick 이벤트에 연결한다.
    public void OnClickZoomIn()
    {
        _minimapCamera.ZoomIn();
    }

    public void OnClickZoomOut()
    {
        _minimapCamera.ZoomOut();
    }
}
