﻿using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Modularity;
using Prism.Regions;
using ThorCyte.CarrierModule.Common;
using ThorCyte.CarrierModule.Views;
using ThorCyte.Infrastructure.Commom;
using ThorCyte.Infrastructure.Events;
using ThorCyte.Infrastructure.Interfaces;

namespace ThorCyte.CarrierModule
{
    public class CarrierModule : IModule
    {

        #region Static Members
        private readonly IRegionViewRegistry _regionViewRegistry;
        private readonly IEventAggregator _eventAggregator;
        private readonly IUnityContainer _container;
        public static DisplayMode Mode;
        public static ILog Logger;
        #endregion

        #region Constructor
        public CarrierModule(IRegionViewRegistry regionViewRegistry, IEventAggregator eventAggregator, IUnityContainer container)
        {
            _regionViewRegistry = regionViewRegistry;
            _eventAggregator = eventAggregator;
            _container = container;
            ShowRegionEventHandler("ReviewModule");
            Mode = DisplayMode.Review;
            Logger = ServiceLocator.Current.GetInstance<ILog>();
        }
        #endregion

        #region Methods
        public void Initialize()
        {
            _container.RegisterInstance(this);
            _regionViewRegistry.RegisterViewWithRegion(RegionNames.ReviewCarrierRegion, typeof(CarrierView));
            _eventAggregator.GetEvent<ShowRegionEvent>().Subscribe(ShowRegionEventHandler, ThreadOption.UIThread, true);
        }

        private void ShowRegionEventHandler(string moduleName)
        {
            switch (moduleName)
            {
                case "ReviewModule":
                    Mode = DisplayMode.Review;
                    break;

                case "ProtocolModule":
                    Mode = DisplayMode.Protocol;
                    break;

                case "AnalysisModule":
                    Mode = DisplayMode.Analysis;
                    break;
            }
        }
        #endregion

    }
}