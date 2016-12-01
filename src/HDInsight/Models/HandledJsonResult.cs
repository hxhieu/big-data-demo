using Microsoft.AspNetCore.Mvc;
using System;

namespace HDInsight.Models
{
    public class HandledJsonResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public object Data { get; set; }

        public HandledJsonResult()
        {
            IsSuccess = true;
        }

        public HandledJsonResult(Exception ex)
        {
            IsSuccess = false;
            ErrorMessage = ex.Message;
        }
    }
}
