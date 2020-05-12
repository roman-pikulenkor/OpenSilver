#if MIGRATION
using System.Windows;
#else
using Windows.UI.Xaml;
#endif

namespace CSHTML5.Internal
{
    internal abstract class DefaultValueFactory
    {
        /// <summary>
        ///     See PropertyMetadata.DefaultValue
        /// </summary>
        internal abstract object DefaultValue
        {
            get;

        }

        /// <summary>
        ///     See PropertyMetadata.CreateDefaultValue
        /// </summary>
        internal abstract object CreateDefaultValue(DependencyObject owner, DependencyProperty property);
    }
}
