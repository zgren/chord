﻿using dp2Command.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dp2Command.Service
{
    // API返回结果
    public class ApiResult
    {
        public string errorInfo = "";

        /// <summary>
        /// -1:表示出错
        /// </summary>
        public int errorCode = 0;
    }

    public class WxUserResult
    {
        public WxUserItem userItem { get; set; }
        public ApiResult apiResult { get; set; }
    }
}
