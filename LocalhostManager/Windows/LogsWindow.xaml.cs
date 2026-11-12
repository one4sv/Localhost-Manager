using LocalhostManager.ViewModels;
using System.Collections.Specialized;
using System.Windows;

namespace LocalhostManager
{
    public partial class LogsWindow : Window
    {
        private System.Windows.Data.ListCollectionView? _view;
        private void Search_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => _view?.Refresh();
        private void ColorLogRow(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBoxItem row)
                row.SetResourceReference(ForegroundProperty, row.Content?.ToString()?.Contains("[stderr]") == true ? "DangerBrush" : "TextBrush");
        }
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var lines = LogsList.SelectedItems.Count > 0 ? LogsList.SelectedItems.Cast<string>() : LogsList.Items.Cast<string>();
            var text = string.Join(Environment.NewLine, lines);
            if (text.Length > 0) System.Windows.Clipboard.SetText(text);
        }
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Log (*.log)|*.log", FileName = "service.log" };
            if (dialog.ShowDialog(this) == true)
                try { System.IO.File.WriteAllLines(dialog.FileName, LogsList.Items.Cast<string>()); }
                catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
        }
        public LogsWindow(LocalhostViewModel localhost)
        {
            InitializeComponent();

            DataContext = localhost;
            _view = new System.Windows.Data.ListCollectionView(localhost.Logs);
            _view.Filter = row => row is string line && line.Contains(Search.Text, StringComparison.OrdinalIgnoreCase);
            LogsList.ItemsSource = _view;

            localhost.Logs.CollectionChanged += Logs_CollectionChanged;

            Closed += (_, _) =>
            {
                localhost.Logs.CollectionChanged -= Logs_CollectionChanged;
            };
        }

        private bool _scrollPending;
        private void Logs_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (_scrollPending || Paused.IsChecked == true) return;
            _scrollPending = true;
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                _scrollPending = false;
                if (Paused.IsChecked == true || LogsList.Items.Count == 0)
                {
                    return;
                }

                LogsList.ScrollIntoView(
                    LogsList.Items[^1]
                );
            }));
        }
    }
}