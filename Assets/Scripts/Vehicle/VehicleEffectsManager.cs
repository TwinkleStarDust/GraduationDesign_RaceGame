using UnityEngine;
using System.Collections.Generic;

namespace Vehicle
{
    /// <summary>
    /// 车辆特效管理器
    /// 负责控制车辆的所有粒子效果
    /// </summary>
    public class VehicleEffectsManager : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private VehicleDriveSystem vehicleDriveSystem;
        [SerializeField] private VehiclePhysics vehiclePhysics;
        [SerializeField] private Rigidbody vehicleRigidbody;

        [Header("轮胎烟雾效果")]
        [SerializeField] private ParticleSystem[] wheelSmokePrefabs;
        [SerializeField] private float minSpeedForWheelSmoke = 10f;
        [SerializeField] private float minSlipForWheelSmoke = 0.2f;
        [SerializeField] private float wheelSmokeIntensityMultiplier = 1f;

        [Header("氮气效果")]
        [SerializeField] private ParticleSystem[] nitroEffectPrefabs;
        [SerializeField] private Light[] nitroLights;
        [SerializeField] private float nitroLightIntensity = 2f;

        [Header("刹车效果")]
        [SerializeField] private ParticleSystem[] brakeEffectPrefabs;
        [SerializeField] private float minSpeedForBrakeEffect = 40f;
        [SerializeField] private float brakeEffectThreshold = 0.7f;

        [Header("排气效果")]
        [SerializeField] private ParticleSystem[] exhaustEffectPrefabs;
        [SerializeField] private float exhaustRateIdle = 5f;
        [SerializeField] private float exhaustRateMax = 30f;

        [Header("碰撞效果")]
        [SerializeField] private ParticleSystem collisionEffectPrefab;
        [SerializeField] private float minCollisionForce = 5f;

        [Header("路面检测设置")]
        [SerializeField] private bool enableSurfaceDetection = true;
        [SerializeField] private Color asphaltSmokeColor = new Color(0.7f, 0.7f, 0.7f, 0.7f); // 沥青路面的灰色烟雾
        [SerializeField] private Color dirtSmokeColor = new Color(0.76f, 0.7f, 0.5f, 0.7f);   // 泥土路面的棕色烟雾
        [SerializeField] private Color grassSmokeColor = new Color(0.7f, 0.76f, 0.5f, 0.7f);  // 草地路面的淡绿色烟雾
        [SerializeField] private Color sandSmokeColor = new Color(0.8f, 0.76f, 0.56f, 0.7f);  // 沙地路面的黄色烟雾

        [Header("漂移烟雾优化")]
        [SerializeField] private float driftEffectFadeInTime = 0.3f;  // 烟雾淡入时间
        [SerializeField] private float driftEffectFadeOutTime = 0.7f; // 烟雾淡出时间
        [SerializeField] private float minDriftAngle = 10f;           // 最小漂移角度
        [SerializeField] private float minDriftSpeed = 20f;           // 最小漂移速度

        [Header("粒子方向设置")]
        [Tooltip("粒子向上倾斜的最小角度")]
        [SerializeField] private float minUpwardAngle = 5f;            // 最小上倾角度
        [Tooltip("粒子向上倾斜的最大角度")]
        [SerializeField] private float maxUpwardAngle = 30f;           // 最大上倾角度
        [Tooltip("粒子与地面的偏移距离")]
        [SerializeField] private float groundOffset = 0.1f;            // 粒子与地面的偏移，防止穿透

        // 缓存已实例化的粒子系统
        private Dictionary<string, List<ParticleSystem>> activeParticleSystems = new Dictionary<string, List<ParticleSystem>>();

        // 上一帧的速度，用于计算加速度
        private float previousSpeed;

        // 是否正在刹车
        private bool isBraking;

        // 是否显示碰撞效果的冷却时间
        private float collisionEffectCooldown;

        // 添加以下字段用于跟踪漂移状态
        private Dictionary<int, float> driftEffectIntensity = new Dictionary<int, float>(); // 每个轮子的漂移烟雾强度
        private bool wasDrifting = false;                                                  // 上一帧是否在漂移

