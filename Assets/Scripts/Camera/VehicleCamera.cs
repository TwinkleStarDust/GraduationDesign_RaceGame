using UnityEngine;

namespace Vehicle
{

    /// 车辆相机控制器
    /// 负责跟随车辆并提供平滑的相机运动

    public class VehicleCamera : MonoBehaviour
    {
        // [Tooltip("目标车辆")] // 不再需要公开的 Transform target 来获取 CarController
        // [SerializeField] private Transform target;
        private Transform m_TargetTransform; // 我们仍然需要Transform来定位，但会从CarController获取

        [Header("跟随设置")]
        [Tooltip("相机距离")]
        [SerializeField] private float distance = 6.0f;

        [Tooltip("相机高度")]
        [SerializeField] private float height = 2.0f;

        [Tooltip("相机平滑度")]
        [SerializeField] private float smoothness = 10.0f;

        [Tooltip("相机旋转速度")]
        [SerializeField] private float rotationSpeed = 5.0f;

        [Header("视角切换")]
        [Tooltip("是否启用视角切换")]
        [SerializeField] private bool enableViewSwitch = true;

        [Tooltip("视角切换按键")]
        [SerializeField] private KeyCode switchViewKey = KeyCode.V;

        [Tooltip("第一人称视角偏移")]
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0, 1.5f, 0.2f);

        [Header("鼠标控制设置")]
        [Tooltip("鼠标水平灵敏度")]
        [SerializeField] private float mouseSensitivityX = 3.0f;

        [Tooltip("鼠标垂直灵敏度")]
        [SerializeField] private float mouseSensitivityY = 2.0f;

        [Tooltip("垂直视角限制（最小值）")]
        [SerializeField] private float minVerticalAngle = -30.0f;

        [Tooltip("垂直视角限制（最大值）")]
        [SerializeField] private float maxVerticalAngle = 60.0f;

        [Tooltip("环绕视角相机距离")]
        [SerializeField] private float orbitDistance = 5.0f;

        [Tooltip("环绕视角相机高度偏移")]
        [SerializeField] private float orbitHeightOffset = 1.0f;

        [Tooltip("是否在鼠标控制模式下隐藏鼠标")]
        [SerializeField] private bool hideCursorInMouseMode = true;

        // 相机视角模式枚举
        public enum CameraViewMode
        {
            ThirdPerson,    // 第三人称视角
            FirstPerson,    // 第一人称视角
            OrbitControl    // 环绕视角（鼠标控制）
        }

        // 当前相机视角模式
        private CameraViewMode currentViewMode = CameraViewMode.ThirdPerson;

        // 私有变量
        private Vector3 currentVelocity;
        private float currentRotationAngle = 0;
        private CarController m_TargetCarController;

        // 环绕视角相关变量
        private float orbitX = 0f;
        private float orbitY = 10f;
        private Vector3 orbitOffset;
        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisible;


        /// 初始化组件

