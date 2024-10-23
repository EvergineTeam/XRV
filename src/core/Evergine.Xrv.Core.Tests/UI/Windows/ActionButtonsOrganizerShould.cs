using Evergine.Framework;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.UI.Buttons;
using Evergine.Xrv.Core.UI.Windows;
using Moq;
using Xunit;

namespace Evergine.Xrv.Core.Tests.UI.Windows
{
    public class ActionButtonsOrganizerShould
    {
        private readonly ActionButtonsOrganizer organizer;
        private readonly Mock<LocalizationService> localization;

        public ActionButtonsOrganizerShould()
        {
            this.localization = new Mock<LocalizationService>();
            this.organizer = new ActionButtonsOrganizer()
            {
                AvailableActionSlots = 3,
            };
            this.organizer.Initialize(
                (_, __) => new Entity().AddComponent(new ButtonCursorFeedback()), 
                this.localization.Object);
        }

        [Fact]
        public void HaveNoButtonDefinitionsWhenInstantiated()
        {
            Assert.Empty(this.organizer.ExtraButtons);
        }

        [Fact]
        public void AddInitialButtonInstancesToContainer()
        {
            Assert.Equal(2, this.organizer.ActionBarButtons.Count);
            Assert.Same(this.organizer.FollowButtonEntity, this.organizer.ActionBarButtons[0]);
            Assert.Same(this.organizer.CloseButtonEntity, this.organizer.ActionBarButtons[1]);
        }

        [Fact]
        public void RegisterNewButtonDefinitions()
        {
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1" });

            Assert.Equal(1, this.organizer.ExtraButtons.Count);
        }

        [Fact]
        public void CreateMoreActionsButtonWhenNoSlotsAvailable()
        {
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1" });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2" });

            Assert.Equal(2, this.organizer.ExtraButtons.Count);
            Assert.True(this.organizer.HasMoreActionsButton);
        }

        [Fact]
        public void CreateMoreActionEntriesWhenNoSlotsAvailable()
        {
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1" });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2" });
            
            Assert.Equal(2, this.organizer.ExtraButtons.Count);
            Assert.Equal(3, this.organizer.ActionBarButtons.Count);
            Assert.Equal(2, this.organizer.MoreActionButtons.Count);
        }

        [Fact]
        public void ConsiderButtonOrderValuesForActionBar()
        {
            this.organizer.AvailableActionSlots = 5;
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1", Order = 1 });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2", Order = 0 });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 3", Order = 2 });

            Assert.Equal("extra 2", this.organizer.ActionBarButtons[0].Name);
            Assert.Equal("extra 1", this.organizer.ActionBarButtons[1].Name);
            Assert.Equal("extra 3", this.organizer.ActionBarButtons[2].Name);
            Assert.Same(this.organizer.FollowButtonEntity, organizer.ActionBarButtons[3]);
            Assert.Same(this.organizer.CloseButtonEntity, organizer.ActionBarButtons[4]);
        }

        [Fact]
        public void ConsiderButtonOrderValuesForMoreActionsList()
        {
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1", Order = 1 });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2", Order = 0 });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 3", Order = 2 });

            Assert.Same(this.organizer.MoreActionsButtonEntity, this.organizer.ActionBarButtons[0]);
            Assert.Same(this.organizer.FollowButtonEntity, this.organizer.ActionBarButtons[1]);
            Assert.Same(this.organizer.CloseButtonEntity, this.organizer.ActionBarButtons[2]);
            Assert.Equal("extra 2", this.organizer.MoreActionButtons[0].Name);
            Assert.Equal("extra 1", this.organizer.MoreActionButtons[1].Name);
            Assert.Equal("extra 3", this.organizer.MoreActionButtons[2].Name);
        }

        [Fact]
        public void StopAddingMoreActionButtonsWhenNoLongerRequired()
        {
            var toRemove = new ButtonDescription { Name = "extra 1" };
            this.organizer.ExtraButtons.Add(toRemove);
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2" });
            this.organizer.ExtraButtons.Remove(toRemove);

            Assert.Equal(1, this.organizer.ExtraButtons.Count);
            Assert.Equal(3, this.organizer.ActionBarButtons.Count);
            Assert.False(this.organizer.HasMoreActionsButton);
            Assert.Empty(this.organizer.MoreActionButtons);
        }

        [Fact]
        public void RecreateInitialButtonsOnCollectionClear()
        {
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1", Order = 1 });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2", Order = 2 });
            this.organizer.ExtraButtons.Clear();

            Assert.Equal(0, this.organizer.ExtraButtons.Count);
            Assert.Equal(2, this.organizer.ActionBarButtons.Count);
        }

        [Fact]
        public void RecalculateOrganizationOnSlotsNumberChange()
        {
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1", Order = 1 });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2", Order = 2 });
            this.organizer.AvailableActionSlots = 5;
            Assert.Equal(4, this.organizer.ActionBarButtons.Count);
            Assert.Empty(this.organizer.MoreActionButtons);
        }

        [Fact]
        public void NotAddCloseButtonWhenIndicated()
        {
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1", Order = 1 });
            this.organizer.IncludeCloseButton = false;
            Assert.Equal(2, this.organizer.ActionBarButtons.Count);
        }

        [Fact]
        public void NotAddFollowButtonWhenIndicated()
        {
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1", Order = 1 });
            this.organizer.IncludeFollowButton = false;
            Assert.Equal(2, this.organizer.ActionBarButtons.Count);
        }

        [Fact]
        public void ReturnIndexedActionBarButton()
        {
            var description = new ButtonDescription { Name = "extra 1" };
            this.organizer.ExtraButtons.Add(description);

            var indexed = this.organizer[description];
            Assert.NotNull(indexed);
        }

        [Fact]
        public void ReturnIndexedMoreActionsButton()
        {
            var description = new ButtonDescription { Name = "extra more" };
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1", Order = 1 });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2", Order = 1 });
            this.organizer.ExtraButtons.Add(description);

            var indexed = this.organizer[description];
            Assert.NotNull(indexed);
        }

        [Fact]
        public void PlaceMoreActionButtonsBeforeRestOfButtons()
        {
            this.organizer.AvailableActionSlots = 4;
            this.organizer.MoreActionsPlacement = MoreActionsButtonPlacement.BeforeActionButtons;
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 1" });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 2" });
            this.organizer.ExtraButtons.Add(new ButtonDescription { Name = "extra 3" });

            Assert.Same(this.organizer.MoreActionsButtonEntity, this.organizer.ActionBarButtons[0]);
            Assert.Same(this.organizer.FollowButtonEntity, this.organizer.ActionBarButtons[2]);
            Assert.Same(this.organizer.CloseButtonEntity, this.organizer.ActionBarButtons[3]);
        }

        [Fact]
        public void GetButtonDescriptionByEntity()
        {
            var expectedDescription = new ButtonDescription { Name = "extra 1" };
            this.organizer.ExtraButtons.Add(expectedDescription);

            var buttonEntity = this.organizer.GetButtonEntityByDescription(expectedDescription);
            var foundDescription = this.organizer.GetButtonDescriptionByEntity(buttonEntity);

            Assert.Same(expectedDescription, foundDescription);
        }

        [Fact]
        public void NotReturnCloseButtonEntityWhenNotIncluded()
        {
            this.organizer.IncludeCloseButton = false;
            Assert.Null(this.organizer.CloseButtonEntity);
        }
    }
}
