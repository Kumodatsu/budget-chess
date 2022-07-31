extends ColorRect

export var square_color: Color  setget set_square_color
export var text_color:   Color  setget set_text_color
export var square_size:  float  setget set_square_size

export var file: int = 0 setget set_file
export var rank: int = 0 setget set_rank

signal on_square_selected(square)

func _ready():
  var _r = $Button.connect("pressed", self, "on_selected")

func set_square_color(value: Color):
  color = value

func set_text_color(value: Color):
  $Label.set("custom_colors/font_color", value)

func set_square_size(value: float):
  rect_size = Vector2(value, value)

func set_file(value: int):
  file = value
  set_text(get_square_name())

func set_rank(value: int):
  rank = value
  set_text(get_square_name())

func set_text(value: String):
  $Label.text = value

func get_square_name() -> String:
  return "abcdefghijklmnopqrstuvwxyz".substr(file, 1) + str(rank + 1)

func on_selected():
  emit_signal("on_square_selected", self)
