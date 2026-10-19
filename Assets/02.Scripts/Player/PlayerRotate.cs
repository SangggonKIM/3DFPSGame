using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;

    private float _accumulationX;

    private void Update()
    {
        if (!Input.GetMouseButton(1)) return;
        float mouseX = Input.GetAxis("Mouse X");

        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;

        transform.eulerAngles = new Vector3(0f, _accumulationX, 0f);
    }
}
