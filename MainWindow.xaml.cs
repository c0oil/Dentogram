using System;
using System.Linq;
using System.Windows;
using Infragistics.Controls.Maps;

namespace Dentogram
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowModel();
        }

        public MainWindowModel ViewModel => (MainWindowModel) DataContext;

        private void SelectedNodesChanged(object sender, OrgChartNodeSelectionEventArgs e)
        {
            Node node = e.CurrentSelectedNodes.FirstOrDefault()?.Data as Node;
            if (node != null && !node.Children.Any())
            {
                ViewModel.SelectedNode = node;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Start();
        }

        private void UIElement_OnGotFocus(object sender, RoutedEventArgs e)
        {
            OrgChartNodeControl nodeControl = e.OriginalSource as OrgChartNodeControl;
            Node node = nodeControl?.Node.Data as Node;
            if (node != null && !node.Children.Any())
            {
                ViewModel.SelectedNode = node;
            }
        }
    }
}
