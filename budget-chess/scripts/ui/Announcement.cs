using Godot;
using System;

namespace BudgetChess.UI {

  public class Announcement : Node2D {

    [Export] public float DisplayDuration { get; set; }
    
    [Signal] public delegate void OnDisplayFinished();

    private Timer           timer;
    private Label           label;
    private AnimationPlayer animation_player;

    public override void _Ready() {
      base._Ready();
      timer            = GetNode<Timer>("Timer");
      label            = GetNode<Label>("CenterContainer/Label");
      animation_player = label.GetNode<AnimationPlayer>("AnimationPlayer");

      animation_player.Connect("animation_finished", this,
        nameof(OnAnimationFinished));
      timer.Connect("timeout", this, nameof(OnTimerTimeout));
    }

    public void Display(string text) {
      label.Text = text;
      animation_player.Play("Spin");
    }

    private void OnAnimationFinished(string anim_name) {
      if (anim_name == "Spin")
        timer.Start(DisplayDuration);
      else if (anim_name == "Fade")
        EmitSignal(nameof(OnDisplayFinished));
    }

    private void OnTimerTimeout() {
      animation_player.Play("Fade");
    }

  }

}
