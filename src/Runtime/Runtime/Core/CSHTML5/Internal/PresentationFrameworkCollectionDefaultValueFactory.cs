#if MIGRATION
using System.Windows;
#else
using Windows.UI.Xaml;
#endif

namespace CSHTML5.Internal
{
    internal class PresentationFrameworkCollectionDefaultValueFactory<T> : DefaultValueFactory
    {
        private readonly PresentationFrameworkCollection<T> _defaultValue;

        public PresentationFrameworkCollectionDefaultValueFactory(PresentationFrameworkCollection<T> defaultValue)
        {
            global::System.Diagnostics.Debug.Assert(defaultValue != null, "default value should not be null !");
            this._defaultValue = defaultValue;
        }

        internal override object DefaultValue
        {
            get
            {
                return this._defaultValue;
            }
        }

        internal override object CreateDefaultValue(DependencyObject owner, DependencyProperty property)
        {
            return this._defaultValue.CreateInstance();
        }
    }
}