        private void Awake()
        {
            // 获取组件引用
            if (vehicleController == null)
                vehicleController = GetComponent<VehicleController>();

            if (vehicleDriveSystem == null)
                vehicleDriveSystem = GetComponent<VehicleDriveSystem>();

            if (vehiclePhysics == null)
                vehiclePhysics = GetComponent<VehiclePhysics>();

            if (vehicleRigidbody == null)
                vehicleRigidbody = GetComponent<Rigidbody>();

            // 检查必要组件是否存在
            if (vehicleController == null)
            {
                Debug.LogError("VehicleEffectsManager: 未找到VehicleController组件!");
                enabled = false;
                return;
            }

            if (vehicleDriveSystem == null)
            {
                Debug.LogError("VehicleEffectsManager: 未找到VehicleDriveSystem组件!");
                enabled = false;
                return;
            }

            if (vehiclePhysics == null)
            {
                Debug.LogError("VehicleEffectsManager: 未找到VehiclePhysics组件!");
                enabled = false;
                return;
            }

            if (vehicleRigidbody == null)
            {
                Debug.LogError("VehicleEffectsManager: 未找到Rigidbody组件!");
                enabled = false;
                return;
            }

            // 初始化粒子系统
            InitializeParticleSystems();
        }

        private void Start()
        {
            // 确保所有效果最初都是关闭的
            SetAllEffectsActive(false);
        }

        private void Update()
        {
            // 更新所有粒子效果
            UpdateWheelSmokeEffects();
            UpdateNitroEffects();
            UpdateBrakeEffects();
            UpdateExhaustEffects();

            // 更新上一帧速度
            previousSpeed = vehicleController.GetCurrentSpeed();

            // 更新碰撞冷却
            if (collisionEffectCooldown > 0)
                collisionEffectCooldown -= Time.deltaTime;
        }

        /// <summary>
        /// 初始化所有粒子系统
        /// </summary>
        private void InitializeParticleSystems()
        {
            // 初始化轮胎烟雾效果
            InitializeParticleSystemGroup("WheelSmoke", wheelSmokePrefabs, 4); // 4个轮子

            // 初始化氮气效果
            InitializeParticleSystemGroup("Nitro", nitroEffectPrefabs, 2); // 左右两个排气管

            // 初始化刹车效果
            InitializeParticleSystemGroup("Brake", brakeEffectPrefabs, 2); // 左右两个后轮

            // 初始化排气效果
            InitializeParticleSystemGroup("Exhaust", exhaustEffectPrefabs, 2); // 左右两个排气管
        }

        /// <summary>
        /// 初始化指定类型的粒子系统组
        /// </summary>
        private void InitializeParticleSystemGroup(string groupName, ParticleSystem[] prefabs, int count)
        {
            if (prefabs == null || prefabs.Length == 0) return;

            List<ParticleSystem> systems = new List<ParticleSystem>();
            activeParticleSystems[groupName] = systems;

            for (int i = 0; i < count; i++)
            {
                // 选择预制体
                ParticleSystem prefab = prefabs[i % prefabs.Length];
                if (prefab == null) continue;

                // 实例化粒子系统
                ParticleSystem instance = Instantiate(prefab, transform);
                instance.Stop();
                systems.Add(instance);
            }
        }

        /// <summary>
        /// 设置所有效果的激活状态
        /// </summary>
        private void SetAllEffectsActive(bool active)
        {
            foreach (var systemGroup in activeParticleSystems.Values)
            {
                foreach (var system in systemGroup)
                {
                    if (active && !system.isPlaying)
                        system.Play();
                    else if (!active && system.isPlaying)
                        system.Stop();
                }
            }

            // 关闭氮气灯光
            if (nitroLights != null)
            {
                foreach (var light in nitroLights)
                {
                    if (light != null)
                        light.enabled = active && vehicleDriveSystem.IsNitroActive();
                }
            }
        }

