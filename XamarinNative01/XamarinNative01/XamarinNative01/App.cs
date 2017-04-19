using XamarinNative01.Helpers;
using XamarinNative01.Interfaces;
using XamarinNative01.Services;
using XamarinNative01.Model;

namespace XamarinNative01
{
    public partial class App
    {
        public App()
        {
        }

        public static void Initialize()
        {
            ServiceLocator.Instance.Register<IDataStore<Item>, MockDataStore>();
        }
    }
}