using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.ComponentModel;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json.Linq;

namespace KakaoVision
{

    public sealed class AdultDetect : CodeActivity
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

        //이미지가 정상인 확률 
        [Category("Output")]
        public OutArgument<System.Double> Normal { get; set; }

        //이미지가 약간의 노출이 있을 확률, 주로 수영복 
        [Category("Output")]
        public OutArgument<System.Double> Soft { get; set; }

        //이미지가 노출이 많은 성인 이미지일 확률, 주로 음란물 
        [Category("Output")]
        public OutArgument<System.Double> Adult { get; set; }

        //Kakao API 호출 결과 오류 코드  
        [Category("Output")]
        public OutArgument<string> ErrorCode { get; set; }

        public static string API_ENDPOINT = "https://kapi.kakao.com/v1/vision/adult/detect";

        // 작업 결과 값을 반환할 경우 CodeActivity<TResult>에서 파생되고
        // Execute 메서드에서 값을 반환합니다.
        protected override void Execute(CodeActivityContext context)
        {
            //매개변수로 부터 실제 값을 가져오기 
            var rest_api_key = context.GetValue(RestApiKey);
            var file_path = context.GetValue(FilePath);
            var image_url = context.GetValue(ImageUrl);

            if( string.IsNullOrEmpty(rest_api_key) )
            {
                throw new Exception("Kakao API를 호출하기 위한 REST API KEY 값이 없습니다.");
            }
            if( string.IsNullOrEmpty( file_path) && string.IsNullOrEmpty( image_url))
            {
                throw new Exception("성인 이미지 판단을 위한 이미지 정보가 없습니다.");
            }
            HttpResponseMessage response = null;
            var client = new HttpClient();
            MultipartFormDataContent formData = new MultipartFormDataContent();
            client.DefaultRequestHeaders.Add("Authorization", string.Format("KakaoAK {0}", rest_api_key));
            if (string.IsNullOrEmpty(image_url))
            {
                try
                {
                    FileInfo imgFile = new FileInfo(file_path);
                    byte[] imgData = new byte[imgFile.Length];

                    if (imgFile.Exists && imgFile.Length > 0)
                    {
                        imgData = File.ReadAllBytes(imgFile.FullName);
                        if (imgData.Length > 0)
                            formData.Add(new StreamContent(new MemoryStream(imgData)), "file", imgFile.Name);
                    }
                    response = client.PostAsync( API_ENDPOINT, formData).Result;
                }
                catch (IOException ioe)
                {
                    System.Console.WriteLine(ioe.Message);

                }
            }
            else if ( string.IsNullOrEmpty( file_path))
            {
                var body = new StringContent( String.Format("image_url={0}", image_url), Encoding.UTF8, "application/x-www-form-urlencoded");
                response = client.PostAsync(API_ENDPOINT, body).Result;
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                System.Console.WriteLine(content);
                JObject respJson = JObject.Parse(content);
                //정상 이미지  확률 설정  
                Normal.Set(context, (System.Double)respJson["result"]["normal"]);
                //약간 노출 확률 설정 
                Soft.Set(context, (System.Double)respJson["result"]["soft"]);
                //성인이미지 노출 확률 설정 
                Adult.Set(context, (System.Double)respJson["result"]["adult"]);
            }
            else
            {
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                System.Console.WriteLine(content);
                Normal.Set(context, 0.0);
                Normal.Set(context, 0.0);
                Normal.Set(context, 0.0); 
            }
        }
    }
}
