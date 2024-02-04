using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hikari.Common;
using Hikari.Common.Net.Http;
using Hikari.Common.Security;

namespace Hikari.WeChatCloud.Sdk
{
    /// <summary>
    /// 访问云数据基类
    /// </summary>
    public class DataBaseClient
    {
        private readonly string _appId;
        private readonly string _appSecret;
        private readonly string _env;
        private readonly HttpClientHelper _httpClient;
        private readonly string _url;
        private string _accessToken;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <param name="env"></param>
        public DataBaseClient(string appId, string appSecret, string env = "")
        {
            this._appId = appId;
            this._appSecret = appSecret;
            this._env = env;
            _httpClient = new HttpClientHelper("https://api.weixin.qq.com");
            var headerItem = new Dictionary<string, string>()
            {
                { "Content-Type", "application/json" },
            };
            _httpClient.SetHeaderItem(headerItem);
        }

        /// <summary>
        /// 小程序云数据库查询方法
        /// </summary>
        /// <param name="collectionName">数据库名</param>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<string> QueryStringAsync(string collectionName, QueryParameter query)
        {
            //小程序云数据库查询接口API地址
            string url = $"https://api.weixin.qq.com/tcb/databasequery?access_token={_accessToken}";
            //小程序云数据库查询接口参数
            string ws = JsonSerializer.Serialize(query.Where);
            string whereStr = query.Where == null || string.IsNullOrEmpty(ws) ? string.Empty : $".where({ws})";
            string limitStr = ".limit(1)";
            string skipStr = ".skip(0)";  // 分页计算
            string orderBy = "";

            string queryString = $"db.collection(\"{collectionName}\"){whereStr}{limitStr}{skipStr}{orderBy}.get()";

            var queryBase = new Dictionary<string, object>()
            {
                { "env", _env },
                { "query", queryString }
            };
            string json = await _httpClient.PostAsync(url, queryBase);

            var jo = System.Text.Json.JsonDocument.Parse(json);
            int errcode = jo.RootElement.GetProperty("errcode").GetInt32();
            if (errcode == 0)
            {
                var b = jo.RootElement.TryGetProperty("data", out var r);
                if (b)
                {
                    var sb = Regex.Unescape(r.EnumerateArray().FirstOrDefault().GetString() ?? "");
                    return sb;
                }

            }
            return "";
        }
        /// <summary>
        /// 小程序云数据库查询方法
        /// </summary>
        /// <param name="collectionName">数据库名</param>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<T?> QueryAsync<T>(string collectionName, QueryParameter query) where T : class
        {
            var res = await QueryStringAsync(collectionName, query);
            if (res == "") return null;
            var resData = System.Text.Json.JsonSerializer.Deserialize<T>(res);
            return resData;
        }

