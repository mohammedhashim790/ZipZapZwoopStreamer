using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamer.AWS;
class FileObjectParams
{
    public String bucket;
    public String region;
    public String relativePath;
    public String key;
    public String size = "";




    public override string ToString()
    {
        return String.Format("bucket {0}, region {1}, relativePath {2}, key {3}, size {4}",bucket,region,relativePath,key,size);
    }
}


class FileObjectBody
{
    String Url;
}


namespace Streamer.Controllers
{


    public enum ZipFileObjectResults : int
    {
        FILE_NOT_FOUND,
        FILE_EXPIRED,
        UNEXPECTED_ERROR,
        SERVICE_OUTRAGE
    }


    [Route("download")]
    [ApiController]
    public class FileStreamer : ControllerBase
    {

        String FileName = "zipzapzwoop_transfer";

        String TestUrl = "";

        AWSHelper awsHelper;

        AWS.Environment environment;


        private readonly ILogger logger;

        public FileStreamer(AWS.Environment environment, ILogger<FileStreamer> logger)
        {

            this.environment = environment;
            this.logger = logger;
            System.Diagnostics.Trace.WriteLine("Table Name");
            System.Diagnostics.Trace.WriteLine(System.Environment.GetEnvironmentVariable("SessionTableName"));
            int a = 1;

            awsHelper = new AWSHelper(System.Environment.GetEnvironmentVariable("SessionTableName"));

            this.TestUrl = System.Environment.GetEnvironmentVariable("StorageURL");
        }


        [HttpGet("Env")]
        public JsonResult ListEnv()
        {
            return new JsonResult("" +
                "{SessionTableName:" +
                 awsHelper.TableName +
                "," +
                "StorageURl :" +
                this.TestUrl +
                "," +
                "New Version update" +
                "}");
        }


