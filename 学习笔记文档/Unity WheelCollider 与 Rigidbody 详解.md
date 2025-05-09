# Unity WheelCollider 与 Rigidbody 详解

## 1. WheelCollider 介绍
`WheelCollider` 是 Unity 提供的一个专门用于模拟车辆轮胎物理的组件。它基于物理引擎计算轮胎的摩擦力、悬挂系统等，是车辆运动的核心。

### 1.1 WheelCollider 参数详解
| 参数                       | 说明                       | 过大影响                               | 过小影响                     | 推荐值范围                                   |
| -------------------------- | -------------------------- | -------------------------------------- | ---------------------------- | -------------------------------------------- |
| `Mass`                     | 轮胎的质量                 | 影响悬挂响应，过重会降低悬挂效果       | 轮胎惯性不足，容易跳动       | 20 - 50 kg (轿车) / 50 - 200 kg (卡车)       |
| `Radius`                   | 轮胎半径                   | 影响地面接触点计算，过大会导致碰撞异常 | 轮胎碰撞检测不准确，导致穿模 | 0.3 - 0.5 (轿车) / 0.5 - 1.2 (卡车)          |
| `Suspension Distance`      | 悬挂最大伸展长度           | 车辆可能变得不稳定，容易倾斜           | 车身过硬，影响操控和舒适度   | 0.1 - 0.5 (轿车) / 0.3 - 1.0 (越野车)        |
| `Force App Point Distance` | 作用力与轮轴的距离         | 影响车身倾斜度，过大会导致翻车         | 影响力传递，使车辆响应变差   | 轮胎半径的 10%-20%                           |
| `Center`                   | 轮胎在局部坐标系的位置     | 可能导致车辆偏移                       | 可能影响物理模拟精度         | (0, 0, 0) 默认                               |
| `Suspension Spring`        | 悬挂弹簧刚度               | 车辆震动剧烈，影响抓地力               | 车身下陷，影响通过性         | 35000 - 60000 (轿车) / 60000 - 100000 (赛车) |
| `Spring Damper`            | 阻尼系数                   | 过大使悬挂过硬，舒适度降低             | 过小使车辆振荡过久，稳定性差 | 2000 - 5000 (轿车) / 5000 - 10000 (赛车)     |
| `Target Position`          | 目标位置，用于控制悬挂压缩 | 影响悬挂压缩点                         | 影响车辆姿态                 | 0 - 1 (一般 0.3 - 0.5)                       |
| `Wheel Damping Rate`       | 轮胎的阻尼系数             | 过大会导致车轮旋转减缓，影响速度       | 过小可能导致车辆滑行不稳定   | 0.1 - 1.0 (一般 0.25)                        |
| `Forward Friction`         | 前向摩擦力                 | 见下表                                 | 见下表                       | 见下表                                       |
| `Sideways Friction`        | 侧向摩擦力                 | 见下表                                 | 见下表                       | 见下表                                       |

### 1.2 Friction 轮胎摩擦参数
| 参数              | 说明                       | 轿车      | 赛车      | 卡车      |
| ----------------- | -------------------------- | --------- | --------- | --------- |
| `Extremum Slip`   | 轮胎在最大抓地力前的滑移量 | 0.3 - 0.5 | 0.2 - 0.4 | 0.5 - 1.0 |
| `Extremum Value`  | 最大摩擦力                 | 1.0 - 1.5 | 1.5 - 2.5 | 2.0 - 3.5 |
| `Asymptote Slip`  | 进入滑移状态的临界值       | 1.0 - 2.0 | 0.8 - 1.5 | 2.0 - 3.5 |
| `Asymptote Value` | 进入滑移状态后的摩擦力     | 0.5 - 1.0 | 0.8 - 1.5 | 1.0 - 2.5 |
| `Stiffness`       | 摩擦曲线的刚度             | 1.0 - 2.0 | 2.0 - 4.0 | 1.5 - 3.0 |

## 2. Rigidbody 介绍
`Rigidbody` 是 Unity 中用于物理模拟的刚体组件。车辆通常需要加上 `Rigidbody` 来受力运动。

### 2.1 Rigidbody 参数详解
| 参数                  | 说明           | 过大影响                     | 过小影响                               | 推荐值范围                                     |
| --------------------- | -------------- | ---------------------------- | -------------------------------------- | ---------------------------------------------- |
| `Mass`                | 质量           | 影响加速度，过大会降低操控性 | 过轻会导致车身易被外力影响             | 1000 - 2000 kg (轿车) / 3000 - 10000 kg (卡车) |
| `Drag`                | 空气阻力系数   | 速度降低过快，影响行驶性能   | 过小导致空气阻力影响不足，车辆难以减速 | 0.1 - 0.3 (轿车) / 0.3 - 0.5 (卡车)            |
| `Angular Drag`        | 角阻力         | 过大使车辆旋转响应迟钝       | 过小可能导致车辆旋转过度               | 0.01 - 0.05 (轿车) / 0.05 - 0.1 (卡车)         |
| `Use Gravity`         | 是否受重力影响 | 无                           | 车辆可能漂浮                           | 开启                                           |
| `Is Kinematic`        | 是否由代码控制 | 影响物理交互                 | 影响动力学                             | 关闭                                           |
| `Interpolation`       | 插值模式       | 可能影响平滑度               | 可能影响物理稳定性                     | Interpolate (轿车) / None (卡车)               |
| `Collision Detection` | 碰撞检测模式   | 影响物理稳定性               | 影响碰撞检测精准度                     | Continuous (赛车) / Discrete (普通车辆)        |
| `Center Of Mass`      | 质量中心       | 过高可能导致翻车             | 过低影响转向响应                       | 车辆重心略低于车身中心                         |

## 3. 车辆物理参数推荐表
| 车辆类型 | 质量(kg)     | 轮胎半径(m) | 悬挂刚度       | 悬挂阻尼     | 最大摩擦力 |
| -------- | ------------ | ----------- | -------------- | ------------ | ---------- |
| 轿车     | 1000 - 2000  | 0.3 - 0.5   | 35000 - 60000  | 2000 - 5000  | 1.0 - 1.5  |
| 赛车     | 600 - 1200   | 0.3 - 0.5   | 60000 - 100000 | 5000 - 10000 | 1.5 - 2.5  |
| 越野车   | 1500 - 3000  | 0.4 - 0.7   | 40000 - 80000  | 3000 - 6000  | 1.5 - 2.5  |
| 卡车     | 3000 - 10000 | 0.5 - 1.2   | 50000 - 120000 | 5000 - 15000 | 2.0 - 3.5  |

## 4. 总结
- `WheelCollider` 控制轮胎摩擦、悬挂系统和驱动力。
- `Rigidbody` 负责车辆整体的物理运动。
- 调整参数时需根据车辆类型和物理效果进行合理调整。