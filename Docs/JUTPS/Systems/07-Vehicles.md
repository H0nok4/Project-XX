# JUTPS 系统拆解 07：载具物理与驾驶

## 1. 模块定位

JUTPS 载具系统是一个相对独立的支线能力，核心由：

- `JUVehicle`
- `JUWheeledVehicle`
- `CarController`
- `MotorcycleController`
- 载具输入资产
- 角色上车桥接

组成。

## 2. 基类架构

### `JUVehicle`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Libraries/Vehicle System Libs/JUVehicleEngine.cs`

职责：

- 统一车辆输入轴
- 缓存刚体与速度
- 提供输入平滑
- 提供地面检测、翻车检测等通用结构

核心属性：

- `IsOn`
- `ControlsEnabled`
- `UsePlayerInputs`
- `Vertical`
- `Horizontal`
- `Velocity`
- `LocalVelocity`
- `RigidBody`

结论：

- 所有载具共享一套基础运行时接口

### `JUWheeledVehicle`

位于同文件中，是 `JUVehicle` 的有轮载具扩展层。

作用：

- 封装轮胎数据
- 处理加速/转向/刹车等轮系行为

## 3. 具体载具实现

### `CarController`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Physics/Vehicle Physics/Car Physics/CarController.cs`

特点：

- 用 `WheelAxle[]` 定义前后轴
- 每根轴有：
  - 油门影响
  - 刹车影响
  - 最大转角
  - 左右轮碰撞体与模型
- 支持翻车检测与自动扶正

### `MotorcycleController`

作用类似，但更偏摩托车平衡与倾斜逻辑。

### 车辆附加能力

包里还提供：

- `JUVehicleEngineSound`
- `JUVehicleNitro`
- `JUVehicleAirRotation`
- `VehicleJump`
- `JUVehicleSteerWheel`
- `JUVehicleCharacterIK`

这意味着载具不是一个脚本包打天下，而是：

- 基础控制器 + 多个表现/附加能力组件

## 4. 输入架构

### `JUVehicleInputAsset`

路径：

- `Assets/Julhiecio TPS Controller/Scripts/Physics/Vehicle Physics/JUVehicleInputAsset.cs`

职责：

- 承载油门、转向、刹车、氮气输入
- 可以启停
- 内置 Classic / Advanced 两种模板

这让“不同载具控制方案”具备了基本扩展空间。

## 5. 角色与载具的桥接

### `DriveVehicles`

关键职责：

- 从交互系统接收上车请求
- 记录当前驾驶载具
- 处理进入/离开载具状态
- 更新角色在座位上的位置与旋转
- 与 `JUVehicleCharacterIK` 联动

这层桥接做得不错，因为它把“车”和“人”的职责分开了：

- 车只负责自己能怎么开
- 人只负责能不能上车、上车后怎么附着

## 6. 运行流程

1. 玩家接近车辆交互点。
2. `JUInteractionSystem` 触发交互。
3. `DriveVehicles` 接管流程并把角色附着到当前载具。
4. 角色输入停用，载具输入启用。
5. 载具控制器根据输入和轮系参数进行物理推进。
6. 下车时角色回到地面，并恢复人物控制。

## 7. 对 Project-XX 的价值

如果你的项目地图里需要：

- 汽车
- 摩托
- 巡逻载具
- 地图交通工具

这套系统非常值得保留，原因是：

- 已经有基本可玩的物理与驾驶交互
- 已有角色上车桥接
- 已有载具 AI 示例

## 8. 对 Project-XX 的限制

直接限制主要在于：

- 没有燃油经济系统
- 没有零部件损坏分区
- 没有多人座位逻辑
- 没有战术载具库存/后备箱完整系统
- 没有复杂碰撞损伤与维修流程

## 9. 面向搜打撤的建议

适合直接用的部分：

- 移动平台
- 可驾驶撤离载具
- 地图热点交通工具
- NPC 巡逻车

建议后续补的系统：

- `VehicleFuelRuntime`
- `VehicleDamageRuntime`
- `VehicleStorageRuntime`
- `VehicleExtractionRule`
- `VehicleOwnershipOrSpawnRule`

如果你想把载具做成核心玩法，需要再补一层“资源与损坏”系统；如果只是地图辅助手段，这套现成能力已经够用。
