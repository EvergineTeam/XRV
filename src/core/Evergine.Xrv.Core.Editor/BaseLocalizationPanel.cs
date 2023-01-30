using Evergine.Editor.Extension;
using Evergine.Editor.Extension.Attributes;
using System;
using System.Linq;
using Evergine.Xrv.Core.Localization;

namespace Evergine.Xrv.Core.Editor
{
    [CustomPanelEditor(typeof(BaseLocalization))]
    public class BaseLocalizationPanel : PanelEditor
    {
        private static LocalizationService localizationService;

        static BaseLocalizationPanel()
        {
            localizationService = new LocalizationService();
            localizationService.LoadSearchAssemblies();
        }

        public new BaseLocalization Instance => (BaseLocalization)base.Instance;

        public override void GenerateUI()
        {
            base.GenerateUI();

            this.Instance.DictionaryNameChanged += Instance_DictionaryNameChanged;

            var availableDictionaries = localizationService
                .GetAllDictionaries()
                .OrderBy(name => name)
                .ToList();
            availableDictionaries.Insert(0, string.Empty);

            this.propertyPanelContainer.AddSelector(
                nameof(BaseLocalization.DictionaryName),
                nameof(BaseLocalization.DictionaryName),
                availableDictionaries,
                () => this.Instance.DictionaryName,
                x => this.Instance.DictionaryName = x);

            this.TryLoadDictionaryKeys(this.Instance.DictionaryName, out var keys);
            this.propertyPanelContainer.AddSelector(
                nameof(BaseLocalization.DictionaryKey),
                nameof(BaseLocalization.DictionaryKey),
                keys,
                () => this.Instance.DictionaryKey,
                x => this.Instance.DictionaryKey = x);
        }

        private void Instance_DictionaryNameChanged(object sender, EventArgs e) =>

            this.propertyPanelContainer.InvalidateLayout();

        private bool TryLoadDictionaryKeys(string dictionaryName, out string[] keys)
        {
            if (string.IsNullOrWhiteSpace(dictionaryName))
            {
                keys = new string[0];
                return false;
            }

            bool succeded = false;

            try
            {
                keys = localizationService
                    .GetAllKeysByDictionary(dictionaryName)
                    .OrderBy(name => name)
                    .ToArray();
                succeded= true;
            }
            catch (InvalidOperationException)
            {
                keys = new string[0];
            }

            return succeded;
        }
    }
}
