using Evergine.Framework;
using Evergine.Xrv.Core.Extensions;
using System.Linq;
using Xunit;

namespace Evergine.Xrv.Core.Tests.Extensions
{
    public class EvergineExtensionsShould
    {
        [Fact]
        public void RemoveAllChildrenWhenThereAreNoChildren()
        {
            var entity = new Entity();
            entity.RemoveAllChildren();
        }

        [Fact]
        public void RemoveAllChildrenWhenThereIsNoCondition()
        {
            var entity = new Entity()
                .AddChild(new Entity())
                .AddChild(new Entity());
            entity.RemoveAllChildren();

            Assert.Empty(entity.ChildEntities);
        }

        [Fact]
        public void RemoveAllChildrenByCondition()
        {
            var entity = new Entity()
                .AddChild(new Entity("1"))
                .AddChild(new Entity("2"))
                .AddChild(new Entity("3"))
                .AddChild(new Entity("4"))
                .AddChild(new Entity("5"));
            entity.RemoveAllChildren(child => int.Parse(child.Name) % 2 == 0);

            Assert.Equal(3, entity.ChildEntities.Count());
        }
    }
}
