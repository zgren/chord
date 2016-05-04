﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DigitalPlatform;

namespace dp2Capo
{
    /// <summary>
    /// 用于进行基本管理和初始化的线程
    /// </summary>
    public class DefaultThread : ThreadBase
    {
        public DefaultThread()
        {
            this.PerTime = 60 * 1000;
        }

        // 工作线程每一轮循环的实质性工作
        public override void Worker()
        {
            if (this.Stopped == true)
                return;

            ServerInfo.BackgroundWork();
        }
    }

}