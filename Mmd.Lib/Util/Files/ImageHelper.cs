using com.google.zxing;
using com.google.zxing.common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Util.Files
{
    public static class ImageHelper
    {
        /// <summary>
        /// 合并图片
        /// </summary>
        /// <param name="backgroudImg">背景图</param>
        /// <param name="addImg">要绘制的image</param>
        /// <param name="x">所绘制图像的左上角的 x 坐标</param>
        /// <param name="y">所绘制图像的左上角的 y 坐标</param>
        /// <param name="width">所绘制图像的宽度</param>
        /// <param name="height">所绘制图像的高度</param>
        /// <returns></returns>
        public static Bitmap MergeImages(Bitmap backgroudImg, Bitmap addImg, int x, int y, int? width = null, int? height = null)
        {
            Graphics g = Graphics.FromImage(backgroudImg);
            g.DrawImage(addImg, x, y, width == null ? addImg.Width : width.Value, height == null ? addImg.Height : height.Value);
            g.Dispose();
            return backgroudImg;
        }

        /// <summary>
        /// 图片增加文字
        /// </summary>
        /// <param name="backgroudImg">背景色</param>
        /// <param name="drawstring">需要增加的文字</param>
        /// <param name="drawcolor">字体颜色</param>
        /// <param name="size">字体大小</param>
        /// <param name="x">左上角的 x 坐标</param>
        /// <param name="y">左上角的 y 坐标</param>
        /// <param name="fontfamily">字体，默认Arial</param>
        /// <param name="fontstyle">字体，默认Regular</param>
        /// <returns></returns>
        public static Bitmap DrawString(Bitmap backgroudImg,string drawstring, Color drawcolor, int size, float x,float y,  string fontfamily= "Arial", FontStyle fontstyle=FontStyle.Regular)
        {
            Graphics g = Graphics.FromImage(backgroudImg);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            FontFamily fm = new FontFamily("Arial");
            Font font = new Font(fm, size, FontStyle.Regular, GraphicsUnit.Pixel);
            SolidBrush sb = new SolidBrush(drawcolor);
            g.DrawString(drawstring, font, sb, new PointF(x, y));
            g.Dispose();
            return backgroudImg;
        }

        /// <summary>
        /// 旋转图片
        /// </summary>
        /// <param name="bmp">需要旋转的图片</param>
        /// <param name="angle">向左旋转的度数</param>
        /// <param name="bkColor">背景色</param>
        /// <returns></returns>
        public static Bitmap KiRotate(Bitmap bmp, float angle, Color bkColor)
        {
            int w = bmp.Width + 2;
            int h = bmp.Height + 2;
            PixelFormat pf;
            if (bkColor == Color.Transparent)
            {
                pf = PixelFormat.Format32bppArgb;
            }
            else
            {
                pf = bmp.PixelFormat;
            }
            Bitmap tmp = new Bitmap(w, h, pf);
            Graphics g = Graphics.FromImage(tmp);
            g.Clear(bkColor);
            g.DrawImageUnscaled(bmp, 1, 1);
            g.Dispose();
            GraphicsPath path = new GraphicsPath();
            path.AddRectangle(new RectangleF(0f, 0f, w, h));
            Matrix mtrx = new Matrix();
            mtrx.Rotate(angle);
            RectangleF rct = path.GetBounds(mtrx);
            Bitmap dst = new Bitmap((int)rct.Width, (int)rct.Height, pf);
            g = Graphics.FromImage(dst);
            g.Clear(bkColor);
            g.TranslateTransform(-rct.X, -rct.Y);
            g.RotateTransform(angle);
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.DrawImageUnscaled(tmp, 0, 0);
            g.Dispose();
            tmp.Dispose();
            return dst;
        }

        /// <summary>
        /// 生成图片二维码
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="width">生成二维码的宽度</param>
        /// <param name="height">生成二维码的高度</param>
        /// <returns></returns>
        public static Bitmap GenQr_Code(string url,int width,int height)
        {
            MultiFormatWriter mutiWriter = new com.google.zxing.MultiFormatWriter();
            ByteMatrix bm = mutiWriter.encode(url, com.google.zxing.BarcodeFormat.QR_CODE, width, width);
            return bm.ToBitmap();
        }
    }
}
