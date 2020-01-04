using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Data.SQLite.Linq;

namespace ms_ui
{
    public enum IconType
    {
        Dir, Music, Video, Text, Application, Picture, Unknow
    }
    public class VFileBase
    {
        public string Name { get; set; }
        public Guid id { get; set; }

        public virtual IconType IconType { get; set; }
        protected bool _marked=false;
        public bool Marked
        {
            get{ return _marked;  }
            set{ _marked = value;   }
        }
        public string path { get; set; }
        public Guid parentId { get; set; }
        public DateTime ModifiedTime { get; set; }
    }
    public class VFile : VFileBase
    {
        //public string Name { get; set; }
        public long Size { get; set; }
        //public int id;
        //bool _marked;

        public override IconType IconType
        {
            get
            {
                string ext = Name.Substring(Name.LastIndexOf('.') + 1);
                switch (ext.ToLower())
                {
                    case "mp3": return IconType.Music;
                    case "exe": return IconType.Application;
                    case "mp4": return IconType.Video;
                    case "txt": return IconType.Text;
                    default:
                        return IconType.Unknow;
                }
            }
        }
        //public bool Marked
        //{
        //    get
        //    {
        //        //if (partFile == null)
        //        //    return MainWindow.markedFaceFileDict[-1].Contains(id);
        //        //else return MainWindow.markedFaceFileDict.ContainsKey(id);
        //        return _marked;
        //    }
        //    set
        //    {
        //        _marked = value;
        //
        //    }
        //}
        public object ThisObject { get => this; }
        public Dictionary<int, int> partFile;
    }
    public class VDirectory : VFileBase, INotifyPropertyChanged
    {
        //public string Name { get; set; }
        //public int id;
        public bool AccessedSubDir = false;
        protected ObservableCollection<VDirectory> _subDirs;
        protected ObservableCollection<VFile> _files;

        public ObservableCollection<VDirectory> SubDirs
        {
            get => _subDirs;
            set { _subDirs = value; OnPropertyChanged("SubDirs"); }
        }
        public ObservableCollection<VFile> Files
        {
            get => _files;
            set { _files = value; OnPropertyChanged("Files"); }
        }
        public IEnumerable<VFileBase> Children
        {
            get
            {
                foreach (var x in SubDirs)
                    yield return x;
                foreach (var x in Files)
                    yield return x;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }
        //public DateTime ModifiedTime { get; set; }
        public override IconType IconType { get => IconType.Dir; }
    }

