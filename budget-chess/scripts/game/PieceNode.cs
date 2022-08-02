using Godot;

namespace BudgetChess {

  class PieceNode : Node2D {

    [Export] public float SquareSize {
      get => texture_rect.RectSize.x;
      set => texture_rect.RectSize = new Vector2(value, value);
    }

    public Piece Piece {
      get => piece;
      set {
        piece = value;

        var player_str = piece.Player.ToString().ToLower();
        var type_str   = piece.PieceType.ToString().ToLower();
        texture_rect.Texture = ResourceLoader.Load<Texture>(
          $"res://assets/sprites/chessmen/{player_str}-{type_str}.png"
        );
      }
    }

    private TextureRect texture_rect;

    private Piece piece;

    public override void _Ready() {
      base._Ready();
      texture_rect = GetNode<TextureRect>("TextureRect");
    }

  }

}
