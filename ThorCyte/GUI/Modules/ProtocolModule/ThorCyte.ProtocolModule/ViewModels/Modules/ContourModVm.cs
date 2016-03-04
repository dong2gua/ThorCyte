﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using ComponentDataService.Types;
using ImageProcess;
using ThorCyte.ProtocolModule.Models;
using ThorCyte.ProtocolModule.Utils;
using ThorCyte.ProtocolModule.Views.Modules;

namespace ThorCyte.ProtocolModule.ViewModels.Modules
{
    public class ContourModVm : ModuleBase
    {
        #region Properties and Fields
        private ImageData _img;

        public override bool Executable
        {
            get {
                return _componentName != string.Empty;
            }
        }

        public override string CaptionString { get { return ComponentName; } }

        private string _componentName;

        public string ComponentName
        {
            get { return _componentName; }
            set
            {
                if (value == _componentName)
                {
                    return;
                }

                SetProperty(ref _componentName, value);
                OnPropertyChanged("CaptionString");

            }
        }

        private bool _isMaxAreaChecked;
        public bool IsMaxAreaChecked
        {
            get { return _isMaxAreaChecked; }
            set
            {
                if (value == _isMaxAreaChecked)
                {
                    return;
                }
                SetProperty(ref _isMaxAreaChecked, value);
            }
        }

        private bool _isConcaveChecked;

        public bool IsConcaveChecked
        {
            get { return _isConcaveChecked; }
            set
            {
                if (value == _isConcaveChecked)
                {
                    return;
                }
                SetProperty(ref _isConcaveChecked, value);
            }
        }

        private bool _isBoundaryChecked;

        public bool IsBoundaryChecked
        {
            get { return _isBoundaryChecked; }
            set
            {
                if (value == _isBoundaryChecked)
                {
                    return;
                }
                SetProperty(ref _isBoundaryChecked, value);
            }
        }

        private UnitType _maxAreaUnit;

        public UnitType MaxAreaUnit
        {
            get { return _maxAreaUnit; }
            set
            {
                if (value == _maxAreaUnit)
                {
                    return;
                }
                SetProperty(ref _maxAreaUnit, value);
            }
        }

        private UnitType _minAreaUnit;

        public UnitType MinAreaUnit
        {
            get { return _minAreaUnit; }
            set
            {
                if (value == _minAreaUnit)
                {
                    return;
                }
                SetProperty(ref _minAreaUnit, value);
            }
        }

        private double _minArea;

        public double MinArea
        {
            get { return _minArea; }
            set
            {
                if (Math.Abs(value - _minArea) < double.Epsilon)
                {
                    return;
                }
                SetProperty(ref _minArea, value);
            }
        }
        private double _maxArea;

        public double MaxArea
        {
            get { return _maxArea; }
            set
            {
                if (Math.Abs(value - _maxArea) < double.Epsilon)
                {
                    return;
                }
                SetProperty(ref _maxArea, value);
            }
        }
        #endregion

        #region Constructor

        public ContourModVm()
        {
            ComponentName = "Primary";
            IsMaxAreaChecked = false;
            MinArea = 20;
            MaxArea = 1000;
            MinAreaUnit = UnitType.Micron;
            MaxAreaUnit = UnitType.Micron;
        }

        #endregion

        #region Methods

        public override void InitialRun()
        {
            SetComponentFeatures();
        }

        private void SetComponentFeatures()
        {
            Macro.CurrentConponentService.AddComponent(_componentName, new List<Feature>
            {
                new Feature(FeatureType.Area),
                new Feature(FeatureType.Background),
                new Feature(FeatureType.Circularity),
                new Feature(FeatureType.Cycle),
                new Feature(FeatureType.Diameter),
                new Feature(FeatureType.Eccentricity),
                new Feature(FeatureType.Elongation),
                new Feature(FeatureType.HalfRadius),
                new Feature(FeatureType.Id),
                new Feature(FeatureType.Integral),
                new Feature(FeatureType.Intensity),
                new Feature(FeatureType.MajorAxis),
                new Feature(FeatureType.MaxPixel),
                new Feature(FeatureType.Merged),
                new Feature(FeatureType.MinorAxis),
                new Feature(FeatureType.Perimeter),
                new Feature(FeatureType.PeripheralIntegral),
                new Feature(FeatureType.PeripheralIntensity),
                new Feature(FeatureType.PeripheralMax),
                new Feature(FeatureType.RegionNo),
                new Feature(FeatureType.Scan),
                new Feature(FeatureType.Stdv),
                new Feature(FeatureType.Time),
                new Feature(FeatureType.WellNo),
                new Feature(FeatureType.XPos),
                new Feature(FeatureType.YPos),
                new Feature(FeatureType.ZPos)
            });

        }


