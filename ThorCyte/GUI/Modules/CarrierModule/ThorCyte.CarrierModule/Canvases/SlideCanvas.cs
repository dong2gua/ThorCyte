﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.ServiceLocation;
using Prism.Events;
using ThorCyte.CarrierModule.Carrier;
using ThorCyte.CarrierModule.Common;
using ThorCyte.CarrierModule.Events;
using ThorCyte.CarrierModule.Graphics;
using ThorCyte.CarrierModule.Tools;
using ThorCyte.Infrastructure.Events;
using ThorCyte.Infrastructure.Interfaces;
using ThorCyte.Infrastructure.Types;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using CaptureMode = ThorCyte.Infrastructure.Types.CaptureMode;


namespace ThorCyte.CarrierModule.Canvases
{
    public class SlideCanvas : Canvas
    {
        #region Static Members
        private const double Tolerance = 0.00000001;
        private int _currentRegionId = -1;
        private int _lastRegionId = -1;
        #endregion

        #region Class Members
        private readonly VisualCollection _graphicsList;
        private readonly Hashtable _regionGraphicHashtable;
        private readonly List<Rect> _roomRectList = new List<Rect>();
        private Slide _slideMod;

        public static readonly DependencyProperty ToolProperty;
        public static readonly DependencyProperty OuterWidthProperty;
        public static readonly DependencyProperty OuterHeightProperty;
        public static readonly DependencyProperty MousePositionProperty;
        public static readonly DependencyProperty CarrierDescriptionProperty;
        public static readonly DependencyProperty CarrierDescriptionFontSizeProperty;

        private readonly Dictionary<DisplayMode, List<ScanRegion>> _regionListDic;

        private readonly Tool[] _tools;                   // Array of tools

        private double _slideWidth = 75000;
        private double _slideHeight = 25000;
        private float _rx;
        private float _ry;

        public bool IsShowing = false;

        #endregion Class Members

        #region Constructors
        public SlideCanvas()
        {
            EventAggregator.GetEvent<MacroRunEvent>().Subscribe(MacroRun, ThreadOption.UIThread, true);
            EventAggregator.GetEvent<MacroStartEvnet>().Subscribe(MacroStart, ThreadOption.UIThread, true);
            EventAggregator.GetEvent<MacroFinishEvent>().Subscribe(MacroFinish, ThreadOption.UIThread, true);
            EventAggregator.GetEvent<ShowRegionEvent>().Subscribe(ShowRegionEventHandler, ThreadOption.UIThread, true);


            _graphicsList = new VisualCollection(this);
            _regionGraphicHashtable = new Hashtable();
            // create array of drawing tools
            _tools = new Tool[(int)ToolType.Max];
            _tools[(int)ToolType.Select] = new ToolSelect();

            Loaded += DrawingCanvas_Loaded;
            MouseDown += DrawingCanvas_MouseDown;
            MouseMove += DrawingCanvas_MouseMove;
            MouseUp += DrawingCanvas_MouseUp;
            MouseLeave += DrawingCanvas_MouseLeave;


            var drwidth = (int)(_slideWidth * 0.5 * 0.01f);
            var drHeight = (int)(_slideHeight * 0.5 * 0.01f);
            _rx = (float)(_slideWidth / drwidth);
            _ry = (float)(_slideHeight / drHeight);
            IsLocked = true;
            ActualScale = 0.5;


            _regionListDic = new Dictionary<DisplayMode, List<ScanRegion>>();
            ShowRegionEventHandler("ReviewModule");
        }