        /// <summary>
        /// 获取路面材质对应的烟雾颜色
        /// </summary>
        private Color GetSurfaceSmokeColor(WheelHit hit)
        {
            if (!enableSurfaceDetection)
                return asphaltSmokeColor;

            // 检查碰撞对象是否有物理材质
            if (hit.collider != null && hit.collider.sharedMaterial != null)
            {
                string materialName = hit.collider.sharedMaterial.name.ToLower();

                // 根据物理材质名称判断表面类型
                if (materialName.Contains("dirt") || materialName.Contains("mud"))
                    return dirtSmokeColor;
                else if (materialName.Contains("grass"))
                    return grassSmokeColor;
                else if (materialName.Contains("sand"))
                    return sandSmokeColor;
            }

            // 如果没有物理材质或无法识别，尝试通过游戏对象标签判断
            if (hit.collider != null && hit.collider.gameObject != null)
            {
                string tag = hit.collider.gameObject.tag;

                if (tag == "Dirt" || tag == "Mud")
                    return dirtSmokeColor;
                else if (tag == "Grass")
                    return grassSmokeColor;
                else if (tag == "Sand")
                    return sandSmokeColor;
            }

            // 默认使用沥青路面的灰色烟雾
            return asphaltSmokeColor;
        }

        /// <summary>
        /// 更新轮胎烟雾效果
        /// </summary>
        private void UpdateWheelSmokeEffects()
        {
            if (!activeParticleSystems.TryGetValue("WheelSmoke", out List<ParticleSystem> smokeSystems) || smokeSystems.Count == 0)
                return;

            // 获取车辆状态
            float speed = vehicleController.GetCurrentSpeed();
            bool isSystemDrifting = vehicleController.IsDrifting();
            bool isInAir = vehicleController.IsInAir();
            float driftFactor = vehicleController.GetDriftFactor();

            // 获取车辆角速度，用于更精确地检测漂移
            float angularVelocity = Mathf.Abs(vehicleRigidbody.angularVelocity.y);

            // 通过车身角度和前进方向的差异来检测漂移
            float driftAngle = 0f;
            if (vehicleRigidbody != null && speed > minDriftSpeed) // 只在一定速度下检测漂移角度
            {
                // 获取车辆局部空间中的速度方向
                Vector3 localVelocity = vehicleRigidbody.transform.InverseTransformDirection(vehicleRigidbody.linearVelocity).normalized;
                driftAngle = Mathf.Abs(Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg);
            }

            // 综合判断漂移状态
            bool isDrifting = (isSystemDrifting || (speed > minDriftSpeed && driftAngle > minDriftAngle)) && !isInAir;

            // 只在漂移时显示轮胎烟雾
            bool shouldShowSmoke = isDrifting && !isInAir;

            // 根据漂移状态变化应用渐变效果
            if (isDrifting != wasDrifting)
            {
                wasDrifting = isDrifting;
            }

            // 设置烟雾强度
            float targetIntensity = isDrifting ? 1f + driftFactor * wheelSmokeIntensityMultiplier : 0f;

            // 轮胎主循环
            for (int i = 0; i < smokeSystems.Count && i < 4; i++)
            {
                ParticleSystem system = smokeSystems[i];

                // 初始化轮子的漂移效果强度
                if (!driftEffectIntensity.ContainsKey(i))
                {
                    driftEffectIntensity[i] = 0f;
                }

                // 获取对应车轮
                WheelCollider wheel = null;
                switch (i)
                {
                    case 0: wheel = vehiclePhysics.GetFrontLeftWheel(); break;
                    case 1: wheel = vehiclePhysics.GetFrontRightWheel(); break;
                    case 2: wheel = vehiclePhysics.GetRearLeftWheel(); break;
                    case 3: wheel = vehiclePhysics.GetRearRightWheel(); break;
                }

                if (wheel == null) continue;

                // 检查车轮是否打滑
                wheel.GetGroundHit(out WheelHit hit);
                float slipAmount = Mathf.Abs(hit.sidewaysSlip) + Mathf.Abs(hit.forwardSlip);

                // 应用平滑过渡
                float currentIntensity = driftEffectIntensity[i];
                float targetForWheel = isDrifting && slipAmount > minSlipForWheelSmoke && wheel.isGrounded ? targetIntensity : 0f;

                // 应用淡入淡出效果
                if (targetForWheel > currentIntensity)
                {
                    // 淡入
                    currentIntensity = Mathf.MoveTowards(currentIntensity, targetForWheel, Time.deltaTime / driftEffectFadeInTime);
                }
                else
                {
                    // 淡出
                    currentIntensity = Mathf.MoveTowards(currentIntensity, targetForWheel, Time.deltaTime / driftEffectFadeOutTime);
                }

                // 保存当前强度
                driftEffectIntensity[i] = currentIntensity;

                // 只有在强度大于0时才显示粒子效果
                bool wheelShouldSmoke = currentIntensity > 0.01f && wheel.isGrounded;

                if (wheelShouldSmoke)
                {
                    // 更新粒子系统位置 - 确保效果位于地面之上
                    Vector3 wheelPos = wheel.transform.position;
                    float groundDist = wheel.radius;

                    // 使用射线检测实际地面位置，更精确地确定粒子生成高度
                    RaycastHit groundHit;
                    if (Physics.Raycast(wheelPos, Vector3.down, out groundHit, wheel.radius * 2f))
                    {
                        // 将粒子系统放置在检测到的地面位置上方
                        system.transform.position = groundHit.point + Vector3.up * groundOffset;
                    }
                    else
                    {
                        // 如果没有检测到地面，使用轮子位置计算
                        system.transform.position = wheelPos - new Vector3(0, wheel.radius * 0.8f, 0) + Vector3.up * groundOffset;
                    }

                    // 计算车轮滑动的方向向量，用于调整粒子系统的朝向
                    Vector3 slipDirection = new Vector3(hit.sidewaysSlip, 0, hit.forwardSlip).normalized;

                    // 如果滑动足够明显，调整粒子系统的朝向
                    if (slipDirection.magnitude > 0.1f)
                    {
                        // 将粒子系统旋转至滑动方向，使烟雾朝向滑动方向喷射
                        system.transform.rotation = Quaternion.LookRotation(slipDirection, Vector3.up);

                        // 根据漂移强度动态调整向上倾斜角度
                        float upAngle = Mathf.Lerp(minUpwardAngle, maxUpwardAngle, currentIntensity);
                        system.transform.rotation *= Quaternion.Euler(upAngle, 0, 0);
                    }

                    // 调整粒子发射速率和其他参数
                    var emission = system.emission;
                    var shape = system.shape;
                    var main = system.main;

                    // 设置粒子形状为锥形
                    shape.shapeType = ParticleSystemShapeType.Cone;

                    // 调整粒子形状为圆锥，角度随漂移强度变化
                    shape.angle = Mathf.Lerp(10f, 25f, currentIntensity * 0.5f);

                    // 调整粒子速度基于漂移强度和滑动量
                    main.startSpeed = Mathf.Lerp(2f, 6f, slipAmount * currentIntensity);

                    // 启用粒子碰撞以避免穿透地面
                    var collision = system.collision;
                    if (!collision.enabled)
                    {
                        collision.enabled = true;
                        collision.type = ParticleSystemCollisionType.World;
                        collision.mode = ParticleSystemCollisionMode.Collision3D;
                        collision.bounce = 0.1f; // 低反弹系数
                        collision.lifetimeLoss = 0.5f; // 碰撞后生命值损失
                    }

                    // 调整粒子发射速率，基于当前强度的平方（使淡入淡出更加明显）
                    emission.rateOverTimeMultiplier = slipAmount * currentIntensity * currentIntensity * 25f;

                    // 添加这一部分以设置烟雾颜色
                    if (enableSurfaceDetection)
                    {
                        // 根据路面材质设置烟雾颜色
                        Color smokeColor = GetSurfaceSmokeColor(hit);

                        // 设置粒子的起始颜色
                        main.startColor = smokeColor;
                    }

                    // 调整粒子大小基于漂移强度
                    main.startSize = Mathf.Lerp(0.5f, 1.5f, currentIntensity);

                    if (!system.isPlaying)
                        system.Play();
                }
                else
                {
                    if (system.isPlaying)
                        system.Stop();
                }
            }
        }

