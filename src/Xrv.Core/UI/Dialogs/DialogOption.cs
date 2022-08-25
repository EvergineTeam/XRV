// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.MRTK.SDK.Features.UX.Components.Configurators;

namespace Xrv.Core.UI.Dialogs
{
    public class DialogOption
    {
        public DialogOption(string key)
        {
            this.Key = key;
            this.Configuration = new StandardButtonConfigurator();
        }

        public string Key { get; private set; }

        public StandardButtonConfigurator Configuration { get; private set; }
    }
}
