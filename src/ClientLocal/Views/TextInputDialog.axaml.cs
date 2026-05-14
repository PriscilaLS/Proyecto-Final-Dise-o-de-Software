using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClientLocal.Views;

public partial class TextInputDialog : Window
{
    public TextInputDialog(string title, string label)
    {
        InitializeComponent();
        Title = title;
        Label.Text = label;

        AcceptBtn.Click += (_, _) => Close(Entry.Text);
        CancelBtn.Click += (_, _) => Close(null);
    }
    
}