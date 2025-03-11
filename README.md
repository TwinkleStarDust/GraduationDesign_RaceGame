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
