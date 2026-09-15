# 几何槽位与面内容分离

原版 Hundred Dice 用同一个整数表示「哪一个采样点最高」和「这颗骰子掷出几点」，统计、信号、UI 全部依赖 `rolled_side: int`。若继续把 Core 建成 `int Side`，自定义贴图、运行时改面、卡牌/RPG 载荷都只能往整数上打补丁，以后无法干净地嵌入其他游戏。

**决定：** 朝上判定只返回 FaceSlot（几何）；FaceLayout 把槽位映射为 FaceContent（含义与展示）。Session 与对外事件使用 RollOutcome（槽位 + 内容快照）。数值合计是可选的 NumericScore 查询，不是 Core 的主模型。

## Considered Options

1. **保留 int 面值，贴图当皮肤** — 移植最快，但 RPG 面（剑/盾/暴击）没有第一类身份，动态改面只能改贴图不能改规则。
2. **槽位与内容分离（采用）** — 壳体与判定算法保持原版；内容表可按实例替换。演示应用用标准数字 FaceLayout，对等原版 Sum。
3. **面结果做成无约束的 `object`/`Variant`** — 最灵活，但 Hundred Dice 对等、测试与 UI 都会失去结构。

## Consequences

- 第一期必须出现 `FaceSlotId`、`FaceContent`、`FaceLayout`、`RollOutcome` 类型，即使演示仍显示 1–20。
- `NumericScore.Sum` 不能作为唯一读取结果的方式；宿主应订阅 RollOutcome。`IDiceTable` 没有 `Sum`。
- 自定义图片是 Presenter 适配器，不进入 Core；默认 `BakedNumeralPresenter`，换图用 `ImageFacePresenter`（Decal）。
- 原 GDScript 的 `sides: { 点数: Vector3 }` 在移植时拆成「采样点表」和「数字 FaceLayout」两份数据，采样向量保持原值。
