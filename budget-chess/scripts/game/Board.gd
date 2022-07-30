extends Control

const N_FILES:           int   = 8
const N_RANKS:           int   = 8
const SQUARE_SIZE:       float = 72.0
const ODD_SQUARE_COLOR:  Color = Color.lightblue
const EVEN_SQUARE_COLOR: Color = Color.cornflower
const TEXT_COLOR:        Color = Color(0.3, 0.3, 0.3)

func _ready() -> void:
  initialize_board()

func initialize_board():
  for file in N_FILES:
    for rank in N_RANKS:
      var square = preload("res://components/Square.tscn").instance()
      add_child(square)
      square.set_file(file)
      square.set_rank(rank)
      square.set_name("Square-" + square.get_square_name())
      square.set_square_size(SQUARE_SIZE)
      square.set_square_color(get_square_color(file, rank))
      square.set_position(get_square_position(file, rank))
      square.set_text_color(TEXT_COLOR)

func get_square_position(file: int, rank: int) -> Vector2:
  return SQUARE_SIZE * Vector2(
    -N_FILES / 2.0 + file,
     N_RANKS / 2.0 - (rank + 1)
  )

func get_square_color(file: int, rank: int) -> Color:
  if (file + rank) % 2 == 0:
    return EVEN_SQUARE_COLOR
  else:
    return ODD_SQUARE_COLOR
