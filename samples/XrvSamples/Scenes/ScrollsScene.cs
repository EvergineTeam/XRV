using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK;
using Evergine.MRTK.SDK.Features.UX.Components.Scrolls;
using Xrv.Core;

namespace XrvSamples.Scenes
{
    public class ScrollsScene : BaseScene
    {

        protected override void OnPostCreateXRScene()
        {
            var xrv = Application.Current.Container.Resolve<XrvService>();
            xrv.Initialize(this);


            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var scrollViewer = assetsService.Load<Prefab>(MRTKResourceIDs.Prefabs.ListView_weprefab);
            var scrollViewerEntity = scrollViewer.Instantiate();            

            var data = new ListViewData(3);
            for (int i = 0; i < 40; i++)
            {
                data.Add($"Column {i}.0", $"Column {i}.1", $"Column {i}.2");
            }

            scrollViewerEntity.AddComponent(new ListView()
            {
                DataSource = data,
                Render = new ListViewRender()
                                .AddColumn("Title1", 0.3f, TextCellRenderer.Instance)
                                .AddColumn("Title2", 0.3f, TextCellRenderer.Instance)
                                .AddColumn("Title2", 0.3f, TextCellRenderer.Instance),
                HeaderEnabled = true,
            });

            ////TextCellRenderer.Instance.Debug = true;

            this.Managers.EntityManager.Add(scrollViewerEntity);
        }
    }
}
