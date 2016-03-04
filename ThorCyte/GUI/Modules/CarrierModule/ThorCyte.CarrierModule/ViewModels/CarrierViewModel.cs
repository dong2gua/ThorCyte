﻿using System;
using System.Windows.Controls;
using Microsoft.Practices.ServiceLocation;
using Prism.Events;
using Prism.Mvvm;
using ThorCyte.CarrierModule.Carrier;
using ThorCyte.CarrierModule.Common;
using ThorCyte.CarrierModule.Views;
using ThorCyte.Infrastructure.Events;
using ThorCyte.Infrastructure.Exceptions;
using ThorCyte.Infrastructure.Interfaces;

namespace ThorCyte.CarrierModule.ViewModels
{
    public class CarrierViewModel : BindableBase
    {
        #region Static Members
        private static Carrier.Carrier _carrier;
        #endregion

        #region Fileds
        private readonly SlideView _slideView;
        private readonly PlateView _plateView;
        private TileView _tileview;
        private int _currentScanId;
        private ScanInfo _currentScanInfo;
        private string _currentCarrierType = "00000000-0000-0000-0001-000000000001";

        #endregion

        #region Properties
        private IEventAggregator _eventAggregator;
        private IEventAggregator EventAggregator
        {
            get
            {
                return _eventAggregator ?? (_eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>());
            }
        }

        public ScanInfo CurrentScanInfo
        {
            get { return _currentScanInfo; }
            set { _currentScanInfo = value; }
        }


        private UserControl _showingView;

        public UserControl ShowingView
        {
            get { return _showingView; }
            set { SetProperty(ref _showingView, value); }
        }

        public TileView ShowingTile
        {
            get { return _tileview; }
            set { SetProperty(ref _tileview, value); }
        }

        private bool _isTileExpand;
        public bool IsTileExpand
        {
            get { return _isTileExpand; }
            set { SetProperty(ref _isTileExpand, value); }
        }

        private bool _isTileVisible;
        public bool IsTileVisible
        {
            get { return _isTileVisible; }
            set { SetProperty(ref _isTileVisible, value); }
        }

        #endregion


        #region Methods

        public CarrierViewModel()
        {
            CarrierDefMgr.Initialize(@".\XML");
            _tileview = new TileView();
            _plateView = new PlateView();
            _slideView = new SlideView();
            CreateCarrier(_currentCarrierType); 
            
            var loadEvt = EventAggregator.GetEvent<ExperimentLoadedEvent>();
            loadEvt.Subscribe(RequestLoadModule);

            var showRegEvt = EventAggregator.GetEvent<ShowRegionEvent>();
            showRegEvt.Subscribe(ShowRegionEventHandler, ThreadOption.UIThread, true);

            MessageHelper.SetStreaming += SetStreaming;
        }

        private void SetStreaming(bool isStreaming)
        {
            if (isStreaming)
                IsTileExpand = true;
        }

        /// <summary>
        /// Create and show carrier on UI
        /// </summary>
        /// <param name="refId"></param>
        public void CreateCarrier(string refId)
        {
            var def = CarrierDefMgr.Instance.GetCarrierDef(refId, false);
            if (def != null)
            {
                SetCarrier(def);
            }
        }

        public void SetCarrier(CarrierDef carrierDef)
        {
            _plateView.plateCanvas.IsShowing = false;
            _slideView.slideCanvas.IsShowing = false;
            
            if (carrierDef.Type == CarrierType.Microplate)
            {
                _carrier = new Microplate(carrierDef);
                _plateView.CurrentPlate = (Microplate)_carrier;
                ShowingView = _plateView;
                _plateView.plateCanvas.IsShowing = true;
                _plateView.plateCanvas.AnalyzedWells.Clear();
                ShowingView.Tag = "PlateView";
            }
            else
            {
                _carrier = new Slide(carrierDef);
                _slideView.CurrentSlide = (Slide)_carrier;
                _slideView.slideCanvas.CurrentScanInfo = _currentScanInfo;
                ShowingView = _slideView;
                _slideView.slideCanvas.IsShowing = true;
                ShowingView.Tag = "SlideView";
            }
        }

        public void LoadScanArea(ScanInfo info)
        {
            if (_carrier != null )

                _carrier.TotalRegions = info.ScanRegionList;

            if (_carrier is Slide)
            {
                _slideView.UpdateScanArea();
            }
            else
            {
                _plateView.UpdateScanArea();
            }
        }

        private void RequestLoadModule(int scanid)
        {
            try
            {
                _currentScanId = scanid;
                _tileview.vm.SetEmptyContent();
                var exp = ServiceLocator.Current.GetInstance<IExperiment>();
                _currentCarrierType = exp.GetCarrierType();
                _currentScanInfo = exp.GetScanInfo(_currentScanId);
                if(_currentScanInfo == null) throw new CyteException("CarrierViewModel","current scaninfo is null.");
                _tileview.vm.CurrentScanInfo = _currentScanInfo;
                _slideView.slideCanvas.CurrentScanInfo = _currentScanInfo;
                _plateView.plateCanvas.CurrentScanInfo = _currentScanInfo;

                CreateCarrier(_currentCarrierType);
                LoadScanArea(_currentScanInfo);
            }
            catch (Exception)
            {
                CreateCarrier(_currentCarrierType);
            }
        }

        private void ShowRegionEventHandler(string moduleName)
        {
            try
            {
                switch (moduleName)
                {
                    case "ReviewModule":
                        IsTileVisible = true;
                        break;

                    case "ProtocolModule":
                        IsTileVisible = true;
                        break;

                    case "AnalysisModule":
                        IsTileVisible = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                CarrierModule.Logger.Write("Error occoured in CarrierViewModle.ShowRegionEventHandler", ex);
            }
        }

        #endregion


    }
}