        /// <summary>
        /// 小程序云数据库查询方法
        /// </summary>
        /// <param name="collectionName">数据库名</param>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<string> QueryListStringAsync(string collectionName, QueryParameter query)
        {
            int limit = query.Limit > 1000 ? 1000 : query.Limit;
            int skip = query.Skip;
            int count = 0;
            StringBuilder sb = new StringBuilder();
        a:
            //小程序云数据库查询接口API地址
            string url = $"https://api.weixin.qq.com/tcb/databasequery?access_token={_accessToken}";
            //小程序云数据库查询接口参数
            string ws = JsonSerializer.Serialize(query.Where);
            string whereStr = query.Where == null || string.IsNullOrEmpty(ws) ? string.Empty : $".where({ws})";
            string limitStr = $".limit({limit})";
            string skipStr = $".skip({(limit - 1) * skip})";//分页计算
            string orderBy = query.OrderBy ?? "";

            string queryString = $"db.collection(\"{collectionName}\"){whereStr}{limitStr}{skipStr}{orderBy}.get()";

            var queryBase = new Dictionary<string, object>()
            {
                { "env", _env },
                { "query", queryString }
            };
            string json = await _httpClient.PostAsync(url, queryBase);

            var jo = System.Text.Json.JsonDocument.Parse(json);
            int errcode = jo.RootElement.GetProperty("errcode").GetInt32();
            if (errcode == 0)
            {
                query.Limit = query.Limit > jo.RootElement.GetProperty("pager").GetProperty("Total").GetInt32()
                    ? jo.RootElement.GetProperty("pager").GetProperty("Total").GetInt32()
                    : query.Limit;
                var b = jo.RootElement.TryGetProperty("data", out var r);
                if (b)
                {
                    foreach (JsonElement x in r.EnumerateArray())
                    {
                        sb.Append(Regex.Unescape(x.GetString() ?? "") + ",");
                    }

                    count += r.EnumerateArray().Count();
                    if (count < query.Limit)
                    {
                        skip += count;
                        limit = query.Limit - count > 1000 ? 1000 : query.Limit - count;
                        goto a;
                    }
                }

            }

            if (sb.ToString() == "") return "";
            var s = "[" + sb.ToString().TrimEnd(",") + "]";
            return s;

        }
        /// <summary>
        /// 小程序云数据库查询方法
        /// </summary>
        /// <param name="collectionName">数据库名</param>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<List<T>?> QueryListAsync<T>(string collectionName, QueryParameter query) where T : class
        {
            var res = await QueryListStringAsync(collectionName, query);
            if (res == "") return null;
            //var res = jo.RootElement.GetProperty("data").GetProperty("data").EnumerateArray();
            var resData = System.Text.Json.JsonSerializer.Deserialize<List<T>>(res);
            return resData;
        }
        ///// <summary>
        ///// 小程序云数据库添加方法
        ///// </summary>
        ///// <param name="collectionName"></param>
        ///// <param name="paramData"></param>
        ///// <returns></returns>
        //public async Task<string> AddAsync(string collectionName, IDictionary<string, object> paramData)
        //{
        //    var fun = """
        //                {
        //    	"functionTarget": "DCloud-clientDB",
        //    	"functionArgs": {
        //    		"command": {
        //    			"$db": [{
        //    				"$method": "collection",
        //    				"$param": ["$collectionName"]
        //    			}, {
        //    	            "$method": "add",
        //    	            "$param": [$data]
        //                }]
        //    		},
        //    		"uniIdToken": ""
        //    	}
        //    }
        //    """;
        //    fun = fun.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\t", "");
        //    fun = fun.Replace("$collectionName", collectionName);
        //    string data = JsonSerializer.Serialize(paramData);
        //    fun = fun.Replace("$data", data);


        //    var p = new Dictionary<string, object>()
        //    {
        //        { "method", "serverless.function.runtime.invoke" },
        //        { "params",  fun},
        //        { "spaceId", this._spaceId },
        //        { "timestamp", DateTime.Now.ToUnixTimeMilliseconds() },
        //        {"token", _accessToken}
        //    };

        //    string url = "https://api.bspapp.com/client";

        //    var headerItem = new Dictionary<string, string>()
        //        {
        //            { "Content-Type", "application/json" },
        //            { "x-serverless-sign", Sign(p, this._clientSecret) },
        //            {"x-basement-token", p["token"].ToString()}
        //        };
        //    _httpClient.SetHeaderItem(headerItem);
        //    var json = await _httpClient.PostAsync(url, p);
        //    var jo = System.Text.Json.JsonDocument.Parse(json);
        //    bool success = jo.RootElement.GetProperty("success").GetBoolean();
        //    var id = "";
        //    if (success)
        //    {
        //        id = jo.RootElement.GetProperty("data").GetProperty("id").GetString();

        //    }

        //    return id;
        //}

        ///// <summary>
        ///// 小程序云数据库添加方法
        ///// </summary>
        ///// <param name="collectionName"></param>
        ///// <param name="paramData"></param>
        ///// <returns></returns>
        //public async Task<List<string>> AddListAsync(string collectionName, List<IDictionary<string, object>> paramData)
        //{

        //    var fun = """
        //                {
        //    	"functionTarget": "DCloud-clientDB",
        //    	"functionArgs": {
        //    		"command": {
        //    			"$db": [{
        //    				"$method": "collection",
        //    				"$param": ["$collectionName"]
        //    			}, {
        //    	            "$method": "add",
        //    	            "$param": [$data]
        //                }]
        //    		},
        //    		"uniIdToken": ""
        //    	}
        //    }
        //    """;
        //    fun = fun.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\t", "");
        //    fun = fun.Replace("$collectionName", collectionName);
        //    string data = JsonSerializer.Serialize(paramData);
        //    fun = fun.Replace("$data", data);

        //    var p = new Dictionary<string, object>()
        //    {
        //        { "method", "serverless.function.runtime.invoke" },
        //        { "params",  fun},
        //        { "spaceId", this._spaceId },
        //        { "timestamp", DateTime.Now.ToUnixTimeMilliseconds() },
        //        {"token", _accessToken}
        //    };

