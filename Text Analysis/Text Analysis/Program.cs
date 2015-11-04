using Entities;
using SoufunLab.Framework;
using SoufunLab.Framework.Data;
using SoufunLab.Framework.Diagnostics;
using SoufunLab.Framework.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Text_Analysis
{
    class Program
    {

        /// <summary>
        /// 获取文件目录中指定的文件信息集合
        /// </summary>
        /// <param name="folder">目录</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>返回文件信息集合</returns>
        static List<FileDataInfos> GetDataFiles(string folder, string extension=".txt")
        {
            List<FileDataInfos> fileinfos = new List<FileDataInfos>();
            string directorys = folder;
            if (Directory.Exists(directorys))
            {
                string[] allfiles = Directory.GetFiles(directorys);
                for (int j = 0; j < allfiles.Length; j++)
                {
                    string filePath = allfiles[j];
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string fileExtension = Path.GetExtension(filePath);
                    if (fileExtension.ToLower() == extension.Trim().ToLower())
                    {
                        fileinfos.Add(new FileDataInfos() { Name = fileName, Path = filePath });
                    }
                }
            }
            return fileinfos;
        }


        static void Main(string[] args)
        {
            //检索的日志目录
            string dirPath = "C:\\LogPath";
            //每行的内容
            string line = string.Empty;
            string time = string.Empty;
            //每行提取的字段
            string url = string.Empty;
            string service = string.Empty;

            foreach (FileDataInfos dataFile in GetDataFiles(dirPath))
            {
                Console.WriteLine("正在扫描日志：" + dataFile.Name);
                SlLog.Write(SlTraceType.Log, DateTime.Now.ToString() + " 正在扫描日志：" + dataFile.Name);
                using (StreamReader streamReader = SlFile.GetStreamReader(dataFile.Path))
                {
                    while (!streamReader.EndOfStream)
                    {
                        time = DateTime.Now.ToString();
                        line = streamReader.ReadLine();
                        try
                        {
                            url = line.Substring(line.IndexOf("/interface/") + 11, line.IndexOf("?service") - line.IndexOf("/interface/") - 11).Trim().Replace("/", "");
                            service = line.Substring(line.IndexOf("?service") + 9, line.IndexOf("&") - line.IndexOf("?service") - 9).Trim();
                            Console.WriteLine("正在读取数据：" + url + "     " + service);

                            #region  处理从行里提取的字段
                            #endregion

                            Console.WriteLine(time + "  " + service + "    " + url);
                        }

                        catch (Exception ex)
                        {

                        }

                    }

                }

            }
        }

    }
}
