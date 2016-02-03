﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using FreeImageAPI;
using ImageProcess;
using ThorCyte.Infrastructure.Commom;
using ThorCyte.Infrastructure.Types;

namespace ThorCyte.Infrastructure.Interfaces
{
    internal class ImageInfo
    {
        public string ImagePath;
        public ScanRegion SRegion;
        public uint XSize;
        public uint YSize;
        public double XPixselSize;
        public double YPixselSize;
        public string ChannelName;
    }

    public class ThorImageData : IData
    {
        #region Filed
        private readonly List<string> _channelNameList = new List<string>
        {
            "ChanA",
            "ChanB",
            "ChanC",
            "ChanD",
        };
        #endregion

        private IExperiment _experiment;
        private Dictionary<int, ImageInfo> _imageInfoDictionary;

        public ThorImageData()
        {
            _imageInfoDictionary = new Dictionary<int, ImageInfo>();
        }

        public void SetExperimentInfo(IExperiment experiment)
        {
            _imageInfoDictionary.Clear();
            _experiment = experiment;
        }

        public ImageData GetData(int scanId, int scanRegionId, int channelId, int planeId, int timingFrameId)
        {
            return GetDetailData(scanId, scanRegionId, channelId, planeId, timingFrameId);
        }

        public ImageData GetData(int scanId, int scanRegionId, int channelId, int planeId, int timingFrameId,
            double scale, Int32Rect regionRect)
        {
            return GetDetailData(scanId, scanRegionId, channelId, planeId, timingFrameId, scale, regionRect);
        }

        //3D tile and stream raw data
        public ImageData GetTileData(int scanId, int scanRegionId, int channelId, int streamFrameId, int planeId, int tileId,
            int timingFrameId)
        {
            return GetTileDetailData(scanId, scanRegionId, channelId, streamFrameId, planeId, tileId, timingFrameId);
        }

        //2D
        public ImageData GetData(int scanId, int scanRegionId, int channelId, int timingFrameId)
        {
            return GetDetailData(scanId, scanRegionId, channelId, 1, timingFrameId);
        }

        public ImageData GetData(int scanId, int scanRegionId, int channelId, int timingFrameId,
            double scale, Int32Rect regionRect)
        {
            return GetDetailData(scanId, scanRegionId, channelId, 1, timingFrameId, scale, regionRect);
        }

        //2D tile and stream raw data
        public ImageData GetTileData(int scanId, int scanRegionId, int channelId, int streamFrameId, int tileId, int timingFrameId)
        {
            return GetTileDetailData(scanId, scanRegionId, channelId, streamFrameId, 1, tileId, timingFrameId);
        }

        //1D
        public ImageData GetData(int scanId, int scanRegionId, int channelId)
        {
            throw new NotImplementedException();
        }

        private ImageInfo GetImageInfo(int scanId, int scanRegionId, int channelId, Dictionary<int, ImageInfo> dictionary)
        {
            int keyId = scanId << 2 + scanRegionId << 1 + channelId;
            ImageInfo imageInfo;
            if (!dictionary.ContainsKey(keyId))
            {
                ScanInfo scanInfo = _experiment.GetScanInfo(scanId);

                imageInfo = new ImageInfo();
                imageInfo.ImagePath = scanInfo.DataPath;
                imageInfo.XPixselSize = scanInfo.XPixcelSize;
                imageInfo.YPixselSize = scanInfo.YPixcelSize;

                var rList = scanInfo.ScanRegionList as List<ScanRegion>;
                imageInfo.SRegion = rList.Find(x => x.RegionId == scanRegionId);
                var cList = scanInfo.ChannelList as List<Channel>;
                imageInfo.ChannelName = cList.Find(x => x.ChannelId == channelId).ChannelName;

                imageInfo.XSize = (uint)(Math.Round(imageInfo.SRegion.Bound.Width / scanInfo.XPixcelSize, MidpointRounding.AwayFromZero));
                imageInfo.YSize = (uint)(Math.Round(imageInfo.SRegion.Bound.Height / scanInfo.YPixcelSize, MidpointRounding.AwayFromZero));

                dictionary.Add(keyId, imageInfo);
            }
            else
            {
                dictionary.TryGetValue(keyId, out imageInfo);
            }

            return imageInfo;
        }

