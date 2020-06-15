

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


using System.Windows.Markup;
using System.Diagnostics;
using CSHTML5.Internal;
using System.Collections.Generic;

#if MIGRATION
using System.Windows.Data;
#else
using Windows.UI.Xaml.Data;
#endif

#if MIGRATION
namespace System.Windows.Controls
#else
namespace Windows.UI.Xaml.Controls
#endif
{
    /// <summary>
    /// Displays the content of a ContentControl.
    /// </summary>
    [ContentProperty("Content")]
    public class ContentPresenter : FrameworkElement
    {
        #region Data

        private UIElement _visualChild;
        private DataTemplate _templateCache;

        private static readonly DataTemplate _defaultTemplate;
        private static readonly DataTemplate _uiElementTemplate;

        #endregion Data

        #region Constructor

        static ContentPresenter()
        {
            // Default template
            DataTemplate template = new DataTemplate();
            template._methodToInstantiateFrameworkTemplate = owner =>
            {
                TemplateInstance templateInstance = new TemplateInstance();

                TextBlock textBlock = new TextBlock();
                textBlock.SetBinding(TextBlock.TextProperty, new Binding(""));

                templateInstance.TemplateContent = textBlock;

                return templateInstance;
            };
            template.Seal();
            _defaultTemplate = template;

            // Default template when content is UIElement.
            template = new UseContentTemplate();
            template.Seal();
            _uiElementTemplate = template;
        }

        public ContentPresenter()
        {

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
                typeof(ContentPresenter),
                new PropertyMetadata(null, OnContentChanged));

        /// <summary>
        /// Gets or sets the data that is used to generate the child elements of a <see cref="ContentPresenter"/>.
        /// </summary>
        /// <returns>
        /// The data that is used to generate the child elements. The default is null.
        /// </returns>
        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentPresenter cp = (ContentPresenter)d;
            bool reevaluateTemplate;

            if (cp.ContentTemplate != null)
            {
                reevaluateTemplate = false; // explicit template - do not re-apply
            }
            else if (cp.Template == UIElementContentTemplate)
            {
                reevaluateTemplate = true; // direct template - always re-apply
                cp.Template = null; // clear the template so it can be re-generated.
            }
            else if (cp.Template == DefaultContentTemplate)
            {
                reevaluateTemplate = true; // default template - always re-apply
            }
            else
            {
                // implicit template - re-apply if content type changed
                Type oldDataType = e.OldValue != null ? e.OldValue.GetType() : null;
                Type newDataType = e.NewValue != null ? e.NewValue.GetType() : null;

                reevaluateTemplate = (oldDataType != newDataType);
            }

            // keep the DataContext in sync with Content
            if (e.NewValue is UIElement)
            {
                cp.ClearValue(FrameworkElement.DataContextProperty);
            }
            else
            {
                cp.DataContext = e.NewValue;
            }

            if (reevaluateTemplate)
            {
                cp.ReevaluateTemplate();
            }
        }

        /// <summary>
        /// Identifies the ContentTemplate dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentTemplateProperty =
            DependencyProperty.Register(
                "ContentTemplate",
                typeof(DataTemplate),
                typeof(ContentPresenter),
                new PropertyMetadata(null, OnContentTemplateChanged));

        /// <summary>
        /// Gets or sets the template that is used to display the content of the control.
        /// </summary>
        /// <returns>
        /// A <see cref="DataTemplate"/> that defines the visualization of the content.
        /// The default is null.
        /// </returns>
        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)this.GetValue(ContentTemplateProperty); }
            set { this.SetValue(ContentTemplateProperty, value); }
        }

        private static void OnContentTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentPresenter cp = (ContentPresenter)d;
            cp.ReevaluateTemplate();
        }

        /// <summary>
        /// TemplateProperty
        /// </summary>
        internal static readonly DependencyProperty TemplateProperty =
            DependencyProperty.Register(
                "Template",
                typeof(DataTemplate),
                typeof(ContentPresenter),
                new PropertyMetadata(null, OnTemplateChanged));


        /// <summary>
        /// Template Property
        /// </summary>
        private DataTemplate Template
        {
            get { return this._templateCache; }
            set { this.SetValue(TemplateProperty, value); }
        }

        // Property invalidation callback invoked when TemplateProperty is invalidated
        private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ContentPresenter cp = (ContentPresenter)d;

            // Update template cache
            cp._templateCache = (DataTemplate)e.NewValue;    

            cp.ApplyTemplate();
        }

        #endregion Dependency Properties

        #region Internal Properties

        internal static DataTemplate DefaultContentTemplate
        {
            get
            {
                return _defaultTemplate;
            }
        }

        internal static DataTemplate UIElementContentTemplate
        {
            get
            {
                return _uiElementTemplate;
            }
        }

        #endregion Internal Properties

        #region Internal Methods

        //  Searches through resource dictionaries to find a DataTemplate
        //  that matches the type of the 'item' parameter.  Failing an exact
        //  match of the type, return something that matches one of its parent
        //  types.
        internal static object FindTemplateResourceInternal(DependencyObject target, object item)
        {
            // Data styling doesn't apply to UIElement.
            if (item == null || (item is UIElement))
            {
                return null;
            }

            Type dataType = item.GetType();

            List<DataTemplateKey> keys = new List<DataTemplateKey>();

            // construct the list of acceptable keys, in priority ord
            int exactMatch = 1;    // number of entries that count as an exact match

            // add compound keys for the dataType and all its base types
            while (dataType != null)
            {
                keys.Add(new DataTemplateKey(dataType));

                if (dataType != null)
                {
                    dataType = dataType.BaseType;
                    if (dataType == typeof(object)) // don't search for Object - perf (Note: Silverlight also includes object)
                    {
                        dataType = null;
                    }
                }
            }

            int bestMatch = keys.Count; // index of best match so far

            // Search the parent chain
            object resource = FindTemplateResourceInTree(target, keys, exactMatch, ref bestMatch);

            if (bestMatch >= exactMatch)
            {
                // Exact match not found in the parent chain.  Try App Resources.
                object appResource = FindTemplateResourceFromApp(target, keys, exactMatch, ref bestMatch);

                if (appResource != null)
                    resource = appResource;
            }

            return resource;
        }

        // Find a data template resource
        private static object FindTemplateResourceFromApp(
            DependencyObject target,
            List<DataTemplateKey> keys,
            int exactMatch,
            ref int bestMatch)
        {
            object resource = null;
            int k;

            Application app = Application.Current;
            if (app != null)
            {
                // If the element is rooted to a Window and App exists, defer to App.
                for (k = 0; k < bestMatch; ++k)
                {
                    object appResource = Application.Current.FindResourceInternal(keys[k]);
                    if (appResource != null)
                    {
                        bestMatch = k;
                        resource = appResource;

                        if (bestMatch < exactMatch)
                            return resource;
                    }
                }
            }

            return resource;
        }

        // Search the parent chain for a DataTemplate in a ResourceDictionary.
        private static object FindTemplateResourceInTree(
            DependencyObject target, 
            List<DataTemplateKey> keys, 
            int exactMatch, 
            ref int bestMatch)
        {
            Debug.Assert(target != null, "Don't call FindTemplateResource with a null target object");

            ResourceDictionary table;
            object resource = null;

            FrameworkElement fe = target as FrameworkElement;

            while (fe != null)
            {
                object candidate;

                // -------------------------------------------
                //  Lookup ResourceDictionary on the current instance
                // -------------------------------------------

                // Fetch the ResourceDictionary
                // for the given target element
                table = fe.HasResources ? fe.Resources : null;
                if (table != null)
                {
                    candidate = FindBestMatchInResourceDictionary(table, keys, exactMatch, ref bestMatch);
                    if (candidate != null)
                    {
                        resource = candidate;
                        if (bestMatch < exactMatch)
                        {
                            // Exact match found, stop here.
                            return resource;
                        }
                    }
                }

                // -------------------------------------------
                //  Find the next parent instance to lookup
                // -------------------------------------------

                // Get Framework Parent
                fe = fe.Parent as FrameworkElement;
            }

            return resource;
        }

        // Given a ResourceDictionary and a set of keys, try to find the best
        //  match in the resource dictionary.
        private static object FindBestMatchInResourceDictionary(
            ResourceDictionary table, 
            List<DataTemplateKey> keys, 
            int exactMatch, 
            ref int bestMatch)
        {
            object resource = null;
            int k;

            // Search target element's ResourceDictionary for the resource
            if (table != null)
            {
                for (k = 0; k < bestMatch; ++k)
                {
                    object candidate = table[keys[k]];
                    if (candidate != null)
                    {
                        resource = candidate;
                        bestMatch = k;

                        // if we found an exact match, no need to continue
                        if (bestMatch < exactMatch)
                            return resource;
                    }
                }
            }

            return resource;
        }

        /// <summary>
        /// Return the template to use.  This may depend on the Content, or
        /// other properties.
        /// </summary>
        /// <remarks>
        /// The base class implements the following rules:
        ///   (a) If ContentTemplate is set, use it.
        ///   (b) Look for a DataTemplate whose DataType matches the
        ///         Content among the resources known to the ContentPresenter
        ///         (including application, theme, and system resources).
        ///         If one is found, use it.
        ///   (c) If the type of Content is "common", use a standard template.
        ///         The common types are String, UIElement.
        ///   (d) Otherwise, use a default template that essentially converts
        ///         Content to a string and displays it in a TextBlock.
        /// </remarks>
        private DataTemplate ChooseTemplate()
        {
            DataTemplate template = null;
            object content = this.Content;

            template = this.ContentTemplate;

            if (template == null)
            {
                // Lookup template for typeof(Content) in resource dictionaries.
                if (content != null)
                {
                    template = (DataTemplate)FindTemplateResourceInternal(this, content);
                }

                // default templates
                if (template == null)
                {
                    if (content is UIElement)
                    {
                        template = UIElementContentTemplate;
                    }
                    else
                    {
                        template = DefaultContentTemplate;
                    }
                }
            }

            return template;
        }

        private void ReevaluateTemplate()
        {
            DataTemplate template = this.ChooseTemplate();

            if (this.Template != template)
            {
                this.Template = template;
            }
        }

        private void ApplyTemplate()
        {
            if (this._visualChild != null)
            {
                INTERNAL_VisualTreeManager.DetachVisualChildIfNotNull(this._visualChild, this);
                this._visualChild = null;
            }

            if (this.Template != null)
            {
                FrameworkElement visualChild = this.Template.INTERNAL_InstantiateFrameworkTemplate(this);

#if REWORKLOADED
                this.AddVisualChild(visualChild);
#else
                INTERNAL_VisualTreeManager.AttachVisualChildIfNotAlreadyAttached(visualChild, this);
#endif
                this._visualChild = visualChild;
            }
        }

        protected internal override void INTERNAL_OnAttachedToVisualTree()
        {
            base.INTERNAL_OnAttachedToVisualTree();

            // If the template is the default template, it could be because
            // we were not able to find an implicit DataTemplate since the
            // control was not in the visual tree yet.
            if (this.Template == DefaultContentTemplate)
            {
                this.ReevaluateTemplate();
            }

            if (this._visualChild != null)
            {
#if REWORKLOADED
                this.AddVisualChild(this._visualChild);
#else
                INTERNAL_VisualTreeManager.AttachVisualChildIfNotAlreadyAttached(this._visualChild, this);
#endif
            }
        }

#endregion Private Methods

#region Private classes

        private class UseContentTemplate : DataTemplate
        {
            public UseContentTemplate()
            {
                this._methodToInstantiateFrameworkTemplate = owner =>
                {
                    TemplateInstance template = new TemplateInstance();

                    FrameworkElement root = ((ContentPresenter)owner).Content as FrameworkElement;

                    template.TemplateContent = root;

                    return template;
                };
            }
        }

#endregion Private classes
    }
}

