# DiceRoller

物理骰子在 3D 场地中被掷出；朝上的是几何槽位，槽位上的含义由可变的面内容提供。本词汇表约束 Core 与宿主游戏的用语，避免把「几点」和「哪一面」写成同一个整数。

## Language

### 几何

**HullKind**:
一种多面体壳体（d4、d6、d8、d10、d12、d20），只包含碰撞、网格锚点与 FaceSlot 采样点。
_Avoid_: die type, dice type（这两个词在原项目里同时表示壳体和含义）

**FaceSlot**:
壳体上的一个几何面。身份由 FaceSlotId 固定，不随贴图或规则改变。
_Avoid_: side（单独使用时分不清几何还是点数）

**FaceSlotId**:
FaceSlot 的稳定标识。经典骰与原版脚本对齐为 1..N；0 不是槽位。
_Avoid_: 把 pip、点数、side index 混成同一个 int

**Invalid**:
没有唯一朝上 FaceSlot 的姿态。它不是一种 FaceContent，也不占用 FaceSlotId。
_Avoid_: 点数 0, rolled_side = 0（作为内容）

### 含义

**FaceContent**:
某个 FaceSlot 当前代表的东西：稳定 Id、展示文案、可选数值、可选贴图键、可选宿主载荷。
_Avoid_: side, pip, rolled_side（当作唯一结果类型）

**FaceLayout**:
一份 FaceSlotId → FaceContent 的对照表。生成时按实例复制；运行期可改某一格而不改壳体。
_Avoid_: sides 字典（原 GDScript 把采样点与点数写在同一张表里）

**DieDefinition**:
生成一颗骰子所需的壳体种类、默认 FaceLayout，以及 Presenter 选择。
_Avoid_: 只用 `"d6"` 字符串表示「这颗骰子是什么」

**RollOutcome**:
一次朝上读取的结果：Invalid，或 FaceSlotId 加上当时的 FaceContent 快照。
_Avoid_: int side, die_rolled(type, rolled_side)

**NumericScore**:
对若干 RollOutcome 的可选查询，只加总 FaceContent 上的数值。卡牌/RPG 宿主可以不用它。
_Avoid_: 把 Session 本身做成「点数账本」

### 实例与呈现

**Die**:
场上的一颗刚体实例，拥有 HullKind、FaceLayout 与交互状态。
_Avoid_: 用 Type 字符串代替整颗实例的身份

**Presenter**:
把 FaceLayout 画到网格上的适配器（烘焙数字、分面贴图、Decal 等）。Core 不引用 Texture2D。
_Avoid_: 在判定逻辑里读材质或 UV

**Session**:
当局仍在场的 Die 及其最近一次 RollOutcome。不落盘。
_Avoid_: DiceData 里只存 int 面值
