

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


using System;

#if MIGRATION
namespace System.Windows
#else
namespace Windows.UI.Xaml
#endif
{
    /// <summary>
    /// Describes the visual structure of a data object.
    /// </summary>
    public partial class DataTemplate : FrameworkTemplate
    {
        private Type _dataType;

        /// <summary>
        /// Initializes a new instance of the DataTemplate class.
        /// </summary>
        public DataTemplate()
        {

        }

        /// <summary>
        /// Initializes a new instance of the DataTemplate class.
        /// </summary>
        public DataTemplate(object dataType)
        {
            this.DataType = dataType as Type;
        }

        /// <summary>
        /// Creates the UIElement objects in the DataTemplate./>.
        /// </summary>
        /// <returns>
        /// The root UIElement of the DataTemplate.
        /// </returns>
        public DependencyObject LoadContent()
        {
            return this.INTERNAL_InstantiateFrameworkTemplate();
        }

        /// <summary>
        /// Gets or sets the type for which this <see cref="DataTemplate"/> is intended.
        /// </summary>
        /// <returns>
        /// The type of object to which this template is applied.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// When setting this property, the specified value is not of type <see cref="Type"/>.
        /// </exception>
        public Type DataType
        {
            get
            {
                return this._dataType;
            }
            set
            {
#if MIGRATION
                Exception ex = System.Windows.DataTemplateKey.ValidateDataType(value, "value");
#else
                Exception ex = Windows.UI.Xaml.DataTemplateKey.ValidateDataType(value, "value");
#endif
                if (ex != null)
                {
                    throw ex;
                }

                this._dataType = value;
            }
        }

        public object DataTemplateKey 
        { 
            get
            {
                return this.DataType != null ? 
                       new DataTemplateKey(this.DataType) : 
                       null;
            } 
        } 
    }
}
