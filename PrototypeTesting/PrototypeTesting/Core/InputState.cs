namespace PrototypeTesting.Core;

public class InputState
{
    private static readonly HashSet<string> SupportedKeys =
    [
        "KeyW",
        "KeyA",
        "KeyS",
        "KeyD",
        "Space",
        "ShiftLeft",
        "ShiftRight"
    ];

    private readonly HashSet<string> _pressedKeys = [];

    public bool IsPressed(string code) => _pressedKeys.Contains(code);

    public void Reset() => _pressedKeys.Clear();

    public bool Apply(string code, bool isPressed, out InputEdge edge)
    {
        edge = default;

        if (!SupportedKeys.Contains(code))
        {
            return false;
        }

        if (isPressed)
        {
            if (!_pressedKeys.Add(code))
            {
                return false;
            }

            edge = new InputEdge(code, true);
            return true;
        }

        if (!_pressedKeys.Remove(code))
        {
            return false;
        }

        edge = new InputEdge(code, false);
        return true;
    }
}

public readonly record struct InputEdge(string Code, bool IsPressed);
