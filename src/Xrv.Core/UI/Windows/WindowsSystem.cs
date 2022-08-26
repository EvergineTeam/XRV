// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using System.Collections.Generic;
using Xrv.Core.UI.Dialogs;

namespace Xrv.Core.UI.Windows
{
    public class WindowsSystem
    {
        private readonly EntityManager entityManager;
        private readonly AssetsService assetsService;

        public IEnumerable<Window> AllWindows
        {
            get => this.entityManager.FindComponentsOfType<Window>(isExactType: false);
        }

        public WindowsSystem(EntityManager entityManager, AssetsService assetsService)
        {
            this.entityManager = entityManager;
            this.assetsService = assetsService;
            this.Distances = new Distances();
        }

        public Distances Distances { get; private set; }

        public Material OverrideIconMaterial { get; set; }

        public Window ShowWindow()
        {
            var windowEntity = this.BuildWindow(new Window(), (BaseWindowConfigurator)null);
            this.entityManager.Add(windowEntity);

            return windowEntity.FindComponent<Window>();
        }

        public AlertDialog ShowAlertDialog(string title, string text, string acceptText)
        {
            var owner = this.CreateDialogAux(new AlertDialog(), title, text);
            var dialog = owner.FindComponent<AlertDialog>();
            dialog.AcceptOption.Configuration.Text = acceptText;

            this.entityManager.Add(owner);

            var contentPrefab = this.GetBaseDialogPrefab();
            dialog.Configurator.Content = contentPrefab.Instantiate();
            dialog.Open();

            return dialog;
        }

        public ConfirmDialog ShowConfirmDialog(string title, string text, string cancelText, string acceptText)
        {
            var owner = this.CreateDialogAux(new ConfirmDialog(), title, text);
            var dialog = owner.FindComponent<ConfirmDialog>();
            dialog.CancelOption.Configuration.Text = cancelText;
            dialog.AcceptOption.Configuration.Text = acceptText;

            this.entityManager.Add(owner);

            var contentPrefab = this.GetBaseDialogPrefab();
            dialog.Configurator.Content = contentPrefab.Instantiate();
            dialog.Open();

            return dialog;
        }

        public Entity BuildWindow<TWindow, TConfigurator>(TWindow instance, TConfigurator configInstance)
            where TWindow : Window
            where TConfigurator : BaseWindowConfigurator
        {
            var prefab = this.GetWindowPrefab();
            var windowEntity = prefab.Instantiate();
            windowEntity.AddComponent(instance);

            if (configInstance != default(TConfigurator))
            {
                var configurator = windowEntity.FindComponent<BaseWindowConfigurator>(isExactType: false);
                windowEntity.RemoveComponent(configurator);
                windowEntity.AddComponent(configInstance);
            }
            else if (this.OverrideIconMaterial != null)
            {
                var configurator = windowEntity.FindComponent<WindowConfigurator>(isExactType: false);
                configurator.LogoMaterial = this.OverrideIconMaterial;
            }

            windowEntity.IsEnabled = false;

            return windowEntity;
        }

        private Entity CreateDialogAux<TDialog>(TDialog dialog, string title, string text)
            where TDialog : Dialog
        {
            const float DialogWidth = 0.2f;
            const float DialogHeight = 0.11f;

            var dialogConfigurator = new DialogConfigurator
            {
                Title = title,
                Text = text,
            };
            var owner = this.BuildWindow(dialog, dialogConfigurator);
            dialog.AllowPin = false;
            dialog.EnableManipulation = false;

            var size = dialogConfigurator.Size;
            size.X = DialogWidth;
            size.Y = DialogHeight;
            dialogConfigurator.Size = size;
            dialogConfigurator.FrontPlateSize = size;
            var offset = dialogConfigurator.FrontPlateOffsets;
            offset.X = 0;
            offset.Y = 0;
            dialogConfigurator.FrontPlateOffsets = offset;
            dialogConfigurator.DisplayLogo = false;

            return owner;
        }

        private Prefab GetWindowPrefab() =>
            this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.Window);

        private Prefab GetBaseDialogPrefab() =>
            this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.BaseDialogContents);
    }
}
