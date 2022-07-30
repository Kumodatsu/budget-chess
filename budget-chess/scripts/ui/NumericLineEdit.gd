extends LineEdit

export var min_value: int = 0
export var max_value: int = 100
export var value:     int = 0   setget set_value, get_value

var regex: RegEx = null

func _ready():
  var _r
  regex = RegEx.new()
  _r = regex.compile("^[0-9]*$")
  _r = connect("text_changed", self, "on_text_changed")
  _r = connect("text_entered", self, "on_text_entered")
  set_value(value)

func set_value(value_: int):
  value = int(clamp(value_, min_value, max_value))
  text  = String(value)

func get_value():
  return value

func on_text_changed(new_text: String):
  if not regex.search(new_text):
    text = String(value)

func on_text_entered(new_text: String):
  set_value(int(new_text))