    public class PartViewStream : Stream
    {
        protected static int block_copy_bufSize = 16 * 1024;
        protected Stream _stm;
        //protected long? _from;
        //protected long? _to;
        protected long x_from;
        protected long x_to;
        protected long _pos;
        //log4net.ILog logger;
        public PartViewStream(string filepath)
        {
            _stm = File.Open(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            SetRange(null, null);
            //logger = log4net.LogManager.GetLogger("aa");
        }
        public PartViewStream(Stream stm)
        {
            _stm = stm;
            SetRange(null, null);
            //logger = log4net.LogManager.GetLogger("aa");
        }
        public void BlockCopyTo(Stream stm)
        {
            byte[] buf = new byte[block_copy_bufSize];
            int rest = (int)(Length % block_copy_bufSize);
            long fullBlock = Length - rest ;
            for (int i = 0; i < fullBlock; i+= block_copy_bufSize)
            {
                Read(buf);//, 0, block_copy_bufSize);
                stm.Write(buf);//, 0, block_copy_bufSize);
            }

            if( rest != 0)
            {
                //int offset = (int)(Length - rest);
                Read(buf,0, rest);
                stm.Write(buf, 0, rest);
            }
        }        
        public static void StreamBlockCopy(Stream srcStm,Stream dstStm)
        {
            byte[] buf = new byte[block_copy_bufSize];
            int rest = (int)(srcStm.Length % block_copy_bufSize);
            long fullBlock = srcStm.Length - rest ;
            for (int i = 0; i < fullBlock; i+= block_copy_bufSize)
            {
                srcStm.Read(buf);//, 0, block_copy_bufSize);
                dstStm.Write(buf);//, 0, block_copy_bufSize);
            }
            if( rest != 0)
            {
                //int offset = (int)(Length - rest);
                srcStm.Read(buf,0, rest);
                dstStm.Write(buf, 0, rest);
            }
        }        
        public static Task StreamBlockCopyAsync(Stream srcStm,Stream dstStm)
        {
            return Task.Run(() => StreamBlockCopy(srcStm, dstStm));
        }
        public (long from, long to) Range
        {
            get
            {
                return new ValueTuple<long, long>(x_from, x_to);
            }
            set
            {
                if (!SetRange(value.from, value.to))
                    throw new ArgumentOutOfRangeException();
            }
        }
        public bool SetRange(long? from, long? to)
        {
            var j_from = from.GetValueOrDefault();
            var j_to = (to == null ? _stm.Length : to.Value);
            if (j_from < 0 || j_to < 0)
                return false;
            if (j_from > j_to)
                return false;
            //_from = from; _to = to;
            x_from = j_from;
            x_to = j_to;
            _pos = 0;
            //_stm.Position = x_from;
            return true;
        }
        //protected long getRangeTo()
        //{
        //    return (_to == null ? _fs.Length : _to.Value);
        //}
        protected bool isInRange(long pos)
        {
            return pos >= 0 && pos <= x_to - x_from;
        }
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => x_to - x_from;

        public override long Position
        {
            get => _pos;// _stm.Position - x_from;
            set
            {

                if (!isInRange(value))
                    throw new ArgumentOutOfRangeException();
                _pos = value;//_stm.Position = x_from + value;
            }
        }
        public override void Flush()
        {
            _stm.Flush();
        }
        //
        // SeekOrigin.Current
        // offset 指的不是在流中的偏移量，而是写入buffer 中的偏移量
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException();
            if (_pos == x_to)
                return 0;
            if (x_from + _pos + count > x_to)
            {
                count = (int)(x_to - (_pos + x_from) );
            }
            //Console.WriteLine($"offset:{offset},count:{count}");
            //logger.Info($"_fs.Position:{_fs.Position},pf.Pos:{Position}");

            lock(_stm){
                if (_stm.Position != _pos + x_from)
                    _stm.Position = _pos + x_from;
                int cnt = _stm.Read(buffer, offset, count);
                _pos += cnt;
                return cnt;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long x_offset = 0;
            //Console.WriteLine($"seek offset:{0},count:{1}", offset, origin);
            switch (origin)
            {
                case SeekOrigin.Begin: x_offset = offset + x_from; break;
                case SeekOrigin.Current: x_offset = _pos + offset; break;
                case SeekOrigin.End: x_offset = x_to - offset; break;
                default: break;
            }
            if (x_from <= x_offset && x_offset <= x_to)
            {
                _pos = x_offset -x_from;
                return _pos;
            }
            else throw new ArgumentOutOfRangeException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException();
            if (_pos + count > x_to)
                throw new ArgumentOutOfRangeException();
            //Console.WriteLine($"offset:{offset},count:{count}");
            lock (_stm)
            {
                if (_stm.Position != _pos + x_from)
                    _stm.Position = _pos + x_from;
                _stm.Write(buffer, offset, count);
            }

        }
        protected override void Dispose(bool disposing)
        {
            if (_stm != null)
                _stm.Dispose();
        }

    }
    class QETag
    {
        private const int CHUNK_SIZE = 1 << 22;

        public static byte[] sha1(byte[] data)
        {
            return System.Security.Cryptography.SHA1.Create().ComputeHash(data);
        }

