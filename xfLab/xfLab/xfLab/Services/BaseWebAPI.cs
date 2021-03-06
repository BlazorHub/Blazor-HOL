﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using xfLab.Helpers.Constants;
using xfLab.Helpers.Storages;
using xfLab.Helpers.WebAPIs;
using xfLab.Models;
using xfLab.Models.BaseAPIModels;

namespace xfLab.Services
{
    /// <summary>
    /// 存取Http服務的Base Class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseWebAPI<T>
    {
        #region Field
        /// <summary>
        /// WebAPI主機位置
        /// </summary>
        public string host = ConstantHelper.APIHostBase;

        /// <summary>
        /// WebAPI方法網址
        /// </summary>
        public string url { get; set; }
        public EncodingMethod EncodingType { get; set; }

        /// <summary>
        /// 資料夾名稱
        /// </summary>
        public string 現在資料夾名稱 = "";
        public string 子資料夾名稱 = "";
        public string 最上層資料夾名稱 = "Data";

        /// <summary>
        /// 檔案名稱
        /// </summary>
        public string 資料檔案名稱 { get; set; }

        public string ResponseCookie { get; set; }

        #region 系統用到的訊息字串
        public static readonly string APIInternalError = "System Exception = null, Result = null";
        #endregion

        #endregion

        // =========================================================================================================

        #region protected

        #endregion

        // =========================================================================================================

        #region Public
        /// <summary>
        /// 透過Http取得的資料，也許是一個物件，也許是List
        /// </summary>
        public List<T> Items { get; set; }
        public T Item { get; set; }
        public bool IsCollection { get; set; } = false;
        public bool NeedSaveToStorage { get; set; } = true;
        public StandardResponse<T> standardResponse { get; set; }
        public StandardResponse<List<T>> standardResponseCollection { get; set; }
        /// <summary>
        /// 此次呼叫的處理結果
        /// </summary>
        public ManagerResult managerResult { get; set; }

        public bool 資料加密處理 { get; set; } = false;

        #endregion

        // =========================================================================================================

        /// <summary>
        /// 建構子，經由繼承後使用反射取得類別的名稱當作，檔案名稱及WebAPI的方法名稱
        /// </summary>
        public BaseWebAPI()
        {
            SetWebAccessCondition("/api/", this.GetType().Name, "Datas", this.GetType().Name);
            EncodingType = EncodingMethod.FORMURLENCODED;
            現在資料夾名稱 = 最上層資料夾名稱;
            url = "";
            資料檔案名稱 = this.GetType().Name;
            子資料夾名稱 = 資料檔案名稱;
        }

        /// <summary>
        /// 建立存取 Web 服務的參數
        /// </summary>
        /// <param name="_url">存取服務的URL</param>
        /// <param name="_DataFileName">儲存資料的名稱</param>
        /// <param name="_DataFolderName">資料要儲存的目錄</param>
        /// <param name="_className">類別名稱</param>
        public void SetWebAccessCondition(string _url, string _DataFileName, string _DataFolderName, string _className = "")
        {
            string className = _className;

            this.url = string.Format("{0}{1}", _url, _className);
            this.資料檔案名稱 = _DataFileName;
            this.現在資料夾名稱 = _DataFolderName;
            this.managerResult = new ManagerResult();
        }

        /// <summary>
        /// 從網路取得相對應WebAPI的資料
        /// </summary>
        /// <param name="dic">所要傳遞的參數 Dictionary </param>
        /// <param name="httpMethod">Get Or Post</param>
        /// <returns></returns>
        protected virtual async Task<ManagerResult> GetItemsFromNetAsync(Dictionary<string, string> dic, HttpMethod httpMethod,
            bool 要傳送Cookie = false)
        {
            this.managerResult = new ManagerResult();
            ManagerResult mr = this.managerResult;

            //檢查網路狀態
            var current = Connectivity.NetworkAccess;

            if (current != NetworkAccess.Internet)
            {
                mr.Success = false;
                mr.Message = "無網路連線可用，請檢查網路狀態";
                return mr;
            }
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            LoggingHandler handler = new LoggingHandler(httpClientHandler);
            using (HttpClient client = new HttpClient(handler))
            {
                try
                {
                    ResponseCookie = "";
                    //client.Timeout = TimeSpan.FromMinutes(3);
                    string FooUrl = $"{host}{url}";
                    UriBuilder ub = new UriBuilder(FooUrl);
                    HttpResponseMessage response = null;

                    #region  Get Or Post
                    if (httpMethod == HttpMethod.Get)
                    {
                        // 使用 Get 方式來呼叫
                        var foo = ub.Uri + dic.ToQueryString();
                        Debug.WriteLine(foo);
                        response = await client.GetAsync(foo);
                    }
                    else if (httpMethod == HttpMethod.Post)
                    {
                        // 使用 Post 方式來呼叫
                        if (EncodingType == EncodingMethod.FORMURLENCODED)
                        {
                            // 使用 FormUrlEncoded 方式來進行傳遞資料的編碼
                            response = await client.PostAsync(ub.Uri, dic.ToFormUrlEncodedContent());
                        }
                        else if (EncodingType == EncodingMethod.XML)
                        {
                            response = await client.PostAsync(ub.Uri, new StringContent(dic["XML"], Encoding.UTF8, "application/xml"));
                        }
                        else if (EncodingType == EncodingMethod.JSON)
                        {
                            client.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
                            response = await client.PostAsync(ub.Uri, new StringContent(dic["JSON"], Encoding.UTF8, "application/json"));
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Not Found HttpMethod");
                    }
                    #endregion

                    #region Response
                    if (response != null)
                    {
                        String strResult = await response.Content.ReadAsStringAsync();

                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                                mr.Success = true;
                                if (IsCollection == true)
                                {
                                    standardResponseCollection = JsonConvert
                                        .DeserializeObject<StandardResponse<List<T>>>(strResult,
                                        new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });

                                    if (standardResponseCollection == null)
                                    {
                                        mr.Success = false;
                                        mr.Message = "沒有得到正確的呼叫回傳格式內容";
                                        return mr;
                                    }
                                    mr.Success = standardResponseCollection.Success;
                                    if (standardResponseCollection.Success == true)
                                    {
                                        Items = standardResponseCollection.Payload;
                                        ResetItems();
                                    }
                                    else
                                    {
                                        ResetItems();
                                        mr.Message = standardResponse.ErrorMessage;
                                    }
                                    if (NeedSaveToStorage == true)
                                    {
                                        await this.WriteToFileAsync();
                                    }
                                }
                                else
                                {
                                    standardResponse = JsonConvert.DeserializeObject<StandardResponse<T>>(strResult,
                                        new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });

                                    if (standardResponse == null)
                                    {
                                        mr.Success = false;
                                        mr.Message = "沒有得到正確的呼叫回傳格式內容";
                                        return mr;
                                    }
                                    mr.Success = standardResponse.Success;
                                    if (standardResponse.Success == true)
                                    {
                                        Item = standardResponse.Payload;
                                        ResetItems();
                                    }
                                    else
                                    {
                                        ResetItems();
                                        mr.Message = standardResponse.ErrorMessage;
                                    }
                                    if (NeedSaveToStorage == true)
                                    {
                                        await this.WriteToFileAsync();
                                    }
                                }
                                break;

                            default:
                                mr.Success = false;
                                mr.WebResponse = strResult;
                                mr.Message = string.Format("Error Code:{0}, Error Message:{1}", response.StatusCode, strResult);
                                break;
                        }
                    }
                    else
                    {
                        mr.Success = false;
                        mr.Message = APIInternalError;
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    mr.Success = false;
                    mr.Exception = ex;
                    mr.Message = ex.Message;
                }
            }

            return mr;
        }

