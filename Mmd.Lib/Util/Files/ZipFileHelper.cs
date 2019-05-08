﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Checksums;

namespace MD.Lib.Util.Files
{
    public class ZipFileHelper
    {
        /// <summary>
        /// 压缩文件或文件夹
        /// </summary>
        /// <param name="sourceFilePath">待压缩的文件或文件夹路径</param>
        /// <param name="destinationZipFilePath">打包结果的zip文件路径（类似 D:\WorkSpace\a.zip）,全路径包括文件名和.zip扩展名</param>
        public static void CreateZip(string sourceFilePath, string destinationZipFilePath)
        {
            if (sourceFilePath[sourceFilePath.Length - 1] != Path.DirectorySeparatorChar)
                sourceFilePath += Path.DirectorySeparatorChar;
            ZipOutputStream zipStream = new ZipOutputStream(File.Create(destinationZipFilePath));
            zipStream.SetLevel(6);  // 压缩级别 0-9
            CreateZipFiles(sourceFilePath, zipStream);
            zipStream.Finish();
            zipStream.Close();
        }
        /// <summary>
        /// 递归压缩文件或文件夹
        /// </summary>
        /// <param name="sourceFilePath">待压缩的文件或文件夹路径</param>
        /// <param name="zipStream"></param>
        /// <param name="parentPath">文件或文件夹的父级路径</param>
        private static void CreateZipFiles(string sourceFilePath, ZipOutputStream zipStream,string parentPath = "")
        {
            Crc32 crc = new Crc32();
            char separ = Path.DirectorySeparatorChar;
            string[] filesArray = Directory.GetFileSystemEntries(sourceFilePath);
            foreach (string file in filesArray)
            {
                if (Directory.Exists(file))                     //如果当前是文件夹，递归
                {
                    string path = parentPath == "" ? "" : parentPath + separ;
                    string parent = path + file.Substring(file.LastIndexOf("\\") + 1);
                    CreateZipFiles(file, zipStream, parent);
                }
                else                                            //如果是文件，开始压缩
                {
                    FileStream fileStream = File.OpenRead(file);
                    byte[] buffer = new byte[fileStream.Length];
                    fileStream.Read(buffer, 0, buffer.Length);
                    string path = parentPath == "" ? "" : parentPath + separ;
                    string tempFile = path + file.Substring(file.LastIndexOf("\\") + 1);
                    ZipEntry entry = new ZipEntry(tempFile);
                    entry.DateTime = DateTime.Now;
                    entry.Size = fileStream.Length;
                    fileStream.Close();
                    crc.Reset();
                    crc.Update(buffer);
                    entry.Crc = crc.Value;
                    zipStream.PutNextEntry(entry);
                    zipStream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}