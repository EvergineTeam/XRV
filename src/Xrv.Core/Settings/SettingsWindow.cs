using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.Settings
{
    public class SettingsWindow : Window
    {
        private TabControl tabControl;
        private ObservableCollection<Section> sections;

        public SettingsWindow()
        {
            this.sections = new ObservableCollection<Section>();
        }

        public IList<Section> Sections { get => this.sections; }


        protected override void OnActivated()
        {
            base.OnActivated();

            if (this.tabControl == null)
            {
                this.tabControl = this.Owner.FindComponentInChildren<TabControl>();
            }

            this.tabControl.SelectedItemChanged += this.TabControl_SelectedItemChanged;
            this.sections.CollectionChanged += this.Sections_CollectionChanged;
            this.tabControl.Items.Clear();
            this.InternalAddSections(this.sections); // We can have items added before this component has been attached
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (this.tabControl != null)
            {
                this.tabControl.SelectedItemChanged -= this.TabControl_SelectedItemChanged;
            }

            this.sections.CollectionChanged -= this.Sections_CollectionChanged;
        }

        private void TabControl_SelectedItemChanged(object sender, SelectedItemChangedEventArgs args)
        {
            if (args.Item.Data is Section section)
            {
                this.tabControl.Content = section.Contents.Invoke();
            }
        }

        private void Sections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.InternalAddSections(args.NewItems.OfType<Section>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.InternalRemoveSections(args.OldItems.OfType<Section>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.InternalClearSections();
                    break;
            }
        }

        private void InternalAddSections(IEnumerable<Section> sections)
        {
            if (this.tabControl == null)
            {
                return;
            }

            foreach (var section in sections)
            {
                this.tabControl.Items.Add(new TabItem
                {
                    Text = section.Name,
                    Data = section,
                });
            }
        }

        private void InternalRemoveSections(IEnumerable<Section> sections)
        {
            if (this.tabControl == null)
            {
                return;
            }

            foreach (var section in sections)
            {
                var tabItem = this.tabControl.Items.FirstOrDefault(item => item.Data == section);
                if (tabItem != null)
                {
                    this.tabControl.Items.Remove(tabItem);
                }
            }
        }

        private void InternalClearSections() => this.tabControl?.Items.Clear();
    }
}