        private void ResetItems()
        {
            if (Items == null)
            {
                Items = (List<T>)Activator.CreateInstance(typeof(List<T>));
            }
            if (Item == null)
            {
                Item = (T)Activator.CreateInstance(typeof(T));
            }
        }

        /// <summary>
        /// 將物件資料從檔案中讀取出來
        /// </summary>
        public virtual async Task ReadFromFileAsync(bool ReadCollection)
        {
            Items = (List<T>)Activator.CreateInstance(typeof(List<T>));
            Item = (T)Activator.CreateInstance(typeof(T));
            string data = await StorageUtility.ReadFromDataFileAsync("", this.現在資料夾名稱, this.資料檔案名稱, false);

            if (string.IsNullOrEmpty(data) == true)
            {

            }
            else
            {
                try
                {
                    if (ReadCollection == true)
                    {
                        this.Items = JsonConvert
                            .DeserializeObject<List<T>>(data,
                            new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
                    }
                    else
                    {
                        this.Item = JsonConvert
                            .DeserializeObject<T>(data,
                            new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

        }

        /// <summary>
        /// 將物件資料寫入到檔案中
        /// </summary>
        public virtual async Task WriteToFileAsync()
        {
            string data = "";
            if (IsCollection == true)
            {
                data = JsonConvert.SerializeObject(this.Items);
            }
            else
            {
                data = JsonConvert.SerializeObject(this.Item);
            }
            await StorageUtility.WriteToDataFileAsync("", this.現在資料夾名稱, this.資料檔案名稱, data, false);
        }

    }

    /// <summary>
    /// POST資料的時候，將要傳遞的參數，使用何種方式來進行編碼
    /// </summary>
    public enum EncodingMethod
    {
        /// <summary>
        /// 使用 multipart/form-data 方式來進行傳遞參數的編碼
        /// </summary>
        MULTIPART,
        /// <summary>
        /// 使用 application/x-www-form-urlencoded 方式來進行傳遞參數的編碼
        /// </summary>
        FORMURLENCODED,
        XML,
        /// <summary>
        /// 使用 application/json 方式來進行傳遞參數的編碼
        /// </summary>
        JSON
    }
}
