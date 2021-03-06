﻿using ilovelibrary.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace ilovelibrary.ApiControllers
{
    public class PatronController : ApiController
    {
        // 参数值常量
        public const string C_format_summary = "summary";
        public const string C_format_borrowinfo = "borrowinfo";
        public const string C_format_verifyBarcode = "verifyBarcode";

        /// <summary>
        /// 获得读者基本信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [NotImplExceptionFilter]
        public PatronResult GetPatron(string id)
        {
            if (HttpContext.Current.Session[SessionInfo.C_Session_sessioninfo] == null)
            {
                throw new Exception("尚未登录!");
            }
            SessionInfo sessionInfo = (SessionInfo)HttpContext.Current.Session[SessionInfo.C_Session_sessioninfo];
            
            // 获取读者基本信息
            PatronResult patronResult = ilovelibraryServer.Instance.GetPatronInfo(sessionInfo, id);
            return patronResult;
        }

        /// <summary>
        /// 获得读者的借阅信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public object GetPatronInfo(string id, [FromUri] string format)
        {            
            if (HttpContext.Current.Session[SessionInfo.C_Session_sessioninfo] == null)
            {
                throw new Exception("尚未登录");
            }
            SessionInfo sessionInfo = (SessionInfo)HttpContext.Current.Session[SessionInfo.C_Session_sessioninfo] ;
            /*
            // 取在借册
            if (format == C_format_borrowinfo)
            {
                BorrowInfoResult result = ilovelibraryServer.Instance.GetBorrowInfo(sessionInfo, id);
                return result;
            }
             */

            // 取summary
            if (format == C_format_summary)
            {

                //id=HtmlEncoding 
                return  ilovelibraryServer.Instance.GetPatronSummary(sessionInfo, id);
            }

            if (format == C_format_verifyBarcode)
            {
                //C_format_verifyBarcode
                return ilovelibraryServer.Instance.VerifyBarcode(sessionInfo, id);
            }

            return "";
        }


    }


}