        private void ReadTiffData(string filePath, IntPtr buffer, int startX, int startY, uint sizeRow)
        {
            FIBITMAP fi = FreeImage.Load(FREE_IMAGE_FORMAT.FIF_TIFF, filePath, FREE_IMAGE_LOAD_FLAGS.DEFAULT);
            uint width = FreeImage.GetWidth(fi);
            uint height = FreeImage.GetHeight(fi);

            unsafe
            {
                ushort* des = (ushort*)buffer.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    int startIndex = (int)(startX + sizeRow * (startY + height - i - 1));
                    IntPtr ptr = FreeImage.GetScanLine(fi, i);
                    ushort* buf = (ushort*)ptr.ToPointer();
                    for (int x = 0; x < width; x++)
                        des[startIndex + x] = buf[x];
                }
            }
            
            FreeImage.Unload(fi);
        }

        private void ReadTiffData(string filePath, IntPtr buffer, int startX, int startY, int width, int height, uint sizeRow, int tilePosX, int tilePosY)
        {
            FIBITMAP fi = FreeImage.Load(FREE_IMAGE_FORMAT.FIF_TIFF, filePath, FREE_IMAGE_LOAD_FLAGS.DEFAULT);
            unsafe
            {
                ushort* desBuff = (ushort*)buffer.ToPointer();
                for (int i = 0; i < height; i++)
                {
                    int startIndex = (int)(startX + sizeRow * (startY + height - i - 1));
                    IntPtr ptr = FreeImage.GetScanLine(fi, i + tilePosY);

                    ushort* buf = (ushort*)ptr.ToPointer();
                    for (int x = 0; x < width; x++)
                        desBuff[startIndex + x] = buf[tilePosX + x];

                }
            }
            
            FreeImage.Unload(fi);
        }

        private bool CheckIds(int scanId, int scanRegionId, int channelId, int planeId, int streamFrameId, int timingFrameId)
        {
            ScanInfo scanInfo = _experiment.GetScanInfo(scanId);
            if (scanInfo == null)
                return false;

            if (scanRegionId < 0 || scanRegionId >= scanInfo.ScanRegionList.Count)
                return false;

            if (channelId < 0 || channelId >= scanInfo.ChannelList.Count)
                return false;

            switch (scanInfo.Mode)
            {
                case CaptureMode.Mode2DStream:
                    if (streamFrameId > scanInfo.StreamFrameCount || streamFrameId < 1) return false;
                    break;
                case CaptureMode.Mode2DTiming:
                    if (timingFrameId > scanInfo.TimingFrameCount || timingFrameId < 1) return false;
                    break;
                case CaptureMode.Mode2DTimingStream:
                    if (streamFrameId > scanInfo.StreamFrameCount || streamFrameId < 1) return false;
                    if (timingFrameId > scanInfo.TimingFrameCount || timingFrameId < 1) return false;
                    break;
                case CaptureMode.Mode3D:
                    if (planeId < 1 || planeId > scanInfo.ThirdDimensionSteps) return false;
                    break;
                case CaptureMode.Mode3DStream:
                case CaptureMode.Mode3DFastZStream:
                    if (planeId < 1 || planeId > scanInfo.ThirdDimensionSteps) return false;
                    if (streamFrameId > scanInfo.StreamFrameCount || streamFrameId < 1) return false;
                    break;
                case CaptureMode.Mode3DTiming:
                    if (planeId < 1 || planeId > scanInfo.ThirdDimensionSteps) return false;
                    if (timingFrameId > scanInfo.TimingFrameCount || timingFrameId < 1) return false;
                    break;
                case CaptureMode.Mode3DTimingStream:
                    if (planeId < 1 || planeId > scanInfo.ThirdDimensionSteps) return false;
                    if (streamFrameId > scanInfo.StreamFrameCount || streamFrameId < 1) return false;
                    if (timingFrameId > scanInfo.TimingFrameCount || timingFrameId < 1) return false;
                    break;
            }
            return true;
        }

