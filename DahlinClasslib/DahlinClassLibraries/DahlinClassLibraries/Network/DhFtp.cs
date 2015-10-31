using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DahlinClassLibraries.Network
{

    #region 文件信息结构

    /// <summary>
    /// 文件属性结构
    /// </summary>
    public struct FileStruct
    {
        public string Flags;
        public string Owner;
        public string Group;
        public bool IsDirectory;
        public DateTime CreateTime;
        public string Name;
    }

    /// <summary>
    /// 文件类别
    /// </summary>
    public enum FileListStyle
    {
        UnixStyle,
        WindowsStyle,
        Unknown
    }

    #endregion

    /// <summary>
    /// FTP操作类
    /// </summary>
    public class DhFtp
    {
        /// <summary> 
        /// ftp用户名，匿名为""
        /// </summary> 
        private string ftpUser;

        /// <summary> 
        /// ftp用户密码，匿名为"" 
        /// </summary> 
        private string ftpPassWord;

        /// <summary> 
        ///通过用户名，密码连接到FTP服务器 
        /// </summary> 
        /// <param name="ftpUser">ftp用户名，匿名为“”</param> 
        /// <param name="ftpPassWord">ftp登陆密码，匿名为“”</param> 
        public DhFtp(string ftpUser, string ftpPassWord)
        {
            this.ftpUser = ftpUser;
            this.ftpPassWord = ftpPassWord;
        }

        public DhFtp()
        {
            this.ftpUser = "";
            this.ftpPassWord = "";
        }
        /// <summary>
        /// 删除FTP服务器文件夹（包括文件）
        /// </summary>
        /// <param name="ftpPath"></param>
        public void DeleteAllFile(string ftpPath)
        {

            try
            {
                List<FileStruct> files = ListFilesAndDirectories(ftpPath);
                foreach (FileStruct f in files)
                {
                    if (f.IsDirectory) //文件夹，递归查询 
                    {
                        DeleteAllFile(ftpPath + "/" + f.Name);
                    }
                    else
                    {
                        DeleteFile(ftpPath + "/" + f.Name);
                    }
                }
                if (ftpPath.Split('/').Length > 4)
                {
                    DeleteDirectory(ftpPath);
                }//是否是根目录

            }
            catch (System.Net.WebException we)
            {
                DeleteDirectory(ftpPath);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="currenFilePath"></param>
        private void DeleteFile(string currenFilePath)
        {
            try
            {
                //实例化FTP连接
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(new Uri(currenFilePath));
                request.Credentials = new NetworkCredential(ftpUser, ftpPassWord);
                request.UsePassive = false;
                //指定FTP操作类型为创建目录
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                //获取FTP服务器的响应
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="currentDirectoryPath"></param>
        private void DeleteDirectory(string currentDirectoryPath)
        {
            try
            {
                //实例化FTP连接
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(new Uri(currentDirectoryPath));
                request.Credentials = new NetworkCredential(ftpUser, ftpPassWord);
                request.UsePassive = false;
                //指定FTP操作类型为创建目录
                request.Method = WebRequestMethods.Ftp.RemoveDirectory;
                //获取FTP服务器的响应
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary> 
        /// 上传文件到Ftp服务器 
        /// </summary> 
        /// <param name="uri">把上传的文件保存为ftp服务器文件的uri,如"ftp://192.168.1.1/test.txt"</param> 
        /// <param name="upLoadFile">要上传的本地的文件路径，如D:\test.txt</param>
        /// <param name="usePassive">主动模式/被动模式</param>
        public void UpLoadFile(string UpLoadUri, string upLoadFile,bool usePassive=false)
        {
            Stream requestStream = null;
            FileStream fileStream = null;
            FtpWebResponse uploadResponse = null;

            try
            {
                Uri uri = new Uri(UpLoadUri);
                FtpWebRequest uploadRequest = (FtpWebRequest)WebRequest.Create(uri);
                uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;
                uploadRequest.Credentials = new NetworkCredential(ftpUser, ftpPassWord);
                uploadRequest.UsePassive = usePassive;
                uploadRequest.Timeout = 900000;
                uploadRequest.ReadWriteTimeout = 900000;
                requestStream = uploadRequest.GetRequestStream();
                fileStream = File.Open(upLoadFile, FileMode.Open);
                byte[] buffer = new byte[2048];
                int bytesRead;
                while (true)
                {
                    bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    requestStream.Write(buffer, 0, bytesRead);
                }
                requestStream.Close();

                uploadResponse = (FtpWebResponse)uploadRequest.GetResponse();


            }
            catch (Exception ex)
            {
                throw new Exception("上传文件到ftp服务器出错，文件名：" + upLoadFile + "异常信息：" + ex.ToString());
            }
            finally
            {
                if (uploadResponse != null)
                    uploadResponse.Close();
                if (fileStream != null)
                    fileStream.Close();
                if (requestStream != null)
                    requestStream.Close();
            }
        }

        /// <summary>
        /// 上传整个文件夹
        /// </summary>
        /// <param name="ftpUri">上传到FTP的路径</param>
        /// <param name="upLoadFile">要上传的本地文件夹目录</param>
        public void UploadDirectory(string ftpPath, string upLoadFile)
        {
            try
            {
                string dirName = upLoadFile.Substring(upLoadFile.LastIndexOf(@"\") + 1);//文件夹名称
                if (!CheckDirectoryExist(ftpPath, dirName))
                {
                    MakeDir(ftpPath, dirName);
                }
                List<List<string>> infos = GetDirDetails(upLoadFile); //获取当前目录下的所有文件和文件夹 
                //先上传文件
                for (int i = 0; i < infos[0].Count; i++)
                {
                    UpLoadFile(ftpPath + "/" + dirName + "/" + infos[0][i], upLoadFile + @"\" + infos[0][i]);
                }
                //再处理文件夹
                for (int i = 0; i < infos[1].Count; i++)
                {
                    UploadDirectory(ftpPath + @"/" + dirName, upLoadFile + @"\" + infos[1][i]);
                }
            }
            catch (System.Net.WebException we)
            {
                UploadDirectory(ftpPath, upLoadFile);
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }
        /// <summary>
        /// 获取目录下的详细信息
        /// </summary>
        /// <param name="localDir">本机目录</param>
        /// <returns></returns>
        private static List<List<string>> GetDirDetails(string localDir)
        {
            List<List<string>> infos = new List<List<string>>();
            try
            {
                infos.Add(Directory.GetFiles(localDir).ToList()); //获取当前目录的文件
                infos.Add(Directory.GetDirectories(localDir).ToList()); //获取当前目录的目录
                for (int i = 0; i < infos[0].Count; i++)
                {
                    int index = infos[0][i].LastIndexOf(@"\");
                    infos[0][i] = infos[0][i].Substring(index + 1);
                }
                for (int i = 0; i < infos[1].Count; i++)
                {
                    int index = infos[1][i].LastIndexOf(@"\");
                    infos[1][i] = infos[1][i].Substring(index + 1);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return infos;
        }

        /// <summary>
        /// 新建目录
        /// </summary>
        /// <param name="ftpPath">FTP目录路径 例如：ftp://222.76.208.41/xmjsapproval</param>
        /// <param name="dirName">需创建的文件夹名称</param>
        public void MakeDir(string ftpPath, string dirName)
        {
            try
            {
                //实例化FTP连接
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpPath + @"/" + dirName));
                request.Credentials = new NetworkCredential(ftpUser, ftpPassWord);
                //指定FTP操作类型为创建目录
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                //获取FTP服务器的响应
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary> 
        /// 从ftp下载文件到本地服务器 
        /// </summary> 
        /// <param name="downloadUrl">要下载的ftp文件路径，如ftp://192.168.1.1/test.txt</param> 
        /// <param name="saveFileUrl">本地保存文件的路径，如(@"D:\test.txt"</param> 
        public void DownLoadFile(string downloadUrl, string saveFileUrl)
        {
            Stream responseStream = null;
            FileStream fileStream = null;
            StreamReader reader = null;

            try
            {
                // string downloadUrl = "ftp://192.168.1.1/test.txt"; 

                FtpWebRequest downloadRequest = (FtpWebRequest)WebRequest.Create(downloadUrl);
                downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;

                //string ftpUser = "dada"; 
                //string ftpPassWord = "123456"; 
                downloadRequest.Credentials = new NetworkCredential(ftpUser, ftpPassWord);
                downloadRequest.UsePassive = false;
                downloadRequest.ReadWriteTimeout = 900000;
                downloadRequest.Timeout = 900000;
                downloadRequest.KeepAlive = true;
                FtpWebResponse downloadResponse = (FtpWebResponse)downloadRequest.GetResponse();
                responseStream = downloadResponse.GetResponseStream();
                fileStream = File.Create(saveFileUrl);
                byte[] buffer = new byte[1024];
                int bytesRead;
                while (true)
                {
                    bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    fileStream.Write(buffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {

                throw new Exception("从ftp服务器下载文件出错，文件名：" + downloadUrl + "异常信息：" + ex.ToString());
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                if (responseStream != null)
                {
                    responseStream.Close();
                }
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }


        /// <summary> 
        /// 从FTP下载文件到本地服务器,支持断点下载 
        /// </summary> 
        /// <param name="ftpUri">ftp文件路径，如"ftp://localhost/test.txt"</param> 
        /// <param name="saveFile">保存文件的路径，如C:\\test.txt</param> 
        public void BreakPointDownLoadFile(string ftpUri, string saveFile)
        {
            System.IO.FileStream fs = null;
            System.Net.FtpWebResponse ftpRes = null;
            System.IO.Stream resStrm = null;
            try
            {
                //下载文件的URI 
                Uri u = new Uri(ftpUri);
                //设定下载文件的保存路径 
                string downFile = saveFile;

                //FtpWebRequest的作成 
                System.Net.FtpWebRequest ftpReq = (System.Net.FtpWebRequest)
                 System.Net.WebRequest.Create(u);
                //设定用户名和密码 
                ftpReq.Credentials = new System.Net.NetworkCredential(ftpUser, ftpPassWord);
                //MethodにWebRequestMethods.Ftp.DownloadFile("RETR")设定 
                ftpReq.Method = System.Net.WebRequestMethods.Ftp.DownloadFile;
                //要求终了后关闭连接 
                ftpReq.KeepAlive = false;
                //使用ASCII方式传送 
                ftpReq.UseBinary = false;
                //设定PASSIVE方式无效 
                ftpReq.UsePassive = false;

                //判断是否继续下载 
                //继续写入下载文件的FileStream 

                if (System.IO.File.Exists(downFile))
                {
                    //继续下载 
                    ftpReq.ContentOffset = (new System.IO.FileInfo(downFile)).Length;
                    fs = new System.IO.FileStream(
                       downFile, System.IO.FileMode.Append, System.IO.FileAccess.Write);
                }
                else
                {
                    //一般下载 
                    fs = new System.IO.FileStream(
                        downFile, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                }

                //取得FtpWebResponse 
                ftpRes = (System.Net.FtpWebResponse)ftpReq.GetResponse();
                //为了下载文件取得Stream 
                resStrm = ftpRes.GetResponseStream();
                //写入下载的数据 
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int readSize = resStrm.Read(buffer, 0, buffer.Length);
                    if (readSize == 0)
                        break;
                    fs.Write(buffer, 0, readSize);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("从ftp服务器下载文件出错，文件名：" + ftpUri + "异常信息：" + ex.ToString());
            }
            finally
            {
                fs.Close();
                resStrm.Close();
                ftpRes.Close();
            }
        }
        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="ftpPath">要检查的目录的上一级目录</param>
        /// <param name="dirName">要检查的目录名</param>
        /// <returns>存在返回true，否则false</returns>
        public bool CheckDirectoryExist(string ftpPath, string dirName)
        {
            bool result = false;
            try
            {
                List<FileStruct> files = ListFilesAndDirectories(ftpPath);
                foreach (FileStruct f in files)
                {
                    if (f.IsDirectory && f.Name == dirName)
                    {
                        result = true;
                        break;
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }


        #region 从FTP上下载整个文件夹，包括文件夹下的文件和文件夹

        /// <summary> 
        /// 列出FTP服务器上面当前目录的所有文件和目录 
        /// </summary> 
        /// <param name="ftpUri">FTP目录</param> 
        /// <returns></returns> 
        public List<FileStruct> ListFilesAndDirectories(string ftpUri)
        {
            WebResponse webresp = null;
            StreamReader ftpFileListReader = null;
            FtpWebRequest ftpRequest = null;
            try
            {
                ftpRequest = (FtpWebRequest)WebRequest.Create(new Uri(ftpUri));
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                ftpRequest.Credentials = new NetworkCredential(ftpUser, ftpPassWord);
                ftpRequest.UsePassive = false;//链接模式
                ftpRequest.Timeout = 900000;
                ftpRequest.ReadWriteTimeout = 900000;
                ftpRequest.KeepAlive = true;
                webresp = ftpRequest.GetResponse();
                ftpFileListReader = new StreamReader(webresp.GetResponseStream(), Encoding.Default);
            }
            catch (Exception ex)
            {
                throw ex;
                //throw new Exception("获取文件列表出错，错误信息如下：" + ex.ToString());
            }
            string Datastring = ftpFileListReader.ReadToEnd();
            return GetList(Datastring);

        }

        /// <summary> 
        /// 列出FTP目录下的所有文件 
        /// </summary> 
        /// <param name="ftpUri">FTP目录</param> 
        /// <returns></returns> 
        public List<FileStruct> ListFiles(string ftpUri)
        {
            List<FileStruct> listAll = ListFilesAndDirectories(ftpUri);
            List<FileStruct> listFile = new List<FileStruct>();
            foreach (FileStruct file in listAll)
            {
                if (!file.IsDirectory)
                {
                    listFile.Add(file);
                }
            }
            return listFile;
        }


        /// <summary> 
        /// 列出FTP目录下的所有目录 
        /// </summary> 
        /// <param name="ftpUri">FRTP目录</param> 
        /// <returns>目录列表</returns> 
        public List<FileStruct> ListDirectories(string ftpUri)
        {
            List<FileStruct> listAll = ListFilesAndDirectories(ftpUri);
            List<FileStruct> listDirectory = new List<FileStruct>();
            foreach (FileStruct file in listAll)
            {
                if (file.IsDirectory)
                {
                    listDirectory.Add(file);
                }
            }
            return listDirectory;
        }

        /// <summary> 
        /// 获得文件和目录列表 
        /// </summary> 
        /// <param name="datastring">FTP返回的列表字符信息</param> 
        private List<FileStruct> GetList(string datastring)
        {
            List<FileStruct> myListArray = new List<FileStruct>();
            string[] dataRecords = datastring.Split('\n');
            FileListStyle _directoryListStyle = GuessFileListStyle(dataRecords);
            foreach (string s in dataRecords)
            {
                if (s.IndexOf("total") != 0)
                {
                    if (_directoryListStyle != FileListStyle.Unknown && s != "")
                    {
                        FileStruct f = new FileStruct();
                        f.Name = "..";
                        switch (_directoryListStyle)
                        {
                            case FileListStyle.UnixStyle:
                                f = ParseFileStructFromUnixStyleRecord(s);
                                break;
                            case FileListStyle.WindowsStyle:
                                f = ParseFileStructFromWindowsStyleRecord(s);
                                break;
                        }
                        if (!(f.Name == "." || f.Name == ".."))
                        {
                            myListArray.Add(f);
                        }
                    }
                }
            }
            return myListArray;
        }
        /// <summary> 
        /// 从Unix@ 
        /// </summary> 
        /// <param name="Record">文件信息</param> 
        private FileStruct ParseFileStructFromUnixStyleRecord(string Record)
        {
            FileStruct f = new FileStruct();
            string processstr = Record.Trim();
            f.Flags = processstr.Substring(0, 10);
            f.IsDirectory = (f.Flags[0] == 'd');
            processstr = (processstr.Substring(11)).Trim();
            _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);   //跳过一部分 
            f.Owner = _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
            f.Group = _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
            _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);   //跳过一部分 
            string yearOrTime = processstr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[2];
            if (yearOrTime.IndexOf(":") >= 0)  //time 
            {
                processstr = processstr.Replace(yearOrTime, DateTime.Now.Year.ToString());
            }
            f.CreateTime = DateTime.Parse(_cutSubstringFromStringWithTrim(ref processstr, ' ', 8));
            f.Name = processstr;   //最后就是名称 
            return f;
        }

        /// <summary> 
        /// 从Windows格式中返回文件信息 
        /// </summary> 
        /// <param name="Record">文件信息</param> 
        private FileStruct ParseFileStructFromWindowsStyleRecord(string Record)
        {
            FileStruct f = new FileStruct();
            string processstr = Record.Trim();
            string dateStr = processstr.Substring(0, 8);
            processstr = (processstr.Substring(8, processstr.Length - 8)).Trim();
            string timeStr = processstr.Substring(0, 7);
            processstr = (processstr.Substring(7, processstr.Length - 7)).Trim();
            DateTimeFormatInfo myDTFI = new CultureInfo("en-US", true).DateTimeFormat;
            myDTFI.ShortTimePattern = "t";
            f.CreateTime = DateTime.Parse(dateStr + " " + timeStr, myDTFI);
            if (processstr.Substring(0, 5) == "<DIR>")
            {
                f.IsDirectory = true;
                processstr = (processstr.Substring(5, processstr.Length - 5)).Trim();
            }
            else
            {
                string[] strs = processstr.Split(new char[] { ' ' }, 2);// StringSplitOptions.RemoveEmptyEntries);   // true); 
                processstr = strs[1];
                f.IsDirectory = false;
            }
            f.Name = processstr;
            return f;
        }
        /// <summary> 
        /// 按照一定的规则进行字符串截取 
        /// </summary> 
        /// <param name="s">截取的字符串</param> 
        /// <param name="c">查找的字符</param> 
        /// <param name="startIndex">查找的位置</param> 
        private string _cutSubstringFromStringWithTrim(ref string s, char c, int startIndex)
        {
            int pos1 = s.IndexOf(c, startIndex);
            string retString = s.Substring(0, pos1);
            s = (s.Substring(pos1)).Trim();
            return retString;
        }
        /// <summary> 
        /// 判断文件列表的方式Window方式还是Unix方式 
        /// </summary> 
        /// <param name="recordList">文件信息列表</param> 
        private FileListStyle GuessFileListStyle(string[] recordList)
        {
            foreach (string s in recordList)
            {
                if (s.Length > 10
                 && Regex.IsMatch(s.Substring(0, 10), "(-|d)(-|r)(-|w)(-|x)(-|r)(-|w)(-|x)(-|r)(-|w)(-|x)"))
                {
                    return FileListStyle.UnixStyle;
                }
                else if (s.Length > 8
                 && Regex.IsMatch(s.Substring(0, 8), "[0-9][0-9]-[0-9][0-9]-[0-9][0-9]"))
                {
                    return FileListStyle.WindowsStyle;
                }
            }
            return FileListStyle.Unknown;
        }

        /// <summary>   
        /// 从FTP下载整个文件夹   
        /// </summary>   
        /// <param name="ftpDir">FTP文件夹路径</param>   
        /// <param name="saveDir">保存的本地文件夹路径</param>   
        public void DownFtpDir(string ftpDir, string saveDir)
        {
            try
            {
                List<FileStruct> files = ListFilesAndDirectories(ftpDir);
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                foreach (FileStruct f in files)
                {
                    if (f.IsDirectory) //文件夹，递归查询 
                    {
                        DownFtpDir(ftpDir + "/" + f.Name, saveDir + "\\" + f.Name);
                    }
                    else //文件，直接下载 
                    {
                        BreakPointDownLoadFile(ftpDir + "/" + f.Name, saveDir + "\\" + f.Name);
                        //DownLoadFile(ftpDir + "/" + f.Name, saveDir + "\\" + f.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}
