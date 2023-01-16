using System.Globalization;
using Xrv.Core.Localization;
using Xunit;

namespace Xrv.Core.Tests.Services.Localization
{
    public class LocalizationServiceShould
    {
        private readonly LocalizationService localizationService;

        public LocalizationServiceShould()
        {
            this.localizationService = new LocalizationService();
            this.localizationService.RegisterAssembly(typeof(XrvService).Assembly);
        }

        [Fact]
        public void GetCoreStrings()
        {
            const string Expected = "Menu";
            string actual = this.localizationService.GetString("Xrv.Core.Resources.Strings", "Menu.Title");

            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void GetCoreStringsInADifferentCulture()
        {
            const string Expected = "Menú";
            this.localizationService.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");

            string actual = this.localizationService.GetString("Xrv.Core.Resources.Strings", "Menu.Title");

            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void GetCoreStringsFallbackIfCultureNotFound()
        {
            const string Expected = "Menu";
            this.localizationService.CurrentCulture = CultureInfo.GetCultureInfo("mr-IN");

            string actual = this.localizationService.GetString("Xrv.Core.Resources.Strings", "Menu.Title");

            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void GetCustomLibraryStringInADifferentCulture()
        {
            const string Expected = "¡Prueba!";

            this.localizationService.RegisterAssembly(typeof(LocalizationServiceShould).Assembly);
            this.localizationService.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");

            string actual = this.localizationService.GetString("Xrv.Core.Tests.Services.Localization.Resources.TestStrings", "MyTest.String");

            Assert.Equal(Expected, actual);
        }
    }
}