        private Int32Rect Intersect(Int32Rect rect1, Int32Rect rect2)
        {
            Int32Rect rect = Int32Rect.Empty;
            Int32Rect left = rect1.X <= rect2.X ? rect1 : rect2;
            Int32Rect right = rect1.X > rect2.X ? rect1 : rect2;
            if ((right.X > left.X + left.Width) || (right.Y + right.Height < left.Y) || (right.Y > left.Y + left.Height))
            {
                return rect;
            }
            rect = new Int32Rect { X = right.X, Y = left.Y > right.Y ? left.Y : right.Y };
            if (right.X + right.Width <= left.X + left.Width)
                rect.Width = right.Width;
            else
                rect.Width = left.Width + left.X - right.X;
            if (right.Height <= left.Height)
            {
                if ((left.Y > right.Y) && (right.Y < left.Y + left.Height))
                    rect.Height = right.Y + right.Height - left.Y;
                else if ((left.Y <= right.Y) && (left.Y + left.Height >= right.Y + right.Height))
                    rect.Height = right.Height;
                else
                    rect.Height = left.Y + left.Height - right.Y;
            }
            else
            {
                if (right.Y + right.Height <= left.Y + left.Height)
                    rect.Height = right.Y + right.Height - left.Y;
                else if ((left.Y > right.Y) && (right.Y + right.Height > left.Y + left.Height))
                    rect.Height = left.Height;
                else
                    rect.Height = left.Y + left.Height - right.Y;
            }
            if (rect.Width > 0 && rect.Height > 0)
                return rect;
            return Int32Rect.Empty;
        }

        private ImageData GetDataFromBuffer(IntPtr buffer, int width, int height, double scale)
        {
            int scaleWidth = (int)(width*scale);
            int scaleHeight = (int)(height*scale);
            var image = new ImageData((uint) scaleWidth, (uint) scaleHeight);
            ImageProcessLib.Resize16U(buffer, width, height, image.DataBuffer, scaleWidth, scaleHeight, 1);
            return image;

        }

        private ImageData GetDetailData(int scanId, int scanRegionId, int channelId, int planeId, int timingFrameId)
        {
            if (!CheckIds(scanId, scanRegionId, channelId, planeId, 1, timingFrameId))
                return null;
            ImageInfo imageInfo = GetImageInfo(scanId, scanRegionId, channelId, _imageInfoDictionary);
            ImageData image = new ImageData(imageInfo.XSize, imageInfo.YSize);

            int tileCount = imageInfo.SRegion.ScanFieldList.Count;

            var tasks = new List<Task>();
            for (int i = 0; i < tileCount; i++)
            {
                Scanfield scanField = imageInfo.SRegion.ScanFieldList[i];
                int startXPixel = (int)((scanField.SFRect.Left - imageInfo.SRegion.Bound.Left) / imageInfo.XPixselSize);
                int startYPixel = (int)((scanField.SFRect.Top - imageInfo.SRegion.Bound.Top) / imageInfo.YPixselSize);

                string filePath = imageInfo.ImagePath + string.Format("\\{0}_{1}_{2}_{3}_{4}.tif", imageInfo.ChannelName,
                    String.Format("{0:D4}", scanRegionId + 1),
                    String.Format("{0:D4}", i + 1),
                    String.Format("{0:D4}", planeId),
                    String.Format("{0:D4}", timingFrameId));
                if (File.Exists(filePath))
                {
                    tasks.Add(Task.Factory.StartNew(() => ReadTiffData(filePath, image.DataBuffer, startXPixel, startYPixel, imageInfo.XSize)));
                }
                else
                {
                    return null;
                }
            }
            if (tasks.Count > 0)
                Task.WaitAll(tasks.ToArray());
            return image;
        }

