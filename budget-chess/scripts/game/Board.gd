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

func _ready():
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
  var squares: Array = board_state.get_occupied_squares()
  for square in squares:
    var piece_info = board_state.get_square(square)
    var piece = Piece.instance()
    add_child(piece)
    piece.set_name("Piece-" + String(piece_info))
    piece.set_square_size(SQUARE_SIZE)
    piece.set_piece(piece_info)
    piece.set_position(get_square_position(square.file, square.rank))
    ui_pieces.append(piece)

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
  reset_selection()
  var file:        int   = square.file
  var rank:        int   = square.rank
  var legal_moves: Array = board_state.get_legal_moves()
  for move in legal_moves:
    if move.source.file == file and move.source.rank == rank:
      get_ui_square(move.destination).modulate = SELECTION_COLOR

func get_ui_square(pos: BoardState.SquarePos) -> Node:
  return ui_squares[pos.rank][pos.file]

func reset_selection():
  for rank in ui_squares:
    for square in rank:
      square.modulate = Color.white

# func on_square_selected(square: Node):
#   var file: int = square.file
#   var rank: int = square.rank
#   var piece = get_piece_at_square(file, rank)
#   if not piece or piece.player != turn:
#     return
#   print(file)
#   print(rank)
#   print(piece.piece_type)
#   var legal_moves: Array = []
#   match piece.piece_type:
#     PieceType.King:
#       for df in [-1, 0, 1]:
#         for dr in [-1, 0, 1]:
#           if df == 0 and dr == 0:
#             continue
#           var f = file + df
#           var r = rank + dr
#           if is_valid(f, r):
#             var target = get_piece_at_square(f, r)
#             if target.player != piece.player:
#               legal_moves.append([f, r])
#     _:
#       pass
#   for move in legal_moves:
#     print(move)
#     var move_square = get_square(move[0], move[1])
#     move_square.modulate = Color.orange

# func get_square(file: int, rank: int) -> Node:
#   return board_state[file][rank][0]

# func is_valid(file: int, rank: int) -> bool:
#   return file >= 0 and file < N_FILES and rank >= 0 and rank < N_RANKS