        private void Start()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                m_TargetCarController = playerObject.GetComponent<CarController>();
                if (m_TargetCarController != null)
                {
                    m_TargetTransform = playerObject.transform; // 获取车辆的 Transform
                }
                else
                {
                    Debug.LogError("VehicleCamera: 在带有 'Player' 标签的对象上未找到 CarController 组件!", this);
                    enabled = false;
                    return;
                }
            }
            else
            {
                Debug.LogError("VehicleCamera: 未在场景中找到带有 'Player' 标签的游戏对象!", this);
                enabled = false;
                return;
            }

            // 初始化相机位置，使用 m_TargetTransform
            if (m_TargetTransform != null)
            {
                UpdateCameraPosition(true);
            }

            // 初始化环绕视角，使用 m_TargetTransform
            if (m_TargetTransform != null) 
            {
                orbitX = m_TargetTransform.eulerAngles.y;
            }
            else // 如果 m_TargetTransform 仍然是 null (理论上不应该发生，因为上面有检查)
            {
                orbitX = 0f; 
            }
            orbitY = 10f; // 初始仰角
            orbitOffset = new Vector3(0, orbitHeightOffset, 0);

            // 保存初始鼠标状态
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
        }


        /// 更新相机位置

        private void LateUpdate()
        {
            if (m_TargetTransform == null) return; // 使用 m_TargetTransform 进行检查

            // 检查视角切换
            if (enableViewSwitch && Input.GetKeyDown(switchViewKey))
            {
                // 循环切换三种视角模式
                switch (currentViewMode)
                {
                    case CameraViewMode.ThirdPerson:
                        SetViewMode(CameraViewMode.FirstPerson);
                        break;
                    case CameraViewMode.FirstPerson:
                        SetViewMode(CameraViewMode.OrbitControl);
                        break;
                    case CameraViewMode.OrbitControl:
                        SetViewMode(CameraViewMode.ThirdPerson);
                        break;
                }
            }

            // 更新相机位置
            UpdateCameraPosition(false);
        }


        /// 更新相机位置

        private void UpdateCameraPosition(bool immediate)
        {
            switch (currentViewMode)
            {
                case CameraViewMode.FirstPerson:
                    UpdateFirstPersonView(immediate);
                    break;
                case CameraViewMode.ThirdPerson:
                    UpdateThirdPersonView(immediate);
                    break;
                case CameraViewMode.OrbitControl:
                    UpdateOrbitView(immediate);
                    break;
            }
        }


        /// 更新第一人称视角

        private void UpdateFirstPersonView(bool immediate)
        {
            // 计算目标位置
            Vector3 targetPosition = m_TargetTransform.TransformPoint(firstPersonOffset); // 使用 m_TargetTransform

            // 计算目标旋转
            Quaternion targetRotation = m_TargetTransform.rotation; // 使用 m_TargetTransform

            // 设置相机位置和旋转
            if (immediate)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
            else
            {
                // 平滑过渡
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, 1.0f / smoothness);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothness);
            }
        }


        /// 更新第三人称视角

        private void UpdateThirdPersonView(bool immediate)
        {
            // 获取车辆速度和氮气状态
            float speed = 0;
            bool isNitroActive = false;
            if (m_TargetCarController != null)
            {
                speed = m_TargetCarController.GetCurrentForwardSpeedMS();
                isNitroActive = m_TargetCarController.IsNitroActiveAndEnabled;
            }

            // 根据车辆速度调整相机跟随平滑度
            float speedFactor = Mathf.Clamp01(speed / 50.0f);

            // 当使用氮气时，增加额外的平滑度
            float nitroSmoothnessFactor = isNitroActive ? 0.5f : 1.0f;

            // 使用更高的平滑度数值使相机移动更加平滑
            float adjustedSmoothness = smoothness;
            if (speed > 50.0f || isNitroActive)
            {
                adjustedSmoothness = smoothness * 1.5f;
            }

            // 根据速度和氮气状态调整相机旋转速度
            float rotationFactor = Mathf.Lerp(0.5f, 0.8f, speedFactor) * nitroSmoothnessFactor;

            // 计算目标位置
            Vector3 targetPosition = m_TargetTransform.position; // 使用 m_TargetTransform
            targetPosition.y += height;

            // 计算相机旋转角度 - 使用更平滑的过渡
            currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, m_TargetTransform.eulerAngles.y, Time.deltaTime * rotationSpeed * rotationFactor); // 使用 m_TargetTransform

            // 计算相机位置
            Vector3 direction = Quaternion.Euler(0, currentRotationAngle, 0) * Vector3.back;
            Vector3 desiredPosition = targetPosition + direction * distance;

            // 设置相机位置和旋转
            if (immediate)
            {
                transform.position = desiredPosition;
                transform.LookAt(targetPosition); // LookAt 内部会使用 m_TargetTransform.position
            }
            else
            {
                // 使用平滑阻尼器而不是线性插值，可以实现更平滑的过渡
                float dampTime = 1.0f / adjustedSmoothness;

                // 当使用氮气时，使用更平滑的跟随
                if (isNitroActive)
                {
                    dampTime *= 1.25f; // 增加氮气时的阻尼时间
                }

                // 平滑过渡 - 使用SmoothDamp而不是线性插值
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, dampTime);

                // 始终看向目标
                Vector3 lookAtPosition = targetPosition;
                transform.LookAt(lookAtPosition);
            }
        }


        /// 更新环绕视角（鼠标控制）

        private void UpdateOrbitView(bool immediate)
        {
            orbitX += Input.GetAxis("Mouse X") * mouseSensitivityX;
            orbitY -= Input.GetAxis("Mouse Y") * mouseSensitivityY;
            orbitY = Mathf.Clamp(orbitY, minVerticalAngle, maxVerticalAngle);

            Quaternion rotation = Quaternion.Euler(orbitY, orbitX, 0);
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -orbitDistance) + m_TargetTransform.position + orbitOffset; // 使用 m_TargetTransform

            if (immediate)
            {
                transform.position = position;
                transform.rotation = rotation;
            }
            else
            {
                // 平滑过渡
                transform.position = Vector3.SmoothDamp(transform.position, position, ref currentVelocity, 1.0f / smoothness);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * smoothness);
            }

            // 确保相机始终看向目标中心点
            transform.LookAt(m_TargetTransform.position);
        }


        /// 获取当前视角模式

        public CameraViewMode GetCurrentViewMode()
        {
            return currentViewMode;
        }


        /// 设置视角模式

        public void SetViewMode(CameraViewMode mode)
        {
            // 如果从环绕模式切换出去，恢复鼠标状态
            if (currentViewMode == CameraViewMode.OrbitControl && mode != CameraViewMode.OrbitControl)
            {
                RestoreCursorState();
            }

            currentViewMode = mode;

            // 如果切换到环绕模式，初始化环绕角度并隐藏鼠标
            if (mode == CameraViewMode.OrbitControl)
            {
                orbitX = m_TargetTransform.eulerAngles.y;
                orbitY = 10f;

                if (hideCursorInMouseMode)
                {
                    // 保存当前鼠标状态
                    previousCursorLockState = Cursor.lockState;
                    previousCursorVisible = Cursor.visible;

                    // 锁定并隐藏鼠标
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }


        /// 恢复鼠标状态

        private void RestoreCursorState()
        {
            if (hideCursorInMouseMode)
            {
                // 恢复之前的鼠标状态
                Cursor.lockState = previousCursorLockState;
                Cursor.visible = previousCursorVisible;
            }
        }


        /// 当脚本被禁用或销毁时调用

        private void OnDisable()
        {
            // 确保恢复鼠标状态
            RestoreCursorState();
        }
    }
}