        private ImageData GetDetailData(int scanId, int scanRegionId, int channelId, int planeId, int timingFrameId,
            double scale, Int32Rect regionRect)
        {
            if (scale <= 0.0 || !regionRect.HasArea || regionRect.X < 0 || regionRect.Y < 0)
                return null;
            if (!CheckIds(scanId, scanRegionId, channelId, planeId, 1, timingFrameId))
                return null;

            IntPtr imageHandle = Marshal.AllocHGlobal(regionRect.Width * regionRect.Height * 2);
            Int32Rect tileRect = new Int32Rect();
            tileRect.Width = _experiment.GetScanInfo(scanId).TileWidth;
            tileRect.Height = _experiment.GetScanInfo(scanId).TiledHeight;

            ImageInfo imageInfo = GetImageInfo(scanId, scanRegionId, channelId, _imageInfoDictionary);
            regionRect.X += (int)(imageInfo.SRegion.Bound.Left / imageInfo.XPixselSize);
            regionRect.Y += (int)(imageInfo.SRegion.Bound.Top / imageInfo.YPixselSize);
            int tileCount = imageInfo.SRegion.ScanFieldList.Count;

            var tasks = new List<Task>();
            for (int i = 0; i < tileCount; i++)
            {
                Scanfield scanField = imageInfo.SRegion.ScanFieldList[i];
                tileRect.X = (int)(scanField.SFRect.Left / imageInfo.XPixselSize);
                tileRect.Y = (int)(scanField.SFRect.Top / imageInfo.YPixselSize);

                Int32Rect rect = Intersect(tileRect, regionRect);
                if (rect != Int32Rect.Empty)
                {
                    string filePath = imageInfo.ImagePath + string.Format("\\{0}_{1}_{2}_{3}_{4}.tif", imageInfo.ChannelName,
                    String.Format("{0:D4}", scanRegionId + 1),
                    String.Format("{0:D4}", i + 1),
                    String.Format("{0:D4}", planeId),
                    String.Format("{0:D4}", timingFrameId));

                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    int tilePosX, tilePosY;
                    if (rect.X == tileRect.X)
                        tilePosX = 0;
                    else
                        tilePosX = rect.X - tileRect.X;


                    if (rect.Y == tileRect.Y)
                        tilePosY = tileRect.Height - rect.Height;
                    else
                        tilePosY = (tileRect.Y + tileRect.Height) - (rect.Y + rect.Height);

                    tasks.Add(Task.Factory.StartNew(() => ReadTiffData(filePath, imageHandle, rect.X - regionRect.X, rect.Y - regionRect.Y,
                        rect.Width, rect.Height, (uint)regionRect.Width, tilePosX, tilePosY)));
                }
            }
            if (tasks.Count > 0)
                Task.WaitAll(tasks.ToArray());

            ImageData image = GetDataFromBuffer(imageHandle, regionRect.Width, regionRect.Height, scale);
            Marshal.FreeHGlobal(imageHandle);
            return image;
        }

