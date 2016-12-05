using HDInsight.Models;
using Hyak.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.HDInsight.Job;
using Microsoft.Azure.Management.HDInsight.Job.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Bigquery.v2;
using Google.Apis.Bigquery.v2.Data;
using System.Data;
using Google.Apis.Services;
using Google.Apis.Http;
using Newtonsoft.Json;
using HDInsight.Helpers;

namespace HDInsight.Controllers
{
    [Route("api/[controller]")]
    public class InsightPhotoController : Controller
    {
        private readonly HDInsightJobManagementClient _jobClient;
        private readonly IHostingEnvironment _env;


        private const string ExistingClusterName = "patrickquang";
        private const string ExistingClusterUri = ExistingClusterName + ".azurehdinsight.net";
        private const string ExistingClusterUsername = "admin";
        private const string ExistingClusterPassword = @"Qu@nght198412";

        private const string DefaultStorageAccountName = "storage4hd";
        private const string DefaultStorageAccountKey = "LPn1Z6oOoNLyTTjVDN06JBMFd1nk593LCudoCIXEOwsIIBhOOTTK3ixjTQXeSTBfGSGol7kmYEbJ0EFvELzNJg==";
        private const string DefaultStorageContainerName = "hdspark";

        private string bigqueryFileKey = @"..\BigDataTest-e2121ee293bd.json";
        private string bigqueryApplicationName = "BigDataTest";
        private string bigqueryProjectId = "bigdatatest-151303";

