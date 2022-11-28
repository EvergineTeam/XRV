using Evergine.Framework;
using XrvSamples.Services;

namespace XrvSamples.Windows
{
    internal class WindowsApplication : MyApplication
    {
        public WindowsApplication()
        {
            var container = Application.Current.Container;
            container.RegisterInstance(new FakeQRWatcherService());
        }
    }
}