        static SlideCanvas()
        {
            // Tool
            var metaData = new PropertyMetadata(ToolType.Select);

            ToolProperty = DependencyProperty.Register(
                "Tool", typeof(ToolType), typeof(SlideCanvas),
                metaData);

            metaData = new PropertyMetadata("");
            MousePositionProperty = DependencyProperty.Register(
                 "MousePosition", typeof(string), typeof(SlideCanvas),
                 metaData);

            metaData = new PropertyMetadata(200.0D, OuterSizeChanged);
            OuterWidthProperty = DependencyProperty.Register("OuterWidth", typeof(double), typeof(SlideCanvas), metaData);

            metaData = new PropertyMetadata(100.0D, OuterSizeChanged);
            OuterHeightProperty = DependencyProperty.Register("OuterHeight", typeof(double), typeof(SlideCanvas), metaData);


            metaData = new PropertyMetadata("");
            CarrierDescriptionProperty = DependencyProperty.Register(
                 "CarrierDescription", typeof(string), typeof(SlideCanvas),
                 metaData);

            metaData = new PropertyMetadata(16.0);
            CarrierDescriptionFontSizeProperty = DependencyProperty.Register(
                 "CarrierDescriptionFontSize", typeof(double), typeof(SlideCanvas),
                 metaData);
        }

        #endregion Constructor

        #region Properties


        public double OuterWidth
        {
            get { return (double)GetValue(OuterWidthProperty); }
            set
            {
                if (value > 0)
                    SetValue(OuterWidthProperty, value);
            }
        }

        public double OuterHeight
        {
            get { return (double)GetValue(OuterHeightProperty); }
            set
            {
                if (value > 0)
                    SetValue(OuterHeightProperty, value);
            }
        }


        private IEventAggregator _eventAggregator;
        private IEventAggregator EventAggregator
        {
            get
            {
                return _eventAggregator ?? (_eventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>());
            }
        }

        public ScanInfo CurrentScanInfo { get; set; }

        public new object Parent
        {
            get { return LogicalTreeHelper.GetParent(this); }
        }

        public double LineWidth
        {
            get
            {
                return 1.0;
            }
        }

        public Color ObjectColor
        {
            get
            {
                return Color.FromArgb(255, 0, 0, 0);
            }
        }

        private double _actualScale;
        public double ActualScale
        {
            get { return _actualScale; }
            set
            {
                _actualScale = value;
                ActualScaleChanged(this);
            }
        }

        public Slide SlideMod
        {
            get { return _slideMod; }
            set
            {
                _slideMod = value;
                InitSlide();
            }
        }

        public int CurrentRoomNo { get; set; }

        public bool IsLocked { get; set; }

        private static void OuterSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cnvs = d as SlideCanvas;

            if (cnvs == null) return;

            if (Math.Abs(cnvs.OuterWidth) < Tolerance || Math.Abs(cnvs.OuterHeight) < Tolerance)
            {
                cnvs.ActualScale = Tolerance;
                return;
            }

            var factorW = cnvs.OuterWidth / (cnvs._slideWidth * 1.1);

            cnvs.ActualScale = factorW * 100;
        }

        /// <summary>
        /// Callback function called when ActualScale dependency property is changed.
        /// </summary>
        static void ActualScaleChanged(DependencyObject property)
        {
            var d = property as SlideCanvas;

            if (d == null) return;
            var scale = d.ActualScale;

            d.Width = d._slideWidth / 100 * scale;
            d.Height = d._slideHeight / 100 * scale;

            var drwidth = (int)(d._slideWidth * scale * 0.01f);
            var drHeight = (int)(d._slideHeight * scale * 0.01f);
            d._rx = (float)(d._slideWidth / drwidth);
            d._ry = (float)(d._slideHeight / drHeight);

            var rg = new RectangleGeometry(new Rect(0, 0, d.Width, d.Height));
            foreach (var o in d.GraphicsList.Cast<GraphicsBase>())
            {
                o.ActualScale = scale;
                o.Clip = rg;
            }
            d.UpdateRoomRects();
        }

        /// <summary>
        /// Get graphic object by index
        /// </summary>
        internal GraphicsBase this[int index]
        {
            get
            {
                if (index >= 0 && index < Count)
                {
                    return (GraphicsBase)_graphicsList[index];
                }

                return null;
            }
        }

        /// <summary>
        /// Get number of graphic objects
        /// </summary>
        internal int Count
        {
            get
            {
                return _graphicsList.Count;
            }
        }

