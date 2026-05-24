using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace ClientLocal.Views;

public partial class IntegrityWarningDialog : Window
{
    public enum Mode
    {
        Corrupt,   
        External,  
    }

    public IntegrityWarningDialog(Mode mode = Mode.Corrupt)
    {
        InitializeComponent();

        try
        {
            WarningIcon.Source = new Bitmap(
                AssetLoader.Open(new Uri("avares://ClientLocal/Assets/error.png"))
            );
        }
        catch
        {
        }
        ReadOnlyBtn.IsVisible = false;
        TrustBtn.IsVisible = false;

        switch (mode)
        {
            case Mode.External:
                TitleText.Text = "Archivo sin firma detectado";
                DescriptionText.Text = "Este archivo no contiene una firma de EduIDE. Parece haber sido creado o copiado desde fuera.";
                DeleteBtn.IsVisible = true;
                break;

            case Mode.Corrupt:
                TitleText.Text = "El archivo fue modificado fuera de EduIDE.";
                DescriptionText.Text = "La firma de integridad no coincide. Este archivo no puede abrirse ni editarse dentro del IDE.";
                ReadOnlyBtn.IsVisible = false;
                TrustBtn.IsVisible = false;
                DeleteBtn.IsVisible = true;
                RestoreBtn.IsVisible = true;
                break;
        }

        CancelBtn.Click += (_, _) => Close("cancel");
        DeleteBtn.Click += (_, _) => Close("delete");
        RestoreBtn.Click += (_, _) => Close("restore");
    }
}