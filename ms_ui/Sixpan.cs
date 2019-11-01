using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Collections.ObjectModel;
using System.Linq;
//using System.Collections.Generic

namespace ms_ui
{
    using JObject = Newtonsoft.Json.Linq.JObject;
    using JToken = Newtonsoft.Json.Linq.JToken;
    using Enumerable = System.Linq.Enumerable;
    class Sixpan
    {
        

        string recentToken;
        string apihost = "https://api.6pan.cn";
        const long block_size = 4 * 1024 * 1024;
        // /bput 上传chunk总是会在第二次(也就是block的第三片)的时候出错,抛出408错误，不知道为什么，
        // 现在的临时解决的办法是1个block只上传1个chunk
        const long chunk_size = 1 * 1024 * 1024;
        HttpClient _hc = new HttpClient();

        System.Net.CookieContainer cookieBag;
        bool isLogin=false;
        public bool IsLogged() => isLogin;
        public string getToken(HttpResponseMessage resp)
        {
            return Common.firstCol(resp.Headers.GetValues("qingzhen-token"));
        }
        public Task<HttpResponseMessage> post(string path,object jsonData)
        {
            System.Diagnostics.Debug.Assert(path.Substring(0, 2) == "/v");
            var jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);
            StringContent sc = new StringContent(jsonStr, Encoding.UTF8, "application/json");
            var resp =  _hc.PostAsync(apihost + path, sc);
            return resp;

        }
        async Task<string> postStr(string url, Dictionary<string, string> custHead, string str)
        {
            HttpWebRequest req = WebRequest.CreateHttp(url);
            req.Method = "POST";
            foreach (var h in custHead)
                req.Headers.Add(h.Key, h.Value);
            req.ContentType = "text/plain;charset=UTF-8";
            var buf = System.Text.Encoding.UTF8.GetBytes(str);
            req.ContentLength = buf.Length;
            var reqStream = await req.GetRequestStreamAsync();
            await reqStream.WriteAsync(buf);
            reqStream.Close();
            var resp = await req.GetResponseAsync();
            var respStream = resp.GetResponseStream();
            var sr = new StreamReader(respStream);
            var retjson = await sr.ReadToEndAsync();
            sr.Close();
            resp.Close();
            req.Abort();
            return retjson;
        }
        public async Task<bool> login(string account,string pwd)
        {
            var data = new {
                value = account,
                password = MD5Encrypt.encode(pwd)
            };
            var resp = await post("/v2/user/login", data);
            var jsr = await resp.Content.ReadAsStringAsync();
            var json = Newtonsoft.Json.Linq.JObject.Parse(jsr);
            bool suc = json["success"].ToObject<bool>();
            updateToken(resp);
            if (suc)
            {
                Common.ilog.Info($"token:{recentToken}");
                isLogin = true;
            }
            return suc;
        }
        public void updateToken(HttpResponseMessage resp)
        {
            recentToken = getToken(resp);
            _hc.DefaultRequestHeaders.Remove("Qingzhen-Token");
            _hc.DefaultRequestHeaders.Add("Qingzhen-Token", recentToken);
        }
        public void updateToken(HttpWebResponse resp)
        {
            recentToken = resp.Headers.Get("Qingzhen-Token");
            _hc.DefaultRequestHeaders.Remove("Qingzhen-Token");
            _hc.DefaultRequestHeaders.Add("Qingzhen-Token", recentToken);
        }
        public void restoreHeaderToken(string token)
        {
            recentToken = token;
            _hc.DefaultRequestHeaders.Remove("Qingzhen-Token");
            _hc.DefaultRequestHeaders.Add("Qingzhen-Token", recentToken);
        }
        public string getHeaderToken()
        {
            return recentToken;
        }
        /// <summary>
        /// 获取所有设备用户上的ssid
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> getLoggedSsid()
        {
            var data = new object();
            var resp = await post("/v2/user/online", data);
            var jsr = await resp.Content.ReadAsStringAsync();
            var json = Newtonsoft.Json.Linq.JObject.Parse(jsr);
            bool suc = json["success"].ToObject<bool>();
            if (!suc)
                return null;
            var online = json["result"]["online"];
            var ssid = new List<string>();
            foreach( var x in online)
            {
                ssid.Add(x["ssid"].ToString());
            }
            return ssid;
        }
        public async void logoutAll()
        {
            var ls = await getLoggedSsid();
            if(ls.Count > 0)
                await logout(ls);
        }
        /// <summary>
        /// 根据列举的ssid列表退出设备用户
        /// </summary>
        /// <param name="ssid">服务器给当前设备分配的id</param>
        /// <returns></returns>
        public async Task<bool> logout(List<string> ssid)
        {
            var data = new { ssid = ssid };
            var resp = await post("/v2/user/logoutOther", data);
            var jsr = await resp.Content.ReadAsStringAsync();
            var json = Newtonsoft.Json.Linq.JObject.Parse(jsr);
            bool suc = json["success"].ToObject<bool>();
            if (!suc)
                return false;
            return true;
        }
        /// <summary>
        /// 退出当前登录
        /// </summary>
        public async void logout()
        {
            await post("/v2/user/logout", null);
        }
        public void restoreCookies(System.Net.CookieContainer cookies)
        {
            cookieBag = cookies;
            HttpClientHandler hander = new HttpClientHandler()
            {
                CookieContainer = cookieBag,
                AllowAutoRedirect = true,
                UseCookies = true
            };
            _hc = new HttpClient(hander);
            isLogin = true;
        }
        public async Task<Newtonsoft.Json.Linq.JObject> jsonPost(string path,object data)
        {
            var resp = await post(path, data);
            var jsr = await resp.Content.ReadAsStringAsync();
            var json = Newtonsoft.Json.Linq.JObject.Parse(jsr);
            updateToken(resp);
            return json;
        }
        public async Task<bool> testLogin()
        {
            throw new NotImplementedException();
            var data = new { };
            var json = await jsonPost("/v2/abc", data);
            bool suc = json["success"].ToObject<bool>();
            if (!suc)
                return false;
            return true;
        }

