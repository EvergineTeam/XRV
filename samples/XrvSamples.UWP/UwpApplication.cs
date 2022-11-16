using Evergine.Framework;
using XrvSamples.Services;

namespace XrvSamples.UWP
{
    internal class UwpApplication : MyApplication
    {
        public UwpApplication()
        {
            var container = Application.Current.Container;
            container.RegisterInstance(new FakeQRWatcherService());
        }
    }
}
