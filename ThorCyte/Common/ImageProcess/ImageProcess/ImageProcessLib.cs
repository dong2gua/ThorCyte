﻿
using System;
using System.Runtime.InteropServices;

namespace ImageProcess
{
    public class ImageProcessLib
    {
        #region DllImport
        private const string DllName = "ImageProcessLib.dll";
        public const int IppStsNoErr = 0;

        [DllImport(DllName, EntryPoint = "fnipp_lib_getCpuClocks", CallingConvention = CallingConvention.Cdecl)]
        internal static extern UInt64 GetCpuClocks();
        [DllImport(DllName, EntryPoint = "fnipp_lib_init", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int InitImageProcessLib();

        [DllImport(DllName, EntryPoint = "fnipp_lib_resize_16u", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Resize16U(IntPtr srcBuffer, int srcWidth, int srcHeight, IntPtr dstBuffer,
            int dstWidth, int dstHeight, int channels);

        [DllImport(DllName, EntryPoint = "fnipp_lib_resize_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Resize16U(SafeHandle srcBuffer, int srcWidth, int srcHeight, SafeHandle dstBuffer,
            int dstWidth, int dstHeight, int channels);

        [DllImport(DllName, EntryPoint = "fnipp_lib_addConstant_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AddConstant16U(ushort value, SafeHandle srcBuffer, int width, int height,
            SafeHandle dstBuffer, int channels, ushort maxValue);

        [DllImport(DllName, EntryPoint = "fnipp_lib_add_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Add16U(SafeHandle srcBuffer1, SafeHandle srcBuffer2, int width, int height,
            SafeHandle dstBuffer, int channels, ushort maxValue);

        [DllImport(DllName, EntryPoint = "fnipp_lib_add_16uI", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Add16UInplace(SafeHandle srcBuffer, int width, int height, int channels,
            ushort maxValue, SafeHandle srcDstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_subConstant_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SubConstant16U(ushort value, SafeHandle srcBuffer, int width, int height,
            SafeHandle dstBuffer, int channels);

        [DllImport(DllName, EntryPoint = "fnipp_lib_sub_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Sub16U(SafeHandle minuendBuffer, SafeHandle subtracterBuffer, int width, int height,
            SafeHandle dstBuffer, int channels);

        [DllImport(DllName, EntryPoint = "fnipp_lib_sub_16uI", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Sub16UInplace(SafeHandle minuendBuffer, SafeHandle subtracterBuffer, int width,
            int height, int channels);

        [DllImport(DllName, EntryPoint = "fnipp_lib_mulConstant_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int MulConstant16U(ushort value, SafeHandle srcBuffer, int width, int height,
            SafeHandle dstBuffer, int channels, ushort maxValue);

        [DllImport(DllName, EntryPoint = "fnipp_lib_max_16u",CallingConvention=CallingConvention.Cdecl)]
        internal static extern int Max16U(SafeHandle srcBuffer, int width, int height, int channels, out ushort maxValue);

        [DllImport(DllName, EntryPoint = "fnipp_lib_min_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Min16U(SafeHandle srcBuffer, int width, int height, int channels, out ushort minValue);

        [DllImport(DllName, EntryPoint = "fnipp_lib_maxEvery_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int MaxEvery16U(SafeHandle firstBuffer, SafeHandle secondBuffer, int width, int height,
            int channels, SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_minEvery_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int MinEvery16U(SafeHandle firstBuffer, SafeHandle secondBuffer, int width, int height,
            int channels, SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_invert_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Invert16U(SafeHandle srcBuffer, int width, int height, int channels, ushort maxValue,
            SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_invert_16uI", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Invert16UInplace(SafeHandle srcDstBuffer, int width, int height, int channels, ushort maxValue);

        [DllImport(DllName, EntryPoint = "fnipp_lib_threshold_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Threshold16UC1(SafeHandle srcBuffer, int width, int height, ushort threshold,
            SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_otsuThreshold_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int OtsuThreshold16UC1(SafeHandle srcBuffer, int width, int height, SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_dilate_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Dilate16UC1(SafeHandle srcBuffer, int width, int height, int maskSize, SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_sum_16uC1M", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Sum16UC1M(SafeHandle srcBuffer, int width, int height, IntPtr mask, int maskStep,
            out double sum);

        [DllImport(DllName, EntryPoint = "fnipp_lib_filter_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Filter16U(SafeHandle srcBuffer, int width, int height, int channels,
            SafeHandle dstBuffer, FilterType type, int maskSize);

        [DllImport(DllName, EntryPoint = "fnipp_lib_mean_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Mean16UC1(SafeHandle srcBuffer, int width, int height, out double mean);

        [DllImport(DllName, EntryPoint = "fnipp_lib_And_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int BitwiseAnd16UC1(SafeHandle srcBuffer1, SafeHandle srcBuffer2, int width, int height,
            SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_And_16uC1I", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int BitwiseAnd16UC1Inplace(SafeHandle srcBuffer, int width, int height, SafeHandle srcDstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_Or_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int BitwiseOr16UC1(SafeHandle srcBuffer1, SafeHandle srcBuffer2, int width, int height,
            SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_Or_16uC1I", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int BitwiseOr16UC1Inplace(SafeHandle srcBuffer, int width, int height, SafeHandle srcDstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_Xor_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int BitwiseXor16UC1(SafeHandle srcBuffer1, SafeHandle srcBuffer2, int width, int height,
            SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_Xor_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int BitwiseXor16UC1Inplace(SafeHandle srcBuffer, int width, int height, SafeHandle srcDstBuffer);

        [DllImport(DllName, EntryPoint = "fnipp_lib_rotateShift_16u", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RotateShift16U(SafeHandle srcBuffer, int width, int height, int channels,
            double angle, int xshift, int yshift, SafeHandle dstBuffer);

        [DllImport(DllName, EntryPoint = "fncv_lib_findContours_16uC1", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int FindContours16UC1(SafeHandle srcBuffer, int width, int height, ref IntPtr pBlobs,
            ref IntPtr pPointsCountPerBlob);

        [DllImport(DllName, EntryPoint = "free_buffer", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreeBuffer(IntPtr pBuffer);
        #endregion

        static ImageProcessLib()
        {
            InitImageProcessLib();
        }
    }
}