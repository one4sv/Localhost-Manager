using LocalhostManager.ViewModels;
using System.Collections.Specialized;
using System.Windows;

namespace LocalhostManager
{
    public partial class LogsWindow : Window
    {
        public LogsWindow(LocalhostViewModel localhost)
        {
            InitializeComponent();

            DataContext = localhost;

            localhost.Logs.CollectionChanged += Logs_CollectionChanged;

            Closed += (_, _) =>
            {
                localhost.Logs.CollectionChanged -= Logs_CollectionChanged;
            };
        }

        private void Logs_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (LogsList.Items.Count == 0)
                {
                    return;
                }

                LogsList.ScrollIntoView(
                    LogsList.Items[^1]
                );
            });
        }
    }
}