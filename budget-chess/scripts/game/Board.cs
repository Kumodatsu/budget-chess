using Godot;

namespace BudgetChess {

  class Board : Control {

    private static readonly PackedScene
      SquareScene =
        ResourceLoader.Load<PackedScene>("res://components/Square.tscn"),
      PieceScene  =
        ResourceLoader.Load<PackedScene>("res://components/Piece.tscn");

    [Export] public float SquareSize       = 72.0f;
    [Export] public Color DarkSquareColor  = Colors.Cornflower;
    [Export] public Color LightSquareColor = Colors.LightBlue;
    [Export] public Color SelectionColor   = new Color(1.0f, 0.75f, 0.15f);
    [Export] public Color TextColor        = new Color(0.3f, 0.3f, 0.3f);

    private BoardState board_state = new BoardState();

    private (SquareNode Square, PieceNode Piece)[,] ui_nodes =
      new (SquareNode, PieceNode)[SquarePos.FileCount, SquarePos.RankCount];
    
    private SquareNode selection = null;

    public override void _Ready() {
      base._Ready();
      
      InitializeSquareNodes();
      InitializePieceNodes();
    }

    private void InitializeSquareNodes() {
      for (int rank = 0; rank < SquarePos.RankCount; rank++)
        for (int file = 0; file < SquarePos.FileCount; file++) {
          var pos  = new SquarePos(file, rank);
          var node = SquareScene.Instance<SquareNode>();
          AddChild(node);
          node.SquarePos   = pos;
          node.Name        = $"Square-{node.SquareName}";
          node.SquareSize  = SquareSize;
          node.SquareColor = GetSquareColor(pos);
          node.TextColor   = TextColor;
          node.SetPosition(GetSquareWorldPos(pos));
          node.Connect(nameof(SquareNode.OnSquareSelected), this,
            nameof(OnSquareSelected));
          ui_nodes[file, rank].Square = node;
        }
    }

    private void InitializePieceNodes() {
      var squares = board_state.GetSquaresOccupied();
      foreach (var pos in squares) {
        var piece = board_state.GetSquare(pos).Value;
        var node  = PieceScene.Instance<PieceNode>();
        AddChild(node);
        node.Name =
          $"Piece-{piece.Player.ToString()}{piece.PieceType.ToString()}";
        node.SquareSize = SquareSize;
        node.Piece      = piece;
        node.Position   = GetSquareWorldPos(pos);
        ui_nodes[pos.File, pos.Rank].Piece = node;
      }
    }

    private Vector2 GetSquareWorldPos(SquarePos pos)
      => SquareSize * new Vector2(
        -SquarePos.FileCount / 2.0f + pos.File,
         SquarePos.RankCount / 2.0f - (pos.Rank + 1)
      );

    private Color GetSquareColor(SquarePos pos)
      => (pos.File + pos.Rank) % 2 == 0 ? DarkSquareColor : LightSquareColor;

    private void OnSquareSelected(SquareNode node) {

    }

  };

}