        //    string url = "https://api.bspapp.com/client";

        //    var headerItem = new Dictionary<string, string>()
        //        {
        //            { "Content-Type", "application/json" },
        //            { "x-serverless-sign", Sign(p, this._clientSecret) },
        //            {"x-basement-token", p["token"].ToString()}
        //        };
        //    _httpClient.SetHeaderItem(headerItem);
        //    var json = await _httpClient.PostAsync(url, p);
        //    var jo = System.Text.Json.JsonDocument.Parse(json);
        //    bool success = jo.RootElement.GetProperty("success").GetBoolean();
        //    var ids = new List<string>();
        //    if (success)
        //    {
        //        var res = jo.RootElement.GetProperty("data").GetProperty("ids").EnumerateArray();
        //        foreach (JsonElement je in res)
        //        {
        //            ids.Add(je.GetString());
        //        }
        //    }

        //    return ids;
        //}

        ///// <summary>
        ///// 小程序云数据库更新方法
        ///// </summary>
        ///// <param name="collectionName"></param>
        ///// <param name="where"></param>
        ///// <param name="paramData"></param>
        ///// <returns></returns>
        //public async Task<bool> UpdateAsync(string collectionName, string where, IDictionary<string, object> paramData)
        //{
        //    var fun = """
        //                {
        //    	"functionTarget": "DCloud-clientDB",
        //    	"functionArgs": {
        //    		"command": {
        //    			"$db": [{
        //    				"$method": "collection",
        //    				"$param": ["$collectionName"]
        //    			}, $where {
        //    	            "$method": "update",
        //    	            "$param": [$data]
        //                }]
        //    		},
        //    		"uniIdToken": ""
        //    	}
        //    }
        //    """;
        //    string whereStr = "";
        //    if (!string.IsNullOrWhiteSpace(where))
        //    {
        //        whereStr = "{\"$method\":\"where\",\"$param\":[" + where + "]},";
        //    }
        //    fun = fun.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\t", "");
        //    fun = fun.Replace("$collectionName", collectionName);
        //    fun = fun.Replace("$where", whereStr);

        //    string data = JsonSerializer.Serialize(paramData);
        //    fun = fun.Replace("$data", data);


        //    var p = new Dictionary<string, object>()
        //    {
        //        { "method", "serverless.function.runtime.invoke" },
        //        { "params",  fun},
        //        { "spaceId", this._spaceId },
        //        { "timestamp", DateTime.Now.ToUnixTimeMilliseconds() },
        //        {"token", _accessToken}
        //    };

        //    string url = "https://api.bspapp.com/client";

        //    var headerItem = new Dictionary<string, string>()
        //        {
        //            { "Content-Type", "application/json" },
        //            { "x-serverless-sign", Sign(p, this._clientSecret) },
        //            {"x-basement-token", p["token"].ToString()}
        //        };
        //    _httpClient.SetHeaderItem(headerItem);
        //    var json = await _httpClient.PostAsync(url, p);
        //    var jo = System.Text.Json.JsonDocument.Parse(json);
        //    bool success = jo.RootElement.GetProperty("success").GetBoolean();
        //    var id = "";
        //    if (success)
        //    {
        //        id = jo.RootElement.GetProperty("data").GetProperty("id").GetString();

        //    }

        //    return true;
        //}

        /////// <summary>
        /////// 小程序云数据库删除方法
        /////// </summary>
        /////// <param name="dateBaseName"></param>
        /////// <param name="update"></param>
        /////// <returns>删除记录数量</returns>
        ////public async Task<int> Delete(DeleteParameter param)
        ////{
        ////    //小程序云数据库查询接口API地址
        ////    string url = $"https://api.weixin.qq.com/tcb/databasedelete?access_token={AccessTokenInit()}";

        ////    string updateString = $"db.collection(\"{param.TableName}\").where({param.Where}).remove()";

