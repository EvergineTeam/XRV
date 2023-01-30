// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Azure;
using System;

namespace Evergine.Xrv.Core.Storage
{
    internal static class AzureCommon
    {
        public static bool EvaluateCacheUsageOnException(Exception exception)
        {
            RequestFailedException requestFailedException = null;
            if (exception is AggregateException aggregateException && aggregateException.InnerException is RequestFailedException innerException)
            {
                requestFailedException = innerException;
            }
            else if (exception is RequestFailedException failedException)
            {
                requestFailedException = failedException;
            }

            // ErrorCode is null for scenarios where there is no internet connection, but
            // also if account name is not properly set.
            return requestFailedException != null && requestFailedException.ErrorCode == null;
        }
    }
}