        public static String urlSafeBase64Encode(byte[] data)
        {
            String encodedString = Convert.ToBase64String(data);
            encodedString = encodedString.Replace('+', '-').Replace('/', '_');
            return encodedString;
        }
        public static String calcETag(String path)
        {
            if (!System.IO.File.Exists(path))
            {
                return null;
            }
            String etag = "";
            System.IO.FileStream fs;
            fs = System.IO.File.OpenRead(path);
            long fileLength = fs.Length;
            if (fileLength <= CHUNK_SIZE)
            {
                byte[] fileData = new byte[(int)fileLength];
                fs.Read(fileData, 0, (int)fileLength);
                byte[] sha1Data = sha1(fileData);
                int sha1DataLen = sha1Data.Length;
                byte[] hashData = new byte[sha1DataLen + 1];

                System.Array.Copy(sha1Data, 0, hashData, 1, sha1DataLen);
                hashData[0] = 0x16;
                etag = urlSafeBase64Encode(hashData);
            }
            else
            {
                int chunkCount = (int)(fileLength / CHUNK_SIZE);
                if (fileLength % CHUNK_SIZE != 0)
                {
                    chunkCount += 1;
                }
                byte[] allSha1Data = new byte[0];
                for (int i = 0; i < chunkCount; i++)
                {
                    byte[] chunkData = new byte[CHUNK_SIZE];
                    int bytesReadLen = fs.Read(chunkData, 0, CHUNK_SIZE);
                    byte[] bytesRead = new byte[bytesReadLen];
                    System.Array.Copy(chunkData, 0, bytesRead, 0, bytesReadLen);
                    byte[] chunkDataSha1 = sha1(bytesRead);
                    byte[] newAllSha1Data = new byte[chunkDataSha1.Length
                            + allSha1Data.Length];
                    System.Array.Copy(allSha1Data, 0, newAllSha1Data, 0,
                            allSha1Data.Length);
                    System.Array.Copy(chunkDataSha1, 0, newAllSha1Data,
                            allSha1Data.Length, chunkDataSha1.Length);
                    allSha1Data = newAllSha1Data;
                }
                byte[] allSha1DataSha1 = sha1(allSha1Data);
                byte[] hashData = new byte[allSha1DataSha1.Length + 1];
                System.Array.Copy(allSha1DataSha1, 0, hashData, 1,
                        allSha1DataSha1.Length);
                hashData[0] = (byte)0x96;
                etag = urlSafeBase64Encode(hashData);
            }
            fs.Close();
            return etag;

        }
    }
    class Common
    {
        public static log4net.ILog ilog;
        public static void configLogger()
        {
            var repo = log4net.LogManager.CreateRepository("me");
            var hier = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository("me");
            var app = new log4net.Appender.FileAppender();
            app.File = "a:/sixpan/info.log";
            app.AppendToFile = true;

            var lay = new log4net.Layout.PatternLayout("%date - %message%newline"); ;
            app.Layout = lay;
            app.ActivateOptions();
            lay.ActivateOptions();
            hier.Root.AddAppender(app);
            hier.Configured = true;
            ilog = log4net.LogManager.GetLogger("me", "hah_妈蛋，好辛苦");// typeof(MainWindow));
            ilog.Info("config ok!");
        }
        public static T firstCol<T>(IEnumerable<T> e)
        {
            var en = e.GetEnumerator();
            en.MoveNext();
            return en.Current;
        }
        public static void makeSureDirectoryExist(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                var parentDir = Directory.GetParent(dirPath);
                if (!parentDir.Exists)
                    makeSureDirectoryExist(parentDir.FullName);
                else
                {
                    if (dirPath.Length <= 3)
                        return;
                    Directory.CreateDirectory(dirPath);
                    //parentDir.CreateSubdirectory(dirPath);
                }
                    

            }
        }
    }
    internal class UrlSafeBase64
    {
        /// <summary>
        /// 字符串 URL 安全 Base64 编码
        /// </summary>
        /// <param name="text">源字符串</param>
        /// <returns>编码</returns>
        public static string encode(string text)
        {
            return encode(Encoding.UTF8.GetBytes(text));
        }

        /// <summary>
        /// URL 安全的 Base64 编码
        /// </summary>
        /// <param name="data">需要编码的字节数据</param>
        /// <returns>编码</returns>
        public static string encode(byte[] data)
        {
            return Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// bucket:key 编码
        /// == Python SDK: def entry(bucket, key)
        /// </summary>
        /// <param name="bucket">空间名称</param>
        /// <param name="key">文件 Key</param>
        /// <returns>编码</returns>
        public static string encode(string bucket, string key)
        {
            return encode(bucket + ":" + key);
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="text">待解码的字符串</param>
        /// <returns>已解码字节</returns>
        public static byte[] UrlSafeBase64DecodeByte(string text)
        {
            return Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/'));
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="text">待解码的字符串</param>
        /// <returns>已解码字符串</returns>
        public static string UrlSafeBase64Decode(string text)
        {
            return Encoding.UTF8.GetString(UrlSafeBase64DecodeByte(text));
        }
    }
    class MD5Encrypt
    {
        public static string encode(string strText)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(strText));
            StringBuilder sb = new StringBuilder();
            foreach (var c in result)
                sb.AppendFormat("{0:x2}", c);
            return sb.ToString();
        }
    }


    //[Table("Filesytem")]
    //public class Filesytem
    //{
    //    //[Column( TypeName = "pk")]
    //    public UInt32 pk;
    //    //[Column(TypeName = "id")]
    //    public Guid id;
    //    public bool isdir;
    //    public UInt64 size;
    //    public DateTime time;
    //    public Guid paretn;
    //    public string name;
    //    public string path;
    //}
}