        [HttpGet("stream/{accessSpecifier}/{sessionId}")]
        public void StreamDownload(
            [FromRoute] string accessSpecifier,
            [FromRoute] string sessionId
            )
        {
            System.Diagnostics.Trace.WriteLine("**********************");
            System.Diagnostics.Trace.WriteLine("    ");
            System.Diagnostics.Trace.WriteLine(sessionId);
            System.Diagnostics.Trace.WriteLine("    ");
            System.Diagnostics.Trace.WriteLine(accessSpecifier);
            System.Diagnostics.Trace.WriteLine("    ");
            System.Diagnostics.Trace.WriteLine("**********************");


            this.logger.LogInformation("**********************");
            this.logger.LogInformation("    ");
            this.logger.LogInformation(sessionId);
            this.logger.LogInformation("    ");
            this.logger.LogInformation(accessSpecifier);
            this.logger.LogInformation("    ");
            this.logger.LogInformation("**********************");





            String compressedFileName = this.FileName + "_" + DateTime.Now.ToShortDateString();

            //sessionId = this.ConvertUUIDStandard(sessionId);
            System.Diagnostics.Trace.WriteLine(sessionId);

            String session = this.GetSession(sessionId);

            JObject json = JObject.Parse(session);


            List<FileObjectParams> files = new List<FileObjectParams>();


            System.Diagnostics.Trace.WriteLine("Intialising ");
            this.logger.LogInformation("Intialising ");

            var fileList = json["files"].ToList();
            for (var iter = 0; iter < fileList.Count; iter++)
            {
                var file = fileList[iter];
                files.Add(
                    new FileObjectParams
                    {
                        bucket = file["bucket"].ToString(),
                        region = file["region"].ToString(),
                        key = file["key"].ToString(),
                        relativePath = file["relativePath"].ToString(),
                        size = file["size"].ToString()
                    }
                    );
            }

            System.Diagnostics.Trace.WriteLine(files.Count);
            System.Diagnostics.Trace.WriteLine(files[0]);
            this.logger.LogInformation(files[0].ToString());

            System.Diagnostics.Trace.WriteLine("Intialised ");


            this.logger.LogInformation("Intialised ");




            Response.Headers.Add("Content-Disposition", "attachment; filename=" + compressedFileName + ".zip");

            //Response.Headers.Add("Transfer-Encoding", "identity");


            Response.ContentType = "application/zip";


            //Response.ContentLength = long.Parse(json["fileSize"].ToString())-205;

            using (ZipOutputStream zipOutputStream = new ZipOutputStream(Response.Body)) {
                try
                {
                    zipOutputStream.SetLevel(0);
                    zipOutputStream.UseZip64 = UseZip64.Off;
                    zipOutputStream.IsStreamOwner = false;
                    foreach (FileObjectParams file in files)
                    {
                        var name = (file.relativePath == null || file.relativePath == "") ? file.key : file.relativePath;

                        System.Diagnostics.Trace.WriteLine(name);

                        this.logger.LogInformation(name);


                        ZipEntry zipEntry = new ZipEntry(name);

                        zipOutputStream.PutNextEntry(zipEntry);

                        var fileUrl = String.Format("{0}/{1}/{2}/{3}",
                                this.TestUrl,
                                accessSpecifier,
                                sessionId,
                                file.key
                                );

                        System.Diagnostics.Trace.WriteLine(fileUrl);


                        this.logger.LogInformation(fileUrl);

                        HttpWebRequest myHttpWebRequest = (HttpWebRequest)
                            WebRequest.Create(fileUrl);

                        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

                        System.Diagnostics.Trace.WriteLine("   ");
                        System.Diagnostics.Trace.WriteLine("   ");
                        System.Diagnostics.Trace.WriteLine("   ");
                        System.Diagnostics.Trace.WriteLine("   ");

                        System.Diagnostics.Trace.WriteLine("Reading Files " + file.key);
                        System.Diagnostics.Trace.WriteLine(myHttpWebResponse.ContentType);
                        System.Diagnostics.Trace.WriteLine(myHttpWebResponse.ContentLength);
                        //System.Diagnostics.Trace.WriteLine(myHttpWebResponse.Headers);

                        Stream receiveStream = myHttpWebResponse.GetResponseStream();
                        Encoding encode = System.Text.Encoding.GetEncoding("utf-8");


                        //StreamReader readStream = new StreamReader(receiveStream, encode);
                        BinaryReader readStream = new BinaryReader(receiveStream, new UTF8Encoding(false));



                        byte[] buffer = new byte[1024];
                        int sourceBytes = 0;
                        int total = 0;

                        String line = String.Empty;


                        using (BinaryReader readstream = new BinaryReader(receiveStream))
                        {
                            do
                            {
                                sourceBytes = readstream.Read(buffer, 0, buffer.Length);
                                if (sourceBytes != 0)
                                {
                                    //System.Diagnostics.Trace.WriteLine(Encoding.ASCII.GetString(buffer));
                                    zipOutputStream.Write(buffer, 0, sourceBytes);
                                    total += sourceBytes;
                                }

                            } while (sourceBytes > 0);
                        }
                        System.Diagnostics.Trace.WriteLine(file.key + " Completed");

                        this.logger.LogInformation(file.key + " Completed");


                        System.Diagnostics.Trace.WriteLine(myHttpWebResponse.ContentLength + " Acc : " + total);


                        this.logger.LogInformation(myHttpWebResponse.ContentLength + " Acc : " + total);



                        zipOutputStream.CloseEntry();

                        this.logger.LogInformation("Exiting From App");



                    }
                    zipOutputStream.Finish();
                    zipOutputStream.Flush();
                }
                catch (Exception ex)
                {

                    this.logger.LogInformation(ex.Message);
                    this.logger.LogError(new EventId(123), ex.Message);
                    zipOutputStream.Finish();
                    zipOutputStream.Close();
                }
            }

        }

        [HttpGet("Check")]
        public string GetQueryCheck(
            int id,int temp1,string temp2) {

            return "Access Specifier : " + id + ", Temp1 : " + temp1 + ", Temp2 : " +temp2;
        }