        /// <summary>
        /// Return list of graphics
        /// </summary>
        internal VisualCollection GraphicsList
        {
            get
            {
                return _graphicsList;
            }
        }

        /// <summary>
        /// Returns INumerable which may be used for enumeration
        /// of selected objects.
        /// </summary>
        internal IEnumerable<GraphicsBase> Selection
        {
            get
            {
                return from GraphicsBase o in _graphicsList where o.IsSelected select o;
            }
        }

        /// <summary>
        /// Currently active drawing tool
        /// </summary>
        /// 
        public ToolType Tool
        {
            get
            {
                return (ToolType)GetValue(ToolProperty);
            }
            set
            {
                if ((int)value >= 0 && (int)value < (int)ToolType.Max)
                {
                    SetValue(ToolProperty, value);
                }
            }
        }

        public string MousePosition
        {
            get { return (string)GetValue(MousePositionProperty); }
            set { SetValue(MousePositionProperty, value); }
        }

        public string CarrierDescription
        {
            get { return (string)GetValue(CarrierDescriptionProperty); }
            set { SetValue(CarrierDescriptionProperty, value); }
        }

        public double CarrierDescriptionFontSize
        {
            get { return (double)GetValue(CarrierDescriptionFontSizeProperty); }
            set { SetValue(CarrierDescriptionFontSizeProperty, value); }
        }

        #endregion Properties

        #region Override Functions
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            DrawFunction.DrawRectangle(dc,
                null,
                new Pen(new SolidColorBrush(ObjectColor), LineWidth),
                new Rect(0, 0, Width, Height));

            DrawRooms(dc);

