﻿using System;

namespace Samotorcan.HtmlUi.Core.Messages
{
    internal class CallNative
    {
        public Guid? CallbackId { get; set; }
        public string Name { get; set; }
        public string Json { get; set; }
    }
}