        [HttpGet("{sessionId}")]
        public void StreamDownloadFrom(
            [FromRoute] string sessionId
            )
        {

            this.logger.LogInformation("Reading from Default Public");

            System.Diagnostics.Trace.WriteLine("Reading from Default Public");



            String accessSpecifier = "public";
            System.Diagnostics.Trace.WriteLine("**********************");
            System.Diagnostics.Trace.WriteLine("    ");
            System.Diagnostics.Trace.WriteLine(sessionId);
            System.Diagnostics.Trace.WriteLine("    ");
            System.Diagnostics.Trace.WriteLine(accessSpecifier);
            System.Diagnostics.Trace.WriteLine("    ");
            System.Diagnostics.Trace.WriteLine("**********************");


            this.logger.LogInformation("**********************");
            this.logger.LogInformation("    ");
            this.logger.LogInformation(sessionId);
            this.logger.LogInformation("    ");
            this.logger.LogInformation(accessSpecifier);
            this.logger.LogInformation("    ");
            this.logger.LogInformation("**********************");





            String compressedFileName = this.FileName + "_" + DateTime.Now.ToShortDateString();

            //sessionId = this.ConvertUUIDStandard(sessionId);
            System.Diagnostics.Trace.WriteLine(sessionId);

            String session = this.GetSession(sessionId);

            if(session == "None")
            {
                return; 
                //BadRequest("{" +
                //    "id : " + ZipFileObjectResults.FILE_NOT_FOUND + "," +
                //    "Message : File Not Found" +
                //    "}");
            }

            JObject json = JObject.Parse(session);


            List<FileObjectParams> files = new List<FileObjectParams>();


            System.Diagnostics.Trace.WriteLine("Intialising ");
            this.logger.LogInformation("Intialising ");

            var fileList = json["files"].ToList();
            for (var iter = 0; iter < fileList.Count; iter++)
            {
                var file = fileList[iter];
                files.Add(
                    new FileObjectParams
                    {
                        bucket = file["bucket"].ToString(),
                        region = file["region"].ToString(),
                        key = file["key"].ToString(),
                        relativePath = file["relativePath"].ToString(),
                        size = file["size"].ToString()
                    }
                    );
            }

            System.Diagnostics.Trace.WriteLine(files.Count);
            System.Diagnostics.Trace.WriteLine(files[0]);
            this.logger.LogInformation(files[0].ToString());

            System.Diagnostics.Trace.WriteLine("Intialised ");


            this.logger.LogInformation("Intialised ");




            Response.Headers.Add("Content-Disposition", "attachment; filename=" + compressedFileName + ".zip");

            //Response.Headers.Add("Transfer-Encoding", "identity");


            Response.ContentType = "application/zip";


            //Response.ContentLength = long.Parse(json["fileSize"].ToString())-205;

            using (ZipOutputStream zipOutputStream = new ZipOutputStream(Response.Body))
            {
                try
                {
                    zipOutputStream.SetLevel(0);
                    zipOutputStream.UseZip64 = UseZip64.Off;
                    zipOutputStream.IsStreamOwner = false;
                    foreach (FileObjectParams file in files)
                    {
                        var name = (file.relativePath == null || file.relativePath == "") ? file.key : file.relativePath;

                        System.Diagnostics.Trace.WriteLine(name);

                        this.logger.LogInformation(name);


                        ZipEntry zipEntry = new ZipEntry(name);

                        zipOutputStream.PutNextEntry(zipEntry);

                        var fileUrl = String.Format("{0}/{1}/{2}/{3}",
                                this.TestUrl,
                                accessSpecifier,
                                sessionId,
                                file.key
                                );

                        System.Diagnostics.Trace.WriteLine(fileUrl);


                        this.logger.LogInformation(fileUrl);

                        HttpWebRequest myHttpWebRequest = (HttpWebRequest)
                            WebRequest.Create(fileUrl);

                        
                        
                        //NewChange for connection Reset by Peer

                        myHttpWebRequest.KeepAlive = false;
                        myHttpWebRequest.ProtocolVersion = HttpVersion.Version10;



                        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

                        System.Diagnostics.Trace.WriteLine("   ");
                        System.Diagnostics.Trace.WriteLine("   ");
                        System.Diagnostics.Trace.WriteLine("   ");
                        System.Diagnostics.Trace.WriteLine("   ");

                        System.Diagnostics.Trace.WriteLine("Reading Files " + file.key);
                        System.Diagnostics.Trace.WriteLine(myHttpWebResponse.ContentType);
                        System.Diagnostics.Trace.WriteLine(myHttpWebResponse.ContentLength);
                        //System.Diagnostics.Trace.WriteLine(myHttpWebResponse.Headers);

                        Stream receiveStream = myHttpWebResponse.GetResponseStream();
                        Encoding encode = System.Text.Encoding.GetEncoding("utf-8");


                        //StreamReader readStream = new StreamReader(receiveStream, encode);
                        BinaryReader readStream = new BinaryReader(receiveStream, new UTF8Encoding(false));



                        byte[] buffer = new byte[1024];
                        int sourceBytes = 0;
                        int total = 0;

                        String line = String.Empty;


                        using (BinaryReader readstream = new BinaryReader(receiveStream))
                        {
                            do
                            {
                                sourceBytes = readstream.Read(buffer, 0, buffer.Length);
                                if (sourceBytes != 0)
                                {
                                    //System.Diagnostics.Trace.WriteLine(Encoding.ASCII.GetString(buffer));
                                    zipOutputStream.Write(buffer, 0, sourceBytes);
                                    total += sourceBytes;
                                }

                            } while (sourceBytes > 0);
                        }
                        System.Diagnostics.Trace.WriteLine(file.key + " Completed");

                        this.logger.LogInformation(file.key + " Completed");


                        System.Diagnostics.Trace.WriteLine(myHttpWebResponse.ContentLength + " Acc : " + total);


                        this.logger.LogInformation(myHttpWebResponse.ContentLength + " Acc : " + total);



                        zipOutputStream.CloseEntry();

                        this.logger.LogInformation("Exiting From App");



                    }
                    zipOutputStream.Finish();
                    zipOutputStream.Flush();
                }
                catch (Exception ex)
                {

                    this.logger.LogInformation(ex.Message);
                    this.logger.LogError(new EventId(123), ex.Message);
                    zipOutputStream.Finish();
                    zipOutputStream.Close();
                    //return BadRequest("File Does not exist / have expired");
                }
            }
            return ;

        }

