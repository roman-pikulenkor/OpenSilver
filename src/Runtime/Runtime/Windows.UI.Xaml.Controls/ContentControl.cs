

/*===================================================================================
* 
*   Copyright (c) Userware/OpenSilver.net
*      
*   This file is part of the OpenSilver Runtime (https://opensilver.net), which is
*   licensed under the MIT license: https://opensource.org/licenses/MIT
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/


using CSHTML5.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

#if MIGRATION
namespace System.Windows.Controls
#else
namespace Windows.UI.Xaml.Controls
#endif
{
    /// <summary>
    /// Represents a control with a single piece of content. Controls such as Button,
    /// CheckBox, and ScrollViewer directly or indirectly inherit from this class.
    /// </summary>
    [ContentProperty("Content")]
    public class ContentControl : Control
    {
        #region Constructor

        public ContentControl()
        {
            this.DefaultStyleKey = typeof(ContentControl);
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the Content dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                "Content",
                typeof(object),
                typeof(ContentControl),
                new PropertyMetadata(null, OnContentChanged));

        /// <summary>
        /// Gets or sets the content of a ContentControl.
        /// </summary>
        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ContentControl)d).OnContentChanged(e.OldValue, e.NewValue);
        }

        /// <summary>
        /// Identifies the ContentTemplate dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentTemplateProperty =
            DependencyProperty.Register(
                "ContentTemplate",
                typeof(DataTemplate),
                typeof(ContentControl),
                new PropertyMetadata(null, OnContentTemplateChanged));

        /// <summary>
        /// Gets or sets the data template that is used to display the content of the
        /// ContentControl.
        /// </summary>
        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)this.GetValue(ContentTemplateProperty); }
            set { this.SetValue(ContentTemplateProperty, value); }
        }

        private static void OnContentTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        #endregion Dependency Properties

        #region Protected Methods

        /// <summary>
        /// Called when the value of the <see cref="ContentControl.Content"/> property
        /// changes.
        /// </summary>
        /// <param name="oldContent">
        /// The old value of the <see cref="ContentControl.Content"/> property.
        /// </param>
        /// <param name="newContent">
        /// The new value of the <see cref="ContentControl.Content"/> property.
        /// </param>
        protected virtual void OnContentChanged(object oldContent, object newContent)
        {

        }

        #endregion Protected Methods
    }
}
