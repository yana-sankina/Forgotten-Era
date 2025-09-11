using UnityEngine;

public class ThirdPersonCameraCollision : MonoBehaviour
{
    public Transform target;       // объект, за которым следим
    public float distance = 5f;    // стандартная дистанция
    public float minDistance = 1f; // минимальная дистанция камеры
    public float maxDistance = 10f; // максимальная дистанция камеры
    public float zoomSpeed = 2f;   // скорость приближения/отдаления
    public float xSpeed = 120f;    // скорость вращения по горизонтали
    public float ySpeed = 120f;    // скорость вращения по вертикали
    public float yMinLimit = -20f; // минимальный угол по вертикали
    public float yMaxLimit = 80f;  // максимальный угол по вертикали

    private float currentDistance;
    private float rotationYAxis = 0f;
    private float rotationXAxis = 0f;
    private float velocityX = 0f;
    private float velocityY = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        rotationYAxis = angles.y;
        rotationXAxis = angles.x;
        currentDistance = distance;

        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;
    }

    void LateUpdate()
    {
        if (target)
        {
            // вращение камеры мышкой
            velocityX += xSpeed * Input.GetAxis("Mouse X") * 0.02f;
            velocityY += ySpeed * Input.GetAxis("Mouse Y") * 0.02f;

            rotationYAxis += velocityX;
            rotationXAxis -= velocityY;
            rotationXAxis = ClampAngle(rotationXAxis, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(rotationXAxis, rotationYAxis, 0);

            // приближение/отдаление колесиком мыши
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);


            // плавное уменьшение скорости вращения
            velocityX = Mathf.Lerp(velocityX, 0, 0.2f);
            velocityY = Mathf.Lerp(velocityY, 0, 0.2f);
        }
    }

    static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
