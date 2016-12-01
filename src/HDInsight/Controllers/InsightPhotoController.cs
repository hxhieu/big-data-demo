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
using Newtonsoft.Json;
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

        public InsightPhotoController(IHostingEnvironment hostingEnv)
        {
            _env = hostingEnv;

            var clusterCredentials = new BasicAuthenticationCloudCredentials { Username = ExistingClusterUsername, Password = ExistingClusterPassword };
            _jobClient = new HDInsightJobManagementClient(ExistingClusterUri, clusterCredentials);
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