        private ImageData GetTileDetailData(int scanId, int scanRegionId, int channelId, int streamFrameId, int planeId, int tileId,
            int timingFrameId)
        {
            if (!CheckIds(scanId, scanRegionId, channelId, planeId, streamFrameId, timingFrameId))
                return null;
            ImageData image = null;
            ScanInfo scanInfo = _experiment.GetScanInfo(scanId);
            if (scanInfo.ImageFormat == ImageFileFormat.Tiff)
            {
                ImageInfo imageInfo = GetImageInfo(scanId, scanRegionId, channelId, _imageInfoDictionary);
                if (tileId > imageInfo.SRegion.ScanFieldList.Count || tileId <= 0)
                    return null;

                int timePointIndex;
                if (scanInfo.Mode == CaptureMode.Mode3DTiming || scanInfo.Mode == CaptureMode.Mode2DTiming)
                {
                    timePointIndex = timingFrameId;
                }
                else if (scanInfo.Mode == CaptureMode.Mode3D || scanInfo.Mode == CaptureMode.Mode2D)
                {
                    timePointIndex = 1;
                }
                else if (scanInfo.Mode == CaptureMode.Mode2DStream)
                {
                    timePointIndex = streamFrameId;
                }
                else if(scanInfo.Mode == CaptureMode.Mode3DStream)
                {
                    timePointIndex = (planeId - 1) * scanInfo.StreamFrameCount + streamFrameId;
                }
                else if (scanInfo.Mode == CaptureMode.Mode3DTimingStream)
                {
                    timePointIndex = (scanInfo.StreamFrameCount * scanInfo.ThirdDimensionSteps) * (timingFrameId - 1) + (planeId - 1) * scanInfo.StreamFrameCount + streamFrameId;
                }
                else if (scanInfo.Mode == CaptureMode.Mode3DFastZStream)
                {
                    timePointIndex = streamFrameId;
                }
                else
                {
                    return null;
                }

                image = new ImageData((uint)scanInfo.TileWidth, (uint)scanInfo.TiledHeight);
                string filePath = imageInfo.ImagePath + string.Format("\\{0}_{1}_{2}_{3}_{4}.tif", imageInfo.ChannelName,
                    String.Format("{0:D4}", scanRegionId + 1),
                    String.Format("{0:D4}", tileId),
                    String.Format("{0:D4}", planeId),
                    String.Format("{0:D4}", timePointIndex));
                if (File.Exists(filePath))
                    ReadTiffData(filePath, image.DataBuffer, 0, 0, (uint)scanInfo.TileWidth);
            }
            else
            {
                if (tileId > scanInfo.ScanRegionList[scanRegionId].ScanFieldList.Count || tileId <= 0)
                    return null;
                string path = scanInfo.DataPath + string.Format("\\Image_{0}_{1}.raw",
                    String.Format("{0:D4}", scanRegionId + 1),
                    String.Format("{0:D4}", tileId));
                if (File.Exists(path))
                {
                    image = new ImageData((uint)scanInfo.TileWidth, (uint)scanInfo.TiledHeight);
                    int offset;
                    int bufferSize = (int)(image.XSize * image.YSize);

                    int chIndex = _channelNameList.IndexOf(scanInfo.ChannelList[channelId].ChannelName);
                    if (chIndex < 0)
                        return null;

                    int chCount = _channelNameList.Count;

                    if (scanInfo.Mode == CaptureMode.Mode2DStream)
                    {
                        if (scanInfo.ChannelList.Count == 1)
                        {
                            offset = bufferSize * 2 * (streamFrameId - 1);
                        }
                        else
                        {
                            offset = ((streamFrameId - 1) * chCount + chIndex) * bufferSize * 2;
                        }
                    }
                    else if (scanInfo.Mode == CaptureMode.Mode3DFastZStream)
                    {
                        int frameCount = scanInfo.FlybackFrameCount + scanInfo.ThirdDimensionSteps;
                        if (scanInfo.ChannelList.Count == 1)
                        {
                            offset = bufferSize * 2 * ((streamFrameId - 1) * frameCount + planeId);
                        }
                        else
                        {
                            offset = (((streamFrameId - 1) * (frameCount*chCount))+ (planeId - 1)*chCount + chIndex) * bufferSize * 2;
                        }
                    }
                    else
                    {
                        return null;
                    }
                    
                    if (!RawDataReader.ReadBuffer(path, image.DataBuffer, offset, bufferSize))
                        return null;
                }
            }

            return image;
        }
    }
}
