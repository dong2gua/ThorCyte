﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using ThorCyte.ProtocolModule.Events;
using ThorCyte.ProtocolModule.Models;
using ThorCyte.ProtocolModule.ViewModels;

namespace ThorCyte.ProtocolModule.Views
{
    /// <summary>
    /// Interaction logic for ProtocolView.xaml
    /// </summary>
    public partial class MacroEditor : UserControl
    {
        #region Events

        //  public static event MacroEditSizeChangedHandler MacroEditSizeChanged;

        public delegate void CreateModuleHandler(Point location);

        #endregion

        #region Properties and Fields

        public MainWindowViewModel ViewModel
        {
            get { return (MainWindowViewModel)DataContext; }
        }

        private static MacroEditor _macroEdit;

        public static MacroEditor Instance
        {
            get { return _macroEdit ?? (_macroEdit = ServiceLocator.Current.GetInstance<MacroEditor>()); }
        }

        public CreateModuleHandler CreateModule;

        #endregion

        #region Constructors

        public MacroEditor()
        {
            InitializeComponent();
            serTb.Text = DEFAULT_KEYWORD;
            DataContext = MainWindowViewModel.Instance;
            ServiceLocator.Current.GetInstance<IUnityContainer>().RegisterInstance<MacroEditor>(this);
        }

        #endregion

        #region Methods

        private void OnCreateModule(Point location)
        {
            if (CreateModule != null)
            {
                CreateModule(location);
            }
        }

        /// <summary>
        /// Event raised when the user has started to drag out a _connection.
        /// </summary>
        private void networkControl_ConnectionDragStarted(object sender, ConnectionDragStartedEventArgs e)
        {
            var draggedOutConnector = (PortModel)e.ConnectorDraggedOut;
            var curDragPoint = Mouse.GetPosition(pannel);

            // Delegate the real work to the view model.
            var connection = ViewModel.ConnectionDragStarted(draggedOutConnector, curDragPoint);

            // Must return the view-model object that represents the _connection via the event args.
            // This is so that NetworkView can keep track of the object while it is being dragged.
            e.Connection = connection;
        }

        /// <summary>
        /// Event raised while the user is dragging a _connection.
        /// </summary>
        private void networkControl_ConnectionDragging(object sender, ConnectionDraggingEventArgs e)
        {
            var curDragPoint = Mouse.GetPosition(pannel);
            var connection = (ConnectorModel)e.Connection;
            ViewModel.ConnectionDragging(connection, curDragPoint);
        }

        /// <summary>
        /// Event raised when the user has finished dragging out a _connection.
        /// </summary>
        private void networkControl_ConnectionDragCompleted(object sender, ConnectionDragCompletedEventArgs e)
        {
            var connectorDraggedOut = (PortModel)e.ConnectorDraggedOut;
            var connectorDraggedOver = (PortModel)e.ConnectorDraggedOver;
            var newConnection = (ConnectorModel)e.Connection;
            ViewModel.ConnectionDragCompleted(newConnection, connectorDraggedOut, connectorDraggedOver);
        }

        /// <summary>
        /// Event raised to delete the selected _module.
        /// </summary>
        private void DeleteSelectedNodes_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.DeleteSelectedModules();
        }

        /// <summary>
        /// create new _module if no _module is selected
        /// </summary>
        private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = ViewModel.PannelVm.SelectedViewItem;
            if (selectedItem != null && selectedItem.ItemType != ModuleType.None)
            {
                var newNodeLocation = Mouse.GetPosition(pannel);
                OnCreateModule(newNodeLocation);
            }

