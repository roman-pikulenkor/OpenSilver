#if MIGRATION
namespace System.Windows
#else
namespace Windows.UI.Xaml
#endif
{
    /// <summary>
    ///     Validate property value
    /// </summary>
    public delegate bool ValidateValueCallback(object value);
}
