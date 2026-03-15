using System.Windows;

namespace NativeHymnsApp.Views;

public partial class ContentEditorWindow : Window
{
    public ContentEditorWindow(string dialogTitle, string instructions, string initialTitle = "", string initialContent = "")
    {
        InitializeComponent();
        Title = dialogTitle;
        InstructionsText.Text = instructions;
        TitleTextBox.Text = initialTitle;
        ContentTextBox.Text = initialContent;
    }

    public string EntryTitle => TitleTextBox.Text.Trim();

    public string EntryContent => ContentTextBox.Text.Trim();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryTitle) || string.IsNullOrWhiteSpace(EntryContent))
        {
            System.Windows.MessageBox.Show(
                "Both title and content are required.",
                "Incomplete Entry",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