        public override void Initialize()
        {
            HasImage = true;
            ModType = ModuleType.SmtContourCategory;
            InputPorts[0].DataType = PortDataType.BinaryImage;
            InputPorts[0].ParentModule = this;
            OutputPort.DataType = PortDataType.Event;
            OutputPort.ParentModule = this;
        }

        public override void OnExecute()
        {
            try
            {
                _img = InputImage;

                var min = UnitConversion(MinArea, MinAreaUnit, UnitType.Micron);
                var max = UnitConversion(MaxArea, MaxAreaUnit, UnitType.Micron);
                var pixel = Macro.CurrentScanInfo.XPixcelSize * Macro.CurrentScanInfo.YPixcelSize;



                Macro.CurrentConponentService.CreateContourBlobs(ComponentName, Macro.CurrentScanId,
                    Macro.CurrentRegionId + 1,
                    Macro.CurrentTileId, _img, min/pixel, (IsMaxAreaChecked ? max : int.MaxValue)/pixel);

                _img.Dispose();

                SetOutputComponent(ComponentName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Contour Module error: " + ex.Message);
                throw;
            }
        }

        private double UnitConversion(double sourceValue, UnitType sourceUnit, UnitType destUnit)
        {
            var res = -1.0;
            switch (destUnit)
            {
                case UnitType.Mm:
                    switch (sourceUnit)
                    {
                        case UnitType.Mm:
                            res = sourceValue;
                            break;
                        case UnitType.Micron:
                            res = sourceValue / 1000;
                            break;
                    }
                    break;
                case UnitType.Micron:
                    switch (sourceUnit)
                    {
                        case UnitType.Mm:
                            res = sourceValue * 1000;
                            break;
                        case UnitType.Micron:
                            res = sourceValue;
                            break;
                    }
                    break;
            }
            return res;
        }

        public override void OnSerialize(XmlWriter writer)
        {
            writer.WriteAttributeString("component", ComponentName);
            writer.WriteAttributeString("min-area", MinArea.ToString("F"));
            writer.WriteAttributeString("max-area", MaxArea.ToString("F"));
            writer.WriteAttributeString("check-max-area", IsMaxAreaChecked.ToString().ToLower());
            writer.WriteAttributeString("unit", MinAreaUnit.ToString());
            writer.WriteAttributeString("max-unit", MaxAreaUnit.ToString());
            writer.WriteAttributeString("concave", IsConcaveChecked.ToString().ToLower());
            writer.WriteAttributeString("cross-boundary", IsBoundaryChecked.ToString().ToLower());
        }

        public override void OnDeserialize(XmlReader reader)
        {
            ComponentName = reader["component"];
            if (reader["min-area"] != null)
            {
                MinArea = XmlConvert.ToDouble(reader["min-area"]);
            }

            if (reader["max-area"] != null)
            {
                MaxArea = Convert.ToDouble(reader["max-area"]);
            }

            if (reader["check-max-area"] != null)
            {
                IsMaxAreaChecked = XmlConvert.ToBoolean(reader["check-max-area"]);
            }

            if (reader["concave"] != null)
            {
                IsConcaveChecked = XmlConvert.ToBoolean(reader["concave"]);
            }

            if (reader["cross-boundary"] != null)
            {
                IsBoundaryChecked = XmlConvert.ToBoolean(reader["cross-boundary"]);
            }

            if (reader["max-unit"] != null)
            {
                MaxAreaUnit = (UnitType)Enum.Parse(typeof(UnitType), reader["max-unit"]);
            }

            if (reader["unit"] != null)
            {
                MinAreaUnit = (UnitType)Enum.Parse(typeof(UnitType), reader["unit"]);
            }
        }
        #endregion

    }
}