        public InsightPhotoController(IHostingEnvironment hostingEnv)
        {
            _env = hostingEnv;

            var clusterCredentials = new BasicAuthenticationCloudCredentials { Username = ExistingClusterUsername, Password = ExistingClusterPassword };
            _jobClient = new HDInsightJobManagementClient(ExistingClusterUri, clusterCredentials);
        }
        [Route("AddPhotoBigQuery")]
        public JsonResult AddPhotoBigQuery([FromBody] Photo photo)
        {
            try
            {
                return Json(new HandledJsonResult { Data = AddNewPhotoBigQuery(photo) });

            }
            catch (Exception ex)
            {
                return Json(new HandledJsonResult(ex));
            }
        }
        private string AddNewPhotoBigQuery(Photo photo)
        {
            try
            {
                GoogleCredential credential;
                using (Stream stream = new FileStream(bigqueryFileKey, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    credential = GoogleCredential.FromStream(stream);
                }

                string[] scopes = new string[] {
                    BigqueryService.Scope.Bigquery,
                    BigqueryService.Scope.CloudPlatform,
                };
                credential = credential.CreateScoped(scopes);
                BaseClientService.Initializer initializer = new BaseClientService.Initializer()
                {
                    HttpClientInitializer = (IConfigurableHttpClientInitializer)credential,
                    ApplicationName = bigqueryApplicationName,
                    GZipEnabled = true,
                };
                BigqueryService service = new BigqueryService(initializer);
                var rowList = new List<TableDataInsertAllRequest.RowsData>();
                // Check @ https://developers.google.com/bigquery/streaming-data-into-bigquery for InsertId usage
                var row = new TableDataInsertAllRequest.RowsData();
                row.Json = new Dictionary<string, object>();
                row.Json.Add("Id", photo.Id);
                row.Json.Add("Title", photo.Title);
                row.Json.Add("Url", photo.Url);
                rowList.Add(row);

                var content = new TableDataInsertAllRequest();
                content.Rows = rowList;
                content.Kind = "bigquery#tableDataInsertAllRequest";
                content.IgnoreUnknownValues = true;
                content.SkipInvalidRows = true;
                var requestResponse = service.Tabledata.InsertAll(content, bigqueryProjectId, "dsbigquery", "Photo").Execute();



                return "true";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        [Route("GetTop10PhotoBigQuery")]
        public JsonResult GetTop10PhotoBigQuery()
        {

            try
            {
                GoogleCredential credential;
                using (Stream stream = new FileStream(bigqueryFileKey, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    credential = GoogleCredential.FromStream(stream);
                }

                string[] scopes = new string[] {
                    BigqueryService.Scope.Bigquery,
                    BigqueryService.Scope.CloudPlatform,
                };
                credential = credential.CreateScoped(scopes);
                BaseClientService.Initializer initializer = new BaseClientService.Initializer()
                {
                    HttpClientInitializer = (IConfigurableHttpClientInitializer)credential,
                    ApplicationName = bigqueryApplicationName,
                    GZipEnabled = true,
                };
                BigqueryService service = new BigqueryService(initializer);
                var dt = ExecuteSQLQuery(service, bigqueryProjectId, "SELECT Id, Title, Url FROM [dsbigquery.Photo]  limit 10");
                if (dt != null && dt.Rows.Count > 0) {
                    List<Photo> listPhoto = DataHelper.DataTableToList<Photo>(dt);
                    return Json(new HandledJsonResult { Data = listPhoto });
                }    
                return Json(new HandledJsonResult { Data = "No Data" });
            }
            catch (Exception ex)
            {
                return Json(new HandledJsonResult(ex));
            }
        }
        
        public DataTable ExecuteSQLQuery(BigqueryService bqservice, String ProjectID, string sSql)
        {
            QueryRequest _r = new QueryRequest();
            _r.Query = sSql;
            QueryResponse _qr = bqservice.Jobs.Query(_r, ProjectID).Execute();
            DataTable dt = new DataTable();
            string pageToken = null;
            while (!(bool)_qr.JobComplete)
            {
                Thread.Sleep(1000);

            }
            //job not finished yet! expecting more data
            while (true)
            {
                var resultReq = bqservice.Jobs.GetQueryResults(_qr.JobReference.ProjectId, _qr.JobReference.JobId);
                resultReq.PageToken = pageToken;
                var result = resultReq.Execute();
                while (!(bool)result.JobComplete)
                {
                    Thread.Sleep(1000);
                        
                }
                if (result.JobComplete == true)
                {
                    if (dt.Columns.Count == 0)
                    {
                        foreach (var Column in result.Schema.Fields)
                        {
                            dt.Columns.Add(Column.Name);
                        }
                    }
                    foreach (TableRow row in result.Rows)
                    {
                        DataRow dr = dt.NewRow();

                        for (var i = 0; i < dt.Columns.Count; i++)
                        {
                            dr[i] = row.F[i].V;
                        }

                        dt.Rows.Add(dr);
                    }
                        
                    pageToken = result.PageToken;
                    if (pageToken == null)
                        break;
                }
            }
            return dt;
        }

        [Route("add")]
        public JsonResult AddNewPhoto([FromBody] Photo photo)
        {
            try
            {
                return Json(new HandledJsonResult { Data = AddNewPhotoHiveJob(photo) });

            }
            catch (Exception ex)
            {
                return Json(new HandledJsonResult(ex));
            }
        }
        [Route("GetTop10Photo")]
        public JsonResult GetTop10Photo()
        {
            try
            {
                Dictionary<string, string> defines = new Dictionary<string, string> { { "hive.execution.engine", "tez" }, { "hive.exec.reducers.max", "1" } };
                var parameters = new HiveJobSubmissionParameters
                {
                    Query = "SELECT * FROM Photo LIMIT 10;",
                    Defines = defines,
                    Arguments = null
                };
               
                var jobResponse = _jobClient.JobManagement.SubmitHiveJob(parameters);
                var jobId = jobResponse.JobSubmissionJsonResponse.Id;
                // Wait for job completion
                var jobDetail = _jobClient.JobManagement.GetJob(jobId).JobDetail;
                while (!jobDetail.Status.JobComplete)
                {
                    Thread.Sleep(1000);
                    jobDetail = _jobClient.JobManagement.GetJob(jobId).JobDetail;
                }

                // Get job output
                var storageAccess = new AzureStorageAccess(DefaultStorageAccountName, DefaultStorageAccountKey,
                    DefaultStorageContainerName);
                IList<Photo> listPhoto = new List<Photo>();
                Stream output;
                if (jobDetail.ExitValue == 0)
                {
                    output=  _jobClient.JobManagement.GetJobOutput(jobId, storageAccess);
                    using (var reader = new StreamReader(output, Encoding.UTF8))
                    {
                         string[] lines = reader.ReadToEnd().Split("\n".ToCharArray());
                        foreach(var line in lines)
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                string[] splitContent = line.Split("\t".ToCharArray());
                                Photo photo = new Photo();
                                int id;
                                int.TryParse(splitContent[0], out id);
                                photo.Id = id;
                                photo.Title = splitContent[1];
                                photo.Url = splitContent[2];
                                listPhoto.Add(photo);
                            }
                            
                        }                        
                    }
                    return Json(new HandledJsonResult { Data = listPhoto });
                }
                else
                {
                    string message;
                    output = _jobClient.JobManagement.GetJobErrorLogs(jobId, storageAccess);
                    using (var reader = new StreamReader(output, Encoding.UTF8))
                    {
                        message = reader.ReadToEnd();

                    }
                    return Json(new HandledJsonResult { Data = message });
                }
            }
            catch (Exception ex)
            {
                return Json(new HandledJsonResult(ex));
            }
        }
        private string AddNewPhotoHiveJob(Photo photo)
        {
            Dictionary<string, string> defines = new Dictionary<string, string> { { "hive.execution.engine", "tez" }, { "hive.exec.reducers.max", "1" } };
            List<string> args = new List<string> {
                { "--hiveconf" },
                { $"Id={photo.Id}" },
                { "--hiveconf" },
                { $"Title={photo.Title}" },
                { "--hiveconf" },
                { $"Url={photo.Url}" }
                };
            var parameters = new HiveJobSubmissionParameters
            {
                Query = "INSERT INTO TABLE Photo VALUES(${hiveconf:Id}, '${hiveconf:Title}' , '${hiveconf:Url}');", //"INSERT INTO TABLE Photo VALUES('${hiveconf:Id}', '${hiveconf:Title}' , '${hiveconf:Url}');",
                Defines = defines,
                Arguments = args
            };
            var jobResponse = _jobClient.JobManagement.SubmitHiveJob(parameters);
            var jobId = jobResponse.JobSubmissionJsonResponse.Id;
            // Wait for job completion
            var jobDetail = _jobClient.JobManagement.GetJob(jobId).JobDetail;
            while (!jobDetail.Status.JobComplete)
            {
                Thread.Sleep(1000);
                jobDetail = _jobClient.JobManagement.GetJob(jobId).JobDetail;
            }

            // Get job output
            var storageAccess = new AzureStorageAccess(DefaultStorageAccountName, DefaultStorageAccountKey,
                DefaultStorageContainerName);
            IList<Photo> listPhoto = new List<Photo>();
            var result = "";
            Stream output ; 
            if (jobDetail.ExitValue == 0) result = "success";
            else
            {
                output = _jobClient.JobManagement.GetJobErrorLogs(jobId, storageAccess);
                using (var reader = new StreamReader(output, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                    
                }
            }
            
            return result;
        }
    }

}
