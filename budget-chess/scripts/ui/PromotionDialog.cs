using System;
using Godot;

namespace BudgetChess.UI {

  class PromotionDialog : Node2D {
    
    private Control      control;
    private OptionButton option_button;

    [Signal] public delegate void OnPieceSelected(PieceType piece_type);

    public bool IsActive => control.Visible;

    public override void _Ready() {
      base._Ready();
      control       = GetNode<Control>("Control");
      option_button = control.GetNode<OptionButton>("OptionButton");

      option_button.AddItem("Queen");
      option_button.AddItem("Rook");
      option_button.AddItem("Bishop");
      option_button.AddItem("Knight");

      option_button.Connect("item_selected", this, nameof(OnItemSelected));
    }

    public void PopUp() {
      control.Visible = true;
    }

    private void OnItemSelected(int id) {
      PieceType piece_type;
      switch (id) {
        case 0: piece_type = PieceType.Queen;  break;
        case 1: piece_type = PieceType.Rook;   break;
        case 2: piece_type = PieceType.Bishop; break;
        case 3: piece_type = PieceType.Knight; break;
        default: throw new InvalidOperationException("Invalid promotion type.");
      }
      EmitSignal(nameof(OnPieceSelected), piece_type);
      control.Visible = false;
    }
    
  }

}