        public string ConvertUUIDStandard(string sessionID)
        {
            System.Diagnostics.Trace.WriteLine(sessionID.Length);
            var first = sessionID.Substring(0, 8);
            var second = sessionID.Substring(8, 12);
            var third = sessionID.Substring(12, 16);
            var fourth = sessionID.Substring(16, 20);
            var fifth = sessionID.Substring(20, 31);
            return String.Format("{0}-{1}-{2}-{3}-{4}", first, second, third, fourth, fifth);
        }

        private void StreamDownload(String url,ZipEntry zipEntry)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            
            HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();


            Stream receiveStream = myHttpWebResponse.GetResponseStream();
            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");


            StreamReader readStream = new StreamReader(receiveStream, encode);
            Console.WriteLine("\r\nResponse stream received.");
            Char[] read = new Char[256];

            int count = readStream.Read(read, 0, 256);
            System.Diagnostics.Trace.WriteLine("HTML...\r\n");
            while (count > 0)
            {

                String str = new String(read, 0, count);
                count = readStream.Read(read, 0, 256);
            }
            myHttpWebResponse.Close();
            readStream.Close();
        }


        [HttpGet("List")]
        public IActionResult ListTable()
        {
            var res = awsHelper.ListSessions().ToJsonPretty();
            return Ok(res);
        }


        public String GetSession(string sessionId)
        {
            try
            {
                var res = awsHelper.getSession(sessionId).ToJsonPretty();

                System.Diagnostics.Debug.WriteLine("Result from DB");
                System.Diagnostics.Debug.WriteLine(res);

                return res;
            }catch(Exception e)
            {
                this.logger.LogError(e.Message);

                return "None";
            }
        }

        //[HttpGet("{accessSpecifier}/{sessionId}")]
        //public ObjectResult StreamDownload(
        //    [FromRoute] string sessionId,
        //    [FromRoute] string accessSpecifier)
        //{
        //    System.Diagnostics.Debug.WriteLine("**********************");
        //    System.Diagnostics.Debug.WriteLine("    ");
        //    System.Diagnostics.Debug.WriteLine(sessionId);
        //    System.Diagnostics.Debug.WriteLine("    ");
        //    System.Diagnostics.Debug.WriteLine(accessSpecifier);
        //    System.Diagnostics.Debug.WriteLine("    ");
        //    System.Diagnostics.Debug.WriteLine("**********************");


        //    String compressedFileName = this.FileName + "_" + DateTime.Now.ToShortDateString();


        //    List<FileParam> filepath = new List<FileParam>();

        //    //sessionId = this.ConvertUUIDStandard(sessionId);
        //    System.Diagnostics.Debug.WriteLine(sessionId);

        //    String session = this.GetSession(sessionId);

        //    JObject json = JObject.Parse(session);


        //    List<FileObjectParams> files = new List<FileObjectParams>();


        //    System.Diagnostics.Debug.WriteLine("Intialising ");

        //    foreach (var file in json["files"].ToList())
        //    {
        //        files.Add(
        //            new FileObjectParams
        //            {
        //                bucket = file["bucket"].ToString(),
        //                region = file["region"].ToString(),
        //                key = file["key"].ToString(),
        //                relativePath = file["relativePath"].ToString(),
        //            }
        //            );
        //    }

        //    System.Diagnostics.Debug.WriteLine(files.Count);
        //    System.Diagnostics.Debug.WriteLine(files[0]);

