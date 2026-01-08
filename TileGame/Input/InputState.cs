using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TileGame.Input;

public sealed class InputState
{
    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private int _prevScroll;

    public KeyboardState KeyboardState { get; private set; }
    public MouseState MouseState { get; private set; }

    public int MouseWheelDelta { get; private set; }

    public bool LeftClickPressed { get; private set; }
    public Point LeftClickScreenPos { get; private set; }

    public void Update()
    {
        KeyboardState = Keyboard.GetState();
        MouseState = Mouse.GetState();

        // Mouse wheel
        int scroll = MouseState.ScrollWheelValue;
        MouseWheelDelta = scroll - _prevScroll;
        _prevScroll = scroll;

        // Left click edge
        bool leftPressed = MouseState.LeftButton == ButtonState.Pressed;
        bool leftWasPressed = _prevMouse.LeftButton == ButtonState.Pressed;
        LeftClickPressed = leftPressed && !leftWasPressed;
        if (LeftClickPressed)
            LeftClickScreenPos = MouseState.Position;

        _prevMouse = MouseState;
    }

    public bool Pressed(Keys key) => KeyboardState.IsKeyDown(key) && !_prevKb.IsKeyDown(key);

    public void CommitFrame()
    {
        _prevKb = KeyboardState;
    }
}