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
      
      board_state.OnMoveMade += OnMoveMade;

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

    private void ResetSelection() {
      selection = null;
      for (int rank = 0; rank < SquarePos.RankCount; rank++)
        for (int file = 0; file < SquarePos.FileCount; file++)
          ui_nodes[file, rank].Square.Modulate = Colors.White;
    }

    private void OnSquareSelected(SquareNode node) {
      var turn  = board_state.Turn;
      var piece = board_state.GetSquare(node.SquarePos);
      if (selection != null) {
        if (!piece.HasValue || piece.Value.Player != turn) {
          board_state.MakeMove(new Ply(
            selection.SquarePos,
            node.SquarePos
          ));
          ResetSelection();
          return;
        }
        ResetSelection();
      } else if (piece.HasValue && piece.Value.Player != turn) {
        return;
      }

      foreach (var legal in board_state.GetLegalMoves()) {
        if (legal.Source == node.SquarePos) {
          ui_nodes[legal.Destination.File, legal.Destination.Rank]
            .Square.Modulate = SelectionColor;
        }
      }

      selection = node;
    }

    private void OnMoveMade(
      Ply        ply,
      Player     next_player,
      SquarePos? capture_square
    ) {
      if (capture_square.HasValue)
        GetPieceNode(capture_square.Value).QueueFree();
      var piece = GetPieceNode(ply.Source);
      SetPieceNode(ply.Source,      null);
      SetPieceNode(ply.Destination, piece);
      piece.Position = GetSquareWorldPos(ply.Destination);
    }

    private SquareNode GetSquareNode(SquarePos pos)
      => ui_nodes[pos.File, pos.Rank].Square;
    private void SetSquareNode(SquarePos pos, SquareNode node)
      => ui_nodes[pos.File, pos.Rank].Square = node;
    private PieceNode GetPieceNode(SquarePos pos)
      => ui_nodes[pos.File, pos.Rank].Piece;
    private void SetPieceNode(SquarePos pos, PieceNode node)
      => ui_nodes[pos.File, pos.Rank].Piece = node;

  };

}
