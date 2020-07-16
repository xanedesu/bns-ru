﻿using System;

namespace BNSLauncher.Shared.Infrastructure.Internet.Exceptions
{
    class NeedConfirmWithCode : Exception
    {
        public NeedConfirmWithCode(string message, string sessionId): base(message)
        {
            this.SessionId = sessionId;
        }

        public string SessionId { get; }
    }
}