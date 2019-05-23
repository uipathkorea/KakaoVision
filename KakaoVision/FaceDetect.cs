using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.IO;

namespace KakaoVision
{

    public sealed class FaceDetect : CodeActivity
    {
        //Kakao API를 사용하기 위한 REST API KEY
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> RestApiKey { get; set; }

        //분석하고자 하는 이미지 경로
        [Category("Input")]
        public InArgument<string> FilePath { get; set; }

        //분석하고자 하는 이미지 URL 
        [Category("Input")]
        public InArgument<string> ImageUrl { get; set; }

        //얼굴 인식에 대한 threshold 값 
        [Category("Input")]
        public InArgument<double> Threshold { get; set; }

        //결과 성별 및 나이 
        [Category("Output")]
        public OutArgument<double> Male { get; set; }
        [Category("Output")]
        public OutArgument<double> Female { get; set; }
        [Category("Output")]
        public OutArgument<double> Age { get; set; }
        //오류 코드 값 
        [Category("Output")]
        public OutArgument<string> ErrorCode { get; set; }


        public static string API_ENDPOINT = "https://kapi.kakao.com/v1/vision/face/detect";

        // 작업 결과 값을 반환할 경우 CodeActivity<TResult>에서 파생되고
        // Execute 메서드에서 값을 반환합니다.
        protected override void Execute(CodeActivityContext context)
        {
            var rest_api_key = RestApiKey.Get(context);
            var file_path = context.GetValue(FilePath);
            var image_url = context.GetValue(ImageUrl);
            var threshold = context.GetValue(Threshold);
            if (threshold == 0.0)
                threshold = 0.7; //default 

            if (string.IsNullOrEmpty(rest_api_key))
            {
                throw new Exception("Kakao Vision API를 호출하기 위한 REST API KEY 값이 없습니다.");
            }
            if (string.IsNullOrEmpty(file_path) && string.IsNullOrEmpty(image_url))
            {
                throw new Exception("얼굴 이미지 판단을 위한 이미지 정보가 없습니다.");
            }
            HttpResponseMessage response = null;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", string.Format("KakaoAK {0}", rest_api_key));
            if (!string.IsNullOrEmpty(file_path))
            {
                MultipartFormDataContent formData = new MultipartFormDataContent();
                try
                {
                    FileInfo fileInfo = new FileInfo(file_path);
                    if (fileInfo.Exists && fileInfo.Length > 0)
                    {
                        var imageBuf = new byte[fileInfo.Length];
                        imageBuf = File.ReadAllBytes(fileInfo.FullName);
                        if (imageBuf.Length > 0)
                            formData.Add(new StreamContent(new MemoryStream(imageBuf)), "file", fileInfo.Name);
                        response = client.PostAsync(API_ENDPOINT, formData).Result;
                    }
                }
                catch (System.IO.IOException ioe)
                {
                    new Exception(string.Format("이미지 파일을 읽을수 없습니다. 오류 메세지 : {0}", ioe.Message));
                }
            }
            else if( ! string.IsNullOrEmpty( image_url))
            {
                var body = new StringContent(String.Format("image_url={0}", image_url), Encoding.UTF8, "application/x-www-form-urlencoded");
                response = client.PostAsync(API_ENDPOINT, body).Result;
            }

            if( response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var jsonresp = JObject.Parse(content);
                System.Console.WriteLine(content);
                double male = (double)jsonresp["result"]["faces"][0]["facial_attributes"]["gender"]["male"];
                double female = (double)jsonresp["result"]["faces"][0]["facial_attributes"]["gender"]["female"];
                double age = (double)jsonresp["result"]["faces]"][0]["facial_attributes"]["age"];

                Male.Set(context, male);
                Female.Set(context, female);
                Age.Set(context, age);
            }
            else
            {
                ErrorCode.Set(context, response.StatusCode.ToString());
                Male.Set(context, 0.0);
                Female.Set(context, 0.0);
                Age.Set(context, 0.0);
            }
        }
    }
}
