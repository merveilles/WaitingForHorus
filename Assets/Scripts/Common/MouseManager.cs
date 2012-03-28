using System;
using UnityEngine;

public class MouseManager : MonoBehaviour, IMouse
{
    public static int DraggingThreshold = 5;

    MouseState lastState;

    static MouseManager instance;
    public static IMouse Instance 
    {
        get 
        {
            if (instance == null) 
            {
                instance =  FindObjectOfType(typeof (MouseManager)) as MouseManager;
                if (instance == null)
                    throw new InvalidOperationException("No instance in scene!");
            }
            return instance;
        }
    }
    void OnApplicationQuit() 
    {
        instance = null;
    }
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public MouseButtonInfo LeftButton { get; private set; }
    public MouseButtonInfo MiddleButton { get; private set; }
    public MouseButtonInfo RightButton { get; private set; }
    public Point Position { get; private set; }
    public Point Movement { get; private set; }

    void Update()
    {
        var state = GetState();

        Movement = new Point(state.X - Position.X, state.Y - Position.Y);
        Position = new Point(state.X, state.Y);

        if (state != lastState)
        {
            bool hasMoved = Movement.X != 0 || Movement.Y != 0;

            LeftButton = DeduceMouseButtonState(LeftButton, lastState.LeftButton, state.LeftButton, hasMoved);
            MiddleButton = DeduceMouseButtonState(MiddleButton, lastState.MiddleButton, state.MiddleButton, hasMoved);
            RightButton = DeduceMouseButtonState(RightButton, lastState.RightButton, state.RightButton, hasMoved);

            lastState = state;
        }
        else
        {
            LeftButton = ResetButton(LeftButton);
            MiddleButton = ResetButton(MiddleButton);
            RightButton = ResetButton(RightButton);
        }
    }

    MouseButtonInfo DeduceMouseButtonState(MouseButtonInfo lastMouseButtonState, bool lastButtonState, bool buttonState, bool hasMoved)
    {
        if (!lastButtonState && !buttonState)
            return new MouseButtonInfo(MouseButtonState.Idle);

        if (!lastButtonState)
            return new MouseButtonInfo(MouseButtonState.Pressed, new MouseDragState(Position, Position));

        if (buttonState)
            if (hasMoved)
            {
                if (Math.Abs(Position.X - lastMouseButtonState.DragState.Start.X) > DraggingThreshold ||
                    Math.Abs(Position.Y - lastMouseButtonState.DragState.Start.Y) > DraggingThreshold)
                    if (lastMouseButtonState.State == MouseButtonState.DragStarted || lastMouseButtonState.State == MouseButtonState.Dragging)
                        return new MouseButtonInfo(MouseButtonState.Dragging, new MouseDragState(lastMouseButtonState.DragState, Position));
                    else
                        return new MouseButtonInfo(MouseButtonState.DragStarted, new MouseDragState(lastMouseButtonState.DragState, Position));

                return new MouseButtonInfo(MouseButtonState.Pressed, new MouseDragState(lastMouseButtonState.DragState, Position));
            }
            else
                return lastMouseButtonState;

        if ((lastMouseButtonState.State == MouseButtonState.Pressed || lastMouseButtonState.State == MouseButtonState.Down) && !hasMoved)
            return new MouseButtonInfo(MouseButtonState.Clicked);

        return new MouseButtonInfo(MouseButtonState.DragEnded);
    }

    MouseButtonInfo ResetButton(MouseButtonInfo button)
    {
        switch (button.State)
        {
            case MouseButtonState.Pressed:
                return new MouseButtonInfo(MouseButtonState.Down, button.DragState);

            case MouseButtonState.DragEnded:
            case MouseButtonState.Clicked:
                return new MouseButtonInfo(MouseButtonState.Idle);

            case MouseButtonState.DragStarted:
                return new MouseButtonInfo(MouseButtonState.Dragging, button.DragState);

            case MouseButtonState.Dragging:
                if (Movement.X != button.DragState.Movement.X || Movement.Y != button.DragState.Movement.Y)
                    return new MouseButtonInfo(MouseButtonState.Dragging, new MouseDragState(button.DragState, Position));
                break;
        }
        return button;
    }

    static MouseState GetState()
    {
        return new MouseState((int)Input.mousePosition.x, (int)Input.mousePosition.y,
                              Input.GetMouseButton(0), Input.GetMouseButton(2), Input.GetMouseButton(1));
    }
}

public interface IMouse
{
    MouseButtonInfo LeftButton { get; }
    MouseButtonInfo MiddleButton { get; }
    MouseButtonInfo RightButton { get; }
    Point Position { get; }
    Point Movement { get; }
}

public struct MouseButtonInfo
{
    readonly MouseDragState dragState;
    readonly MouseButtonState state;

    internal MouseButtonInfo(MouseButtonState state) : this(state, new MouseDragState()) { }
    internal MouseButtonInfo(MouseButtonState state, MouseDragState dragState)
    {
        this.dragState = dragState;
        this.state = state;
    }

    public MouseButtonState State
    {
        get { return state; }
    }

    public MouseDragState DragState
    {
        get { return dragState; }
    }
}

public struct MouseDragState
{
    readonly Point start;
    readonly Point difference;
    readonly Point position;
    readonly Point movement;

    internal MouseDragState(MouseDragState lastState, Point position) : this(lastState.start, lastState.position, position) { }
    internal MouseDragState(Point start, Point position) : this(start, start, position) { }
    MouseDragState(Point start, Point lastPosition, Point position)
    {
        this.start = start;
        this.position = position;

        difference = new Point(position.X - start.X, position.Y - start.Y);
        movement = new Point(position.X - lastPosition.X, position.Y - lastPosition.Y);
    }

    public Point Start
    {
        get { return start; }
    }

    public Point Movement
    {
        get { return movement; }
    }

    public Point Difference
    {
        get { return difference; }
    }
}

public enum MouseButtonState
{
    Idle, Clicked, DragStarted, Dragging, Pressed, Down, DragEnded
}

public struct MouseState : IEquatable<MouseState>
{
    public readonly int X, Y;
    public readonly bool LeftButton;
    public readonly bool MiddleButton;
    public readonly bool RightButton;

    public MouseState(int x, int y, bool leftButton, bool middleButton, bool rightButton)
    {
        X = x;
        Y = y;
        LeftButton = leftButton;
        MiddleButton = middleButton;
        RightButton = rightButton;
    }

    public static bool operator==(MouseState a, MouseState b)
    {
        return a.X == b.X &&
               a.Y == b.Y &&
               a.LeftButton == b.LeftButton &&
               a.MiddleButton == b.MiddleButton &&
               a.RightButton == b.RightButton;
    }
    public static bool operator !=(MouseState a, MouseState b)
    {
        return a.X != b.X ||
               a.Y != b.Y ||
               a.LeftButton != b.LeftButton ||
               a.MiddleButton != b.MiddleButton ||
               a.RightButton != b.RightButton;
    }

    public bool Equals(MouseState other)
    {
        return this == other;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != typeof (MouseState)) return false;
        return Equals((MouseState) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int result = X;
            result = (result * 397) ^ Y;
            result = (result * 397) ^ LeftButton.GetHashCode();
            result = (result * 397) ^ MiddleButton.GetHashCode();
            result = (result * 397) ^ RightButton.GetHashCode();
            return result;
        }
    }
}
