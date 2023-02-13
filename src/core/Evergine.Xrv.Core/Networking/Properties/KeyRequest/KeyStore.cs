// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Linq;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Networking.Components;
using Evergine.Xrv.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.Core.Networking.Properties.KeyRequest
{
    internal class KeyStore : UpdatableService, IKeyStore
    {
        private readonly Dictionary<NetworkPropertyProviderFilter, Dictionary<byte, KeyRegister>> reservedProperties;
        private readonly Dictionary<Guid, NetworkPropertyProviderFilter> correlationIdsToFilter;

        private readonly object lockObject = new object();
        private readonly List<byte> keysToFlush;

        [BindService]
        private XrvService xrvService = null;

        private TimeSpan lastFlushTime;
        private HashSet<byte> coreKeys;
        private ILogger logger;

        private static Guid coreKeyCorrelation = Guid.Empty;

        public KeyStore()
        {
            this.reservedProperties = new Dictionary<NetworkPropertyProviderFilter, Dictionary<byte, KeyRegister>>();
            this.reservedProperties[NetworkPropertyProviderFilter.Room] = new Dictionary<byte, KeyRegister>();
            this.reservedProperties[NetworkPropertyProviderFilter.Player] = new Dictionary<byte, KeyRegister>();
            this.correlationIdsToFilter = new Dictionary<Guid, NetworkPropertyProviderFilter>();
            this.keysToFlush = new List<byte>();
            this.coreKeys = new HashSet<byte>();
            this.correlationIdsToFilter[coreKeyCorrelation] = NetworkPropertyProviderFilter.Room;
        }

        public TimeSpan KeyReservationTime { get; set; } = TimeSpan.FromSeconds(5);

        public override void Update(TimeSpan gameTime)
        {
            this.lastFlushTime = this.lastFlushTime + gameTime;
            if (this.lastFlushTime >= this.KeyReservationTime)
            {
                _ = EvergineBackgroundTask.Run(this.Flush);
            }
        }

        public KeyRegister[] ReserveKeys(
            byte numberOfKeys,
            Guid correlationId,
            int reservedByClientId,
            NetworkPropertyProviderFilter filter)
        {
            using (this.logger?.BeginScope("Networking key store"))
            using (this.logger?.BeginScope("Keys reservation"))
            {
                lock (this.lockObject)
                {
                    var dictionary = this.GetDictionaryByFilter(filter);
                    var available = byte.MaxValue - dictionary.Count;
                    if (available < numberOfKeys)
                    {
                        throw new FullKeyStoreException($"There are not available keys for {filter}");
                    }

                    var reservedKeys = new KeyRegister[numberOfKeys];
                    int currentIndex = 0;

                    for (byte currentKey = 0x00; currentKey < byte.MaxValue; currentKey++)
                    {
                        if (!dictionary.ContainsKey(currentKey))
                        {
                            var register = new KeyRegister
                            {
                                Key = currentKey,
                                CorrelationId = correlationId,
                                ReservedByClientId = reservedByClientId,
                                ExpiresOn = DateTime.UtcNow.Add(this.KeyReservationTime),
                            };
                            reservedKeys[currentIndex++] = register;
                            dictionary[currentKey] = register;
                            this.logger?.LogDebug($"Reserved key {register.Key} for correlation {register.CorrelationId}");
                        }

                        if (currentIndex == numberOfKeys)
                        {
                            break;
                        }
                    }

                    this.correlationIdsToFilter[correlationId] = filter;

                    return reservedKeys;
                }
            }
        }

        public void ConfirmKeys(Guid correlationId, int reservedByClientId)
        {
            using (this.logger?.BeginScope("Networking key store"))
            using (this.logger?.BeginScope("Keys confirmation"))
            {
                lock (this.lockObject)
                {
                    this.logger?.LogDebug($"Request received for correlation {correlationId}");

                    var dictionary = this.GetDictionaryByCorrelationId(correlationId);
                    var targetEntries = dictionary.Values.Where(v => v.CorrelationId == correlationId);
                    bool anyEntry = targetEntries.Any();
                    if (!targetEntries.Any())
                    {
                        this.logger?.LogWarning($"Not succeeded: no entries found for correlation {correlationId}");
                        throw new KeysToConfirmNotAvailableException();
                    }

                    var confirmedInTime = targetEntries.All(entry =>
                        entry.ReservedByClientId == reservedByClientId
                        &&
                        entry.ExpiresOn >= DateTime.UtcNow);

                    if (!confirmedInTime)
                    {
                        this.logger?.LogWarning($"Not succeeded: some of the keys reservation time expired for correlation {correlationId}");
                        throw new KeysToConfirmNotAvailableException();
                    }

                    foreach (var entry in targetEntries)
                    {
                        entry.ExpiresOn = null;
                    }

                    this.logger?.LogDebug($"Succeeded for correlation {correlationId}");
                }
            }
        }

        public void FreeKeys(Guid correlationId, int reservedByClientId)
        {
            using (this.logger?.BeginScope("Networking key store"))
            using (this.logger?.BeginScope("Releasing keys"))
            {
                lock (this.lockObject)
                {
                    this.logger?.LogDebug($"Request received for correlation {correlationId}");

                    var dictionary = this.GetDictionaryByCorrelationId(correlationId);
                    var targetEntries = dictionary.Values.Where(v => v.CorrelationId == correlationId && v.ReservedByClientId == reservedByClientId);
                    foreach (var entry in targetEntries)
                    {
                        dictionary.Remove(entry.Key);
                    }

                    this.correlationIdsToFilter.Remove(correlationId);
                }
            }
        }

        public void Flush()
        {
            lock (this.lockObject)
            {
                this.FlushDictionary(this.reservedProperties[NetworkPropertyProviderFilter.Room]);
                this.FlushDictionary(this.reservedProperties[NetworkPropertyProviderFilter.Player]);
            }
        }

        public void Clear()
        {
            lock (this.lockObject)
            {
                this.keysToFlush.Clear();
                var dictionary = this.GetDictionaryByFilter(NetworkPropertyProviderFilter.Player);
                dictionary.Clear();

                dictionary = this.GetDictionaryByFilter(NetworkPropertyProviderFilter.Room);
                var keysToRemove = dictionary
                    .Where(kvp => kvp.Value.CorrelationId != coreKeyCorrelation)
                    .ToList();
                foreach (var toRemove in keysToRemove)
                {
                    dictionary.Remove(toRemove.Key);
                }

                var correlationsToRemove = this.correlationIdsToFilter
                    .Where(kvp => kvp.Key != coreKeyCorrelation)
                    .ToList();
                foreach (var toRemove in correlationsToRemove)
                {
                    this.correlationIdsToFilter.Remove(toRemove.Key);
                }
            }
        }

        internal void ReserveKeyForCore(byte key) => this.ReserveKeysForCore(new[] { key });

        internal void ReserveKeysForCore(byte[] keys)
        {
            if (this.coreKeys.Intersect(keys).Any())
            {
                throw new InvalidOperationException("Some of the keys are already registered");
            }

            this.coreKeys.AddRange(keys);
            var dictionary = this.GetDictionaryByFilter(NetworkPropertyProviderFilter.Room);
            foreach (var key in keys)
            {
                dictionary[key] = new KeyRegister
                {
                    CorrelationId = coreKeyCorrelation,
                    Key = key,
                    ReservedByClientId = -1,
                };
            }
        }

        internal Dictionary<byte, KeyRegister> GetDictionaryByFilter(NetworkPropertyProviderFilter filter)
        {
            if (filter == NetworkPropertyProviderFilter.Any)
            {
                throw new ArgumentOutOfRangeException(nameof(filter), "NetworkPropertyProviderFilter.Any not allowed");
            }

            return this.reservedProperties[filter];
        }

        internal Dictionary<byte, KeyRegister> GetDictionaryByCorrelationId(Guid correlationId)
        {
            var filter = this.correlationIdsToFilter[correlationId];
            return this.GetDictionaryByFilter(filter);
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        private void FlushDictionary(Dictionary<byte, KeyRegister> dictionary)
        {
            this.keysToFlush.Clear();

            foreach (var entry in dictionary)
            {
                KeyRegister registration = entry.Value;
                if (!registration.IsConfirmed && registration.ExpiresOn < DateTime.UtcNow)
                {
                    this.keysToFlush.Add(registration.Key);
                }
            }

            this.keysToFlush.ForEach(key => dictionary.Remove(key));
        }
    }
}
