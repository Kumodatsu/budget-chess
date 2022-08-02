extends Control

const BoardState = preload("res://scripts/game/BoardState.gd")
const Square     = preload("res://components/Square.tscn")
const Piece      = preload("res://components/Piece.tscn")

const SQUARE_SIZE:        float = 72.0
const DARK_SQUARE_COLOR:  Color = Color.cornflower
const LIGHT_SQUARE_COLOR: Color = Color.lightblue
const SELECTION_COLOR:    Color = Color(1.0, 0.75, 0.15)
const TEXT_COLOR:         Color = Color(0.3, 0.3, 0.3)

var board_state: BoardState = BoardState.new()

var ui_squares:  Array      = []
var ui_pieces:   Array      = []
var selection:   Node       = null

func _ready():
  var _r = board_state.connect("on_move_made", self, "on_move_made")
  initialize_board_graphics()
  initialize_piece_graphics()

func initialize_board_graphics():
  for rank in board_state.N_RANKS:
    ui_squares.append([])
    for file in board_state.N_FILES:
      var square = Square.instance()
      add_child(square)
      square.set_name("Square-" + square.get_square_name())
      square.set_file(file)
      square.set_rank(rank)
      square.set_square_size(SQUARE_SIZE)
      square.set_square_color(get_square_color(file, rank))
      square.set_position(get_square_position(file, rank))
      square.set_text_color(TEXT_COLOR)
      var _r = square.connect("on_square_selected", self, "on_square_selected")
      ui_squares[rank].append(square)

func initialize_piece_graphics():
  for rank in board_state.N_RANKS:
    ui_pieces.append([])
    for file in board_state.N_FILES:
      ui_pieces[rank].append(null)
  var squares: Array = board_state.get_occupied_squares()
  for square in squares:
    var file: int = square.file
    var rank: int = square.rank
    var piece_info = board_state.get_square(square)
    var piece = Piece.instance()
    add_child(piece)
    piece.set_name("Piece-" + String(piece_info))
    piece.set_square_size(SQUARE_SIZE)
    piece.set_piece(piece_info)
    piece.set_position(get_square_position(square.file, square.rank))
    ui_pieces[rank][file] = piece

func get_square_position(file: int, rank: int) -> Vector2:
  return SQUARE_SIZE * Vector2(
    -board_state.N_FILES / 2.0 + file,
     board_state.N_RANKS / 2.0 - (rank + 1)
  )

func get_square_color(file: int, rank: int) -> Color:
  if (file + rank) % 2 == 0:
    return DARK_SQUARE_COLOR
  else:
    return LIGHT_SQUARE_COLOR

func on_square_selected(square: Node):
  var turn: int = board_state.get_turn()
  var file: int = square.file
  var rank: int = square.rank
  var pos = BoardState.SquarePos.new(file, rank)
  if selection:
    if board_state.get_square(pos) & turn == 0:
      var move_result = board_state.make_move(BoardState.Ply.new(
        BoardState.SquarePos.new(selection.file, selection.rank),
        pos
      ))
      match move_result:
        BoardState.MoveResult.Check:
          print("CHECK")
        BoardState.MoveResult.Checkmate:
          var winner_str = "WHITE" \
            if board_state.get_turn() == BoardState.Player.White else "BLACK"
          print(winner_str + " WINS: CHECKMATE")
        BoardState.MoveResult.Stalemate:
          print("DRAW: STALEMATE")
        _:
          print(move_result)
      reset_selection()
      return
    reset_selection()
  elif board_state.get_square(pos) & board_state.other_player(turn) != 0:
    return

  var legal_moves: Array = board_state.get_legal_moves()
  for move in legal_moves:
    if move.source.file == file and move.source.rank == rank:
      get_ui_square(move.destination).modulate = SELECTION_COLOR
  selection = square

func on_move_made(
    ply:            BoardState.Ply, 
    _next_player:   int,
    capture_square: BoardState.SquarePos,
    _move_result:   int):
  if capture_square:
    ui_pieces[capture_square.rank][capture_square.file].queue_free()
  var piece = ui_pieces[ply.source.rank][ply.source.file]
  ui_pieces[ply.source.rank][ply.source.file] = null
  ui_pieces[ply.destination.rank][ply.destination.file] = piece
  piece.set_position(get_square_position(
    ply.destination.file, ply.destination.rank
  ))
  pass

func get_ui_square(pos: BoardState.SquarePos) -> Node:
  return ui_squares[pos.rank][pos.file]

func reset_selection():
  selection = null
  for rank in ui_squares:
    for square in rank:
      square.modulate = Color.white
