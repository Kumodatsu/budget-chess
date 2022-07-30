extends Button

export var target_scene: PackedScene

func _ready():
  var _r = connect("button_up", self, "on_button_up")

func on_button_up():
  var _r = get_tree().change_scene_to(target_scene)