        //    System.Diagnostics.Debug.WriteLine("Intialised ");


        //    List<FileParam> filePaths = new List<FileParam> {
        //        new FileParam{
        //            Name = "SampleDir/Colors.txt",
        //            Url="https://zip-zap-zwoop-beta-storage214657-dev.s3.ap-south-1.amazonaws.com/public/res/public/34d3c879-64ca-436c-a486-43dd61f5fb58/MISCELLENEOUS/Colors/COLORS.txt",
        //            Content = "Hello@123"
        //        },
        //        new FileParam{
        //            Name = "image.png",
        //            Url="https://zip-zap-zwoop-beta-storage214657-dev.s3.ap-south-1.amazonaws.com/public/res/public/34d3c879-64ca-436c-a486-43dd61f5fb58/MISCELLENEOUS/154-1542390_software-application-icon-png.png",
        //            Content = "Hello@123"
        //        },
        //        new FileParam{
        //            Name = "yolov3.cfg",
        //            Url="https://raw.githubusercontent.com/pjreddie/darknet/master/cfg/yolov3.cfg",
        //            Content = "Hello@123"
        //        },
        //        new FileParam{
        //            Name = "COLORS.xlsx",
        //            Url="https://zip-zap-zwoop-beta-storage214657-dev.s3.ap-south-1.amazonaws.com/public/res/public/34d3c879-64ca-436c-a486-43dd61f5fb58/MISCELLENEOUS/COLORS.xlsx",
        //            Content = "Hello@123"
        //        },
        //        new FileParam{
        //            Name = "yolv3.weights",
        //            Url="https://pjreddie.com/media/files/yolov3.weights",
        //            Content="Nothing"
        //        }
        //    };

        //    String url = "https://images.unsplash.com/photo-1543373014-cfe4f4bc1cdf?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxzZWFyY2h8Mnx8aGlnaCUyMHJlc29sdXRpb258ZW58MHx8MHx8&w=1000&q=80";


        //    Response.Headers.Add("Content-Disposition", "attachment; filename=" + compressedFileName + ".zip");
        //    Response.ContentType = "application/zip";

        //    ZipOutputStream zipOutputStream = new ZipOutputStream(Response.Body);
        //    foreach (FileParam fileName in filePaths)
        //    {

        //        ZipEntry zipEntry = new ZipEntry(fileName.Name);

        //        zipOutputStream.PutNextEntry(zipEntry);


        //        HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(fileName.Url);

        //        HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();

        //        System.Diagnostics.Debug.WriteLine("   ");
        //        System.Diagnostics.Debug.WriteLine("   ");
        //        System.Diagnostics.Debug.WriteLine("   ");
        //        System.Diagnostics.Debug.WriteLine("   ");

        //        System.Diagnostics.Debug.WriteLine("Reading Files " + fileName.Name);
        //        System.Diagnostics.Debug.WriteLine(myHttpWebResponse.ContentType);
        //        System.Diagnostics.Debug.WriteLine(myHttpWebResponse.ContentLength);
        //        System.Diagnostics.Debug.WriteLine(myHttpWebResponse.Headers);

        //        Stream receiveStream = myHttpWebResponse.GetResponseStream();
        //        Encoding encode = System.Text.Encoding.GetEncoding("utf-8");


        //        //StreamReader readStream = new StreamReader(receiveStream, encode);
        //        BinaryReader readStream = new BinaryReader(receiveStream, encode);



        //        byte[] buffer = new byte[1024];
        //        int sourceBytes = 0;
        //        int total = 0;

        //        String line = String.Empty;

        //        using (BinaryReader readstream = new BinaryReader(receiveStream))
        //        {
        //            do
        //            {
        //                sourceBytes = readstream.Read(buffer, 0, buffer.Length);
        //                if (sourceBytes != 0)
        //                {
        //                    //System.Diagnostics.Debug.WriteLine(Encoding.ASCII.GetString(buffer));
        //                    zipOutputStream.Write(buffer, 0, sourceBytes);
        //                    total += sourceBytes;
        //                }

        //            } while (sourceBytes > 0);
        //        }
        //        System.Diagnostics.Debug.WriteLine(fileName.Name + " Completed");
        //        System.Diagnostics.Debug.WriteLine(myHttpWebResponse.ContentLength + " Acc : " + total);
        //        zipOutputStream.CloseEntry();
        //    }
        //    zipOutputStream.Finish();

        //}

    }
}