            //DrawGrid(dc);
            var rcWidth = 10 * ActualScale;
            DrawFunction.DrawRectangle(dc,
                Brushes.Black,
                null,
                new Rect(Width - rcWidth, 0, rcWidth, rcWidth));
        }
        #endregion

        #region Mouse Event Handlers
        void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_tools[(int)Tool] == null)
            {
                return;
            }

            Focus();

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    if (e.ClickCount == 2)
                    {
                        //HandleDoubleClick(e);        // special case for GraphicsText
                    }
                    else
                    {
                        _tools[(int)Tool].OnMouseDown(this, e);
                    }
                    break;

                case MouseButton.Right:
                    break;
                //ShowContextMenu(e);
            }
        }

        void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_tools[(int)Tool] == null)
            {
                return;
            }

            var pt = e.GetPosition(this);

            var x = (int)(_slideWidth - pt.X * _rx - 1);
            var y = (int)(pt.Y * _ry);

            if (x < 0) x = 0;
            if (x >= _slideWidth)
                x = (int)_slideWidth - 1;
            if (y < 0) y = 0;
            if (y >= _slideHeight)
                y = (int)_slideHeight - 1;

            MousePosition = x + ", " + y;

            if (e.MiddleButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
            {
                _tools[(int)Tool].OnMouseMove(this, e);
                //UpdateState();
            }
            else
            {
                Cursor = HelperFunctions.DefaultCursor;
            }
        }

        void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_tools[(int)Tool] == null)
            {
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                _tools[(int)Tool].OnMouseUp(this, e);
            }

        }

        void DrawingCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            MousePosition = "0, 0";
        }

        #endregion

        #region Other Event Handlers

        void DrawingCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            Focusable = true;      // to handle keyboard messages
            foreach (var o in _graphicsList.Cast<GraphicsBase>())
            {
                o.RefreshDrawing();
            }
        }

        protected override int VisualChildrenCount
        {
            get
            {
                var n = _graphicsList.Count;
                return n;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return _graphicsList[index];
        }
        #endregion

        #region Private Functions

        private void InitSlide()
        {
            HelperFunctions.DeleteAll(this);

            CarrierDescription = SlideMod.Description;

            Width = SlideMod.Size.Width / 100 * ActualScale;
            Height = SlideMod.Size.Height / 100 * ActualScale;

            _slideWidth = SlideMod.Width;
            _slideHeight = SlideMod.Height;
            UpdateRoomRects();
            InvalidateVisual();
        }

        private DisplayMode _currentDisplayMode = DisplayMode.Review;

        private void ShowRegionEventHandler(string moduleName)
        {
            try
            {
                if (!IsShowing) return;
                var mode = DisplayMode.Review;
                switch (moduleName)
                {
                    case "ReviewModule":
                        mode = DisplayMode.Review;
                        break;
                    case "ProtocolModule":
                        mode = DisplayMode.Protocol;
                        break;
                    case "AnalysisModule":
                        mode = DisplayMode.Analysis;
                        break;
                }

                if (mode == _currentDisplayMode) return;
                SwitchSelections(mode);
                _currentDisplayMode = mode;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Occurred in SlideCanvas ShowRegionEventHandler " + ex.Message);
            }

        }

        public void SwitchSelections(DisplayMode mode)
        {
            RecordSelections(_currentDisplayMode);
            ApplySelections(mode);
        }

        private void RecordSelections(DisplayMode mode)
        {
            if (_slideMod.ActiveRegions == null) return;
            var rList = _slideMod.ActiveRegions.ToList();

            if (_regionListDic.ContainsKey(mode))
            {
                _regionListDic.Remove(mode);
            }

            _regionListDic.Add(mode, rList);
        }

        private void ClearSelections()
        {
            _slideMod.ClearActiveRegions();
            foreach (var gphcs in _graphicsList.Cast<GraphicsBase>())
            {
                gphcs.IsSelected = false;
            }
        }

        private void ApplySelections(DisplayMode mode)
        {
            ClearSelections();

            if (!_regionListDic.ContainsKey(mode)) return;

            foreach (var region in _regionListDic[mode])
            {
                var gph = (GraphicsBase)_regionGraphicHashtable[region];

                if (gph == null) continue;

                gph.IsSelected = true;
                _slideMod.AddActiveRegion(region);
            }

            InvalidateVisual();

        }

        private void DrawRooms(DrawingContext dc)
        {
            if (SlideMod == null)
            {
                return;
            }

            const double ftMaxScale = 0.5;
            double ftsize;
            if (ActualScale > ftMaxScale)
            {
                const double tempscale = ftMaxScale;
                ftsize = 50 * tempscale;
            }
            else
            {
                ftsize = 50 * ActualScale;
            }
            CarrierDescriptionFontSize = ftsize;


            var index = 0;
            foreach (CarrierRoom room in SlideMod.Rooms)
            {
                var rect = _roomRectList[index];

                var formattedText = new FormattedText(room.No.ToString(CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Verdana"),
                    10,
                    Brushes.Gray);

                switch (room.ScannableShape)
                {
                    case RegionShape.Rectangle:
                        {
                            DrawFunction.DrawRectangle(dc, Brushes.White, new Pen(new SolidColorBrush(ObjectColor), LineWidth), rect);

                            dc.DrawText(formattedText, new Point(rect.Left + 1, rect.Top));
                            break;
                        }
                    case RegionShape.Ellipse:
                        {
                            var center = new Point(
                                (rect.Left + rect.Right) / 2.0,
                                (rect.Top + rect.Bottom) / 2.0);

                            var radiusX = (rect.Right - rect.Left) / 2.0;
                            var radiusY = (rect.Bottom - rect.Top) / 2.0;

                            dc.DrawEllipse(
                                Brushes.White,
                                new Pen(new SolidColorBrush(ObjectColor), LineWidth),
                                center,
                                radiusX,
                                radiusY);

                            var pt = new Point
                            {
                                X = (rect.Left + rect.Width / 2) - 4 * ActualScale,
                                Y = (rect.Top + rect.Height / 2) - 4 * ActualScale
                            };
                            dc.DrawText(formattedText, pt);

                            break;
                        }
                }
                index++;
            }
        }

        private void UpdateRoomRects()
        {
            if (SlideMod == null) return;

            _roomRectList.Clear();
            foreach (CarrierRoom room in SlideMod.Rooms)
            {
                var rect = room.ScannableRect;
                var scale = ActualScale / 100;
                rect.X *= scale;
                rect.Y *= scale;
                rect.Width *= scale;
                rect.Height *= scale;

                rect.X = Width - rect.X - rect.Width;
                _roomRectList.Add(rect);
            }
        }

        #endregion

        #region Public Functions

        public void Draw(DrawingContext drawingContext)
        {
            Draw(drawingContext, false);
        }

        private void Draw(DrawingContext drawingContext, bool withSelection)
        {
            var oldSelection = false;

            foreach (var b in _graphicsList.Cast<GraphicsBase>())
            {
                if (!withSelection)
                {
                    // Keep selection state and unselect
                    oldSelection = b.IsSelected;
                    b.IsSelected = false;
                }

                b.Draw(drawingContext);

                if (!withSelection)
                {
                    // Restore selection state
                    b.IsSelected = oldSelection;
                }
            }
        }

        public void SelectAllGraphics()
        {
            HelperFunctions.SelectAll(this);
            SetActiveRegions();
        }

        public void Delete()
        {
            HelperFunctions.DeleteSelection(this);
        }

        public void SetDefault()
        {
            Tool = ToolType.Select;
            Cursor = HelperFunctions.DefaultCursor;
        }

        public Rect GetRoomRect(int roomNo)
        {
            return _roomRectList[roomNo - 1];
        }

        public Point ClientToWorld(Point pt)
        {
            var rPt = new Point { X = pt.X * _rx / 100.0 * ActualScale, Y = pt.Y * _ry / 100.0 * ActualScale };
            return rPt;
        }

        private bool IsRegionChanged(int wellid)
        {
            if (wellid == _currentRegionId)
                return false;
            return true;
        }

        private void MacroRun(int obj)
        {
            foreach (GraphicsBase gph in _regionGraphicHashtable.Values)
            {
                gph.ObjectColor = Colors.Black;
            }
        }

        private void MacroStart(MacroStartEventArgs args)
        {
            if (!IsShowing) return;
            if (!IsRegionChanged(args.RegionId)) return;
            _lastRegionId = _currentRegionId;
            _currentRegionId = args.RegionId;

            var rgncurrent = _slideMod.TotalRegions.FirstOrDefault(r => r.RegionId == _currentRegionId);

            if (rgncurrent != null && _regionGraphicHashtable.ContainsKey(rgncurrent))
            {
                ((GraphicsBase)_regionGraphicHashtable[rgncurrent]).ObjectColor = Colors.Lime;
            }

            var rgn = _slideMod.TotalRegions.FirstOrDefault(r => r.RegionId == _lastRegionId);

            if (rgn != null && _regionGraphicHashtable.ContainsKey(rgn))
            {
                ((GraphicsBase)_regionGraphicHashtable[rgn]).ObjectColor = Colors.LimeGreen;
            }
        }

        private void MacroFinish(int scanid)
        {
            if (!IsShowing) return;

            var rgn = _slideMod.TotalRegions.FirstOrDefault(r => r.RegionId == _currentRegionId);

            if (rgn != null && _regionGraphicHashtable.ContainsKey(rgn))
            {
                ((GraphicsBase)_regionGraphicHashtable[rgn]).ObjectColor = Colors.LimeGreen;

            }
            _currentRegionId = -1;
            _lastRegionId = -1;
        }

        public void UpdateScanArea()
        {
            _graphicsList.Clear();
            _regionGraphicHashtable.Clear();
            _regionListDic.Clear();
            var scale = ActualScale / 100;
            foreach (var rgn in _slideMod.TotalRegions)
            {
                var rc = rgn.Bound;

                switch (rgn.ScanRegionShape)
                {
                    case RegionShape.Ellipse:
                        var left = (_slideWidth - rc.X - rc.Width) * scale;
                        var top = rc.Y * scale;
                        var right = (_slideWidth - rc.X) * scale;
                        var bottom = (rc.Y + rc.Height) * scale;
                        var ellipse = new GraphicsEllipse(left, top, right, bottom, LineWidth, Colors.Black, ActualScale, 0);
                        _graphicsList.Add(ellipse);
                        _regionGraphicHashtable.Add(rgn, ellipse);
                        ellipse.RefreshDrawing();
                        break;
                    case RegionShape.Polygon:
                        var ptList = new Point[rgn.Points.Length];
                        for (var i = 0; i < rgn.Points.Length; i++)
                        {
                            ptList[i] = rgn.Points[i];
                            ptList[i].X = (_slideWidth - ptList[i].X) * scale;
                            ptList[i].Y = ptList[i].Y * scale;
                        }
                        var polygon = new GraphicsPolygon(ptList, LineWidth, Colors.Black, ActualScale, 0);
                        _graphicsList.Add(polygon);
                        _regionGraphicHashtable.Add(rgn, polygon);
                        polygon.RefreshDrawing();
                        break;
                    case RegionShape.Rectangle:
                        var left1 = (_slideWidth - rc.X - rc.Width) * scale;
                        var top1 = rc.Y * scale;
                        var right1 = (_slideWidth - rc.X) * scale;
                        var bottom1 = (rc.Y + rc.Height) * scale;
                        var rectangle = new GraphicsRectangle(left1, top1, right1, bottom1, LineWidth, Colors.Black, ActualScale, 0);
                        _graphicsList.Add(rectangle);
                        _regionGraphicHashtable.Add(rgn, rectangle);
                        rectangle.RefreshDrawing();
                        break;
                }
            }
        }

        private List<CaptureMode> GetBypassMode()
        {
            var ret = new List<CaptureMode> 
            {
                CaptureMode.Mode2DStream,
                CaptureMode.Mode2DTimingStream,
                CaptureMode.Mode3DFastZStream,
                CaptureMode.Mode3DStream,
                CaptureMode.Mode3DTimingStream
            };

            return ret;
        }


        public void SetActiveRegions()
        {
            var prevCount = _slideMod.ActiveRegions.Count;
            _slideMod.ClearActiveRegions();
            foreach (var rgn in from GraphicsBase o in _graphicsList where o.IsSelected select _regionGraphicHashtable.Keys.OfType<ScanRegion>().FirstOrDefault(s => Equals(_regionGraphicHashtable[s], o)))
            {
                _slideMod.AddActiveRegion(rgn);
            }
            if (_slideMod.ActiveRegions.Count == prevCount && prevCount == 0)
            {
                return;
            }

            using (new WaitCursor())
            {
                //No region event publish to outside.
                //just internal use
                var eventArgs = new List<int>();
                eventArgs.Clear();
                eventArgs.AddRange(_slideMod.ActiveRegions.Select(region => region.RegionId));
                EventAggregator.GetEvent<RegionsSelected>().Publish(eventArgs);

                if (GetBypassMode().Contains(CurrentScanInfo.Mode))
                {
                    MessageHelper.SendStreamingStatus(true);
                    return;
                }

                MessageHelper.SendStreamingStatus(false);

                switch (CarrierModule.Mode)
                {
                    case DisplayMode.Review:
                        //case DisplayMode.Protocol:
                        eventArgs = new List<int>();
                        eventArgs.Clear();
                        eventArgs.AddRange(_slideMod.ActiveRegions.Select(region => region.RegionId));
                        EventAggregator.GetEvent<SelectRegions>().Publish(eventArgs);
                        break;

                    case DisplayMode.Analysis:
                        eventArgs = new List<int>();
                        eventArgs.Clear();
                        eventArgs.AddRange(_slideMod.ActiveRegions.Select(region => region.WellId));
                        EventAggregator.GetEvent<SelectWells>().Publish(eventArgs);
                        break;
                }
            }
        }
        #endregion
    }
}