        ////    var queryBase = new Dictionary<string, object>()
        ////    {
        ////        {"env", _env},
        ////        {"query", updateString.Replace("\n", "").Replace("\r", "")}
        ////    };
        ////    string json = await _httpClient.PostAsync(url, queryBase);
        ////    ResponseBase resultData = System.Text.Json.JsonSerializer.Deserialize<ResponseBase>(json);
        ////    return resultData.Deleted;
        ////}
        ///// <summary>
        ///// 上传文件
        ///// </summary>
        ///// <param name="file">文件内容</param>
        ///// <param name="fileName">文件名</param>
        ///// <returns></returns>
        //public async Task<string> UploadAsync(byte[] file, string fileName)
        //{
        //    var fileInfo = await CreatFileNameAsync(fileName);
        //    await UploadFileAsync(file, fileInfo);
        //    bool b = await CheckFileAsync(fileInfo.Id);
        //    return b ? $"https://{fileInfo.CdnDomain}/{fileInfo.OssPath}" : "";
        //}

        //private async Task<FileInfoResponse> CreatFileNameAsync(string fileName)
        //{
        //    IDictionary<string, object> options = new Dictionary<string, object>(){
        //        {"method", "serverless.file.resource.generateProximalSign"},
        //        {"params","{\"env\":\"public\",\"filename\":\""+fileName+"\"}"},
        //        {"spaceId", this._spaceId},
        //        { "timestamp", DateTime.Now.ToUnixTimeMilliseconds() },
        //        {"token", _accessToken}
        //    };

        //    var headerItem = new Dictionary<string, string>()
        //    {
        //        { "Content-Type", "application/json" },
        //        { "x-serverless-sign", Sign(options, this._clientSecret) },
        //        {"x-basement-token", options["token"].ToString()}
        //    };
        //    _httpClient.SetHeaderItem(headerItem);

        //    var json = await _httpClient.PostAsync(_url, options);
        //    var jo = System.Text.Json.JsonDocument.Parse(json);
        //    bool success = jo.RootElement.GetProperty("success").GetBoolean();
        //    var fileInfo = new FileInfoResponse();
        //    if (success)
        //    {
        //        var res = jo.RootElement.GetProperty("data");
        //        fileInfo = res.Deserialize<FileInfoResponse>();
        //    }
        //    return fileInfo;
        //}
        //private async Task UploadFileAsync(byte[] file, FileInfoResponse fileInfo)
        //{
        //    string url = "https://" + fileInfo.Host + "/";
        //    IDictionary<string, object> options = new Dictionary<string, object>(){
        //        {"Cache-Control", "max-age=2592000"},
        //        {"Content-Disposition", "attachment"},
        //        {"OSSAccessKeyId", fileInfo.AccessKeyId},
        //        {"Signature", fileInfo.Signature},
        //        {"host", fileInfo.Host},
        //        {"id", fileInfo.Id},
        //        {"key", fileInfo.OssPath},
        //        {"policy", fileInfo.Policy},
        //        {"success_action_status", "200"},
        //        {"file", file}
        //    };
        //    IDictionary<string, string> headerItem = new Dictionary<string, string>()
        //    {
        //        {"Content-Type", "multipart/form-data"},
        //        {"X-OSS-server-side-encrpytion", "AES256" },
        //    };
        //    _httpClient.SetHeaderItem(headerItem);
        //    await _httpClient.PostAsync(url, options);
        //}
        //private async Task<bool> CheckFileAsync(string id)
        //{
        //    IDictionary<string, object> options = new Dictionary<string, object>(){
        //        {"method", "serverless.file.resource.report"},
        //        {"params", "{\"id\":\""+id+"\"}"},
        //        {"spaceId", _spaceId},
        //        { "timestamp", DateTime.Now.ToUnixTimeMilliseconds() },
        //        {"token", _accessToken}
        //    };
        //    var headerItem = new Dictionary<string, string>()
        //    {
        //        { "Content-Type", "application/json" },
        //        { "x-serverless-sign", Sign(options, this._clientSecret) },
        //        { "x-basement-token", options["token"].ToString()! }
        //    };
        //    _httpClient.SetHeaderItem(headerItem);
        //    var html = await _httpClient.PostAsync(_url, options);
        //    var jo = System.Text.Json.JsonDocument.Parse(html);
        //    return jo.RootElement.GetProperty("success").GetBoolean();
        //}
        /// <summary>
        /// 获取接口调用凭证
        /// </summary>
        /// <remarks>参考自：https://github.com/79W/uni-cloud-storage</remarks>
        /// <returns></returns>
        public async Task GetAccessTokenAsync()
        {
            string re = await _httpClient.GetAsync($"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={_appId}&secret={_appSecret}");
            var jo = System.Text.Json.JsonDocument.Parse(re);
            var accessToken = jo.RootElement.GetProperty("access_token").GetString() ?? "";
            _accessToken = accessToken;
        }
    }
}