            if (selectedItem != null)
            {
                selectedItem.IsSelected = false;
            }
        }

        private void OnTreeviewSelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tree = sender as TreeView;
            if (tree != null)
            {
                ViewModel.PannelVm.SelectedViewItem = tree.SelectedItem as TreeViewItemModel;
            }
        }

        private bool IsChildInTree(DependencyObject child, Type parentType)
        {
            var parent = child;
            while (parent != null)
            {
                if (VisualTreeHelper.GetParent(parent) == null)
                {
                    return false;
                }
                if (parent.GetType() == parentType)
                {
                    return true;
                }
                parent = VisualTreeHelper.GetParent(parent);

                if (parent.GetType() == parentType)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            var isModule = IsChildInTree((DependencyObject)e.OriginalSource, typeof(Module));
            if (isModule)
            {
                var vm = ViewModel.GetSelectedModule();
                ViewModel.PannelVm.SelectedModuleViewModel = vm;
            }
            else
            {
                ViewModel.PannelVm.UnSelectedAll();
                ViewModel.PannelVm.SelectedModuleViewModel = null;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ReAnalysisImage();
        }

        private void PannelKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                ViewModel.PannelVm.SelectedModuleViewModel = null;
            }
        }

        private void ToolboxClick(object sender, RoutedEventArgs e)
        {
            var isChecked = (bool)(sender as ToggleButton).IsChecked;
            if (isChecked)
            {
                treeview.Visibility = Visibility.Collapsed;
                splitter1.Visibility = Visibility.Collapsed;
                Grid.SetColumn(PannelBorder, 0);
                Grid.SetColumnSpan(PannelBorder, 3);
            }
            else
            {
                treeview.Visibility = Visibility.Visible;
                splitter1.Visibility = Visibility.Visible;
                Grid.SetColumn(PannelBorder, 2);
                Grid.SetColumnSpan(PannelBorder, 1);
            }
        }

        private List<GridLength> recentGridLengths = new List<GridLength>();

        private void CollapseClick(object sender, RoutedEventArgs e)
        {
            var isChecked = (bool)(sender as ToggleButton).IsChecked;
            if (isChecked)
            {
                recentGridLengths.Clear();
                foreach (var t in gridMain.ColumnDefinitions)
                {
                    recentGridLengths.Add(t.Width);
                }

                //Collapse
                gridMain.ColumnDefinitions[0].Width = new GridLength(0);
                gridMain.ColumnDefinitions[1].Width = new GridLength(0);
                gridMain.ColumnDefinitions[2].Width = new GridLength(0);
                gridMain.ColumnDefinitions[3].Width = new GridLength(0);
                gridMain.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);

                expandcollapse.Source = (BitmapImage)Resources["expandImg"];

            }
            else
            {
                for (var i = 0; i < gridMain.ColumnDefinitions.Count; i++)
                {
                    gridMain.ColumnDefinitions[i].Width = recentGridLengths[i];
                }

                expandcollapse.Source = (BitmapImage)Resources["collapseImg"];
            }
        }

        private string _searchKeyword;
        private const string DEFAULT_KEYWORD = "Find...";

        private void SerchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var searchBox = sender as TextBox;
            if (searchBox == null) return;

            if (searchBox.Text == DEFAULT_KEYWORD && Equals(searchBox.Foreground, Brushes.LightGray))
            {
                searchBox.Text = string.Empty;
                searchBox.Foreground = (Brush)ForegroundProperty.DefaultMetadata.DefaultValue;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var searchBox = sender as TextBox;
            if (searchBox == null) return;

            if (searchBox.Text == string.Empty)
            {
                searchBox.Text = DEFAULT_KEYWORD;
                searchBox.Foreground = Brushes.LightGray;
            }
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            _searchKeyword = ((TextBox)sender).Text;
            SetModuleSelection(_searchKeyword);
        }

        private void SetModuleSelection(string moduleKeyWord)
        {
            //collpse tree view 
            ManipulateTree(treeview, false);
            ViewModel.PannelVm.FilterModuleInfo(moduleKeyWord);
            if (moduleKeyWord == string.Empty)
            {
                ManipulateTree(treeview, false);
                return;
            }
            //Expand treeview
            ManipulateTree(treeview, true);
        }


        /// <summary>
        /// Collapse or Expand treeview 
        /// </summary>
        /// <param name="treeContainer">treeview need to operate</param>
        /// <param name="mode">true--Expand false--Collapse</param>
        private void ManipulateTree(ItemsControl treeContainer, bool mode)
        {
            var inStyle = new Style
            {
                TargetType = typeof(TreeViewItem),
                BasedOn = (Style)FindResource(typeof (TreeViewItem))
            };

            inStyle.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, mode));
            treeContainer.ItemContainerStyle = inStyle;
        }

        #endregion


    }
}