        public bool download(int fileid,string filepath)
        {

            return true;
        }
        public async Task<string> mkdir(string dirname,string parentId)
        {
            var data = new {  parent = parentId, name = dirname };
            var json = await jsonPost("/v2/files/createDirectory", data);
            bool suc = json["success"].ToObject<bool>();
            if (!suc)
                return null;
            return json["result"]["identity"].ToString();
        }        
        public async Task<string> mkdir(string dirname)
        {
            var data = new {  path = dirname };
            var json = await jsonPost("/v2/files/createDirectory", data);
            bool suc = json["success"].ToObject<bool>();
            if (!suc)
                return null;
            return json["result"]["identity"].ToString();
        }

        async Task<string> postStream(string url, Dictionary<string, string> heads,Stream stm, long stmLen)
        {
            HttpWebRequest req = WebRequest.CreateHttp(url);
            req.Method = "POST";
            foreach (var h in heads) req.Headers.Add(h.Key, h.Value);
            req.ContentLength = stmLen;
            req.ContentType = "application/octet-stream";
            var reqStream = await req.GetRequestStreamAsync();
            // CopyTo(reqSteam) 使用在这个请求里可能发生异常，故用自己写的BlockCopyTo
            await PartViewStream.StreamBlockCopyAsync(stm, reqStream);
            reqStream.Close();
            HttpWebResponse resp = (HttpWebResponse)await req.GetResponseAsync();
            var respStream = resp.GetResponseStream();
            var sr = new StreamReader(respStream);
            var retjson = await sr.ReadToEndAsync();
            sr.Close();
            resp.Close();
            req.Abort();
            return retjson;
        }
        public async Task upload_streamAsync2(string filehost, System.IO.Stream stream, string uploadToken,string keyname)
        {
            long flength = stream.Length;
            if (flength == 0)
                return;

            void addHeads(HttpWebRequest req, Dictionary<string, string> heads)
            {                
                foreach (var h in heads) req.Headers.Add(h.Key, h.Value);
            }
            /// uuid 赋值给 http 头 uploadBatch，整个文件uuid必须相同
            ///(网上的文档说每一个小片chunk不相同是错误的)
            string uuid = System.Guid.NewGuid().ToString();
            var head = new Dictionary<string, string>()
            {    // { "Qingzhen-Token", recentToken },
                { "Authorization", uploadToken },           
                { "UploadBatch", uuid }
            };
            
            var pvs = new PartViewStream(stream);
            async Task<string> post(string url)
            {
                var retjson = await postStream(url, head, pvs, pvs.Length);
                Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse(retjson);
                return json["ctx"].ToString();
            }
   
            long nblock = flength / block_size;
            List<string> ctxs = new List<string>();
            string url = null;
            //前面的块分片传输，最后的块，整块一片传输，方便写程序
            for (int iblock = 0; iblock < nblock; iblock++)
            {
                //创建块并上传第一片
                long from = iblock * block_size;
                pvs.SetRange(from, from + chunk_size);
                url = $"{filehost}/mkblk/{block_size}/{iblock}";
                string ctx = await post(url);
                for (int ichunk = 1; ichunk < block_size / chunk_size; ichunk++)
                {
                    from += chunk_size;
                    pvs.SetRange(from, from + chunk_size);
                    url = $"{filehost}/bput/{ctx}/{ichunk * chunk_size}";
                    ctx = await post(url);
                    //ctx = await upload_restChunk(uuid, pvs, ctx, uploadToken, host, ichunk, keyname);
                }
                ctxs.Add(ctx);
            }
            //上传剩余的不完整块
            if (flength % block_size != 0)
            {
                long from = nblock * block_size;
                long restLength = flength - from;
                // 上传最后块的第一片,这里只分一片，所以一次上传完毕
                pvs.SetRange(from, flength);
                url = $"{filehost}/mkblk/{pvs.Length}/{nblock}";
                string ctx = await post(url);
                ctxs.Add(ctx);
            }
            //所有的块上传完毕，开始合并所有的块为文件
            url =  $"{filehost}/mkfile/{flength}";
            var outStr = System.Linq.Enumerable.Aggregate(ctxs, (x0, x1) =>  x0 +","+ x1 );
            var retjson = await postStr(url, head, outStr);
            Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse(retjson);
            //json["response"]
            //await upload_mkfile(uuid, stream.Length, ctxs, host, uploadToken);
            //updateToken(resp);
        }
        public async Task download(string vfile_path,string localPath)
        {
            using (var stm = await acquireFileStream(vfile_path))
            using (var fs = File.Create(localPath))
                await stm.CopyToAsync(fs);
        }        
        public async Task<Stream> acquireFileStream(string path)
        {
            JObject jo = await acquireFileInfo_byPath(path);
            var addr = jo["downloadAddress"].ToString();
            var resp = await _hc.GetAsync(addr);
            Stream stm = await resp.Content.ReadAsStreamAsync();
            return stm;
        }
        public async Task<JObject> acquireFileInfo_byPath(string path)
        {
            var data = new { path = path };
            return await acquireFileInfo(data);
        }
        private JObject ensuredJsonResult(JObject json)
        {
            if (!json["success"].ToObject<bool>())
                return null;
            return json["result"].ToObject<JObject>();
        }        
        private bool _ensure(JObject json)
        {
            return json["success"].ToObject<bool>();
        }
        private async Task<JObject> acquireFileInfo(object requestObject)
        {
            var json = await jsonPost("/v2/files/get", requestObject);
            return ensuredJsonResult(json);
        }
        public async Task<JObject> acquireFileInfo_byId(string id)
        {
            var data = new { identity = id };
            return await acquireFileInfo(data);
        }            
        public async Task<JObject> acquireChildren_byPage(string path, int pageNo,int onePageSize,bool dirOnly=false,int filetype=0)
        {
            var data = new
            {
                path = path,
                page = pageNo,
                pageSize = onePageSize
            };
            var json = await jsonPost("/v2/files/page", data);
            return ensuredJsonResult(json);

        }
        public async Task<List<JObject>> acquireAllChildren(string path) {
            int ipage = 1;
            Newtonsoft.Json.Linq.JArray listOb = null;
            int onePageSize = 20;
            var list = new List<JObject>();
            do {
                var result = await acquireChildren_byPage(path, ipage, onePageSize);
                listOb = result["list"].ToObject<Newtonsoft.Json.Linq.JArray>();            
                foreach(var o in listOb.Children<JObject>())
                {
                    list.Add(o);
                }
            } while (listOb.Count == onePageSize);
            return list;
        }
        public async Task<bool> upload(string vdir_path,string filepath,string filename=null)
        {
            string etag = QETag.calcETag(filepath);
            if (filename == null)
                filename = System.IO.Path.GetFileName(filepath);
            var data1 = new { path = vdir_path, hash = etag,name = filename };
            //await post("/v2/user/login", data1);
            var json = await jsonPost("/v2/upload/token", data1);            
            bool suc = json["success"].ToObject<bool>();
            if (!suc)
                return false;
            var cached = json["result"]["hashCached"].ToObject<bool>();
            if (!cached)
            {
                var uploadInfo = json["result"]["uploadInfo"];
                var token = uploadInfo["uploadToken"].ToString();
                var uploadUrl = uploadInfo["uploadUrl"].ToString();
                using (var fs = System.IO.File.OpenRead(filepath))
                    await upload_streamAsync2(uploadUrl,fs, token,filename);
            }
            return true;
        }
        public async Task<VDirectory> listdir(string path)
        {
            VDirectory baseDir = new VDirectory();
            
            var ls = await acquireAllChildren(path);
            //VFileBase vf;
            ObservableCollection<VDirectory> dirs = new ObservableCollection<VDirectory>();
            ObservableCollection<VFile> files = new ObservableCollection<VFile>();
            foreach( var f in ls)
            {
                var isDir = f["directory"].ToObject<bool>();
                var name = f["name"].ToString();
                var mtime = f["mtime"].ToString();
                var id = f["identity"].ToString();
                var share = f["share"].ToObject<bool>();
                var size = f["size"].ToObject<long>();
                if (isDir)
                {
                    var x = new VDirectory()
                    {
                        id = new Guid(id),
                        Name = name,
                        ModifiedTime = new DateTime(long.Parse(mtime))
                    };
                    dirs.Add(x);
                }else
                {
                    var x = new VFile()
                    {
                        id = new Guid(id),
                        Name = name, 
                        ModifiedTime = new DateTime(long.Parse(mtime)),
                        Size = size                      
                    };
                    files.Add(x);
                }
            }
            baseDir.SubDirs = dirs;
            baseDir.Files = files;
            return baseDir;
        }
        public async Task<bool> rm(IEnumerable<string> path)
        {
            //var data2 = String.Format("{source:[{\"path\":\"{1}\"}]}";
            //var data3 = new { source = new[] { new { path = path } } };
            var data = new { source = from x in path select ("path", x) };
            var json = await jsonPost("/v2/files/delete", data);
            return _ensure(json);            
        }        
        public async Task<bool> copy(IEnumerable<string> srcs,string dst)
        {
            var data = new { source = srcs.Select((s)=> ("path",s)) ,destination= new { path=dst} };
            var json = await jsonPost("/v2/files/copy", data);
            return _ensure(json);            
        }        
        public async Task<bool> move(IEnumerable<string> srcs,string dst)
        {
            var data = new { source = srcs.Select((s)=> ("path",s)) ,destination= new { path=dst} };
            var json = await jsonPost("/v2/files/move", data);
            return _ensure(json);            
        }        
        public async Task<bool> rename(string src,string dst)
        {
            var data = new { path = src ,name = dst};
            var json = await jsonPost("/v2/files/rename", data);
            return _ensure(json);            
        }
        public List<string> getfilelist(int folderid)
        {
            List<string> aa= new List<string>();
            return aa;
        }
    }
}
