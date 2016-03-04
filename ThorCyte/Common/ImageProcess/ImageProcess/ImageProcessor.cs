﻿
using ImageProcess.DataType;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace ImageProcess
{
    public enum ThresholdType
    {
        Auto = 0,
        Manual
    };

    public enum FilterType
    {
        HiGauss = 0,
        HiPass,
        LowPass,
        HorizontalEdge,
        VerticalEdge,
        Laplace,
        Fish,
        FishB,
        FishC
    };

    public partial class ImageData
    {
        #region Fields

        private const int KernelCount = 9;
        private static readonly int[][] KernelSizeList = new int[KernelCount][];
        #endregion

        #region constructor

        static ImageData() 
        {
            InitKernelSizeList();
        }
        #endregion

        #region Public

        public static ReadOnlyCollection<int> GetSupportedKernelSize(FilterType type)
        {
            return Array.AsReadOnly(KernelSizeList[(int) type]);
        }

        public ImageData Resize(int dstWidth, int dstHeight)
        {
            if (dstHeight <= 0) throw new ArgumentOutOfRangeException("dstWidth");
            if (dstWidth <= 0) throw new ArgumentOutOfRangeException("dstWidth");
            var dstData = new ImageData((uint) dstWidth, (uint) dstHeight, IsGray);
            ImageProcessLib.Resize16U(this, (int) XSize, (int) YSize, dstData, dstWidth, dstHeight, Channels);
            return dstData;
        }

        public ImageData AddConstant(ushort value, int depth = 14)
        {
            var width = (int) XSize;
            var height = (int) YSize;
            var dstData = new ImageData(XSize, YSize, IsGray);
            var maxValue = (ushort) ((0x01 << depth) - 1);
            ImageProcessLib.AddConstant16U(value, this, width, height, dstData, Channels, maxValue);

            return dstData;
        }

        public ImageData Add(ImageData other, int depth = 14)
        {
            return Add(other, false, depth);
        }

        private ImageData Add(ImageData other, bool isModify, int depth = 14)
        {
            if (XSize != other.XSize) throw new InvalidOperationException("Image data XSize not equal.");
            if (YSize != other.YSize) throw new InvalidOperationException("Image data YSize not equal.");
            if (Channels != other.Channels) throw new InvalidOperationException("Image data Channels not equal.");           
            var height = (int)YSize;
            var width = (int)XSize;
            var maxValue = (ushort)((0x01 << depth) - 1);
            if (isModify)
            {
                ImageProcessLib.Add16UInplace(other, width, height, Channels, maxValue, this);
                return this;
            }
            else
            {
                var dstData = new ImageData(XSize, YSize, IsGray);
                ImageProcessLib.Add16U(this, other, width, height, dstData, Channels, maxValue);
                return dstData;
            }
        }

        public ImageData SubConstant(ushort value)
        {
            var width = (int) XSize;
            var height = (int) YSize;
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.SubConstant16U(value, this, width, height, dstData, Channels);      
            return dstData;
        }

        public ImageData Sub(ImageData subtracter)
        {
            if (XSize != subtracter.XSize) throw new InvalidOperationException("Image data XSize not equal.");
            if (YSize != subtracter.YSize) throw new InvalidOperationException("Image data YSize not equal.");
            if (Channels != subtracter.Channels) throw new InvalidOperationException("Image data Channels not equal.");
            var dstData = new ImageData(XSize, YSize, IsGray);
            var height = (int) YSize;
            var width = (int) XSize;
            ImageProcessLib.Sub16U(this, subtracter, width, height, dstData, Channels);
            return dstData;
        }

      

        //public static ImageData MulConstant(this ImageData srcData, ushort value, int depth= 14)
        //{
        //    var width = (int) srcData.XSize;
        //    var height = (int) srcData.YSize;
        //    var dstData = new ImageData(srcData.XSize, srcData.YSize);
        //    var maxValue = (ushort)((0x01 << depth) - 1);
        //    ImageProcessLib.MulConstant16U(value, srcData.DataBuffer, width, height, dstData.DataBuffer,
        //        dstData.Channels, maxValue);
        //    return dstData;
        //}

        public ImageData MulConstant(double value, int depth = 14)
        {
            return SetBrightnessAndContrast(value, 0, depth);
        }

        public ImageData SetBrightnessAndContrast(double alpha, int beta, int depth = 14)
        {
            var dstData = new ImageData(XSize, YSize, IsGray);
            var maxValue = (ushort)((0x01 << depth) - 1);
            int n = Length;
            for (int i = 0; i < n; i++)
            {
                var value = (int)(alpha * this[i] + beta);
                if (value > maxValue) value = maxValue;
                else if (value < 0) value = 0;
                dstData[i] = (ushort) value;
            }
            return dstData;
        }

        public ushort Max()
        {
            if (Channels != 1) throw new InvalidOperationException("must be 1 channel image data.");
            ushort maxValue;
            ImageProcessLib.Max16U(this, (int) XSize, (int) YSize, Channels, out maxValue);
            return maxValue;
        }

        public  ushort Min()
        {
            if (Channels != 1) throw new InvalidOperationException("must be 1 channel image data.");
            ushort minValue;
            ImageProcessLib.Min16U(this, (int) XSize, (int) YSize, Channels, out minValue);
            return minValue;
        }

        public ImageData Max(ImageData other)
        {
            if (XSize != other.XSize) throw new InvalidOperationException("Image data XSize not equal.");
            if (YSize != other.YSize) throw new InvalidOperationException("Image data YSize not equal.");
            if (Channels != other.Channels) throw new InvalidOperationException("Image data Channels not equal.");
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.MaxEvery16U(this, other, (int) XSize, (int) YSize, Channels, dstData);
            return dstData;
        }

        public  ImageData Min(ImageData other)
        {
            if (XSize != other.XSize) throw new InvalidOperationException("Image data XSize not equal.");
            if (YSize != other.YSize) throw new InvalidOperationException("Image data YSize not equal.");
            if (Channels != other.Channels) throw new InvalidOperationException("Image data Channels not equal.");
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.MinEvery16U(this, other, (int) XSize, (int) YSize, Channels, dstData);
            return dstData;
        }

        public ImageData Invert(int depth = 14)
        {
            var maxValue = (ushort) ((0x01 << depth) - 1);
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.Invert16U(this, (int) XSize, (int) YSize, Channels, maxValue, dstData);
            return dstData;
        }

        public IList<ushort> GetDataInProfileLine(Point start, Point end)
        {
            List<Point> points = GetPointsInProfileLine((int) start.X, (int) start.Y, (int) end.X, (int) end.Y);
            return points.ConvertAll(pt =>
            {
                var x = (int) pt.X;
                var y = (int) pt.Y;
                return this[(int) (y*XSize + x)];
            });
        }

        public IList<IList<ushort>> GetMultiChannelsDataInProfileLine(Point start, Point end)
        {
            if (Channels != 3) throw new InvalidOperationException("only support 3 channel image");
            const int channels = 3;
            List<Point> points = GetPointsInProfileLine((int) start.X, (int) start.Y, (int) end.X, (int) end.Y);
            int n = points.Count;
            var first = new ushort[n];
            var second = new ushort[n];
            var third = new ushort[n];
            Int64 step = XSize*channels;
            for (int i = 0; i < n; i++)
            {
                Point pt = points[i];
                var x = (int) pt.X;
                var y = (int) pt.Y;
                first[i] = this[(int) (y*step + channels*x)];
                second[i] = this[(int)(y * step + channels * x + 1)];
                third[i] = this[(int)(y * step + channels * x + 2)];
            }
            return new IList<ushort>[] {first, second, third};
        }

    
        public IList<Blob> FindContours(double minArea, double maxArea = int.MaxValue)
        {
            IntPtr pBlobs = IntPtr.Zero;
            IntPtr pPointsCountPerBlob = IntPtr.Zero;
            int blobCount = ImageProcessLib.FindContours16UC1(this, (int) XSize, (int) YSize, ref pBlobs,
                ref pPointsCountPerBlob);
            if (blobCount == 0 || pPointsCountPerBlob == IntPtr.Zero || pBlobs == IntPtr.Zero)
                return new Blob[0];
            var pointsCountPerBlob = new int[blobCount];
            Marshal.Copy(pPointsCountPerBlob, pointsCountPerBlob, 0, blobCount);
            var blobs = new List<Blob>(blobCount);
            int maxCount = pointsCountPerBlob.Max();
            var buffer = new int[maxCount*2];
            int offset = 0;
            var points = new List<Point>(maxCount);
            for (int i = 0; i < blobCount; i++)
            {
                int count = pointsCountPerBlob[i];
                Marshal.Copy(pBlobs + sizeof(int) * offset, buffer, 0, 2 * count);
                points.Clear();
                for (int j = 0; j < count; j++)
                {
                    int x = buffer[2 * j];
                    int y = buffer[2 * j + 1];
                    var point = new Point(x, y);
                    points.Add(point);
                }
                offset += 2 * count;
                var blob = new Blob(points);
                if (blob.Area >= minArea && blob.Area <= maxArea)
                    blobs.Add(blob);
            }
            ImageProcessLib.FreeBuffer(pBlobs);
            ImageProcessLib.FreeBuffer(pPointsCountPerBlob);
            return blobs;
        }

        public ImageData Threshold(ushort threshold, ThresholdType thresholdType)
        {
            if (Channels != 1)
                throw new InvalidOperationException("only support 1 channel image");
            var dstData = new ImageData(XSize, YSize, IsGray);
            var width = (int)XSize;
            var height = (int)YSize;
            if (thresholdType == ThresholdType.Auto)
            {
                ImageProcessLib.OtsuThreshold16UC1(this, width, height, dstData);
            }
            else if (thresholdType == ThresholdType.Manual)
            {
                ImageProcessLib.Threshold16UC1(this, width, height, threshold, dstData);
                //for (int i = 0; i < Length; i++)
                //{
                //    dstData[i] = this[i] > threshold ? (ushort) 16383 : (ushort) 0;
                //}
            }
            return dstData;
        }


        public double Mean()
        {
            if (Channels != 1) throw new InvalidOperationException("only support 1 channel image");
            double mean;
            ImageProcessLib.Mean16UC1(this, (int) XSize, (int) YSize, out mean);
            return mean;
        }

        public ImageData ShiftPeak(ushort refPos, int maxBits = 14)
        {
            if (Channels != 1) throw new InvalidOperationException("only support 1 channel image");
            double mean = Mean();
            var offset = (int) (refPos - mean);
            if (offset > 0) return AddConstant((ushort) offset, maxBits);
            else if (offset == 0) return Clone();
            else return SubConstant((ushort)(-offset));
        }

        public ImageData Dilate(int expand)
        {
            if (expand <= 0) throw new ArgumentOutOfRangeException("expand", "must be larger than 0.");
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.Dilate16UC1(this, (int) XSize, (int) YSize, 2*expand + 1, dstData);
            return dstData;

        }

        public double Sum(byte[] mask, int maskStep)
        {
            IntPtr pMask = Marshal.AllocHGlobal(mask.Length);
            Marshal.Copy(mask, 0, pMask, mask.Length);
            double sum;
            ImageProcessLib.Sum16UC1M(this, (int) XSize, (int) YSize, pMask, maskStep, out sum);
            return sum;
        }


        public ImageData CommonFilter(FilterType type, int maskSize, int pass)
        {
            CheckKernelSize(type, maskSize);
            if (pass <= 0)
                throw new ArgumentOutOfRangeException("pass");
            else
            {
                var width = (int) XSize;
                var height = (int) YSize;
                ImageData srcData = this;
                var dstData = new ImageData(XSize, YSize, IsGray);
                for (int i = 0; i < pass; i++)
                {
                    ImageProcessLib.Filter16U(srcData, width, height, Channels, dstData, type, maskSize);
                    srcData = dstData;
                }
                return dstData;
            }
        }

        public ImageData BitwiseAnd(ImageData other)
        {
            if (Channels != 1) throw new InvalidOperationException("only support 1 channel image");
            if (other.Channels != Channels) throw new InvalidOperationException("Image data Channels not equal.");
            if (other.XSize != XSize || other.YSize != YSize)
                throw new InvalidOperationException("Image data size not equal.");
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.BitwiseAnd16UC1(this, other, (int) XSize, (int) YSize, dstData);
            return dstData;

        }

        public ImageData BitwiseOr(ImageData other)
        {
            if (Channels != 1) throw new InvalidOperationException("only support 1 channel image");
            if (other.Channels != Channels) throw new InvalidOperationException("Image data Channels not equal.");
            if (other.XSize != XSize || other.YSize != YSize)
                throw new InvalidOperationException("Image data size not equal.");
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.BitwiseOr16UC1(this, other, (int)XSize, (int)YSize, dstData);
            return dstData;
        }

        public ImageData BitwiseXor(ImageData other)
        {
            if (Channels != 1) throw new InvalidOperationException("only support 1 channel image");
            if (other.Channels != Channels) throw new InvalidOperationException("Image data Channels not equal.");
            if (other.XSize != XSize || other.YSize != YSize)
                throw new InvalidOperationException("Image data size not equal.");
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.BitwiseXor16UC1(this, other, (int)XSize, (int)YSize, dstData);
            return dstData;
        }

        public ImageData Transform(int xshift, int yshift, double angle)
        {
            var dstData = new ImageData(XSize, YSize, IsGray);
            ImageProcessLib.RotateShift16U(this, (int) XSize, (int) YSize, Channels, angle, -xshift, yshift, dstData);
            return dstData;
        }
        #endregion

        #region Private

        private void CheckKernelSize(FilterType type, int maskSize)
        {
            int[] size = KernelSizeList[(int) type];
            bool vaild = false;
            int n = size.Length;
            for (int i = 0; i < n; i++)
            {
                vaild = vaild || (maskSize == size[i]);
            }
            if (vaild == false)
            {
                string msg = GenerateMaskErrorMsg(size);
                throw new ArgumentOutOfRangeException("maskSize", msg);
            }
        }

        private string GenerateMaskErrorMsg(int[] size)
        {
            int n = size.Length;
            var sb = new StringBuilder(2 * n);
            for (int i = 0; i < n; i++)
            {
                sb.Append(size[i]);
                sb.Append(',');
            }
            return string.Format("only support mask size = {0}.", sb);
        }

        private static List<Point> GetPointsInProfileLine(int startX, int startY, int endX, int endY)
        {
            bool steep = Math.Abs(endY - startY) > Math.Abs(endX - startX);
            if (steep)
            {
                int t = startX;
                startX = startY;
                startY = t;
                t = endX; // swap endX and endY
                endX = endY;
                endY = t;
            }
            if (startX > endX)
            {
                int t = startX;
                startX = endX;
                endX = t;
                t = startY; // swap startY and endY
                startY = endY;
                endY = t;
            }
            int dx = endX - startX;
            int dy = Math.Abs(endY - startY);
            int error = dx / 2;
            int ystep = (startY < endY) ? 1 : -1;
            int y = startY;
            var n = (int)(Math.Sqrt(Math.Pow(startX - endX, 2) + Math.Pow(startY - endY, 2)));
            var points = new List<Point>(n);
            for (int x = startX; x <= endX; x++)
            {
                int col = (steep ? y : x);
                int row = (steep ? x : y);
                points.Add(new Point(col, row));
                error = error - dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }
            return points;
        }

       
       
       
      

        private static void InitKernelSizeList()
        {
            const int fishCount = 3;
            int[] hiGuassKernelSize = {5, 7, 9};
            KernelSizeList[0] = hiGuassKernelSize;
            int[] commonKernelSize = {3, 5, 7};
            for (int i = 1; i < KernelCount - fishCount; i++) // except 3 fish filter, all 3,5,7 kernel size
            {
                KernelSizeList[i] = commonKernelSize;
            }
            int[] fishKernelSize = {11};
            for (int i = KernelCount - fishCount; i < KernelCount; i++)
                KernelSizeList[i] = fishKernelSize;

        }
        #endregion
    }
}