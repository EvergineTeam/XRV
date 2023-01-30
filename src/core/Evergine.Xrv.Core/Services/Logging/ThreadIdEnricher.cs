// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Serilog.Core;
using Serilog.Events;
using System.Threading;

namespace Evergine.Xrv.Core.Services.Logging
{
    internal class ThreadIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var property = propertyFactory.CreateProperty("ThreadID", Thread.CurrentThread.ManagedThreadId.ToString("D4"));
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}
