﻿using System.Collections.Generic;
using ComponentDataService.Types;
using ImageProcess;
using ImageProcess.DataType;
using ThorCyte.Infrastructure.Interfaces;

namespace ComponentDataService
{
   

    public interface IComponentDataService
    {
        void Load(IExperiment experiment);
        IList<string> GetComponentNames();
        IList<Blob> GetBlobs(string componentName, int wellId, int tileId, BlobType type);
        IList<BioEvent> GetEvents(string componentName, int wellId);
        IList<Feature> GetFeatures(string componentName);
        void SetComponent(string componentName, IList<Feature> features);
        int GetFeatureIndex(string componentName, FeatureType type, string channelName = null);
        void SaveBlobs(string fileFolder);
        void SaveEvents(string fileFolder);

        IList<Blob> CreateContourBlobs(string componentName, int scanId, int wellId, int tileId,
            ImageData data, double minArea, double maxArea);

        IList<BioEvent> CreateEvents(string componentName, int scanId, int wellId, int tileId, 
            IDictionary<string, ImageData> imageDict, BlobDefine define);
    }
}