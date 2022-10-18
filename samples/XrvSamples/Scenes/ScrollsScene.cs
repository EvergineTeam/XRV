using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK;
using Evergine.MRTK.SDK.Features.UX.Components.Scrolls;
using System.Linq;
using Xrv.Core;

namespace XrvSamples.Scenes
{
    public class ScrollsScene : BaseScene
    {

        protected override void OnPostCreateXRScene()
        {
            var xrv = Application.Current.Container.Resolve<XrvService>();
            xrv.Initialize(this);

            // ListView               
            var listView = this.Managers.EntityManager.FindAllByTag("ListView").First();


            var data = new ListViewData(3);
            for (int i = 0; i < 40; i++)
            {
                data.Add($"Column {i}.0", $"Column {i}.1", $"Column {i}.2");
            }

            var listViewComponent = listView.FindComponent<ListView>();

            if (listViewComponent != null)
            {
                listViewComponent.DataSource = data;
                listViewComponent.Render = new ListViewRender()
                                .AddColumn("Title1", 0.3f, TextCellRenderer.Instance)
                                .AddColumn("Title2", 0.3f, TextCellRenderer.Instance)
                                .AddColumn("Title2", 0.3f, TextCellRenderer.Instance);                
            }

            // ScrollView
            var scrollView = this.Managers.EntityManager.FindAllByTag("ScrollView").First();
            
        }
    }
}
