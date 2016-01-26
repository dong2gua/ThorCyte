﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ThorCyte.GraphicModule.Controls;
using ThorCyte.GraphicModule.Helper;
using ThorCyte.GraphicModule.ViewModels;

namespace ThorCyte.GraphicModule.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class GraphicPanelView
    {
        #region Fields

        private bool _isLoaded;

        private UniformGrid _uniformgrid;

        #endregion

        #region Constructor

        public GraphicPanelView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        #endregion

        #region Methods

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                return;
            }
            var itemsPresenter = VisualHelper.GetVisualChild<ItemsPresenter>(GraphicViewList);
            _uniformgrid = VisualTreeHelper.GetChild(itemsPresenter, 0) as UniformGrid;
            if (_uniformgrid == null)
            {
                return;
            }

            var containerVm = (GraphicContainerVm)DataContext;
            var items = VisualHelper.GetChildObjects<GraphicUcBase>(GraphicViewList, "");
            foreach (var graphicview in items)
            {
                var vm = (GraphicVmBase)graphicview.DataContext;
                if (!containerVm.GraphicDictionary.ContainsKey(vm.Id))
                {
                    containerVm.GraphicDictionary.Add(vm.Id, new Tuple<GraphicUcBase, GraphicVmBase>(graphicview, vm));
                }
            }
            UpdateGridLayout();
            _isLoaded = true;
        }

        private void UpdateGridLayout()
        {
            if (_uniformgrid == null)
            {
                return;
            }
            var containerVm = (GraphicContainerVm)DataContext;
            if (containerVm == null)
            {
                return;
            }
            if (GraphicViewList.ActualWidth <= 600)
            {
                _uniformgrid.Columns = 1;
            }
            else if (GraphicViewList.ActualWidth > 600 && GraphicViewList.ActualWidth <= 900 )
            {
                _uniformgrid.Columns = 2;
            }
            else if (GraphicViewList.ActualWidth > 900 && GraphicViewList.ActualWidth < 1200)
            {
                _uniformgrid.Columns = 3;
            }
            else
            {
                _uniformgrid.Columns = 4;
            }
            if (containerVm.GraphicVmList.Count >= 3 * _uniformgrid.Columns)
            {
                if (containerVm.GraphicVmList.Count % _uniformgrid.Columns == 0)
                {
                    _uniformgrid.Rows = containerVm.GraphicVmList.Count / _uniformgrid.Columns;
                }
                else
                {
                    _uniformgrid.Rows = containerVm.GraphicVmList.Count / _uniformgrid.Columns + 1;
                }
            }
            else
            {
                _uniformgrid.Rows = 3;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_uniformgrid == null)
            {
                return;
            }
            UpdateGridLayout();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var newDataContext = e.NewValue as GraphicContainerVm;
            if (newDataContext == null)
            {
                return;
            }
            GraphicViewList.Items.Clear();
            foreach (var vm in newDataContext.GraphicDictionary)
            {
                GraphicViewList.Items.Add(vm.Value.Item1);
            }
            var index = -1;
            if (newDataContext.SelectedGraphic != null)
            {
                index = newDataContext.GraphicVmList.IndexOf(newDataContext.SelectedGraphic);
            }
            if (index >= 0 && GraphicViewList.Items.Count > index)
            {
                GraphicViewList.SelectedIndex = index;
            }
        }

        public void AddScattergram()
        {
            var containerVm = DataContext as GraphicContainerVm;
            if (containerVm == null)
            {
                return;
            }
            var vm = containerVm.CreateScattergram();
            var scattergram = new ScattergramView
            {
                DataContext = vm
            };
            containerVm.GraphicDictionary.Add(vm.Id,new Tuple<GraphicUcBase, GraphicVmBase>(scattergram,vm));
            GraphicViewList.Items.Add(scattergram);
            if (containerVm.GraphicVmList.Count == 1)
            {
                containerVm.SelectedGraphic = vm;
                GraphicViewList.SelectedIndex = 0;
            }
            UpdateGridLayout();
        }

        public void AddHistogram()
        {
            var containerVm = DataContext as GraphicContainerVm;
            if (containerVm == null)
            {
                return;
            }
            var vm = containerVm.CreateHistogram();
            var histogram = new HistogramView()
            {
                DataContext = vm
            };
            
            containerVm.GraphicDictionary.Add(vm.Id, new Tuple<GraphicUcBase, GraphicVmBase>(histogram, vm));
            GraphicViewList.Items.Add(histogram);

            if (containerVm.GraphicVmList.Count == 1)
            {
                containerVm.SelectedGraphic = vm;
                GraphicViewList.SelectedIndex = 0;
            }
            UpdateGridLayout();
        }

        private void OnSelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            var containerVm = DataContext as GraphicContainerVm;
            if (containerVm == null || GraphicViewList.SelectedItem == null)
            {
                return;
            }
            
            containerVm.SelectedGraphic = (GraphicVmBase)((GraphicUcBase)GraphicViewList.SelectedItem).DataContext;
        }


        public void DeleteGraphic()
        {
            var containerVm = DataContext as GraphicContainerVm;
            if (containerVm == null || GraphicViewList.SelectedItem == null)
            {
                return;
            }
            containerVm.OnDeleteGraphic();
            var selectedIndex = GraphicViewList.SelectedIndex;
            GraphicViewList.Items.RemoveAt(selectedIndex);
            if (GraphicViewList.Items.Count == 0)
            {
                return;
            }
            if (GraphicViewList.Items.Count > selectedIndex)
            {
                GraphicViewList.SelectedItem = GraphicViewList.Items[selectedIndex];
            }
            else
            {
                GraphicViewList.SelectedItem = GraphicViewList.Items[GraphicViewList.Items.Count - 1];
            }
            containerVm.SelectedGraphic = (GraphicVmBase)((GraphicUcBase)GraphicViewList.SelectedItem).DataContext;
            UpdateGridLayout();
        }

        #endregion
    }
}