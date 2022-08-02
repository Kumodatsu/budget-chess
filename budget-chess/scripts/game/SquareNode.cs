using Godot;

namespace BudgetChess {

  class SquareNode : ColorRect {

    [Export] public Color SquareColor {
      get => this.Color;
      set => this.Color = value;
    }

    [Export] public Color TextColor {
      get => (Color) label.Get("custom_colors/font_color");
      set => label.Set("custom_colors/font_color", value);
    }

    [Export] public float SquareSize {
      get => RectSize.x;
      set => RectSize = new Vector2(value, value);
    }

    public SquarePos SquarePos {
      get => square_pos;
      set {
        square_pos = value;
        label.Text = SquareName;
      }
    }

    [Signal] public delegate void OnSquareSelected(SquareNode square);

    private Label  label;
    private Button button;

    private SquarePos square_pos;

    public override void _Ready() {
      base._Ready();
      label  = GetNode<Label>("Label");
      button = GetNode<Button>("Button");

      button.Connect("pressed", this, nameof(OnSelected));
    }

    public string SquareName
      => $"{(char) ('a' + square_pos.File)}{square_pos.Rank + 1}";

    public void OnSelected()
      => EmitSignal(nameof(OnSquareSelected), this);

  };

}
