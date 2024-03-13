// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Evergine.Framework;

namespace Evergine.Xrv.Core.UI.Buttons
{
    internal class OrderedButtonsIterator : IEnumerable<Entity>
    {
        private readonly IEnumerable<ButtonDescription> descriptors;
        private readonly Dictionary<Guid, Entity> instances;

        public OrderedButtonsIterator(IEnumerable<ButtonDescription> descriptors, Dictionary<Guid, Entity> instances)
        {
            this.descriptors = descriptors;
            this.instances = instances;
        }

        public IEnumerator<Entity> GetEnumerator()
        {
            foreach (var descriptor in this.descriptors.OrderBy(d => d.Order))
            {
                if (this.instances.ContainsKey(descriptor.Id))
                {
                    yield return this.instances[descriptor.Id];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
