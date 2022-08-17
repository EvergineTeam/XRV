using Evergine.Common;
using Evergine.Framework;
using Evergine.Framework.Services;
using System;
using System.Linq;

namespace Xrv.Core.Extensions
{
    public static class EvergineExtensions
    {
        public static T LoadIfNotDefaultId<T>(this AssetsService assetsService, Guid assetId)
            where T : ILoadable =>
            assetId != Guid.Empty ? assetsService.Load<T>(assetId) : default(T);

        public static void RemoveAllChildren(this Entity entity)
        {
            while (entity.NumChildren > 0)
            {
                entity.RemoveChild(entity.ChildEntities.ElementAt(0));
            }
        }
    }
}
