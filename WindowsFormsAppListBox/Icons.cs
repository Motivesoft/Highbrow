﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Highbrow
{
    public static class Icons
    {
        [StructLayout( LayoutKind.Sequential )]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
            public string szDisplayName;
            [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 80 )]
            public string szTypeName;
        };

        class Win32
        {
            public const uint SHGFI_ICON = 0x100;
            public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
            public const uint SHGFI_SMALLICON = 0x1; // 'Small icon

            [DllImport( "shell32.dll" )]
            public static extern IntPtr SHGetFileInfo( string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags );

            [DllImport( "User32.dll" )]
            public static extern int DestroyIcon( IntPtr hIcon );

        }

        static Icons()
        {
            // Do nothing
        }

        public static Icon GetPaddedIcon( Icon ico )
        {
            var bmp = new Bitmap( 20, 20 );

            var iconBmp = ico.ToBitmap();

            for ( int x = 0; x < 16; x++ )
            {
                for ( int y = 0; y < 16; y++ )
                {
                    bmp.SetPixel( x + 1, y + 1, iconBmp.GetPixel( x, y ) );
                }
            }

            var hIcon = bmp.GetHicon();
            Icon icon = (Icon) Icon.FromHandle( hIcon ).Clone();
            Win32.DestroyIcon( hIcon );
            return icon;
        }

        public static Icon GetSmallIcon( string fileName )
        {
            return GetIcon( fileName, Win32.SHGFI_SMALLICON );
        }

        public static Icon GetLargeIcon( string fileName )
        {
            return GetIcon( fileName, Win32.SHGFI_LARGEICON );
        }

        private static Icon GetIcon( string fileName, uint flags )
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hImgSmall = Win32.SHGetFileInfo( fileName, 0, ref shinfo, (uint) Marshal.SizeOf( shinfo ), Win32.SHGFI_ICON | flags );

            Icon icon = (Icon) Icon.FromHandle( shinfo.hIcon ).Clone();
            Win32.DestroyIcon( shinfo.hIcon );
            return icon;
        }
    }
}
