﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Practices.ServiceLocation;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using ThorCyte.CarrierModule.Common;
using ThorCyte.CarrierModule.Events;
using ThorCyte.Infrastructure.Events;
using ThorCyte.Infrastructure.Interfaces;
using ThorCyte.Infrastructure.Types;

namespace ThorCyte.CarrierModule.ViewModels
{
    public class TileItem
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int FieldId { get; set; }

        public TileItem()
        {
            Left = 0;
            Top = 0;
            Width = 0;
            Height = 0;
            FieldId = 0;
        }

        public Rect TileRect
        {
            get
            {
                return new Rect(Left, Top, Width, Height);
            }
            set
            {
                Left = (int)value.Left;
                Top = (int)value.Top;
                Width = (int)value.Width;
                Height = (int)value.Height;
            }
        }

    }

    public class TileViewModel : BindableBase
    {
        private ObservableCollection<TileItem> _tilesShowInCanvas;
        public ICommand CmdTileTrigger { get; private set; }

        private double _viewSizeMax;
        private int _initialViewSize = 300;
        private double _pxFactor = 1.0; // 1 pixel equals how many length unit?
        private double _p0x;      // ScanRegion real left position
        private double _p0y;      // ScanRegion real top position

        private int _viewHeight; //in pixel
        private int _viewWidth; //in pixel

        private ScanRegion _inRegion = null;


        private IEventAggregator _eventAggregator;
        private IEventAggregator EventAggregator
        {
            get
            {
                return _eventAggregator ?? (_eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>());
            }
        }

        public ObservableCollection<TileItem> TilesShowInCanvas
        {
            get
            {
                return _tilesShowInCanvas;
            }
            set { SetProperty(ref _tilesShowInCanvas, value); }
        }

        public ScanInfo CurrentScanInfo { get; set; }

        public double ViewSizeMax
        {
            get
            {
                return _viewSizeMax;
            }
            set
            {
                SetProperty(ref _viewSizeMax, value);
                LoadTiles(_inRegion);
            }
        }

        public int InitialViewSize
        {
            get
            {
                return _initialViewSize;
            }
            set { SetProperty(ref _initialViewSize, value); }
        }

        private string _regionid;

        public string RegionID
        {
            get
            {
                return _regionid;
            }
            private set { SetProperty(ref _regionid, value); }
        }


        public int ViewHeight
        {
            get
            {
                return _viewHeight;
            }
            set { SetProperty(ref _viewHeight, value); }
        }

        public int ViewWidth
        {
            get
            {
                return _viewWidth;
            }
            set { SetProperty(ref _viewWidth, value); }
        }



        public TileViewModel()
        {
            //subscribe select region event.
            var selectRegionEvt = EventAggregator.GetEvent<RegionsSelected>();
            var shwoRegionEvent = EventAggregator.GetEvent<ShowRegionEvent>();
            selectRegionEvt.Subscribe(OnSelectRegionChanged);
            shwoRegionEvent.Subscribe(ShowRegionEventHandler, ThreadOption.UIThread, true);

            //delegate button click command
            CmdTileTrigger = new DelegateCommand<object>(OnTileSelect);

            //Define button infomations to show on UI
            _tilesShowInCanvas = new ObservableCollection<TileItem>();
        }

        private void ShowRegionEventHandler(string moduleName)
        {
            object theView;
            switch (moduleName)
            {
                case "ReviewModule":
                    break;
                case "AnalysisModule":
                    SetEmptyContent();
                    break;
            }
        }

        public void SetEmptyContent()
        {
            TilesShowInCanvas.Clear();
            _inRegion = null;
            ViewHeight = 0;
            ViewWidth = 0;
            RegionID = string.Empty;
        }


        /// <summary>
        /// Response the Tile Button Click 
        /// </summary>
        /// <param name="oItem">Tile item.</param>
        private void OnTileSelect(object oItem)
        {
            var tItem = oItem as TileItem;
            if (tItem == null) return;

            EventAggregator.GetEvent<SelectRegionTileEvent>().Publish(new RegionTile()
            {
                TileId = _inRegion.ScanFieldList[tItem.FieldId - 1].ScanFieldId,
                RegionId = _inRegion.RegionId
            });
        }


        /// <summary>
        /// Load All Tiles from the ScanRegion.
        /// </summary>
        /// <param name="sr">ScanRegion</param>
        private void LoadTiles(ScanRegion sr)
        {
            if (sr == null) return;

            _inRegion = sr;
            RegionID = _inRegion != null ? "Region ID: " + _inRegion.RegionId : string.Empty;
            if (_viewHeight == 0)
                _viewSizeMax = InitialViewSize;

            //CalcPxFactor(sr.ScanFieldList[0].SFRect);
            CalcPxFactor(sr.Bound);
            //every tile rect size is same.


            //ResetViewSize
            ViewHeight = GetLengthAsPixel(sr.Bound.Height);
            ViewWidth = GetLengthAsPixel(sr.Bound.Width);

            TilesShowInCanvas.Clear();
            foreach (var scanfield in sr.ScanFieldList)
            {
                TilesShowInCanvas.Add(Convert(scanfield));
            }
        }

        /// <summary>
        /// Convert scanfield
        /// </summary>
        /// <param name="sf"></param>
        /// <returns></returns>
        private TileItem Convert(Scanfield sf)
        {
            var ti = new TileItem() { FieldId = sf.ScanFieldId };
            var p = GetPositionAsPixel(sf.SFRect.Left, sf.SFRect.Top);

            var rT = new Rect((int)p.X, (int)p.Y, GetLengthAsPixel(sf.SFRect.Width), GetLengthAsPixel(sf.SFRect.Height));

            //Trasform to Right/Top coordinate.
            ti.TileRect = CoordinateTransform(rT);

            return ti;
        }

        private void OnSelectRegionChanged(List<int> args)
        {
            try
            {
                if (args.Count != 1 || CarrierModule.Mode == DisplayMode.Analysis)
                {
                    //Clear Canvas items and set size to 0,0
                    SetEmptyContent();
                    return;
                }

                var sr = CurrentScanInfo.ScanRegionList[args[0]];
                LoadTiles(sr);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error Occured in OnSelectRegionChanged! " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Calculate pixel factor according to ScanRegion Bound
        /// </summary>
        /// <param name="sBoudRect">Scan Region bound rectangle</param>
        private void CalcPxFactor(Rect sBoudRect)
        {
            try
            {
                var l = sBoudRect.Width >= sBoudRect.Height ? sBoudRect.Width : sBoudRect.Height;

                _pxFactor = l / ViewSizeMax;
                _p0x = sBoudRect.Left;
                _p0y = sBoudRect.Top;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error Occured in CalcPxFactor! System will use default factor 1 ! " + ex.Message);
                throw;
            }
        }

        private int GetLengthAsPixel(double realLength)
        {
            return (int)(realLength / _pxFactor);
        }

        private Point GetPositionAsPixel(double realpx, double realpy)
        {
            var X = GetLengthAsPixel(realpx - _p0x);
            var Y = GetLengthAsPixel(realpy - _p0y);
            return new Point(X, Y);
        }


        /// <summary>
        /// determine if rect son is inside rect parent.
        /// </summary>
        /// <param name="parent">Parent rectangle</param>
        /// <param name="son">Son rectangle</param>
        /// <returns>Inside --true, Not Inside --false</returns>
        private bool IsRectInside(Rect parent, Rect son)
        {
            var tolerance = 2;
            
            if (son.Left+tolerance < parent.Left)
            {
                return false;
            }

            if (son.Right > parent.Right+tolerance)
            {
                return false;
            }

            if (son.Top+tolerance < parent.Top)
            {
                return false;
            }

            if (son.Bottom > parent.Bottom + tolerance)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Perform Coordinate Transform.
        /// </summary>
        /// <param name="o">original rectangle</param>
        /// <returns>transformed rectangle</returns>
        private Rect CoordinateTransform(Rect o)
        {
            var r = new Rect();

            //r.X = ViewWidth - o.Left;
            r.X = ViewWidth - o.Left - o.Width;
            r.Y = o.Top;
            r.Width = o.Width;
            r.Height = o.Height;
            return r;
        }

    }
}
