using PrototypeTesting.Models;

namespace PrototypeTesting.Core;

public sealed class PrototypeCatalogService
{
    private static readonly IReadOnlyList<PrototypeDefinition> Prototypes =
    [
        new(
            "combat-arena",
            "战斗手感验证",
            "Combat",
            "在封闭场地里对抗追击敌人，测试近战攻击、闪避窗口和危险识别是否清晰。",
            "WASD 移动，空格挥砍，Shift 冲刺闪避，点击画布重新聚焦。",
            "击败全部敌人并尽量保持血量，确认基础战斗循环是否成立。"),
        new(
            "movement-lab",
            "移动反馈验证",
            "Movement",
            "复用同一套敌人追击场景，但强调走位与冲刺逃脱是否顺手。",
            "WASD 移动，Shift 冲刺闪避，空格近身反击。",
            "确认玩家是否愿意持续移动并利用位移创造安全窗口。"),
        new(
            "choice-room",
            "选择压力验证",
            "Decision",
            "在更高敌人数压力下测试玩家是否能边移动边做即时选择。",
            "WASD 移动，空格攻击，Shift 闪避。",
            "确认高压状态下，操作和判断是否仍然清楚。")
    ];

    public IReadOnlyList<PrototypeDefinition> GetAll() => Prototypes;

    public PrototypeDefinition? GetById(string id) =>
        Prototypes.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
}
