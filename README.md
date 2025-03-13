# GraduationDesign_RaceGame

毕业设计 - 赛车游戏

## 项目概述

这是一个基于 Unity 引擎开发的赛车游戏项目，旨在实现车辆的物理模拟、场景碰撞检测和基本的游戏玩法。

## 功能特性

- 基于物理的车辆控制系统
- 真实的车轮碰撞和悬挂系统
- 平滑的相机跟随系统
- 支持第一人称和第三人称视角切换
- 简单的 UI 系统显示车辆信息

## 控制说明

- **W/↑** - 加速
- **S/↓** - 刹车/倒车
- **A/←** - 左转
- **D/→** - 右转
- **空格** - 手刹
- **R** - 重置车辆位置
- **V** - 切换视角（第一人称/第三人称）

## 项目结构

- **Assets/Scripts/Vehicle/** - 车辆相关脚本
  - **VehicleController.cs** - 车辆控制器，处理车辆的物理和运动
  - **VehicleInputHandler.cs** - 输入处理器，处理玩家输入
  - **VehicleCamera.cs** - 相机控制器，处理相机跟随
  - **VehicleUI.cs** - UI 控制器，显示车辆信息

## 如何使用

1. 在场景中创建一个车辆模型（可以使用项目中提供的模型）
2. 为车辆添加以下组件：
   - Rigidbody（刚体）
   - VehicleController 脚本
   - VehicleInputHandler 脚本
3. 为车辆的每个车轮添加 WheelCollider 组件
4. 在 VehicleController 脚本中设置车轮碰撞器和车轮模型
5. 创建一个相机并添加 VehicleCamera 脚本，设置目标为车辆
6. 创建 UI 元素并添加 VehicleUI 脚本

## 开发计划

- [x] 基本车辆控制系统
- [x] 相机跟随系统
- [x] 基本 UI 系统
- [ ] 赛道系统和计时器
- [ ] AI 对手
- [ ] 多人游戏支持
- [ ] 车辆自定义系统
- [ ] 游戏模式（竞速、漂移等）

## 技术细节

- 使用 Unity 的 WheelCollider 组件模拟车轮物理
- 使用 Rigidbody 组件处理车辆物理
- 使用 Unity 的新输入系统处理输入
- 使用 TextMeshPro 显示 UI 文本

## 注意事项

- 车辆控制器需要正确设置车轮碰撞器和车轮模型才能正常工作
- 车辆的重心高度对稳定性有很大影响，可以通过调整 centerOfMassHeight 参数来优化
- 车辆参数（最大速度、加速度等）可以根据需要进行调整

## 车辆参数配置指南

本指南详细说明了车辆控制系统中各项参数的作用、影响和建议值。通过调整这些参数，你可以创建不同类型的车辆，从家用轿车到竞速跑车。

### 基本参数设置

#### 驱动类型 (DriveType)

- **前轮驱动 (FWD)**

  - 特点：加速稳定，转向灵敏，高速可能欠转
  - 适用：家用轿车、紧凑型车
  - 优点：良好的牵引力，适合日常驾驶
  - 缺点：高速转弯性能较差

- **后轮驱动 (RWD)**

  - 特点：加速时后轮易打滑，漂移性能好
  - 适用：跑车、运动型车辆
  - 优点：更好的转向性能，适合漂移
  - 缺点：需要更多驾驶技巧

- **四轮驱动 (AWD)**
  - 特点：综合性能最佳，加速和抓地力都很好
  - 适用：SUV、高性能车辆
  - 优点：最佳的牵引力和稳定性
  - 缺点：车重较大，油耗较高

### 速度与加速度参数

#### 最大速度 (maxSpeed)

- 单位：km/h
- 建议范围：
  - 家用轿车：130-180 km/h
  - 运动型轿车：180-220 km/h
  - 超级跑车：220-350 km/h
  - 赛车：250-400 km/h

#### 最大后退速度 (maxReverseSpeed)

- 单位：km/h
- 建议范围：20-40 km/h
- 注意：后退速度过高可能导致车辆不稳定

#### 加速度 (acceleration)

- 建议范围：
  - 家用轿车：5-8
  - 运动型轿车：8-12
  - 超级跑车：12-15
  - 赛车：15-20

#### 制动力 (brakeForce)

- 建议范围：
  - 家用轿车：10-15
  - 运动型轿车：15-20
  - 超级跑车：20-25
  - 赛车：25-30

### 转向参数

#### 转向速度 (steeringSpeed)

- 建议范围：40-100
- 较低的值使转向更平滑
- 较高的值使转向更快速

#### 最大转向角度 (maxSteeringAngle)

- 建议范围：30-45 度
- 较小角度适合高速行驶
- 较大角度适合低速操作

### 漂移参数

#### 漂移强度 (driftFactor)

- 范围：0-1
- 建议值：
  - 家用轿车：0.3-0.4
  - 运动型轿车：0.5-0.6
  - 漂移车：0.7-0.9

#### 漂移时后轮侧向摩擦力减少系数 (driftSlipFactor)

- 范围：0-1
- 建议值：
  - 家用轿车：0.5-0.6
  - 运动型轿车：0.4-0.5
  - 漂移车：0.2-0.3

#### 漂移恢复速度 (driftRecoverySpeed)

- 建议范围：1-5
- 较低的值使漂移持续时间更长
- 较高的值使车辆更快恢复正常行驶

#### 漂移时转向灵敏度增加 (driftSteeringFactor)

- 范围：1-2
- 建议值：
  - 家用轿车：1.1-1.2
  - 运动型轿车：1.2-1.4
  - 漂移车：1.4-1.8

### 车辆物理参数

#### 重心高度 (centerOfMassHeight)

- 建议范围：-0.8 到 -0.3
- 较低的值（更负）使车辆更稳定
- 较高的值使车辆更容易翻转
- 建议值：
  - 跑车：-0.7 到 -0.6
  - 轿车：-0.6 到 -0.5
  - SUV：-0.5 到 -0.3

### 预设配置示例

#### 家用轿车

```csharp
maxSpeed = 160.0f;
acceleration = 7.0f;
brakeForce = 12.0f;
steeringSpeed = 50.0f;
maxSteeringAngle = 35.0f;
centerOfMassHeight = -0.5f;
driftFactor = 0.3f;
driftSlipFactor = 0.5f;
driveType = DriveType.FrontWheelDrive;
```

#### 运动型轿车

```csharp
maxSpeed = 200.0f;
acceleration = 10.0f;
brakeForce = 18.0f;
steeringSpeed = 70.0f;
maxSteeringAngle = 40.0f;
centerOfMassHeight = -0.6f;
driftFactor = 0.5f;
driftSlipFactor = 0.4f;
driveType = DriveType.RearWheelDrive;
```

#### 超级跑车

```csharp
maxSpeed = 300.0f;
acceleration = 15.0f;
brakeForce = 25.0f;
steeringSpeed = 90.0f;
maxSteeringAngle = 38.0f;
centerOfMassHeight = -0.7f;
driftFactor = 0.7f;
driftSlipFactor = 0.3f;
driveType = DriveType.RearWheelDrive;
```

#### 越野车/SUV

```csharp
maxSpeed = 180.0f;
acceleration = 8.0f;
brakeForce = 15.0f;
steeringSpeed = 45.0f;
maxSteeringAngle = 42.0f;
centerOfMassHeight = -0.4f;
driftFactor = 0.4f;
driftSlipFactor = 0.5f;
driveType = DriveType.AllWheelDrive;
```

### 调试建议

1. **车辆不稳定或容易翻车**

   - 降低重心（减小 centerOfMassHeight）
   - 减小转向速度和最大转向角度
   - 增加车轮悬挂的弹簧力度

2. **漂移效果不理想**

   - 增加 driftFactor 和 driftSteeringFactor
   - 减小 driftSlipFactor
   - 确保车速足够高（建议大于 30km/h）

3. **加速性能调整**

   - 如果加速太快：减小 acceleration
   - 如果加速太慢：增加 acceleration
   - 注意：acceleration 值过高可能导致车轮打滑

4. **制动效果调整**
   - 如果制动距离太长：增加 brakeForce
   - 如果制动太急促：减小 brakeForce
   - 建议同时调整 brakeForce 和 acceleration 以获得平衡

### 音效参数

#### 引擎音效

- minPitch：0.5-0.8（怠速音调）
- maxPitch：1.5-2.0（最高转速音调）
- 音量随速度和油门变化

#### 漂移音效

- 音量随漂移强度变化
- 建议添加适当的淡入淡出效果

注意：所有参数都可以根据具体需求进行微调，上述建议值仅供参考。在调整参数时，建议小幅度改动并及时测试效果。