        /// <summary>
        /// 更新氮气效果
        /// </summary>
        private void UpdateNitroEffects()
        {
            if (!activeParticleSystems.TryGetValue("Nitro", out List<ParticleSystem> nitroSystems) || nitroSystems.Count == 0)
                return;

            bool isNitroActive = vehicleDriveSystem.IsNitroActive() && vehicleDriveSystem.GetNitroAmount() > 0;

            foreach (var system in nitroSystems)
            {
                if (isNitroActive && !system.isPlaying)
                    system.Play();
                else if (!isNitroActive && system.isPlaying)
                    system.Stop();
            }

            // 更新氮气灯光
            if (nitroLights != null)
            {
                foreach (var light in nitroLights)
                {
                    if (light != null)
                    {
                        light.enabled = isNitroActive;
                        if (isNitroActive)
                        {
                            light.intensity = nitroLightIntensity * Random.Range(0.8f, 1.2f); // 添加一些闪烁效果
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新刹车效果
        /// </summary>
        private void UpdateBrakeEffects()
        {
            if (!activeParticleSystems.TryGetValue("Brake", out List<ParticleSystem> brakeSystems) || brakeSystems.Count == 0)
                return;

            float speed = vehicleController.GetCurrentSpeed();
            bool isBraking = vehicleDriveSystem.GetBrakeInput() > brakeEffectThreshold;
            bool showBrakeEffect = isBraking && speed > minSpeedForBrakeEffect && !vehicleController.IsInAir();

            for (int i = 0; i < brakeSystems.Count && i < 2; i++)
            {
                ParticleSystem system = brakeSystems[i];

                // 获取对应车轮
                WheelCollider wheel = i == 0 ? vehiclePhysics.GetRearLeftWheel() : vehiclePhysics.GetRearRightWheel();

                if (wheel == null) continue;

                // 设置粒子系统位置
                system.transform.position = wheel.transform.position - new Vector3(0, wheel.radius * 0.5f, 0);

                if (showBrakeEffect && wheel.isGrounded)
                {
                    if (!system.isPlaying)
                        system.Play();
                }
                else
                {
                    if (system.isPlaying)
                        system.Stop();
                }
            }
        }

        /// <summary>
        /// 更新排气效果
        /// </summary>
        private void UpdateExhaustEffects()
        {
            if (!activeParticleSystems.TryGetValue("Exhaust", out List<ParticleSystem> exhaustSystems) || exhaustSystems.Count == 0)
                return;

            float throttleInput = vehicleDriveSystem.GetThrottleInput();
            float emissionRate = Mathf.Lerp(exhaustRateIdle, exhaustRateMax, throttleInput);

            foreach (var system in exhaustSystems)
            {
                var emission = system.emission;
                emission.rateOverTimeMultiplier = emissionRate;

                if (!system.isPlaying)
                    system.Play();
            }
        }

        /// <summary>
        /// 显示碰撞效果
        /// </summary>
        public void ShowCollisionEffect(Vector3 position, Vector3 normal, float impactForce)
        {
            if (collisionEffectPrefab == null || impactForce < minCollisionForce || collisionEffectCooldown > 0)
                return;

            collisionEffectCooldown = 0.1f; // 限制碰撞效果的频率

            ParticleSystem collisionEffect = Instantiate(collisionEffectPrefab, position, Quaternion.LookRotation(normal));

            // 调整粒子发射数量基于碰撞力度
            var emission = collisionEffect.emission;
            float emissionMultiplier = Mathf.Clamp(impactForce / 10f, 0.5f, 3f);
            emission.rateOverTimeMultiplier *= emissionMultiplier;

            // 自动销毁
            Destroy(collisionEffect.gameObject, collisionEffect.main.duration + 0.5f);
        }

        /// <summary>
        /// 当车辆发生碰撞时调用
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > minCollisionForce)
            {
                ContactPoint contact = collision.contacts[0];
                ShowCollisionEffect(contact.point, contact.normal, collision.relativeVelocity.magnitude);
            }
        }
    }
}